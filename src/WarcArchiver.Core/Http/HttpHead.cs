using System.Globalization;
using System.Text;

namespace WarcArchiver.Core.Http;

/// <summary>Parsed start-line and header block of a raw HTTP/1.x message, as it travelled on the wire.</summary>
public sealed class HttpHead
{
    readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);

    public bool IsResponse { get; private set; }

    public string StartLine { get; private set; } = string.Empty;

    public int StatusCode { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public string Method { get; private set; } = string.Empty;

    public string Target { get; private set; } = string.Empty;

    /// <summary>Number of bytes from the start of the message up to and including the blank line that ends the headers.</summary>
    public int HeadLength { get; internal set; }

    public IReadOnlyDictionary<string, string> Headers => _headers;

    public string? Get(string name) => _headers.TryGetValue(name, out var v) ? v : null;

    public bool IsChunked =>
        Get("Transfer-Encoding") is { } te && te.Contains("chunked", StringComparison.OrdinalIgnoreCase);

    public long? ContentLength =>
        long.TryParse(Get("Content-Length"), NumberStyles.None, CultureInfo.InvariantCulture, out long n) ? n : null;

    public string? ContentEncoding => Get("Content-Encoding")?.Trim().ToLowerInvariant();

    public string? Location => Get("Location")?.Trim();

    public string? MediaType
    {
        get
        {
            string? ct = Get("Content-Type");
            if (string.IsNullOrWhiteSpace(ct))
            {
                return null;
            }
            int semi = ct.IndexOf(';', StringComparison.Ordinal);
            return (semi >= 0 ? ct[..semi] : ct).Trim().ToLowerInvariant();
        }
    }

    public string? Charset
    {
        get
        {
            string? ct = Get("Content-Type");
            if (ct is null)
            {
                return null;
            }
            int idx = ct.IndexOf("charset=", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
            {
                return null;
            }
            string rest = ct[(idx + 8)..].Trim();
            int end = rest.IndexOfAny([';', ' ']);
            string value = end >= 0 ? rest[..end] : rest;
            return value.Trim('"', '\'');
        }
    }

    /// <summary>Parses the first message head found at the start of <paramref name="data"/>, or null if it is incomplete or malformed.</summary>
    public static HttpHead? TryParse(ReadOnlySpan<byte> data, int maxHeadBytes = 65536)
    {
        int limit = Math.Min(data.Length, maxHeadBytes);
        int end = -1;
        for (int i = 0; i < limit; i++)
        {
            if (data[i] != (byte)'\n')
            {
                continue;
            }
            if (i + 1 < limit && data[i + 1] == (byte)'\n')
            {
                end = i + 2;
                break;
            }
            if (i + 2 < limit && data[i + 1] == (byte)'\r' && data[i + 2] == (byte)'\n')
            {
                end = i + 3;
                break;
            }
        }
        if (end < 0)
        {
            return null;
        }

        string text = Encoding.Latin1.GetString(data[..end]);
        string[] lines = text.Split('\n');
        if (lines.Length == 0)
        {
            return null;
        }

        var head = new HttpHead { HeadLength = end, StartLine = lines[0].TrimEnd('\r') };
        if (!head.ParseStartLine())
        {
            return null;
        }

        string? lastName = null;
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            if (line.Length == 0)
            {
                break;
            }
            if ((line[0] == ' ' || line[0] == '\t') && lastName is not null)
            {
                head._headers[lastName] = head._headers[lastName] + " " + line.Trim();
                continue;
            }
            int colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon <= 0)
            {
                continue;
            }
            string name = line[..colon].Trim();
            string value = line[(colon + 1)..].Trim();
            if (name.Length == 0)
            {
                continue;
            }
            head._headers[name] = head._headers.TryGetValue(name, out var existing) ? existing + ", " + value : value;
            lastName = name;
        }
        return head;
    }

    /// <summary>Like <see cref="TryParse"/> but skips interim 1xx responses (e.g. 103 Early Hints) to return the final response head.</summary>
    public static HttpHead? TryParseFinal(ReadOnlySpan<byte> data, int maxHeadBytes = 65536)
    {
        int offset = 0;
        for (int guard = 0; guard < 8; guard++)
        {
            var head = TryParse(data[offset..], maxHeadBytes);
            if (head is null)
            {
                return null;
            }
            head.HeadLength += offset;
            if (head.IsResponse && head.StatusCode is >= 100 and < 200 && head.StatusCode != 101)
            {
                offset = head.HeadLength;
                continue;
            }
            return head;
        }
        return null;
    }

    bool ParseStartLine()
    {
        string[] parts = StartLine.Split(' ', 3, StringSplitOptions.None);
        if (parts.Length < 2)
        {
            return false;
        }
        if (parts[0].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
        {
            IsResponse = true;
            if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int code))
            {
                return false;
            }
            StatusCode = code;
            Reason = parts.Length > 2 ? parts[2] : string.Empty;
            return true;
        }
        if (parts.Length == 3 && parts[2].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase))
        {
            Method = parts[0];
            Target = parts[1];
            return true;
        }
        return false;
    }
}
