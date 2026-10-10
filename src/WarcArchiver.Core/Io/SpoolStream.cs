namespace WarcArchiver.Core.Io;

/// <summary>
/// A seekable read/write buffer that stays in memory while small and transparently spills to a
/// delete-on-close temporary file once it grows past a threshold. Keeps the memory footprint of
/// concurrent downloads low without limiting the size of a captured resource.
/// </summary>
public sealed class SpoolStream(int memoryThreshold = 256 * 1024) : Stream
{
    readonly int _memoryThreshold = Math.Max(4096, memoryThreshold);
    Stream _inner = new MemoryStream();
    bool _spilled;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => _inner.Length;

    public override long Position
    {
        get => _inner.Position;
        set => _inner.Position = value;
    }

    public bool IsSpilledToDisk => _spilled;

    public override void Flush() => _inner.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

    public override int Read(Span<byte> buffer) => _inner.Read(buffer);

    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

    public override void SetLength(long value) => _inner.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        if (!_spilled && _inner.Position + buffer.Length > _memoryThreshold)
        {
            Spill();
        }
        _inner.Write(buffer);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (!_spilled && _inner.Position + buffer.Length > _memoryThreshold)
        {
            Spill();
        }
        return _inner.WriteAsync(buffer, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _inner.ReadAsync(buffer, cancellationToken);

    void Spill()
    {
        string path = Path.Combine(Path.GetTempPath(), "warcarchiver-" + Guid.NewGuid().ToString("N") + ".tmp");
        var file = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            81920,
            FileOptions.DeleteOnClose);
        var memory = (MemoryStream)_inner;
        long position = memory.Position;
        memory.Position = 0;
        memory.CopyTo(file);
        file.Position = position;
        _inner = file;
        _spilled = true;
        memory.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}
