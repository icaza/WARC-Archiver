using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WarcArchiver.Core.Audit;

public sealed class AuditEntry
{
    public long Seq { get; set; }

    public string Utc { get; set; } = string.Empty;

    public string Local { get; set; } = string.Empty;

    public string Event { get; set; } = string.Empty;

    public Dictionary<string, string> Data { get; set; } = new(StringComparer.Ordinal);

    public string Prev { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;
}

public sealed record AuditVerification(bool Ok, long Entries, string? Error, long? FailedSeq, string HeadHash);

/// <summary>
/// Append-only, tamper-evident activity log (one JSON object per line). Every entry carries the SHA-256 of the previous
/// entry, so editing, deleting or re-ordering any line breaks the chain from that point on. Publishing or e-mailing
/// the current head hash to a third party anchors the whole history at that moment.
/// </summary>
public sealed class AuditLog
{
    public static readonly string GenesisHash = new('0', 64);

    public static readonly JsonSerializerOptions LineOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    };

    readonly object _gate = new();
    long _seq;
    string _head = GenesisHash;

    public AuditLog(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        string? dir = System.IO.Path.GetDirectoryName(Path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        LoadTail();
    }

    public string Path { get; }

    /// <summary>
    /// Set when a damaged log was found at start-up and set aside; holds the new name of the quarantined file so the
    /// application can tell the user. Null when the existing log (if any) was intact.
    /// </summary>
    public string? QuarantinedFile { get; private set; }

    public string HeadHash
    {
        get
        {
            lock (_gate)
            {
                return _head;
            }
        }
    }

    public long LastSeq
    {
        get
        {
            lock (_gate)
            {
                return _seq;
            }
        }
    }

    public AuditEntry Append(string eventName, IEnumerable<KeyValuePair<string, string>>? data = null)
    {
        lock (_gate)
        {
            return AppendUnlocked(eventName, data);
        }
    }

    public static string ComputeHash(AuditEntry entry)
    {
        var canonical = new StringBuilder();
        foreach (var pair in entry.Data.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            canonical.Append(pair.Key.Length).Append(':').Append(pair.Key).Append('=')
                .Append(pair.Value.Length).Append(':').Append(pair.Value).Append('\n');
        }

        string input = string.Join(
            '\n',
            entry.Seq.ToString(CultureInfo.InvariantCulture),
            entry.Utc,
            entry.Local,
            entry.Event,
            canonical.ToString(),
            entry.Prev);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>Re-computes the whole chain from the first line and reports the first inconsistency, if any.</summary>
    public static AuditVerification Verify(string path)
    {
        string prev = GenesisHash;
        long expected = 1;
        long count = 0;

        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            AuditEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<AuditEntry>(line, LineOptions);
            }
            catch (JsonException)
            {
                entry = null;
            }
            if (entry is null)
            {
                return new AuditVerification(false, count, $"Line {count + 1} is not a valid audit entry.", expected, prev);
            }
            if (entry.Seq != expected)
            {
                return new AuditVerification(false, count, $"Entry numbered {entry.Seq} found where {expected} was expected (an entry was removed, inserted or reordered).", expected, prev);
            }
            if (!string.Equals(entry.Prev, prev, StringComparison.Ordinal))
            {
                return new AuditVerification(false, count, $"Entry {entry.Seq} does not link to the entry before it.", entry.Seq, prev);
            }
            if (!string.Equals(ComputeHash(entry), entry.Hash, StringComparison.Ordinal))
            {
                return new AuditVerification(false, count, $"Entry {entry.Seq} ({entry.Event}) was modified after it was written.", entry.Seq, prev);
            }

            prev = entry.Hash;
            expected++;
            count++;
        }
        return new AuditVerification(true, count, null, null, prev);
    }

    /// <summary>Reads every well-formed entry (malformed lines are skipped; use <see cref="Verify"/> for integrity).</summary>
    public static List<AuditEntry> ReadAll(string path)
    {
        var entries = new List<AuditEntry>();
        if (!File.Exists(path))
        {
            return entries;
        }
        foreach (string line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            try
            {
                if (JsonSerializer.Deserialize<AuditEntry>(line, LineOptions) is { } entry)
                {
                    entries.Add(entry);
                }
            }
            catch (JsonException)
            {
                // Skipped here; Verify() reports it.
            }
        }
        return entries;
    }

    AuditEntry AppendUnlocked(string eventName, IEnumerable<KeyValuePair<string, string>>? data)
    {
        DateTimeOffset now = DateTimeOffset.Now;
        var entry = new AuditEntry
        {
            Seq = _seq + 1,
            Utc = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
            Local = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
            Event = eventName,
            Prev = _head,
        };
        if (data is not null)
        {
            foreach (var pair in data)
            {
                entry.Data[pair.Key] = pair.Value ?? string.Empty;
            }
        }
        entry.Hash = ComputeHash(entry);

        byte[] line = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry, LineOptions) + "\n");
        using (var fs = new FileStream(Path, FileMode.Append, FileAccess.Write, FileShare.Read))
        {
            fs.Write(line);
            fs.Flush(flushToDisk: true);
        }
        _seq = entry.Seq;
        _head = entry.Hash;
        return entry;
    }

    void LoadTail()
    {
        if (!File.Exists(Path) || new FileInfo(Path).Length == 0)
        {
            return;
        }

        try
        {
            string? lastLine = ReadLastLine(Path);
            AuditEntry? last = lastLine is null ? null : JsonSerializer.Deserialize<AuditEntry>(lastLine, LineOptions);
            if (last is null || last.Seq <= 0 || last.Hash.Length != 64)
            {
                throw new InvalidDataException("The last audit entry is unreadable.");
            }
            _seq = last.Seq;
            _head = last.Hash;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            Quarantine(ex.Message);
        }
    }

    /// <summary>
    /// A damaged log is never overwritten: it is set aside under a new name (with its SHA-256 recorded in the fresh log)
    /// so the evidence of the damage survives and the application can keep working.
    /// </summary>
    void Quarantine(string reason)
    {
        string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string quarantined = Path + ".corrupt-" + stamp;
        File.Move(Path, quarantined);
        QuarantinedFile = quarantined;
        string hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(quarantined)));
        _seq = 0;
        _head = GenesisHash;
        AppendUnlocked("audit.restart",
        [
            new("reason", reason),
            new("quarantinedFile", System.IO.Path.GetFileName(quarantined)),
            new("quarantinedSha256", hash),
        ]);
    }

    static string? ReadLastLine(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        int take = (int)Math.Min(fs.Length, 64 * 1024);
        fs.Seek(-take, SeekOrigin.End);
        byte[] buffer = new byte[take];
        fs.ReadExactly(buffer);
        string text = Encoding.UTF8.GetString(buffer).TrimEnd('\r', '\n');
        int lastBreak = text.LastIndexOf('\n');
        string line = lastBreak >= 0 ? text[(lastBreak + 1)..] : text;
        return line.Length == 0 ? null : line;
    }
}
