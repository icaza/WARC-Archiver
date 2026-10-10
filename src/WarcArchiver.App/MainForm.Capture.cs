using System.Globalization;
using WarcArchiver.App.Theme;
using WarcArchiver.App.Util;
using WarcArchiver.Core.Archive;

namespace WarcArchiver.App;

// Capture page: validates the address, runs the engine off the UI thread and shows what is being archived as it happens.
internal sealed partial class MainForm
{
    CancellationTokenSource? _captureCts;
    bool _cancelRequested;
    ArchiveResult? _lastResult;

    void UrlTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            if (captureButton.Enabled)
            {
                captureButton.PerformClick();
            }
        }
    }

    async void CaptureButton_Click(object? sender, EventArgs e)
    {
        await StartCaptureAsync();
    }

    void CancelButton_Click(object? sender, EventArgs e)
    {
        RequestCaptureCancel();
    }

    void RequestCaptureCancel()
    {
        if (_operation != Operation.Capture || _cancelRequested)
        {
            return;
        }
        _cancelRequested = true;
        progressLabel.ForeColor = DarkTheme.TextMuted;
        progressLabel.Text = "Cancelling — discarding the unfinished archive…";
        UpdateControlStates();
        _captureCts?.Cancel();
    }

    async Task StartCaptureAsync()
    {
        ArchiveEngine? engine = _engine;
        if (engine is null)
        {
            SetStatus("Capturing is disabled: fix the output folder in config.json (Settings page).", DarkTheme.Danger);
            return;
        }

        if (!ArchiveEngine.TryNormalizeUrl(urlTextBox.Text, out Uri? uri, out string? problem))
        {
            progressLabel.ForeColor = DarkTheme.Danger;
            progressLabel.Text = problem ?? "That is not a valid web address.";
            urlTextBox.Focus();
            urlTextBox.SelectAll();
            return;
        }

        if (!TryBeginOperation(Operation.Capture))
        {
            return;
        }

        // Show the exact address that will be archived (a missing scheme is completed with https://).
        urlTextBox.Text = uri!.AbsoluteUri;

        var request = new ArchiveRequest
        {
            Url = uri.AbsoluteUri,
            CaseReference = caseRefTextBox.Text.Trim(),
            Operator = operatorTextBox.Text.Trim(),
            Notes = notesTextBox.Text.Trim(),
            IncludeScripts = scriptsCheckBox.Checked,
            IncludeStylesheets = stylesheetsCheckBox.Checked,
            IncludeImages = imagesCheckBox.Checked,
            IncludeOtherAssets = otherAssetsCheckBox.Checked,
            IncludeThirdParty = thirdPartyCheckBox.Checked,
            SigningCertificate = (signComboBox.SelectedItem as CertChoice)?.Certificate,
        };

        resultsGrid.Rows.Clear();
        captureProgressBar.Maximum = 1;
        captureProgressBar.Value = 0;
        progressLabel.ForeColor = DarkTheme.TextMuted;
        progressLabel.Text = "Starting…";
        summaryLabel.ForeColor = DarkTheme.TextMuted;
        summaryLabel.Text = "Capturing " + uri.Host + "…";
        toolTip.SetToolTip(summaryLabel, string.Empty);
        SetStatus("Capturing " + uri.Host + "…");

        _lastResult = null;
        _cancelRequested = false;
        _captureCts = new CancellationTokenSource();
        CancellationToken token = _captureCts.Token;

        // Progress<T> posts back to this (UI) thread.
        var progress = new Progress<ArchiveEvent>(OnCaptureEvent);

        ArchiveResult result;
        try
        {
            result = await Task.Run(() => engine.RunAsync(request, progress, token), CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            result = new ArchiveResult { Cancelled = true };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or HttpRequestException)
        {
            result = ArchiveResult.Failure(ex.Message);
        }

        _captureCts.Dispose();
        _captureCts = null;

        if (IsDisposed)
        {
            return;
        }

        _lastResult = result.Success ? result : null;
        ShowCaptureResult(result);
        EndOperation();

        if (result.Success)
        {
            _archivesStale = true;
        }
        // The capture appended to the audit log: refresh the status-bar indicator (and the page, if it is showing).
        _ = VerifyAuditChainAsync(userInitiated: false);
        if (ReferenceEquals(_activeNav, navAuditButton))
        {
            RefreshAuditPage();
        }
    }

    void OnCaptureEvent(ArchiveEvent evt)
    {
        if (IsDisposed)
        {
            return;
        }

        if (evt.Discovered > 0)
        {
            captureProgressBar.Maximum = Math.Max(1, evt.Discovered);
            captureProgressBar.Value = Math.Min(evt.Completed, evt.Discovered);
        }

        if (evt.Kind == ArchiveEventKind.Stage)
        {
            if (!_cancelRequested)
            {
                progressLabel.ForeColor = DarkTheme.TextMuted;
                progressLabel.Text = evt.Message;
            }
            return;
        }

        if (evt.Resource is { } resource)
        {
            AddResultRow(resource);
            if (!_cancelRequested)
            {
                progressLabel.ForeColor = DarkTheme.TextMuted;
                progressLabel.Text = evt.Completed.ToString(CultureInfo.InvariantCulture) + " of " +
                                     evt.Discovered.ToString(CultureInfo.InvariantCulture) + " resources — " + UiHelpers.Shorten(resource.Url, 90);
            }
        }
    }

    void AddResultRow(ResourceEntry resource)
    {
        int index = resultsGrid.Rows.Add(
            resource.LocalTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
            resource.Status,
            resource.Kind.ToString(),
            resource.Url,
            resource.Bytes > 0 ? UiHelpers.FormatBytes(resource.Bytes) : string.Empty,
            UiHelpers.Shorten(resource.Sha256, 20),
            resource.DurationMs.ToString(CultureInfo.InvariantCulture),
            resource.Note);

        DataGridViewRow row = resultsGrid.Rows[index];
        row.Cells[resUrlColumn.Index].ToolTipText = resource.Url;
        if (resource.Sha256.Length > 0)
        {
            row.Cells[resShaColumn.Index].ToolTipText = resource.Sha256;
        }
        if (resource.Note.Length > 0)
        {
            row.Cells[resNoteColumn.Index].ToolTipText = resource.Note;
        }

        if (resource.IsProblem)
        {
            row.DefaultCellStyle.ForeColor = DarkTheme.Danger;
        }
        else if (string.Equals(resource.Status, "SKIPPED", StringComparison.OrdinalIgnoreCase))
        {
            row.DefaultCellStyle.ForeColor = DarkTheme.Warning;
        }

        // Keep the newest line in view without jumping when the user has scrolled up to look at something.
        int visible = resultsGrid.DisplayedRowCount(includePartialRow: false);
        int first = resultsGrid.FirstDisplayedScrollingRowIndex;
        if (visible > 0 && first >= 0 && index >= first + visible - 1)
        {
            resultsGrid.FirstDisplayedScrollingRowIndex = Math.Max(0, index - visible + 1);
        }
    }

    void ShowCaptureResult(ArchiveResult result)
    {
        if (result.Cancelled)
        {
            captureProgressBar.Value = 0;
            progressLabel.ForeColor = DarkTheme.TextMuted;
            progressLabel.Text = "Cancelled.";
            summaryLabel.ForeColor = DarkTheme.Warning;
            summaryLabel.Text = "Capture cancelled. The unfinished archive was discarded; the cancellation is recorded in the audit log.";
            SetStatus("Capture cancelled.", DarkTheme.Warning);
            return;
        }

        if (!result.Success)
        {
            captureProgressBar.Value = 0;
            progressLabel.ForeColor = DarkTheme.Danger;
            progressLabel.Text = "Capture failed.";
            summaryLabel.ForeColor = DarkTheme.Danger;
            summaryLabel.Text = result.Error ?? "The capture failed.";
            toolTip.SetToolTip(summaryLabel, summaryLabel.Text);
            SetStatus("Capture failed.", DarkTheme.Danger);
            return;
        }

        captureProgressBar.Maximum = 1;
        captureProgressBar.Value = 1;
        progressLabel.ForeColor = DarkTheme.Success;
        progressLabel.Text = "Done.";

        bool incomplete = result.ResourcesFailed > 0 || result.ResourcesSkipped > 0;
        string sha = result.WarcDigests?.Sha256Hex ?? string.Empty;
        string text =
            "Archived the page and " + result.ResourcesArchived.ToString(CultureInfo.InvariantCulture) + " resource(s), " +
            UiHelpers.FormatBytes(result.ResponseBytes) + " — " +
            (result.Signed ? "signed" : "not signed") +
            (result.ResourcesFailed > 0 ? " — " + result.ResourcesFailed.ToString(CultureInfo.InvariantCulture) + " failed" : string.Empty) +
            (result.ResourcesSkipped > 0 ? " — " + result.ResourcesSkipped.ToString(CultureInfo.InvariantCulture) + " skipped" : string.Empty) +
            " — SHA-256 " + UiHelpers.Shorten(sha, 16);

        summaryLabel.ForeColor = incomplete ? DarkTheme.Warning : DarkTheme.Success;
        summaryLabel.Text = text;
        toolTip.SetToolTip(
            summaryLabel,
            "Archive: " + result.WarcPath + "\nManifest: " + result.ManifestPath + "\nSHA-256: " + sha +
            "\n\nFailed or skipped resources are listed in the table and in the manifest.");
        SetStatus(
            incomplete ? "Archive saved, with some resources missing — see the table." : "Archive saved.",
            incomplete ? DarkTheme.Warning : DarkTheme.Success);
    }

    // ---- follow-up actions

    void OpenFolderButton_Click(object? sender, EventArgs e)
    {
        string? path = _lastResult?.WarcPath ?? _outputDirectory;
        if (!string.IsNullOrEmpty(path))
        {
            ShowInExplorer(path);
        }
    }

    void VerifyThisButton_Click(object? sender, EventArgs e)
    {
        string? path = _lastResult?.ManifestPath ?? _lastResult?.WarcPath;
        if (path is null)
        {
            return;
        }
        ShowPage(navVerifyButton);
        _ = StartVerificationAsync(path);
    }

    void CopyHashButton_Click(object? sender, EventArgs e)
    {
        if (_lastResult?.WarcDigests is { } digests)
        {
            CopyToClipboard(digests.Sha256Hex, "SHA-256 of the archive");
        }
    }
}
