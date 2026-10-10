using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WarcArchiver.App.Theme;
using WarcArchiver.App.Util;
using WarcArchiver.Core.Archive;
using WarcArchiver.Core.Audit;
using WarcArchiver.Core.Config;

namespace WarcArchiver.App;

/// <summary>
/// Application shell: start-up, navigation, configuration, certificates and the "one long operation at a time" rule.
/// Each page's behaviour lives in its own partial file (Capture, Archives, Verify, Audit, Settings). Every control is
/// declared and laid out in MainForm.Designer.cs; this code only reacts to them.
/// </summary>
internal sealed partial class MainForm : Form
{
    const string AuditFileName = "audit-log.jsonl";
    const int TipPadding = 8;

    const TextFormatFlags TipFlags =
        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;

    enum Operation
    {
        None,
        Capture,
        Verify,
        Audit,
    }

    /// <summary>Entry of the signing-certificate drop-down (a null certificate means "do not sign").</summary>
    sealed class CertChoice(X509Certificate2? certificate, string label)
    {
        public X509Certificate2? Certificate { get; } = certificate;

        public override string ToString() => label;
    }

    readonly Dictionary<Button, Control> _pages = [];
    readonly List<X509Certificate2> _certificates = [];

    Button? _activeNav;
    Operation _operation = Operation.None;
    bool _closeWhenIdle;

    ConfigLoadResult? _loaded;
    string _configPath = string.Empty;
    AuditLog? _audit;
    ArchiveEngine? _engine;
    string _outputDirectory = string.Empty;

    public MainForm()
    {
        InitializeComponent();

        _pages[navCaptureButton] = capturePage;
        _pages[navArchivesButton] = archivesPage;
        _pages[navVerifyButton] = verifyPage;
        _pages[navAuditButton] = auditPage;
        _pages[navSettingsButton] = settingsPage;

        versionLabel.Text = "Version " + ArchiveEngine.ToolVersion;

        DarkTheme.Apply(this);
        DarkTheme.UseDarkTitleBar(this);
    }

    // ------------------------------------------------------------------------------------------
    // Start-up and shutdown
    // ------------------------------------------------------------------------------------------

    void MainForm_Load(object? sender, EventArgs e)
    {
        LoadCertificates();

        var notices = new List<string>();
        TryApplyConfiguration(initial: true, notices);

        ShowPage(navCaptureButton);
        UpdateControlStates();

        if (notices.Count > 0)
        {
            string message = string.Join(Environment.NewLine + Environment.NewLine, notices);
            SetStatus(notices[0], DarkTheme.Warning);
            // Shown once the window is up, so the user sees what the message is about.
            BeginInvoke((Action)(() => MessageBox.Show(this, message, "WARC Archiver", MessageBoxButtons.OK, MessageBoxIcon.Warning)));
        }

        _ = VerifyAuditChainAsync(userInitiated: false);
    }

