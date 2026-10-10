using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace WarcArchiver.Core.Http;

public sealed class DecodedBody
{
    public required HttpHead Head { get; init; }

    public required byte[] Bytes { get; init; }

    /// <summary>True when the decoded body hit the output limit and was cut.</summary>
    public bool Truncated { get; init; }

    public string Text => TextDecoding.Decode(Bytes, Head.Charset);
}

/// <summary>
/// Turns a captured raw HTTP response into usable bytes (removes chunking and gzip/deflate/brotli) so the
/// page can be scanned for sub-resources. The raw bytes stored in the WARC are never modified.
/// </summary>
public static class HttpBodyDecoder
{
    public static DecodedBody? TryDecode(Stream rawMessage, long maxOutputBytes)
    {
        rawMessage.Position = 0;
        byte[] raw = new byte[rawMessage.Length];
        rawMessage.ReadExactly(raw);

        var head = HttpHead.TryParseFinal(raw);
        if (head is null || !head.IsResponse)
        {
            return null;
        }

        ReadOnlySpan<byte> payload = raw.AsSpan(head.HeadLength);
        byte[] body;
        if (head.IsChunked)
        {
            using var ms = new MemoryStream();
            var decoder = new ChunkedDecoder(span =>
            {
                if (ms.Length < maxOutputBytes * 2)
                {
                    ms.Write(span);
                }
            });
            decoder.Feed(payload);
            body = ms.ToArray();
        }
        else
        {
            body = payload.ToArray();
        }

        string? encoding = head.ContentEncoding;
        if (string.IsNullOrEmpty(encoding) || encoding == "identity")
        {
            bool cut = body.Length > maxOutputBytes;
            return new DecodedBody { Head = head, Bytes = cut ? body[..(int)maxOutputBytes] : body, Truncated = cut };
        }

        try
        {
            using var input = new MemoryStream(body, writable: false);
            using Stream decoded = encoding switch
            {
                "gzip" or "x-gzip" => new GZipStream(input, CompressionMode.Decompress),
                "br" => new BrotliStream(input, CompressionMode.Decompress),
                "deflate" => LooksLikeZlib(body)
                    ? new ZLibStream(input, CompressionMode.Decompress)
                    : new DeflateStream(input, CompressionMode.Decompress),
                _ => throw new InvalidDataException("Unsupported content-encoding: " + encoding),
            };

            using var output = new MemoryStream();
            byte[] buffer = new byte[81920];
            long total = 0;
            bool truncated = false;
            while (true)
            {
                int read = decoded.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }
                total += read;
                if (total > maxOutputBytes)
                {
                    int keep = (int)(read - (total - maxOutputBytes));
                    if (keep > 0)
                    {
                        output.Write(buffer, 0, keep);
                    }
                    truncated = true;
                    break;
                }
                output.Write(buffer, 0, read);
            }
            return new DecodedBody { Head = head, Bytes = output.ToArray(), Truncated = truncated };
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            return null;
        }
    }

    static bool LooksLikeZlib(byte[] data) =>
        data.Length >= 2 && (data[0] & 0x0F) == 8 && ((data[0] << 8) | data[1]) % 31 == 0;
}

public static partial class TextDecoding
{
    static TextDecoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static string Decode(byte[] bytes, string? headerCharset)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        string? name = headerCharset;
        if (string.IsNullOrWhiteSpace(name))
        {
            string probe = Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, 2048));
            Match m = MetaCharset().Match(probe);
            if (m.Success)
            {
                name = m.Groups[1].Value;
            }
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            try
            {
                return Encoding.GetEncoding(name).GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Unknown charset label: fall through to UTF-8.
            }
        }
        return Encoding.UTF8.GetString(bytes);
    }

    [GeneratedRegex(@"charset\s*=\s*[""']?([A-Za-z0-9_\-]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 500)]
    private static partial Regex MetaCharset();
}
