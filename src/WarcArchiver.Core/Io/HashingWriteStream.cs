using WarcArchiver.Core.Digests;

namespace WarcArchiver.Core.Io;

/// <summary>
/// Write-only pass-through stream that computes SHA-256 and SHA-512 over every byte written, so the
/// final archive file can be fingerprinted without re-reading it from disk.
/// </summary>
public sealed class HashingWriteStream(Stream inner) : Stream
{
    readonly Stream _inner = inner;
    readonly DigestSet _digests = new();
    long _position;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => _position;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public DigestResult FinishDigests() => _digests.Finish();

    public override void Flush() => _inner.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        _inner.Write(buffer);
        _digests.Append(buffer);
        _position += buffer.Length;
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        _digests.Append(buffer.Span);
        _position += buffer.Length;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _digests.Dispose();
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}
