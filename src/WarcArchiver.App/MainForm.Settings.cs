using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using WarcArchiver.App.Theme;
using WarcArchiver.App.Util;
using WarcArchiver.Core.Config;

namespace WarcArchiver.App;

// Settings page: config.json is the single place for settings. The page shows it as it is on disk, opens it in the
// user's editor, applies changes on "Reload", and can change the output folder without destroying the file's comments.
internal sealed partial class MainForm
{
    const long MaxConfigDisplayBytes = 256 * 1024;

    // A line that starts with "outputDirectory": "<json string>" (comments start with // and therefore never match).
    // Keeps accented characters readable in the file; the value is only ever written into a local JSON file.
    static readonly JsonSerializerOptions PathJsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    const string OutputDirectoryPattern = "^(?<pre>[ \\t]*\"outputDirectory\"[ \\t]*:[ \\t]*)\"(?:[^\"\\\\]|\\\\.)*\"";

    void OpenConfigButton_Click(object? sender, EventArgs e)
    {
        if (!File.Exists(_configPath))
        {
            SetStatus("config.json does not exist yet: " + _configPath, DarkTheme.Warning);
            return;
        }
        if (!UiHelpers.TryOpenInEditor(_configPath))
        {
            SetStatus("No editor could be started for " + _configPath, DarkTheme.Warning);
        }
    }

    void ReloadConfigButton_Click(object? sender, EventArgs e)
    {
        ReloadConfiguration();
    }

    void ChangeOutputButton_Click(object? sender, EventArgs e)
    {
        if (_operation != Operation.None)
        {
            return;
        }

        if (Directory.Exists(_outputDirectory))
        {
            folderBrowserDialog.SelectedPath = _outputDirectory;
        }
        if (folderBrowserDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string chosen = folderBrowserDialog.SelectedPath;
        if (!TryWriteOutputDirectory(chosen, out string? error))
        {
            MessageBox.Show(this, error ?? "The folder could not be used.", "WARC Archiver", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        ReloadConfiguration("Output folder changed. Earlier archives stay in the old folder; this folder keeps its own audit log.");
    }

    void ReloadConfiguration(string? successMessage = null)
    {
        if (_operation != Operation.None)
        {
            SetStatus("Another operation is still running.", DarkTheme.Warning);
            return;
        }

        var notices = new List<string>();
        bool applied = TryApplyConfiguration(initial: false, notices);
        RefreshSettingsPage();

        if (notices.Count > 0)
        {
            SetStatus(notices[0].Split('\n')[0], DarkTheme.Warning);
            MessageBox.Show(
                this,
                string.Join(Environment.NewLine + Environment.NewLine, notices),
                "WARC Archiver",
                MessageBoxButtons.OK,
                applied ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        else
        {
            SetStatus(successMessage ?? "Configuration reloaded.", DarkTheme.Success);
        }

        if (applied)
        {
            _ = VerifyAuditChainAsync(userInitiated: false);
        }
    }

    void RefreshSettingsPage()
    {
        configPathLabel.Text = "Configuration file: " + _configPath;

        string text;
        try
        {
            var info = new FileInfo(_configPath);
            if (!info.Exists)
            {
                text = "(config.json does not exist yet.)";
            }
            else if (info.Length > MaxConfigDisplayBytes)
            {
                text = "(config.json is too large to display here. Open it in an editor.)";
            }
            else
            {
                text = File.ReadAllText(_configPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            text = "(config.json could not be read: " + ex.Message + ")";
        }
        settingsTextBox.Text = text.Replace("\r\n", "\n").Replace("\n", "\r\n");
        settingsTextBox.Select(0, 0);

        if (_loaded is not { } loaded)
        {
            settingsNoteLabel.ForeColor = DarkTheme.Danger;
            settingsNoteLabel.Text = "The configuration is not active: capturing is disabled until config.json is fixed.";
            return;
        }

        NetworkConfig network = loaded.Config.Network;
        bool relaxed = network.AllowPrivateNetworks || network.AllowInvalidCertificates;
        settingsNoteLabel.ForeColor = relaxed ? DarkTheme.Warning : DarkTheme.TextMuted;
        settingsNoteLabel.Text =
            "Output folder: " + _outputDirectory + Environment.NewLine +
            "Private networks: " + (network.AllowPrivateNetworks ? "ALLOWED" : "blocked") +
            "   ·   Invalid TLS certificates: " + (network.AllowInvalidCertificates ? "ACCEPTED (recorded as evidence)" : "refused") +
            "   ·   Parallel downloads: " + network.MaxParallelDownloads;
    }

    /// <summary>
    /// Rewrites only the value of "outputDirectory" in config.json, so comments and formatting survive. The folder is
    /// proven writable first. A damaged file is never overwritten: if the key cannot be found, nothing is changed.
    /// </summary>
    bool TryWriteOutputDirectory(string folder, out string? error)
    {
        error = null;

        try
        {
            Directory.CreateDirectory(folder);
            string probe = Path.Combine(folder, ".write-test-" + Guid.NewGuid().ToString("N"));
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            error = "That folder is not writable, so it was not selected:\n\n" + ex.Message;
            return false;
        }

        try
        {
            string text = File.ReadAllText(_configPath);
            Match match = Regex.Match(text, OutputDirectoryPattern, RegexOptions.Multiline | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
            if (!match.Success)
            {
                error = "config.json has no \"outputDirectory\" line that can be updated automatically. Open the file and set it by hand:\n\n" +
                        "\"outputDirectory\": " + JsonSerializer.Serialize(folder, PathJsonOptions) + ",";
                return false;
            }

            string updated = string.Concat(
                text.AsSpan(0, match.Index),
                match.Groups["pre"].Value,
                JsonSerializer.Serialize(folder, PathJsonOptions),
                text.AsSpan(match.Index + match.Length));

            string temp = _configPath + ".tmp";
            File.WriteAllText(temp, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temp, _configPath, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or RegexMatchTimeoutException)
        {
            error = "config.json could not be updated:\n\n" + ex.Message;
            return false;
        }
    }
}
