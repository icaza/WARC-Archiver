using System.Globalization;
using WarcArchiver.App.Theme;
using WarcArchiver.App.Util;
using WarcArchiver.Core.Audit;

namespace WarcArchiver.App;

// Audit page: the hash-chained log of everything the application did (captures, cancellations, verifications).
internal sealed partial class MainForm
{
    const int MaxAuditRowsShown = 2000;

    bool _auditLoading;

    void AuditRefreshButton_Click(object? sender, EventArgs e)
    {
        RefreshAuditPage();
    }

    async void AuditVerifyButton_Click(object? sender, EventArgs e)
    {
        // Refresh first so that a broken entry can be highlighted in the list that is on screen.
        await RefreshAuditPageAsync();
        await VerifyAuditChainAsync(userInitiated: true);
    }

    void AuditCopyHeadButton_Click(object? sender, EventArgs e)
    {
        if (_audit is not { } audit)
        {
            return;
        }
        if (audit.LastSeq == 0)
        {
            SetStatus("The audit log is empty.", DarkTheme.Warning);
            return;
        }
        CopyToClipboard(audit.HeadHash, "Head hash (entry #" + audit.LastSeq.ToString(CultureInfo.InvariantCulture) + ")");
    }

    void AuditOpenButton_Click(object? sender, EventArgs e)
    {
        if (_audit is not { } audit)
        {
            return;
        }
        ShowInExplorer(File.Exists(audit.Path) ? audit.Path : Path.GetDirectoryName(audit.Path) ?? audit.Path);
    }

    void RefreshAuditPage()
    {
        _ = RefreshAuditPageAsync();
    }

    async Task RefreshAuditPageAsync()
    {
        if (_auditLoading)
        {
            return;
        }

        AuditLog? audit = _audit;
        if (audit is null)
        {
            auditGrid.Rows.Clear();
            auditStatusLabel.ForeColor = DarkTheme.Danger;
            auditStatusLabel.Text = "The audit log is unavailable (see Settings).";
            return;
        }

        _auditLoading = true;
        try
        {
            string path = audit.Path;
            List<AuditEntry> entries = await Task.Run(() => AuditLog.ReadAll(path));
            if (IsDisposed || !ReferenceEquals(audit, _audit))
            {
                return;
            }
            PopulateAuditGrid(entries);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!IsDisposed)
            {
                auditStatusLabel.ForeColor = DarkTheme.Danger;
                auditStatusLabel.Text = "The audit log could not be read: " + ex.Message;
            }
        }
        finally
        {
            _auditLoading = false;
        }
    }

    void PopulateAuditGrid(List<AuditEntry> entries)
    {
        auditGrid.SuspendLayout();
        auditGrid.Rows.Clear();

        // Newest first; a very long log shows its most recent part (verification always covers all of it).
        int shown = 0;
        for (int i = entries.Count - 1; i >= 0 && shown < MaxAuditRowsShown; i--, shown++)
        {
            AuditEntry entry = entries[i];
            string details = string.Join("   ", entry.Data.Select(p => p.Key + "=" + p.Value)).ReplaceLineEndings(" ");

            int index = auditGrid.Rows.Add(
                entry.Seq.ToString(CultureInfo.InvariantCulture),
                UiHelpers.FormatTimestamp(entry.Local),
                entry.Event,
                UiHelpers.Shorten(details, 400),
                UiHelpers.Shorten(entry.Hash, 16));

            DataGridViewRow row = auditGrid.Rows[index];
            row.Cells[audDetailsColumn.Index].ToolTipText = UiHelpers.Shorten(
                string.Join("\n", entry.Data.Select(p => p.Key + ": " + p.Value)), 1500);
            row.Cells[audHashColumn.Index].ToolTipText = "Entry hash: " + entry.Hash + "\nPrevious entry: " + entry.Prev;
        }
        auditGrid.ResumeLayout();

        auditStatusLabel.ForeColor = DarkTheme.TextMuted;
        auditStatusLabel.Text = entries.Count == 0
            ? "No entries yet. The log grows with every capture."
            : entries.Count.ToString("N0", CultureInfo.InvariantCulture) + " entries" +
              (entries.Count > shown ? " (showing the newest " + shown.ToString("N0", CultureInfo.InvariantCulture) + ")" : string.Empty) +
              ". Click \"Verify chain\" to re-compute every hash.";
    }

    /// <summary>Called by <see cref="VerifyAuditChainAsync"/> with the outcome of a full re-computation of the chain.</summary>
    void ShowChainResult(AuditVerification result, bool userInitiated)
    {
        if (result.Ok)
        {
            string count = result.Entries.ToString("N0", CultureInfo.InvariantCulture);
            SetChainStatus(
                result.Entries == 0 ? "Audit chain: empty" : "Audit chain: intact (" + count + ")",
                result.Entries == 0 ? DarkTheme.TextMuted : DarkTheme.Success);
            if (userInitiated)
            {
                auditStatusLabel.ForeColor = result.Entries == 0 ? DarkTheme.TextMuted : DarkTheme.Success;
                auditStatusLabel.Text = result.Entries == 0
                    ? "The audit log is empty."
                    : "Chain intact — " + count + " entries re-computed, none altered, none missing. Head hash: " + result.HeadHash;
                SetStatus("Audit chain verified.", DarkTheme.Success);
            }
            return;
        }

        SetChainStatus("Audit chain: BROKEN", DarkTheme.Danger);
        auditStatusLabel.ForeColor = DarkTheme.Danger;
        auditStatusLabel.Text = "CHAIN BROKEN — " + result.Error;
        SetStatus("The audit log failed verification.", DarkTheme.Danger);

        if (result.FailedSeq is { } failed)
        {
            string wanted = failed.ToString(CultureInfo.InvariantCulture);
            foreach (DataGridViewRow row in auditGrid.Rows)
            {
                if (string.Equals(row.Cells[audSeqColumn.Index].Value as string, wanted, StringComparison.Ordinal))
                {
                    row.DefaultCellStyle.ForeColor = DarkTheme.Danger;
                    auditGrid.CurrentCell = row.Cells[0];
                    if (row.Index >= 0)
                    {
                        auditGrid.FirstDisplayedScrollingRowIndex = row.Index;
                    }
                    break;
                }
            }
        }
    }
}
