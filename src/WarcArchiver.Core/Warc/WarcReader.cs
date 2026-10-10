using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace WarcArchiver.Core.Warc;

public sealed class WarcFormatException : Exception
{
    public WarcFormatException(string message)
        : base(message)
    {
    }
}

public sealed class WarcHeader
{
    readonly Dictionary<string, string> _fields = new(StringComparer.OrdinalIgnoreCase);

    public string Version { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Fields => _fields;

    public long ContentLength { get; internal set; }

    public string Type => Get("WARC-Type") ?? string.Empty;

    public string? ContentType => Get("Content-Type");

    public string? TargetUri => Get("WARC-Target-URI");

    public string? Get(string name) => _fields.TryGetValue(name, out var v) ? v : null;

    public Guid? RecordId => WarcFormat.TryParseId(Get("WARC-Record-ID"), out var id) ? id : null;

    public bool IsHttpResponse =>
        Type.Equals(WarcTypes.Response, StringComparison.OrdinalIgnoreCase)
        && ContentType is { } ct
        && ct.StartsWith("application/http", StringComparison.OrdinalIgnoreCase)
        && ct.Contains("msgtype=response", StringComparison.OrdinalIgnoreCase);

    internal void Set(string name, string value) => _fields[name] = value;
}

/// <summary>Forward-only reader of WARC records over an already-decompressed stream.</summary>
public sealed class WarcReader(Stream decompressedStream) : IDisposable
{
    const int MaxHeaderLineLength = 16 * 1024;
    const int MaxHeaderLines = 256;

    readonly Stream _stream = new BufferedStream(decompressedStream, 64 * 1024);

    /// <summary>Opens a .warc or .warc.gz file, detecting gzip by its magic bytes rather than the extension.</summary>
    public static Stream OpenFile(string path)
    {
        var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
        try
        {
            int b1 = file.ReadByte();
            int b2 = file.ReadByte();
            file.Position = 0;
            if (b1 == 0x1F && b2 == 0x8B)
            {
                return new GZipStream(file, CompressionMode.Decompress, leaveOpen: false);
            }
            return file;
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    /// <summary>Reads the next record header; returns null at a clean end of file.</summary>
    public WarcHeader? ReadHeader()
    {
        string? line;
        int blank = 0;
        do
        {
            line = ReadLine();
            if (line is null)
            {
                return null;
            }
            if (line.Length == 0 && ++blank > 8)
            {
                throw new WarcFormatException("Unexpected run of blank lines between records.");
            }
        }
        while (line.Length == 0);

        if (!line.StartsWith("WARC/", StringComparison.Ordinal))
        {
            throw new WarcFormatException("Record does not start with a WARC version line (found: " + Truncate(line) + ").");
        }

        var header = new WarcHeader { Version = line };
        for (int i = 0; i < MaxHeaderLines; i++)
        {
            line = ReadLine() ?? throw new WarcFormatException("File ended inside a record header.");
            if (line.Length == 0)
            {
                if (!long.TryParse(header.Get("Content-Length"), NumberStyles.None, CultureInfo.InvariantCulture, out long length))
                {
                    throw new WarcFormatException("Record is missing a valid Content-Length header.");
                }
                header.ContentLength = length;
                return header;
            }
            int colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                throw new WarcFormatException("Malformed header line: " + Truncate(line));
            }
            header.Set(line[..colon].Trim(), line[(colon + 1)..].Trim());
        }
        throw new WarcFormatException("Record header has too many fields.");
    }

    public Stream OpenBlock(WarcHeader header) => new BoundedReadStream(_stream, header.ContentLength);

    /// <summary>Skips whatever is left of the block and checks the mandatory CRLF CRLF record terminator.</summary>
    public void FinishRecord(Stream block)
    {
        var bounded = (BoundedReadStream)block;
        bounded.Drain();
        if (bounded.EndedEarly)
        {
            throw new WarcFormatException("File ended before the record's declared Content-Length was reached.");
        }
        Span<byte> terminator = stackalloc byte[4];
        int total = 0;
        while (total < 4)
        {
            int n = _stream.Read(terminator[total..]);
            if (n <= 0)
            {
                break;
            }
            total += n;
        }
        if (total < 4 || !terminator.SequenceEqual("\r\n\r\n"u8))
        {
            throw new WarcFormatException("Record is not followed by the required CRLF CRLF terminator.");
        }
    }

    public void Dispose() => _stream.Dispose();

    string? ReadLine()
    {
        var sb = new StringBuilder();
        while (true)
        {
            int b = _stream.ReadByte();
            if (b < 0)
            {
                return sb.Length == 0 ? null : throw new WarcFormatException("File ended in the middle of a header line.");
            }
            if (b == '\n')
            {
                if (sb.Length > 0 && sb[^1] == '\r')
                {
                    sb.Length--;
                }
                return sb.ToString();
            }
            if (sb.Length >= MaxHeaderLineLength)
            {
                throw new WarcFormatException("Header line is too long.");
            }
            sb.Append((char)b);
        }
    }

    static string Truncate(string s) => s.Length <= 60 ? s : s[..60] + "…";

    sealed class BoundedReadStream(Stream inner, long length) : Stream
    {
        readonly Stream _inner = inner;
        long _remaining = length;

        public bool EndedEarly { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_remaining <= 0 || buffer.IsEmpty)
            {
                return 0;
            }
            int n = _inner.Read(buffer[..(int)Math.Min(buffer.Length, _remaining)]);
            if (n <= 0)
            {
                EndedEarly = true;
                _remaining = 0;
                return 0;
            }
            _remaining -= n;
            return n;
        }

        public void Drain()
        {
            byte[] scratch = new byte[16384];
            while (_remaining > 0 && Read(scratch, 0, scratch.Length) > 0)
            {
            }
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
