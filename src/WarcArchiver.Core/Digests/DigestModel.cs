using System.Security.Cryptography;
using WarcArchiver.Core.Util;

namespace WarcArchiver.Core.Digests;

public enum DigestAlgorithm
{
    Sha256,
    Sha512,
}

public static class DigestAlgorithmExtensions
{
    /// <summary>Label used in WARC digest headers ("sha256:..."), per the WARC specification's algorithm-label convention.</summary>
    public static string WarcLabel(this DigestAlgorithm algorithm) =>
        algorithm == DigestAlgorithm.Sha512 ? "sha512" : "sha256";

    public static HashAlgorithmName HashName(this DigestAlgorithm algorithm) =>
        algorithm == DigestAlgorithm.Sha512 ? HashAlgorithmName.SHA512 : HashAlgorithmName.SHA256;

    public static bool TryParse(string? text, out DigestAlgorithm algorithm)
    {
        switch (text?.Trim().Replace("-", "", StringComparison.Ordinal).ToLowerInvariant())
        {
            case "sha256":
                algorithm = DigestAlgorithm.Sha256;
                return true;
            case "sha512":
                algorithm = DigestAlgorithm.Sha512;
                return true;
            default:
                algorithm = DigestAlgorithm.Sha256;
                return false;
        }
    }
}

/// <summary>The SHA-256 and SHA-512 digests of one stream of bytes.</summary>
public sealed class DigestResult(long length, byte[] sha256, byte[] sha512)
{
    public long Length { get; } = length;

    public byte[] Sha256 { get; } = sha256;

    public byte[] Sha512 { get; } = sha512;

    public string Sha256Hex => Convert.ToHexStringLower(Sha256);

    public string Sha512Hex => Convert.ToHexStringLower(Sha512);

    public byte[] Get(DigestAlgorithm algorithm) => algorithm == DigestAlgorithm.Sha512 ? Sha512 : Sha256;

    public string Hex(DigestAlgorithm algorithm) => Convert.ToHexStringLower(Get(algorithm));

    /// <summary>Formats the digest the way WARC headers carry it: "sha256:BASE32".</summary>
    public string WarcValue(DigestAlgorithm algorithm) =>
        algorithm.WarcLabel() + ":" + Base32.Encode(Get(algorithm));
}

/// <summary>Incrementally computes SHA-256 and SHA-512 in a single pass over the data.</summary>
public sealed class DigestSet : IDisposable
{
    private readonly IncrementalHash _sha256 = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly IncrementalHash _sha512 = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);

    public long Length { get; private set; }

    public void Append(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return;
        }
        _sha256.AppendData(data);
        _sha512.AppendData(data);
        Length += data.Length;
    }

    public DigestResult Finish()
    {
        var result = new DigestResult(Length, _sha256.GetHashAndReset(), _sha512.GetHashAndReset());
        Length = 0;
        return result;
    }

    public void Dispose()
    {
        _sha256.Dispose();
        _sha512.Dispose();
    }

    /// <summary>Hashes a file on disk (streaming, constant memory).</summary>
    public static async Task<DigestResult> OfFileAsync(string path, CancellationToken cancellationToken = default)
    {
        using var set = new DigestSet();
        await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int read;
            while ((read = await fs.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false)) > 0)
            {
                set.Append(buffer.AsSpan(0, read));
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
        return set.Finish();
    }

    public static DigestResult OfBytes(ReadOnlySpan<byte> data)
    {
        using var set = new DigestSet();
        set.Append(data);
        return set.Finish();
    }
}
