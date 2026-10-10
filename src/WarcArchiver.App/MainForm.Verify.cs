using System.Globalization;
using System.Text;
using System.Text.Json;
using WarcArchiver.App.Theme;
using WarcArchiver.Core.Verify;

namespace WarcArchiver.App;

// Verify page: independent re-check of an archive (fingerprints, every record, signature, audit trail).
internal sealed partial class MainForm
{
    CancellationTokenSource? _verifyCts;
    VerificationReport? _lastReport;
    Font? _statusFont;

    void VerifyBrowseButton_Click(object? sender, EventArgs e)
    {
        if (Directory.Exists(_outputDirectory))
        {
            openFileDialog.InitialDirectory = _outputDirectory;
        }
        if (openFileDialog.ShowDialog(this) == DialogResult.OK)
        {
            _ = StartVerificationAsync(openFileDialog.FileName);
        }
    }

    void VerifyRunButton_Click(object? sender, EventArgs e)
    {
        if (_operation == Operation.Verify)
        {
            // The same button cancels a running verification.
            verifyRunButton.Enabled = false;
            verifyStatusLabel.ForeColor = DarkTheme.TextMuted;
            verifyStatusLabel.Text = "Cancelling…";
            _verifyCts?.Cancel();
            return;
        }
        _ = StartVerificationAsync(verifyPathTextBox.Text);
    }

    void VerifyDrop_DragEnter(object? sender, DragEventArgs e)
    {
        bool acceptable = _operation == Operation.None && e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
        e.Effect = acceptable ? DragDropEffects.Copy : DragDropEffects.None;
    }

    void VerifyDrop_DragDrop(object? sender, DragEventArgs e)
    {
        if (_operation == Operation.None && e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files)
        {
            _ = StartVerificationAsync(files[0]);
        }
    }

     async Task StartVerificationAsync(string path)
    {
        path = (path ?? string.Empty).Trim().Trim('"');
        verifyPathTextBox.Text = path;
        if (path.Length == 0)
        {
            verifyStatusLabel.ForeColor = DarkTheme.Warning;
            verifyStatusLabel.Text = "Choose or drop an archive (.warc, .warc.gz or .manifest.json) first.";
            return;
        }
        if (!TryBeginOperation(Operation.Verify))
        {
            return;
        }

        verifyGrid.Rows.Clear();
        _lastReport = null;
        verifyVerdictLabel.ForeColor = DarkTheme.TextMuted;
        verifyVerdictLabel.Text = "Verifying…";
        verifyStatusLabel.ForeColor = DarkTheme.TextMuted;
        verifyStatusLabel.Text = "Starting…";
        SetStatus("Verifying archive…");

        _verifyCts = new CancellationTokenSource();
        CancellationToken token = _verifyCts.Token;
        var progress = new Progress<string>(message =>
        {
            if (!IsDisposed && _operation == Operation.Verify)
            {
                verifyStatusLabel.Text = message;
            }
        });

        VerificationReport? report = null;
        string? failure = null;
        bool cancelled = false;
        try
        {
            report = await Task.Run(() => new ArchiveVerifier().VerifyAsync(path, progress, token), CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            failure = ex.Message;
        }

        _verifyCts.Dispose();
        _verifyCts = null;

        if (IsDisposed)
        {
            return;
        }

        if (report is not null)
        {
            _lastReport = report;
            ShowVerificationReport(report);
            RecordVerificationInAuditLog(report);
        }
        else if (cancelled)
        {
            verifyVerdictLabel.ForeColor = DarkTheme.Warning;
            verifyVerdictLabel.Text = "Verification cancelled.";
            verifyStatusLabel.Text = "Cancelled — no verdict was produced.";
            SetStatus("Verification cancelled.", DarkTheme.Warning);
        }
        else
        {
            verifyVerdictLabel.ForeColor = DarkTheme.Danger;
            verifyVerdictLabel.Text = "Verification could not be completed.";
            verifyStatusLabel.ForeColor = DarkTheme.Danger;
            verifyStatusLabel.Text = failure ?? "Unknown error.";
            SetStatus("Verification failed to run.", DarkTheme.Danger);
        }

        EndOperation();
    }

    void ShowVerificationReport(VerificationReport report)
    {
        verifyGrid.SuspendLayout();
        verifyGrid.Rows.Clear();
        foreach (VerificationCheck check in report.Checks)
        {
            int index = verifyGrid.Rows.Add(StatusText(check.Status), check.Name, check.Detail);
            DataGridViewCell statusCell = verifyGrid.Rows[index].Cells[0];
            statusCell.Style.ForeColor = StatusColor(check.Status);
            statusCell.Style.Font = _statusFont ??= new Font(verifyGrid.Font, FontStyle.Bold);
        }
        verifyGrid.ResumeLayout();

        bool warnings = report.Checks.Any(c => c.Status == VerificationStatus.Warning);
        verifyVerdictLabel.ForeColor = !report.Passed ? DarkTheme.Danger : (warnings ? DarkTheme.Warning : DarkTheme.Success);
        verifyVerdictLabel.Text = "Verdict: " + report.Verdict;
        verifyStatusLabel.ForeColor = DarkTheme.TextMuted;
        verifyStatusLabel.Text = report.TargetPath + " — verified " +
                                 report.VerifiedLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " (local time)";
        SetStatus(
            "Verification " + report.Verdict.ToLowerInvariant() + ".",
            !report.Passed ? DarkTheme.Danger : (warnings ? DarkTheme.Warning : DarkTheme.Success));
    }

    static string StatusText(VerificationStatus status) => status switch
    {
        VerificationStatus.Pass => "PASS",
        VerificationStatus.Fail => "FAIL",
        VerificationStatus.Warning => "WARNING",
        VerificationStatus.Skipped => "SKIPPED",
        _ => "INFO",
    };

    static Color StatusColor(VerificationStatus status) => status switch
    {
        VerificationStatus.Pass => DarkTheme.Success,
        VerificationStatus.Fail => DarkTheme.Danger,
        VerificationStatus.Warning => DarkTheme.Warning,
        VerificationStatus.Skipped => DarkTheme.TextMuted,
        _ => DarkTheme.Accent,
    };

    /// <summary>Chain of custody: who looked at the evidence, and what the result was, is itself logged.</summary>
    void RecordVerificationInAuditLog(VerificationReport report)
    {
        try
        {
            _audit?.Append(
                "verify.run",
                [
                    new("target", report.TargetPath),
                    new("verdict", report.Verdict),
                ]);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("The verification ran, but could not be recorded in the audit log: " + ex.Message, DarkTheme.Warning);
        }
    }

    void VerifyExportButton_Click(object? sender, EventArgs e)
    {
        if (_lastReport is not { } report)
        {
            return;
        }

        string stem = Path.GetFileName(report.TargetPath);
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            stem = stem.Replace(invalid, '_');
        }
        saveFileDialog.FileName = "verification-" + stem + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt";
        if (Directory.Exists(_outputDirectory))
        {
            saveFileDialog.InitialDirectory = _outputDirectory;
        }
        if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            File.WriteAllText(saveFileDialog.FileName, report.ToText(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            SetStatus("Verification report saved.", DarkTheme.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "The report could not be saved:\n\n" + ex.Message, "WARC Archiver", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
