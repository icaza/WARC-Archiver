using System.Buffers;
using WarcArchiver.Core.Digests;

namespace WarcArchiver.Core.Http;

public sealed class RecordDigests
{
    public required DigestResult Block { get; init; }

    /// <summary>Digest of the HTTP entity body (transfer-encoding removed, content-encoding kept) — null when the record has no payload.</summary>
    public DigestResult? Payload { get; init; }

    public HttpHead? Head { get; init; }

    public bool ChunkedMalformed { get; init; }

    /// <summary>True when the stream ended before the declared block length was read.</summary>
    public bool ShortRead { get; init; }
}

/// <summary>
/// Computes the digests the WARC format needs for one record, in one streaming pass. The same code is used when
/// writing an archive and when verifying it, so the two can never disagree about what a digest covers.
/// </summary>
public static class RecordDigester
{
    const int HeadWindow = 65536 + 8;

    public static RecordDigests Compute(Stream block, long length, bool isHttpResponse)
    {
        using var blockSet = new DigestSet();
        DigestSet? payloadSet = null;
        ChunkedDecoder? decoder = null;
        HttpHead? head = null;
        bool shortRead = false;

        byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            long remaining = length;

            if (isHttpResponse && remaining > 0)
            {
                int want = (int)Math.Min(remaining, HeadWindow);
                byte[] first = ArrayPool<byte>.Shared.Rent(want);
                try
                {
                    int got = ReadFully(block, first, want);
                    if (got < want)
                    {
                        shortRead = true;
                    }
                    remaining -= got;
                    blockSet.Append(first.AsSpan(0, got));

                    head = HttpHead.TryParseFinal(first.AsSpan(0, got));
                    if (head is not null)
                    {
                        payloadSet = new DigestSet();
                        if (head.IsChunked)
                        {
                            var set = payloadSet;
                            decoder = new ChunkedDecoder(span => set.Append(span));
                        }
                        FeedPayload(first.AsSpan(head.HeadLength, got - head.HeadLength), payloadSet, decoder);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(first);
                }
            }

            while (remaining > 0 && !shortRead)
            {
                int read = block.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (read <= 0)
                {
                    shortRead = true;
                    break;
                }
                remaining -= read;
                blockSet.Append(buffer.AsSpan(0, read));
                if (payloadSet is not null)
                {
                    FeedPayload(buffer.AsSpan(0, read), payloadSet, decoder);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        DigestResult? payload = null;
        if (payloadSet is not null)
        {
            DigestResult result = payloadSet.Finish();
            payloadSet.Dispose();
            payload = result.Length > 0 ? result : null;
        }

        return new RecordDigests
        {
            Block = blockSet.Finish(),
            Payload = payload,
            Head = head,
            ChunkedMalformed = decoder?.IsMalformed ?? false,
            ShortRead = shortRead,
        };
    }

    static void FeedPayload(ReadOnlySpan<byte> data, DigestSet payloadSet, ChunkedDecoder? decoder)
    {
        if (data.IsEmpty)
        {
            return;
        }
        if (decoder is not null)
        {
            decoder.Feed(data);
        }
        else
        {
            payloadSet.Append(data);
        }
    }

    static int ReadFully(Stream stream, byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int n = stream.Read(buffer, total, count - total);
            if (n <= 0)
            {
                break;
            }
            total += n;
        }
        return total;
    }
}
