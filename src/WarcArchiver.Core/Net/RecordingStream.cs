using WarcArchiver.Core.Io;

namespace WarcArchiver.Core.Net;

/// <summary>
/// Wraps the decrypted connection stream and keeps a byte-exact copy of everything written (the request) and
/// read (the response). This is what makes the archive a faithful record of the HTTP conversation rather than a
/// reconstruction from parsed objects.
/// </summary>
internal sealed class RecordingStream(Stream inner, SpoolStream requestSink, SpoolStream responseSink, long responseLimit) : Stream
{
    const long MaxRequestBytes = 1024 * 1024;

    readonly Stream _inner = inner;
    readonly SpoolStream _requestSink = requestSink;
    readonly SpoolStream _responseSink = responseSink;
    readonly long _responseLimit = responseLimit;

    /// <summary>True once the response exceeded the configured per-resource limit and was cut.</summary>
    public bool LimitExceeded { get; private set; }

    public override bool CanRead => _inner.CanRead;

    public override bool CanSeek => false;

    public override bool CanWrite => _inner.CanWrite;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        int n = _inner.Read(buffer);
        if (n > 0)
        {
            RecordResponse(buffer[..n]);
        }
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int n = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        if (n > 0)
        {
            RecordResponse(buffer.Span[..n]);
        }
        return n;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        RecordRequest(buffer);
        _inner.Write(buffer);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        RecordRequest(buffer.Span);
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    void RecordRequest(ReadOnlySpan<byte> data)
    {
        long room = MaxRequestBytes - _requestSink.Length;
        if (room > 0)
        {
            _requestSink.Write(data[..(int)Math.Min(room, data.Length)]);
        }
    }

    void RecordResponse(ReadOnlySpan<byte> data)
    {
        long room = _responseLimit - _responseSink.Length;
        if (data.Length > room)
        {
            if (room > 0)
            {
                _responseSink.Write(data[..(int)room]);
            }
            LimitExceeded = true;
            throw new IOException("The response exceeded the configured per-resource size limit and was truncated.");
        }
        _responseSink.Write(data);
    }
}
