using System.Globalization;
using System.Text;

namespace WarcArchiver.Core.Http;

public delegate void SpanSink(ReadOnlySpan<byte> data);

/// <summary>
/// Push-style decoder for HTTP/1.1 "Transfer-Encoding: chunked" bodies. It is tolerant of truncated input
/// (a capture cut short by a size limit or a dropped connection) and never throws.
/// </summary>
public sealed class ChunkedDecoder(SpanSink sink)
{
    enum Phase
    {
        SizeLine,
        Data,
        DataTerminator,
        Trailers,
        Done,
        Failed,
    }

    const int MaxLineLength = 4096;

    readonly SpanSink _sink = sink;
    readonly StringBuilder _line = new();
    Phase _phase = Phase.SizeLine;
    long _remaining;

    /// <summary>True once the terminating zero-length chunk and trailers were seen.</summary>
    public bool IsComplete => _phase == Phase.Done;

    public bool IsMalformed => _phase == Phase.Failed;

    public void Feed(ReadOnlySpan<byte> data)
    {
        int i = 0;
        while (i < data.Length)
        {
            switch (_phase)
            {
                case Phase.SizeLine:
                case Phase.Trailers:
                    byte b = data[i++];
                    if (b == (byte)'\n')
                    {
                        HandleLine();
                        _line.Clear();
                    }
                    else if (b != (byte)'\r')
                    {
                        if (_line.Length >= MaxLineLength)
                        {
                            _phase = Phase.Failed;
                            return;
                        }
                        _line.Append((char)b);
                    }
                    break;

                case Phase.Data:
                    int n = (int)Math.Min(_remaining, data.Length - i);
                    _sink(data.Slice(i, n));
                    i += n;
                    _remaining -= n;
                    if (_remaining == 0)
                    {
                        _phase = Phase.DataTerminator;
                    }
                    break;

                case Phase.DataTerminator:
                    byte t = data[i++];
                    if (t == (byte)'\n')
                    {
                        _phase = Phase.SizeLine;
                    }
                    else if (t != (byte)'\r')
                    {
                        _phase = Phase.Failed;
                    }
                    break;

                default:
                    return;
            }
        }
    }

    void HandleLine()
    {
        string line = _line.ToString();
        if (_phase == Phase.Trailers)
        {
            if (line.Length == 0)
            {
                _phase = Phase.Done;
            }
            return;
        }

        int semicolon = line.IndexOf(';', StringComparison.Ordinal);
        string hex = (semicolon >= 0 ? line[..semicolon] : line).Trim();
        if (hex.Length == 0)
        {
            return;
        }
        if (!long.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out long size) || size < 0)
        {
            _phase = Phase.Failed;
            return;
        }
        if (size == 0)
        {
            _phase = Phase.Trailers;
            return;
        }
        _remaining = size;
        _phase = Phase.Data;
    }
}
