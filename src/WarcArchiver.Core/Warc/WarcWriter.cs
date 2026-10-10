using System.IO.Compression;
using System.Globalization;
using System.Text;
using WarcArchiver.Core.Digests;
using WarcArchiver.Core.Http;
using WarcArchiver.Core.Io;

namespace WarcArchiver.Core.Warc;

public static class WarcTypes
{
    public const string WarcInfo = "warcinfo";
    public const string Response = "response";
    public const string Request = "request";
    public const string Metadata = "metadata";
}

public static class WarcFormat
{
    public const string Version = "WARC/1.1";
    public const string SpecUrl = "https://iipc.github.io/warc-specifications/specifications/warc-format/warc-1.1/";
    public const string HttpResponseContentType = "application/http; msgtype=response";
    public const string HttpRequestContentType = "application/http; msgtype=request";
    public const string FieldsContentType = "application/warc-fields";

    public static string FormatDate(DateTime utc) =>
        utc.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static string FormatId(Guid id) => "<urn:uuid:" + id.ToString("D") + ">";

    public static bool TryParseId(string? value, out Guid id)
    {
        id = Guid.Empty;
        if (value is null)
        {
            return false;
        }
        string v = value.Trim().Trim('<', '>');
        return v.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase) && Guid.TryParse(v[9..], out id);
    }
}

/// <summary>Description of one record to be written.</summary>
public sealed class WarcRecordSpec
{
    public required string Type { get; init; }

    public required string ContentType { get; init; }

    public Guid? Id { get; init; }

    public DateTime DateUtc { get; init; } = DateTime.UtcNow;

    public string? TargetUri { get; init; }

    public string? IpAddress { get; init; }

    public Guid? ConcurrentTo { get; init; }

    /// <summary>WARC-Truncated reason: "length", "time", "disconnect" or "unspecified".</summary>
    public string? Truncated { get; init; }

    public string? Filename { get; init; }

    public bool IsHttpResponse { get; init; }

    public int? HttpStatus { get; init; }

    public string? MediaType { get; init; }

    public List<KeyValuePair<string, string>> ExtraHeaders { get; } = [];
}

/// <summary>What the writer knows about a record it wrote — everything the manifest needs.</summary>
public sealed class WarcRecordInfo
{
    public required Guid Id { get; init; }

    public required string Type { get; init; }

    public string? TargetUri { get; init; }

    public DateTime DateUtc { get; init; }

    /// <summary>Byte offset of the record (or of its gzip member) in the archive file.</summary>
    public long Offset { get; init; }

    /// <summary>Bytes the record occupies in the file (compressed size for .warc.gz).</summary>
    public long StoredLength { get; init; }

    public long BlockLength { get; init; }

    public string? IpAddress { get; init; }

    public int? HttpStatus { get; init; }

    public string? MediaType { get; init; }

    public string? Truncated { get; init; }

    public required DigestResult Block { get; init; }

    public DigestResult? Payload { get; init; }
}

/// <summary>
/// Writes ISO 28500 (WARC 1.1) records. In compressed mode every record is its own gzip member, which is the
/// layout every WARC tool expects for random access. All bytes written are fingerprinted on the fly.
/// </summary>
public sealed class WarcWriter(Stream output, bool compress, DigestAlgorithm primaryDigest) : IDisposable
{
    static readonly byte[] RecordTerminator = "\r\n\r\n"u8.ToArray();

    readonly HashingWriteStream _out = new HashingWriteStream(output);
    readonly bool _compress = compress;
    readonly DigestAlgorithm _primary = primaryDigest;
    readonly object _gate = new();
    readonly List<WarcRecordInfo> _records = [];
    bool _completed;

    public IReadOnlyList<WarcRecordInfo> Records
    {
        get
        {
            lock (_gate)
            {
                return _records.ToArray();
            }
        }
    }

    public long BytesWritten
    {
        get
        {
            lock (_gate)
            {
                return _out.Length;
            }
        }
    }

