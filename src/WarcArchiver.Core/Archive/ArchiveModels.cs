using System.Security.Cryptography.X509Certificates;
using WarcArchiver.Core.Digests;
using WarcArchiver.Core.Parsing;

namespace WarcArchiver.Core.Archive;

/// <summary>Everything the operator chooses for one capture.</summary>
public sealed class ArchiveRequest
{
    public required string Url { get; init; }

    public string CaseReference { get; init; } = string.Empty;

    public string Operator { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public bool IncludeScripts { get; init; } = true;

    public bool IncludeStylesheets { get; init; } = true;

    public bool IncludeImages { get; init; } = true;

    public bool IncludeOtherAssets { get; init; } = true;

    public bool IncludeThirdParty { get; init; } = true;

    /// <summary>Certificate (with private key) used to sign the manifest; null for hash-only evidence.</summary>
    public X509Certificate2? SigningCertificate { get; init; }
}

public enum ArchiveEventKind
{
    Stage,
    Resource,
}

/// <summary>One line of the live capture table.</summary>
public sealed class ResourceEntry
{
    public DateTime LocalTime { get; init; } = DateTime.Now;

    public string Status { get; init; } = string.Empty;

    public ResourceKind Kind { get; init; }

    public string Url { get; init; } = string.Empty;

    public long Bytes { get; init; }

    public string Sha256 { get; init; } = string.Empty;

    public int DurationMs { get; init; }

    public string Note { get; init; } = string.Empty;

    public bool IsProblem { get; init; }
}

public sealed class ArchiveEvent
{
    public ArchiveEventKind Kind { get; init; }

    public string Message { get; init; } = string.Empty;

    public ResourceEntry? Resource { get; init; }

    public int Completed { get; init; }

    public int Discovered { get; init; }
}

public sealed class ArchiveResult
{
    public bool Success { get; init; }

    public bool Cancelled { get; init; }

    public string? Error { get; init; }

    public string? WarcPath { get; init; }

    public string? ManifestPath { get; init; }

    public string? SignaturePath { get; init; }

    public string? ChecksumPath { get; init; }

    public DigestResult? WarcDigests { get; init; }

    public string? ManifestSha256 { get; init; }

    public bool Signed { get; init; }

    public string? FinalUrl { get; init; }

    public int MainStatus { get; init; }

    public int ResourcesArchived { get; init; }

    public int ResourcesFailed { get; init; }

    public int ResourcesSkipped { get; init; }

    public long ResponseBytes { get; init; }

    public IReadOnlyList<ManifestIssue> Issues { get; init; } = [];

    public static ArchiveResult Failure(string error) => new() { Error = error };
}
