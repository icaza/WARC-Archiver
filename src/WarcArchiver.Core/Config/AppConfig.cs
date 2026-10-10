using WarcArchiver.Core.Digests;

namespace WarcArchiver.Core.Config;

/// <summary>
/// Application settings, stored as config.json next to the executable so the user can edit them.
/// Every value is range-checked by <see cref="Normalize"/> because the file is user-editable input.
/// </summary>
public sealed class AppConfig
{
    /// <summary>Folder that receives archives, manifests and the audit log. Environment variables such as %USERPROFILE% are expanded.</summary>
    public string OutputDirectory { get; set; } = @"%USERPROFILE%\Documents\WARC Archives";

    /// <summary>Write .warc.gz (one gzip member per record, the standard layout) instead of an uncompressed .warc.</summary>
    public bool CompressWarc { get; set; } = true;

    /// <summary>Digest placed in the WARC-Block-Digest / WARC-Payload-Digest headers: "sha256" or "sha512". Both are always recorded in the manifest.</summary>
    public string WarcPrimaryDigest { get; set; } = "sha256";

    public NetworkConfig Network { get; set; } = new();

    public LimitsConfig Limits { get; set; } = new();

    public DefaultsConfig Defaults { get; set; } = new();

    public SigningConfig Signing { get; set; } = new();

    public SecurityConfig Security { get; set; } = new();

    public DigestAlgorithm PrimaryDigest =>
        DigestAlgorithmExtensions.TryParse(WarcPrimaryDigest, out var a) ? a : DigestAlgorithm.Sha256;

    public string ResolveOutputDirectory()
    {
        string raw = string.IsNullOrWhiteSpace(OutputDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WARC Archives")
            : OutputDirectory.Trim();
        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(raw));
    }

    /// <summary>Clamps every numeric setting into a safe range and repairs missing sections.</summary>
    public void Normalize()
    {
        Network ??= new NetworkConfig();
        Limits ??= new LimitsConfig();
        Defaults ??= new DefaultsConfig();
        Signing ??= new SigningConfig();
        Security ??= new SecurityConfig();

        if (!DigestAlgorithmExtensions.TryParse(WarcPrimaryDigest, out var primary))
        {
            primary = DigestAlgorithm.Sha256;
        }
        WarcPrimaryDigest = primary.WarcLabel();

        Network.ConnectTimeoutSeconds = Math.Clamp(Network.ConnectTimeoutSeconds, 3, 120);
        Network.RequestTimeoutSeconds = Math.Clamp(Network.RequestTimeoutSeconds, 5, 600);
        Network.MaxRedirects = Math.Clamp(Network.MaxRedirects, 0, 20);
        Network.MaxParallelDownloads = Math.Clamp(Network.MaxParallelDownloads, 1, 16);
        Network.UserAgent = string.IsNullOrWhiteSpace(Network.UserAgent) ? new NetworkConfig().UserAgent : SanitizeHeader(Network.UserAgent);
        Network.AcceptLanguage = string.IsNullOrWhiteSpace(Network.AcceptLanguage) ? "en-US,en;q=0.9" : SanitizeHeader(Network.AcceptLanguage);
        Network.ProxyUrl = (Network.ProxyUrl ?? string.Empty).Trim();

        Limits.MaxResources = Math.Clamp(Limits.MaxResources, 1, 5000);
        Limits.MaxResourceMegabytes = Math.Clamp(Limits.MaxResourceMegabytes, 1, 2048);
        Limits.MaxTotalMegabytes = Math.Clamp(Limits.MaxTotalMegabytes, 1, 20480);
        Limits.MaxCssDepth = Math.Clamp(Limits.MaxCssDepth, 0, 5);
        Limits.MaxParseMegabytes = Math.Clamp(Limits.MaxParseMegabytes, 1, 64);
        Limits.MaxMemoryKilobytesPerBuffer = Math.Clamp(Limits.MaxMemoryKilobytesPerBuffer, 16, 4096);

        Signing.CertificateThumbprint = (Signing.CertificateThumbprint ?? string.Empty)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    static string SanitizeHeader(string value)
    {
        var chars = value.Where(c => c >= 0x20 && c != 0x7F).ToArray();
        return new string(chars).Trim();
    }
}

public sealed class NetworkConfig
{
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 WarcArchiver/1.0";

    public string AcceptLanguage { get; set; } = "en-US,en;q=0.9";

    public int ConnectTimeoutSeconds { get; set; } = 15;

    public int RequestTimeoutSeconds { get; set; } = 90;

    public int MaxRedirects { get; set; } = 10;

    public int MaxParallelDownloads { get; set; } = 4;

    /// <summary>When false (default) the app refuses loopback, private, link-local and other non-public addresses, including via redirects.</summary>
    public bool AllowPrivateNetworks { get; set; }

    /// <summary>When false (default) a certificate error aborts the request. When true the page is captured anyway and the error is recorded as evidence.</summary>
    public bool AllowInvalidCertificates { get; set; }

    /// <summary>Optional explicit HTTP proxy, e.g. http://proxy.example.com:8080. Empty means a direct connection.</summary>
    public string ProxyUrl { get; set; } = string.Empty;
}

public sealed class LimitsConfig
{
    public int MaxResources { get; set; } = 500;

    public int MaxResourceMegabytes { get; set; } = 50;

    public int MaxTotalMegabytes { get; set; } = 1000;

    /// <summary>How many levels of stylesheet @import / url() references are followed after the page itself.</summary>
    public int MaxCssDepth { get; set; } = 2;

    /// <summary>Largest HTML/CSS body (after decompression) that is parsed for further links.</summary>
    public int MaxParseMegabytes { get; set; } = 8;

    /// <summary>Per-download in-memory buffer; larger downloads spill to a temporary file.</summary>
    public int MaxMemoryKilobytesPerBuffer { get; set; } = 256;
}

public sealed class DefaultsConfig
{
    public bool IncludeScripts { get; set; } = true;

    public bool IncludeStylesheets { get; set; } = true;

    public bool IncludeImages { get; set; } = true;

    /// <summary>Fonts, audio/video sources, frames and other referenced assets.</summary>
    public bool IncludeOtherAssets { get; set; } = true;

    /// <summary>Also archive resources hosted on other domains (CDNs, trackers).</summary>
    public bool IncludeThirdParty { get; set; } = true;

    public string OperatorName { get; set; } = string.Empty;
}

public sealed class SigningConfig
{
    /// <summary>SHA-1 thumbprint of a certificate in the Windows "Current User\Personal" store used to sign manifests. Empty means hash-only.</summary>
    public string CertificateThumbprint { get; set; } = string.Empty;
}

public sealed class SecurityConfig
{
    /// <summary>Set the read-only attribute on finished archives, manifests and checksum files.</summary>
    public bool MarkFilesReadOnly { get; set; } = true;
}