    /// <summary>Writes one record. <paramref name="block"/> must be seekable; it is read twice (digest pass, then write pass).</summary>
    public WarcRecordInfo WriteRecord(WarcRecordSpec spec, Stream block)
    {
        lock (_gate)
        {
            if (_completed)
            {
                throw new InvalidOperationException("The archive has already been finalized.");
            }

            block.Position = 0;
            long length = block.Length;
            RecordDigests digests = RecordDigester.Compute(block, length, spec.IsHttpResponse);
            block.Position = 0;

            Guid id = spec.Id ?? Guid.NewGuid();
            byte[] header = BuildHeader(spec, id, length, digests);

            long start = _out.Length;
            if (_compress)
            {
                using var gzip = new GZipStream(_out, CompressionLevel.Optimal, leaveOpen: true);
                gzip.Write(header);
                block.CopyTo(gzip, 81920);
                gzip.Write(RecordTerminator);
            }
            else
            {
                _out.Write(header);
                block.CopyTo(_out, 81920);
                _out.Write(RecordTerminator);
            }

            var info = new WarcRecordInfo
            {
                Id = id,
                Type = spec.Type,
                TargetUri = spec.TargetUri,
                DateUtc = spec.DateUtc.ToUniversalTime(),
                Offset = start,
                StoredLength = _out.Length - start,
                BlockLength = length,
                IpAddress = spec.IpAddress,
                HttpStatus = spec.HttpStatus,
                MediaType = spec.MediaType,
                Truncated = spec.Truncated,
                Block = digests.Block,
                Payload = digests.Payload,
            };
            _records.Add(info);
            return info;
        }
    }

    public WarcRecordInfo WriteRecord(WarcRecordSpec spec, ReadOnlySpan<byte> block)
    {
        using var ms = new MemoryStream(block.ToArray(), writable: false);
        return WriteRecord(spec, ms);
    }

    /// <summary>Flushes and closes the file, returning the SHA-256/SHA-512 of the exact bytes on disk.</summary>
    public DigestResult Complete()
    {
        lock (_gate)
        {
            if (_completed)
            {
                throw new InvalidOperationException("The archive has already been finalized.");
            }
            _completed = true;
            _out.Flush();
            DigestResult digests = _out.FinishDigests();
            _out.Dispose();
            return digests;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (!_completed)
            {
                _completed = true;
                _out.Dispose();
            }
        }
    }

    byte[] BuildHeader(WarcRecordSpec spec, Guid id, long length, RecordDigests digests)
    {
        var sb = new StringBuilder(512);
        void Field(string name, string value) => sb.Append(name).Append(": ").Append(Clean(value)).Append("\r\n");

        sb.Append(WarcFormat.Version).Append("\r\n");
        Field("WARC-Type", spec.Type);
        Field("WARC-Record-ID", WarcFormat.FormatId(id));
        Field("WARC-Date", WarcFormat.FormatDate(spec.DateUtc));
        if (spec.Filename is not null)
        {
            Field("WARC-Filename", spec.Filename);
        }
        if (spec.TargetUri is not null)
        {
            Field("WARC-Target-URI", spec.TargetUri);
        }
        if (spec.IpAddress is not null)
        {
            Field("WARC-IP-Address", spec.IpAddress);
        }
        if (spec.ConcurrentTo is { } concurrent)
        {
            Field("WARC-Concurrent-To", WarcFormat.FormatId(concurrent));
        }
        if (spec.Truncated is not null)
        {
            Field("WARC-Truncated", spec.Truncated);
        }
        if (digests.Payload is { } payload)
        {
            Field("WARC-Payload-Digest", payload.WarcValue(_primary));
        }
        Field("WARC-Block-Digest", digests.Block.WarcValue(_primary));
        foreach (var (name, value) in spec.ExtraHeaders)
        {
            if (IsToken(name))
            {
                Field(name, value);
            }
        }
        Field("Content-Type", spec.ContentType);
        Field("Content-Length", length.ToString(CultureInfo.InvariantCulture));
        sb.Append("\r\n");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>Header values must stay on one line; control characters would allow header injection.</summary>
    static string Clean(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            sb.Append(c < 0x20 || c == 0x7F ? ' ' : c);
        }
        return sb.ToString();
    }

    static bool IsToken(string name) =>
        name.Length > 0 && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_');

    /// <summary>Builds the body of an application/warc-fields block.</summary>
    public static byte[] BuildFields(IEnumerable<KeyValuePair<string, string>> fields)
    {
        var sb = new StringBuilder();
        foreach (var (name, value) in fields)
        {
            sb.Append(Clean(name)).Append(": ").Append(Clean(value)).Append("\r\n");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
