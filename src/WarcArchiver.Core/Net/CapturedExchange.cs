using System.Net;
using WarcArchiver.Core.Http;
using WarcArchiver.Core.Io;

namespace WarcArchiver.Core.Net;

/// <summary>Evidence about the TLS peer, captured during the handshake that carried the request.</summary>
public sealed class TlsEvidence
{
    public string? PeerSubject { get; set; }

    public string? PeerIssuer { get; set; }

    public string? PeerSerial { get; set; }

    public string? PeerSha256Hex { get; set; }

    public string? PeerDerBase64 { get; set; }

    public DateTime? NotBeforeUtc { get; set; }

    public DateTime? NotAfterUtc { get; set; }

    public List<string> Chain { get; } = [];

    public string PolicyErrors { get; set; } = "None";

    public bool HasPolicyErrors => !string.Equals(PolicyErrors, "None", StringComparison.Ordinal);
}

/// <summary>
/// The raw outcome of one HTTP request: the exact bytes sent and received on the wire (after TLS decryption),
/// plus the network facts around them. Disposing releases the buffers (and any temporary file).
/// </summary>
public sealed class CapturedExchange(Uri uri, int memoryThreshold) : IDisposable
{
    public Uri Uri { get; } = uri;

    public SpoolStream Request { get; } = new SpoolStream(memoryThreshold);

    public SpoolStream Response { get; } = new SpoolStream(memoryThreshold);

    public DateTime StartUtc { get; set; }

    public TimeSpan Duration { get; set; }

    public IPAddress? RemoteIp { get; set; }

    public List<string> ResolvedAddresses { get; } = [];

    public TlsEvidence? Tls { get; set; }

    /// <summary>Parsed head of the final response, or null when nothing parseable was received.</summary>
    public HttpHead? Head { get; set; }

    public int StatusCode => Head?.StatusCode ?? 0;

    /// <summary>WARC-Truncated reason when the response is incomplete ("length", "time", "disconnect").</summary>
    public string? Truncated { get; set; }

    public string? Error { get; set; }

    public bool HasResponse => Head is not null && Response.Length > 0;

    public void Dispose()
    {
        Request.Dispose();
        Response.Dispose();
    }
}

public sealed class NetworkPolicyException : Exception
{
    public NetworkPolicyException(string message)
        : base(message)
    {
    }
}