    void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_operation == Operation.Capture)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    "A capture is still running. Cancel it and close the application?\n\nThe unfinished archive is discarded.",
                    "WARC Archiver",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                e.Cancel = true;
                if (answer == DialogResult.Yes)
                {
                    // Wait for the engine to clean up its partial files, then close (see EndOperation).
                    _closeWhenIdle = true;
                    RequestCaptureCancel();
                }
                return;
            }

            _captureCts?.Cancel();
        }

        _verifyCts?.Cancel();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        base.OnFormClosed(e);
        _captureCts?.Dispose();
        _verifyCts?.Dispose();
        _statusFont?.Dispose();
        DisposeCertificates();
    }

    // ------------------------------------------------------------------------------------------
    // Navigation
    // ------------------------------------------------------------------------------------------

    void NavButton_Click(object? sender, EventArgs e)
    {
        if (sender is Button nav)
        {
            ShowPage(nav);
        }
    }

    void NavButton_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is Button button && ReferenceEquals(button, _activeNav))
        {
            using var brush = new SolidBrush(DarkTheme.Accent);
            e.Graphics.FillRectangle(brush, 0, 8, 4, Math.Max(0, button.Height - 16));
        }
    }

    void ShowPage(Button nav)
    {
        if (!_pages.TryGetValue(nav, out Control? page))
        {
            return;
        }

        contentPanel.SuspendLayout();
        foreach ((Button button, Control control) in _pages)
        {
            bool active = ReferenceEquals(button, nav);
            control.Visible = active;
            button.BackColor = active ? DarkTheme.SurfaceRaised : DarkTheme.Surface;
            button.ForeColor = active ? DarkTheme.Text : DarkTheme.TextMuted;
            button.Invalidate();
        }
        _activeNav = nav;
        contentPanel.ResumeLayout(performLayout: true);

        if (ReferenceEquals(page, archivesPage))
        {
            _ = RefreshArchivesAsync();
        }
        else if (ReferenceEquals(page, auditPage))
        {
            RefreshAuditPage();
        }
        else if (ReferenceEquals(page, settingsPage))
        {
            RefreshSettingsPage();
        }
    }

    // ------------------------------------------------------------------------------------------
    // One long operation at a time: keeps resource use low and keeps the audit log's reader and writer apart
    // ------------------------------------------------------------------------------------------

    bool TryBeginOperation(Operation operation)
    {
        if (_operation != Operation.None)
        {
            SetStatus("Another operation is still running.", DarkTheme.Warning);
            return false;
        }
        _operation = operation;
        UpdateControlStates();
        return true;
    }

    void EndOperation()
    {
        _operation = Operation.None;
        if (IsDisposed)
        {
            return;
        }
        UpdateControlStates();
        if (_closeWhenIdle)
        {
            _closeWhenIdle = false;
            BeginInvoke((Action)Close);
        }
    }

    void UpdateControlStates()
    {
        bool idle = _operation == Operation.None;

        captureButton.Enabled = idle && _engine is not null;
        cancelButton.Enabled = _operation == Operation.Capture && !_cancelRequested;
        refreshCertsButton.Enabled = idle;

        openFolderButton.Enabled = _lastResult?.WarcPath is not null;
        verifyThisButton.Enabled = idle && _lastResult?.WarcPath is not null;
        copyHashButton.Enabled = _lastResult?.WarcDigests is not null;

        verifySelectedButton.Enabled = idle;
        verifyBrowseButton.Enabled = idle;
        verifyRunButton.Enabled = idle || _operation == Operation.Verify;
        verifyRunButton.Text = _operation == Operation.Verify ? "Cancel" : "Run verification";
        verifyExportButton.Enabled = _lastReport is not null && _operation != Operation.Verify;

        auditVerifyButton.Enabled = idle && _audit is not null;
        reloadConfigButton.Enabled = idle;
        changeOutputButton.Enabled = idle;
    }

    // ------------------------------------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------------------------------------

    /// <summary>
    /// Reads config.json and (re)creates the audit log and engine. On failure the previous configuration stays active.
    /// Problems and warnings are appended to <paramref name="notices"/>; the return value says whether it was applied.
    /// </summary>
    bool TryApplyConfiguration(bool initial, List<string> notices)
    {
        ConfigLoadResult load;
        string directory;
        AuditLog audit;
        ArchiveEngine engine;

        _configPath = ConfigStore.ResolvePath();
        try
        {
            load = ConfigStore.Load(_configPath);
            if (load.Warning is not null && !initial && _engine is not null)
            {
                // A typo while editing must not silently replace working settings with defaults.
                notices.Add(load.Warning + "\n\nThe previous settings stay active. Fix config.json and click Reload again.");
                return false;
            }
            directory = load.Config.ResolveOutputDirectory();
            Directory.CreateDirectory(directory);
            audit = new AuditLog(Path.Combine(directory, AuditFileName));
            engine = new ArchiveEngine(load.Config, audit, load.Sha256Hex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException
                                       or System.Security.SecurityException)
        {
            notices.Add(
                "The configuration could not be applied: " + ex.Message +
                (_engine is null ? "\n\nCapturing is disabled until config.json points to a writable output folder." : "\n\nThe previous settings stay active."));
            return false;
        }

        if (load.Warning is not null)
        {
            notices.Add(load.Warning);
        }
        if (audit.QuarantinedFile is not null)
        {
            notices.Add(
                "The existing audit log was damaged. It was set aside as \"" + Path.GetFileName(audit.QuarantinedFile) +
                "\" (its SHA-256 is recorded in the new log) and a fresh log was started. Keep the damaged file as evidence.");
        }

        _loaded = load;
        _audit = audit;
        _engine = engine;
        _outputDirectory = directory;

        AppConfig config = load.Config;
        scriptsCheckBox.Checked = config.Defaults.IncludeScripts;
        stylesheetsCheckBox.Checked = config.Defaults.IncludeStylesheets;
        imagesCheckBox.Checked = config.Defaults.IncludeImages;
        otherAssetsCheckBox.Checked = config.Defaults.IncludeOtherAssets;
        thirdPartyCheckBox.Checked = config.Defaults.IncludeThirdParty;
        if (string.IsNullOrWhiteSpace(operatorTextBox.Text))
        {
            operatorTextBox.Text = config.Defaults.OperatorName;
        }

        string wanted = UiHelpers.NormalizeThumbprint(config.Signing.CertificateThumbprint);
        if (wanted.Length > 0)
        {
            if (!SelectCertificate(wanted))
            {
                notices.Add("The signing certificate named in config.json (thumbprint " + wanted + ") was not found in your Windows \"Personal\" certificate store, so manifests will not be signed unless you pick another certificate.");
            }
        }
        else if (initial)
        {
            signComboBox.SelectedIndex = 0;
        }

        outputStatusLabel.Text = "Output: " + ShortenPath(directory, 52);
        outputStatusLabel.ToolTipText = directory;

        _archivesStale = true;
        UpdateControlStates();
        return true;
    }

    static string ShortenPath(string path, int max) =>
        path.Length <= max ? path : "…" + path[^(max - 1)..];

    // ------------------------------------------------------------------------------------------
    // Signing certificates (Windows "Current User > Personal" store)
    // ------------------------------------------------------------------------------------------

    void RefreshCertsButton_Click(object? sender, EventArgs e)
    {
        LoadCertificates();
        int count = signComboBox.Items.Count - 1;
        SetStatus(
            count == 0
                ? "No usable signing certificate found in the Windows \"Personal\" store."
                : count.ToString(CultureInfo.InvariantCulture) + " signing certificate(s) available.");
    }

    void LoadCertificates()
    {
        string previous = (signComboBox.SelectedItem as CertChoice)?.Certificate?.Thumbprint ?? string.Empty;

        signComboBox.Items.Clear();
        DisposeCertificates();
        signComboBox.Items.Add(new CertChoice(null, "None — hash-only evidence (SHA-256 / SHA-512)"));

        try
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

            var usable = new List<(string Label, X509Certificate2 Certificate)>();
            foreach (X509Certificate2 certificate in store.Certificates)
            {
                if (certificate.HasPrivateKey && HasSupportedKey(certificate))
                {
                    usable.Add((DescribeCertificate(certificate), certificate));
                }
                else
                {
                    certificate.Dispose();
                }
            }

            foreach ((string label, X509Certificate2 certificate) in usable.OrderBy(c => c.Label, StringComparer.OrdinalIgnoreCase))
            {
                _certificates.Add(certificate);
                signComboBox.Items.Add(new CertChoice(certificate, label));
            }
        }
        catch (CryptographicException)
        {
            // The store could not be opened (locked down profile): signing is simply unavailable.
        }

        signComboBox.SelectedIndex = 0;
        if (previous.Length > 0)
        {
            SelectCertificate(UiHelpers.NormalizeThumbprint(previous));
        }
    }

    bool SelectCertificate(string normalizedThumbprint)
    {
        for (int i = 1; i < signComboBox.Items.Count; i++)
        {
            if (signComboBox.Items[i] is CertChoice { Certificate: { } certificate }
                && string.Equals(UiHelpers.NormalizeThumbprint(certificate.Thumbprint), normalizedThumbprint, StringComparison.Ordinal))
            {
                signComboBox.SelectedIndex = i;
                return true;
            }
        }
        return false;
    }

    static bool HasSupportedKey(X509Certificate2 certificate)
    {
        using RSA? rsa = certificate.GetRSAPublicKey();
        if (rsa is not null)
        {
            return true;
        }
        using ECDsa? ecdsa = certificate.GetECDsaPublicKey();
        return ecdsa is not null;
    }

    static string DescribeCertificate(X509Certificate2 certificate)
    {
        string name = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = certificate.Subject;
        }
        string validity = certificate.NotAfter < DateTime.Now ? "EXPIRED " : "expires ";
        string thumbprint = certificate.Thumbprint.Length > 8 ? certificate.Thumbprint[..8] : certificate.Thumbprint;
        return name + " — " + validity + certificate.NotAfter.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " — " + thumbprint;
    }

    void DisposeCertificates()
    {
        foreach (X509Certificate2 certificate in _certificates)
        {
            certificate.Dispose();
        }
        _certificates.Clear();
    }

    // ------------------------------------------------------------------------------------------
    // Status bar and tooltips
    // ------------------------------------------------------------------------------------------

    void SetStatus(string text, Color? color = null)
    {
        statusLabel.Text = text;
        statusLabel.ForeColor = color ?? DarkTheme.TextMuted;
    }

    void SetChainStatus(string text, Color color)
    {
        chainStatusLabel.Text = text;
        chainStatusLabel.ForeColor = color;
    }

    void ToolTip_Popup(object? sender, PopupEventArgs e)
    {
        string? tip = e.AssociatedControl is { } control ? toolTip.GetToolTip(control) : null;
        string text = tip ?? string.Empty;
        Size measured = TextRenderer.MeasureText(text, SystemFonts.StatusFont ?? Font, new Size(380, 0), TipFlags);
        e.ToolTipSize = new Size(measured.Width + 2 * TipPadding, measured.Height + 2 * TipPadding);
    }

    void ToolTip_Draw(object? sender, DrawToolTipEventArgs e)
    {
        using var fill = new SolidBrush(DarkTheme.SurfaceHover);
        using var border = new Pen(DarkTheme.Border);
        e.Graphics.FillRectangle(fill, e.Bounds);
        e.Graphics.DrawRectangle(border, 0, 0, e.Bounds.Width - 1, e.Bounds.Height - 1);

        var textBounds = new Rectangle(TipPadding, TipPadding, e.Bounds.Width - 2 * TipPadding, e.Bounds.Height - 2 * TipPadding);
        TextRenderer.DrawText(e.Graphics, e.ToolTipText, e.Font, textBounds, DarkTheme.Text, TipFlags);
    }

    // ------------------------------------------------------------------------------------------
    // Shared by several pages
    // ------------------------------------------------------------------------------------------

    void CopyToClipboard(string text, string what)
    {
        if (UiHelpers.TryCopyToClipboard(text))
        {
            SetStatus(what + " copied to the clipboard.", DarkTheme.Success);
        }
        else
        {
            SetStatus("The clipboard is in use by another program. Try again.", DarkTheme.Warning);
        }
    }

    void ShowInExplorer(string path)
    {
        if (!UiHelpers.TryShowInExplorer(path))
        {
            SetStatus("Could not open \"" + path + "\".", DarkTheme.Warning);
        }
    }

    /// <summary>Re-checks the audit chain (the status-bar indicator and the Audit page share this).</summary>
    async Task VerifyAuditChainAsync(bool userInitiated)
    {
        AuditLog? audit = _audit;
        if (audit is null)
        {
            SetChainStatus("Audit chain: unavailable", DarkTheme.Danger);
            return;
        }
        if (!TryBeginOperation(Operation.Audit))
        {
            return;
        }

        string path = audit.Path;
        SetChainStatus("Audit chain: checking…", DarkTheme.TextMuted);
        try
        {
            AuditVerification result = File.Exists(path)
                ? await Task.Run(() => AuditLog.Verify(path))
                : new AuditVerification(true, 0, null, null, AuditLog.GenesisHash);

            if (IsDisposed || !ReferenceEquals(audit, _audit))
            {
                return;
            }
            ShowChainResult(result, userInitiated);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (!IsDisposed)
            {
                SetChainStatus("Audit chain: unreadable", DarkTheme.Danger);
                auditStatusLabel.ForeColor = DarkTheme.Danger;
                auditStatusLabel.Text = "The audit log could not be read: " + ex.Message;
            }
        }
        finally
        {
            EndOperation();
        }
    }
}
