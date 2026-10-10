using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WarcArchiver.Core.Archive;

/// <summary>The chain-of-custody record written next to every archive.</summary>
public sealed class Manifest
{
    public string Schema { get; set; } = "warc-archiver-manifest/1";

    public ManifestTool Tool { get; set; } = new();

    public ManifestCase Case { get; set; } = new();

    public ManifestCapture Capture { get; set; } = new();

    public ManifestSettings Settings { get; set; } = new();

    public ManifestWarc Warc { get; set; } = new();

    public ManifestAudit Audit { get; set; } = new();

    public List<ManifestIssue> Issues { get; set; } = [];

    public List<ManifestRecord> Records { get; set; } = [];
}

public sealed class ManifestTool
{
    public string Name { get; set; } = "WarcArchiver";

    public string Version { get; set; } = string.Empty;

    public string Runtime { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;
}

public sealed class ManifestCase
{
    public string Reference { get; set; } = string.Empty;

    public string Operator { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public sealed class ManifestCapture
{
    public string TargetUrl { get; set; } = string.Empty;

    public string FinalUrl { get; set; } = string.Empty;

    public int MainStatus { get; set; }

    /// <summary>Capture start in UTC (the WARC-Date basis).</summary>
    public string StartedUtc { get; set; } = string.Empty;

    /// <summary>Capture start in the operator's local time, with UTC offset.</summary>
    public string StartedLocal { get; set; } = string.Empty;

    public string FinishedUtc { get; set; } = string.Empty;

    public string FinishedLocal { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string UtcOffset { get; set; } = string.Empty;

    /// <summary>The "Date" header of the first response — an independent time reference supplied by the server.</summary>
    public string? ServerDateHeader { get; set; }

    /// <summary>Server Date header minus local UTC clock at request time, in seconds (includes network latency and 1 s rounding).</summary>
    public double? ClockSkewSeconds { get; set; }

    public int ResourcesArchived { get; set; }

    public int ResourcesFailed { get; set; }

    public int ResourcesSkipped { get; set; }

    public long ResponseBytes { get; set; }
}

public sealed class ManifestSettings
{
    public bool IncludeScripts { get; set; }

    public bool IncludeStylesheets { get; set; }

    public bool IncludeImages { get; set; }

    public bool IncludeOtherAssets { get; set; }

    public bool IncludeThirdParty { get; set; }

    public string UserAgent { get; set; } = string.Empty;

    public int MaxRedirects { get; set; }

    public int MaxResources { get; set; }

    public int MaxResourceMegabytes { get; set; }

    public bool AllowPrivateNetworks { get; set; }

    public bool AllowInvalidCertificates { get; set; }

    public string ConfigSha256 { get; set; } = string.Empty;
}

public sealed class ManifestWarc
{
    public string FileName { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public bool Compressed { get; set; }

    public long Bytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string Sha512 { get; set; } = string.Empty;

    public string PrimaryDigest { get; set; } = string.Empty;

    public int RecordCount { get; set; }
}

public sealed class ManifestAudit
{
    public string LogFile { get; set; } = string.Empty;

    /// <summary>Sequence number of the audit entry written when this capture started.</summary>
    public long AnchorSeq { get; set; }

    /// <summary>Hash of that entry; it commits this capture to everything the log recorded before it.</summary>
    public string AnchorHash { get; set; } = string.Empty;
}

public sealed class ManifestIssue
{
    /// <summary>"failed", "skipped" or "warning".</summary>
    public string Type { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class ManifestRecord
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Uri { get; set; }

    public string DateUtc { get; set; } = string.Empty;

    public long Offset { get; set; }

    public long StoredLength { get; set; }

    public long BlockLength { get; set; }

    public int? HttpStatus { get; set; }

    public string? MediaType { get; set; }

    public string? IpAddress { get; set; }

    public string? Truncated { get; set; }

    public string BlockSha256 { get; set; } = string.Empty;

    public string BlockSha512 { get; set; } = string.Empty;

    public string? PayloadSha256 { get; set; }

    public string? PayloadSha512 { get; set; }
}

public static class ManifestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}
