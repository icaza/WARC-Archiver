using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using WarcArchiver.App.Theme;
using WarcArchiver.App.Util;
using WarcArchiver.Core.Archive;

namespace WarcArchiver.App;

// Archives page: lists the evidence found in the output folder (read from the *.manifest.json files).
internal sealed partial class MainForm
{
    const long MaxManifestBytes = 64L * 1024 * 1024;

    sealed record ArchiveRow(
        string Local,
        DateTimeOffset? Captured,
        string Url,
        string CaseReference,
        int Records,
        long Bytes,
        bool Signed,
        string Sha256,
        string WarcFile,
        bool WarcMissing,
        string ManifestPath,
        bool Unreadable);

    sealed record CachedManifest(long Length, DateTime LastWriteUtc, ArchiveRow Row);

    Dictionary<string, CachedManifest> _manifestCache = new(StringComparer.OrdinalIgnoreCase);
    List<ArchiveRow> _archiveRows = [];
    bool _archivesStale = true;
    bool _archivesLoading;

    void RefreshArchivesButton_Click(object? sender, EventArgs e)
    {
        _ = RefreshArchivesAsync();
    }

    void OpenOutputFolderButton_Click(object? sender, EventArgs e)
    {
        if (_outputDirectory.Length == 0)
        {
            return;
        }
        try
        {
            Directory.CreateDirectory(_outputDirectory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("The output folder could not be created: " + ex.Message, DarkTheme.Danger);
            return;
        }
        ShowInExplorer(_outputDirectory);
    }

    void VerifySelectedButton_Click(object? sender, EventArgs e)
    {
        VerifySelectedArchive();
    }

    void CopySha256Button_Click(object? sender, EventArgs e)
    {
        if (SelectedArchive() is not { } row)
        {
            SetStatus("Select an archive in the list first.", DarkTheme.Warning);
            return;
        }
        if (row.Sha256.Length == 0)
        {
            SetStatus("That manifest does not contain a SHA-256.", DarkTheme.Warning);
            return;
        }
        CopyToClipboard(row.Sha256, "SHA-256 of the archive");
    }

    void ArchivesFilterTextBox_TextChanged(object? sender, EventArgs e)
    {
        PopulateArchivesGrid();
    }

    void ArchivesGrid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0)
        {
            VerifySelectedArchive();
        }
    }

    ArchiveRow? SelectedArchive() => archivesGrid.CurrentRow?.Tag as ArchiveRow;

    void VerifySelectedArchive()
    {
        if (SelectedArchive() is not { } row)
        {
            SetStatus("Select an archive in the list first.", DarkTheme.Warning);
            return;
        }
        if (_operation != Operation.None)
        {
            SetStatus("Another operation is still running.", DarkTheme.Warning);
            return;
        }
        ShowPage(navVerifyButton);
        _ = StartVerificationAsync(row.ManifestPath);
    }

    // ---- loading

    async Task RefreshArchivesAsync()
    {
        if (_archivesLoading)
        {
            return;
        }

        string directory = _outputDirectory;
        if (_archivesStale)
        {
            _archiveRows = [];
            _manifestCache = new Dictionary<string, CachedManifest>(StringComparer.OrdinalIgnoreCase);
            PopulateArchivesGrid();
        }
        if (directory.Length == 0 || !Directory.Exists(directory))
        {
            _archiveRows = [];
            PopulateArchivesGrid();
            archivesCountLabel.Text = "The output folder does not exist yet. It is created by the first capture.";
            return;
        }

        _archivesLoading = true;
        archivesCountLabel.Text = "Reading archives…";
        try
        {
            Dictionary<string, CachedManifest> previous = _manifestCache;
            (List<ArchiveRow> rows, Dictionary<string, CachedManifest> cache) = await Task.Run(() => ScanArchives(directory, previous));
            if (IsDisposed || !string.Equals(directory, _outputDirectory, StringComparison.Ordinal))
            {
                return;
            }
            _manifestCache = cache;
            _archiveRows = rows;
            _archivesStale = false;
            PopulateArchivesGrid();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!IsDisposed)
            {
                archivesCountLabel.Text = "The output folder could not be read: " + ex.Message;
            }
        }
        finally
        {
            _archivesLoading = false;
        }
    }

    /// <summary>Runs on a worker thread. Manifests are parsed once and re-read only when their size or timestamp changes.</summary>
    static (List<ArchiveRow> Rows, Dictionary<string, CachedManifest> Cache) ScanArchives(
        string directory, IReadOnlyDictionary<string, CachedManifest> previous)
    {
        var rows = new List<ArchiveRow>();
        var cache = new Dictionary<string, CachedManifest>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in Directory.EnumerateFiles(directory, "*.manifest.json"))
        {
            var info = new FileInfo(path);
            if (!previous.TryGetValue(path, out CachedManifest? entry)
                || entry.Length != info.Length
                || entry.LastWriteUtc != info.LastWriteTimeUtc)
            {
                entry = new CachedManifest(info.Length, info.LastWriteTimeUtc, ReadManifestRow(path, info));
            }
            cache[path] = entry;

            // The presence of the signature and of the WARC itself can change without the manifest changing.
            ArchiveRow row = entry.Row;
            bool signed = File.Exists(path + ".sig");
            bool missing = !row.Unreadable && row.WarcFile.Length > 0 && !File.Exists(Path.Combine(directory, row.WarcFile));
            rows.Add(row with { Signed = signed, WarcMissing = missing });
        }
        return (rows, cache);
    }

    static ArchiveRow ReadManifestRow(string path, FileInfo info)
    {
        static ArchiveRow Unreadable(string manifestPath, string reason) => new(
            string.Empty, null, "(unreadable manifest: " + reason + ")", string.Empty, 0, 0, false, string.Empty,
            Path.GetFileName(manifestPath), false, manifestPath, true);

        if (info.Length > MaxManifestBytes)
        {
            return Unreadable(path, "larger than 64 MB");
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Manifest? manifest = JsonSerializer.Deserialize<Manifest>(stream, ManifestJson.Options);
            if (manifest is null)
            {
                return Unreadable(path, "empty");
            }

            string started = manifest.Capture?.StartedLocal ?? string.Empty;
            DateTimeOffset? captured = DateTimeOffset.TryParse(started, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed)
                ? parsed
                : null;
            return new ArchiveRow(
                UiHelpers.FormatTimestamp(started),
                captured,
                manifest.Capture?.TargetUrl ?? string.Empty,
                manifest.Case?.Reference ?? string.Empty,
                manifest.Warc?.RecordCount ?? 0,
                manifest.Warc?.Bytes ?? 0,
                false,
                manifest.Warc?.Sha256 ?? string.Empty,
                manifest.Warc?.FileName ?? string.Empty,
                false,
                path,
                false);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return Unreadable(path, ex is JsonException ? "not valid JSON" : ex.Message);
        }
    }

    // ---- display

    static bool ContainsText(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    void PopulateArchivesGrid()
    {
        string filter = archivesFilterTextBox.Text.Trim();
        IEnumerable<ArchiveRow> query = _archiveRows
            .OrderByDescending(r => r.Captured ?? DateTimeOffset.MinValue)
            .ThenBy(r => r.WarcFile, StringComparer.OrdinalIgnoreCase);
        if (filter.Length > 0)
        {
            query = query.Where(r => ContainsText(r.Url, filter) || ContainsText(r.CaseReference, filter) || ContainsText(r.WarcFile, filter));
        }
        List<ArchiveRow> shown = query.ToList();

        string? selectedManifest = SelectedArchive()?.ManifestPath;
        DataGridViewColumn? sortedColumn = archivesGrid.SortedColumn;
        ListSortDirection direction = archivesGrid.SortOrder == SortOrder.Descending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        archivesGrid.SuspendLayout();
        archivesGrid.Rows.Clear();
        foreach (ArchiveRow item in shown)
        {
            int index = archivesGrid.Rows.Add(
                item.Local,
                item.Url,
                item.CaseReference,
                item.Records,
                item.Unreadable ? string.Empty : UiHelpers.FormatBytes(item.Bytes),
                item.Unreadable ? string.Empty : (item.Signed ? "Yes" : "No"),
                UiHelpers.Shorten(item.Sha256, 20),
                item.WarcMissing ? item.WarcFile + " (missing)" : item.WarcFile);

            DataGridViewRow row = archivesGrid.Rows[index];
            row.Tag = item;
            row.Cells[arcUrlColumn.Index].ToolTipText = item.Url;
            row.Cells[arcShaColumn.Index].ToolTipText = item.Sha256;
            row.Cells[arcFileColumn.Index].ToolTipText = item.ManifestPath;

            if (item.Unreadable || item.WarcMissing)
            {
                row.DefaultCellStyle.ForeColor = DarkTheme.Danger;
            }
            else if (item.Signed)
            {
                row.Cells[arcSignedColumn.Index].Style.ForeColor = DarkTheme.Success;
            }
        }

        if (sortedColumn is not null && sortedColumn.SortMode != DataGridViewColumnSortMode.NotSortable)
        {
            archivesGrid.Sort(sortedColumn, direction);
        }
        if (selectedManifest is not null)
        {
            foreach (DataGridViewRow row in archivesGrid.Rows)
            {
                if (row.Tag is ArchiveRow candidate && string.Equals(candidate.ManifestPath, selectedManifest, StringComparison.OrdinalIgnoreCase))
                {
                    archivesGrid.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }
        archivesGrid.ResumeLayout();

        long totalBytes = _archiveRows.Where(r => !r.Unreadable).Sum(r => r.Bytes);
        archivesCountLabel.Text = _archiveRows.Count == 0
            ? "No archives yet. Capture a page to create one."
            : shown.Count.ToString(CultureInfo.InvariantCulture) + " of " + _archiveRows.Count.ToString(CultureInfo.InvariantCulture) +
              " archive(s) — " + UiHelpers.FormatBytes(totalBytes) + " on disk (WARC files)";
    }
}
