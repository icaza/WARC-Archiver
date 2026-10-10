using System.Globalization;
using System.Text;
using System.Text.Json;
using WarcArchiver.Core.Archive;
using WarcArchiver.Core.Audit;
using WarcArchiver.Core.Digests;
using WarcArchiver.Core.Http;
using WarcArchiver.Core.Signing;
using WarcArchiver.Core.Util;
using WarcArchiver.Core.Warc;

namespace WarcArchiver.Core.Verify;

public enum VerificationStatus
{
    Pass,
    Fail,
    Warning,
    Skipped,
    Info,
}

public sealed record VerificationCheck(string Name, VerificationStatus Status, string Detail);

public sealed class VerificationReport
{
    public required string TargetPath { get; init; }

    public DateTime VerifiedLocal { get; init; } = DateTime.Now;

    public List<VerificationCheck> Checks { get; } = [];

    public bool Passed => Checks.All(c => c.Status != VerificationStatus.Fail);

    public string Verdict => Passed
        ? (Checks.Any(c => c.Status == VerificationStatus.Warning) ? "PASSED (with warnings)" : "PASSED")
        : "FAILED";

    public void Add(string name, VerificationStatus status, string detail) => Checks.Add(new VerificationCheck(name, status, detail));

    public string ToText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("WARC Archiver — verification report");
        sb.AppendLine("Archive:  " + TargetPath);
        sb.AppendLine("Verified: " + VerifiedLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " (local time)");
        sb.AppendLine("Verdict:  " + Verdict);
        sb.AppendLine();
        foreach (VerificationCheck check in Checks)
        {
            sb.Append('[').Append(check.Status.ToString().ToUpperInvariant().PadRight(7)).Append("] ").AppendLine(check.Name);
            sb.Append("          ").AppendLine(check.Detail);
        }
        return sb.ToString();
    }
}

/// <summary>
/// Independently re-checks an archive: file fingerprints against the manifest and checksum files, every WARC record's
/// block and payload digest, the manifest signature, and the audit-log chain. It uses its own reader, so a passing
/// result does not depend on trusting the code that wrote the archive.
/// </summary>
public sealed class ArchiveVerifier
{
    public async Task<VerificationReport> VerifyAsync(string path, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        string warcPath = ResolveWarcPath(path, out string? resolveError);
        var report = new VerificationReport { TargetPath = warcPath };
        if (resolveError is not null)
        {
            report.Add("Locate archive", VerificationStatus.Fail, resolveError);
            return report;
        }
        if (!File.Exists(warcPath))
        {
            report.Add("Locate archive", VerificationStatus.Fail, "The file does not exist.");
            return report;
        }

        string directory = Path.GetDirectoryName(warcPath)!;
        string warcName = Path.GetFileName(warcPath);
        string stem = StripWarcExtension(warcName);
        string manifestPath = Path.Combine(directory, stem + ".manifest.json");

        // 1. Fingerprint the file as it is on disk right now.
        progress?.Report("Computing SHA-256 and SHA-512 of the archive file…");
        DigestResult fileDigests = await DigestSet.OfFileAsync(warcPath, cancellationToken).ConfigureAwait(false);
        report.Add("Archive file fingerprint", VerificationStatus.Info,
            $"{fileDigests.Length:N0} bytes\nSHA-256 {fileDigests.Sha256Hex}\nSHA-512 {fileDigests.Sha512Hex}");

        // 2. Manifest.
        Manifest? manifest = null;
        if (File.Exists(manifestPath))
        {
            manifest = ReadManifest(manifestPath, report);
            if (manifest is not null)
            {
                CompareFileDigests(report, manifest, fileDigests);
            }
        }
        else
        {
            report.Add("Manifest", VerificationStatus.Skipped, "No " + stem + ".manifest.json next to the archive; only the WARC itself can be checked.");
        }

        // 3. Checksum files.
        await CheckChecksumFilesAsync(report, directory, stem, warcName, fileDigests, manifestPath, cancellationToken).ConfigureAwait(false);

        // 4. Every record.
        progress?.Report("Checking every WARC record…");
        CheckRecords(report, warcPath, manifest, progress, cancellationToken);

        // 5. Signature.
        string signaturePath = ManifestSigner.SignaturePathFor(manifestPath);
        if (File.Exists(signaturePath) && File.Exists(manifestPath))
        {
            SignatureCheck check = ManifestSigner.Verify(manifestPath, signaturePath);
            report.Add("Digital signature", check.Valid ? VerificationStatus.Pass : VerificationStatus.Fail, check.Detail);
        }
        else
        {
            report.Add("Digital signature", VerificationStatus.Skipped, "This archive is not signed (hash-only evidence).");
        }

        // 6. Audit log.
        CheckAuditLog(report, directory, manifest, fileDigests);
        return report;
    }

    private static string ResolveWarcPath(string path, out string? error)
    {
        error = null;
        path = path.Trim().Trim('"');
        if (path.Length == 0)
        {
            error = "Choose an archive (.warc, .warc.gz) or its .manifest.json file.";
            return path;
        }
        if (path.EndsWith(".manifest.json", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
        {
            try
            {
                Manifest? m = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), ManifestJson.Options);
                if (m is not null && !string.IsNullOrEmpty(m.Warc.FileName))
                {
                    // Take only the file-name part: a manifest must never redirect the check to another folder.
                    return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(path))!, Path.GetFileName(m.Warc.FileName));
                }
            }
            catch (JsonException)
            {
                // Fall through to the error below.
            }
            error = "The manifest could not be read, or does not name its archive file.";
        }
        return Path.GetFullPath(path);
    }

    private static string StripWarcExtension(string fileName)
    {
        if (fileName.EndsWith(".warc.gz", StringComparison.OrdinalIgnoreCase))
        {
            return fileName[..^8];
        }
        return fileName.EndsWith(".warc", StringComparison.OrdinalIgnoreCase) ? fileName[..^5] : Path.GetFileNameWithoutExtension(fileName);
    }

    private static Manifest? ReadManifest(string manifestPath, VerificationReport report)
    {
        try
        {
            Manifest? manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), ManifestJson.Options);
            if (manifest is null)
            {
                report.Add("Manifest", VerificationStatus.Fail, "The manifest is empty.");
                return null;
            }
            report.Add("Manifest", VerificationStatus.Info,
                $"Target {manifest.Capture.TargetUrl}\nCaptured {manifest.Capture.StartedLocal} (local), {manifest.Capture.StartedUtc} (UTC)\n" +
                $"Case reference: {(string.IsNullOrEmpty(manifest.Case.Reference) ? "—" : manifest.Case.Reference)}; operator: {(string.IsNullOrEmpty(manifest.Case.Operator) ? "—" : manifest.Case.Operator)}");
            return manifest;
        }
        catch (JsonException ex)
        {
            report.Add("Manifest", VerificationStatus.Fail, "The manifest is not valid JSON: " + ex.Message);
            return null;
        }
    }

    private static void CompareFileDigests(VerificationReport report, Manifest manifest, DigestResult actual)
    {
        bool sha256 = string.Equals(manifest.Warc.Sha256, actual.Sha256Hex, StringComparison.OrdinalIgnoreCase);
        bool sha512 = string.Equals(manifest.Warc.Sha512, actual.Sha512Hex, StringComparison.OrdinalIgnoreCase);
        bool size = manifest.Warc.Bytes == actual.Length;
        if (sha256 && sha512 && size)
        {
            report.Add("Archive matches manifest", VerificationStatus.Pass, "File size, SHA-256 and SHA-512 are identical to the values recorded at capture time.");
        }
        else
        {
            var problems = new List<string>();
            if (!size)
            {
                problems.Add($"size is {actual.Length:N0} bytes but the manifest says {manifest.Warc.Bytes:N0}");
            }
            if (!sha256)
            {
                problems.Add("SHA-256 differs from the manifest");
            }
            if (!sha512)
            {
                problems.Add("SHA-512 differs from the manifest");
            }
            report.Add("Archive matches manifest", VerificationStatus.Fail,
                "The archive file is not the one that was captured: " + string.Join("; ", problems) + ".");
        }
    }

    private static async Task CheckChecksumFilesAsync(
        VerificationReport report, string directory, string stem, string warcName, DigestResult warcDigests, string manifestPath, CancellationToken ct)
    {
        foreach (DigestAlgorithm algorithm in new[] { DigestAlgorithm.Sha256, DigestAlgorithm.Sha512 })
        {
            string extension = algorithm == DigestAlgorithm.Sha256 ? ".sha256" : ".sha512";
            string sumPath = Path.Combine(directory, stem + extension);
            string label = "Checksum file " + extension;
            if (!File.Exists(sumPath))
            {
                report.Add(label, VerificationStatus.Skipped, "No " + stem + extension + " file found.");
                continue;
            }

            var problems = new List<string>();
            int checkedFiles = 0;
            foreach (string line in await File.ReadAllLinesAsync(sumPath, ct).ConfigureAwait(false))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                int space = trimmed.IndexOf(' ', StringComparison.Ordinal);
                if (space <= 0)
                {
                    problems.Add("unreadable line");
                    continue;
                }
                string expected = trimmed[..space].ToLowerInvariant();
                string name = Path.GetFileName(trimmed[(space + 1)..].TrimStart(' ', '*'));
                string? actual = null;
                if (string.Equals(name, warcName, StringComparison.OrdinalIgnoreCase))
                {
                    actual = warcDigests.Hex(algorithm);
                }
                else if (File.Exists(Path.Combine(directory, name)))
                {
                    DigestResult other = await DigestSet.OfFileAsync(Path.Combine(directory, name), ct).ConfigureAwait(false);
                    actual = other.Hex(algorithm);
                }

                checkedFiles++;
                if (actual is null)
                {
                    problems.Add(name + " is missing");
                }
                else if (!string.Equals(actual, expected, StringComparison.Ordinal))
                {
                    problems.Add(name + " does not match");
                }
            }

            report.Add(label,
                problems.Count == 0 && checkedFiles > 0 ? VerificationStatus.Pass : VerificationStatus.Fail,
                problems.Count == 0 ? $"{checkedFiles} file(s) match the {algorithm.WarcLabel().ToUpperInvariant()} values in the checksum file." : string.Join("; ", problems) + ".");
        }

        _ = manifestPath;
    }

    private static void CheckRecords(VerificationReport report, string warcPath, Manifest? manifest, IProgress<string>? progress, CancellationToken ct)
    {
        int records = 0;
        int digestChecked = 0;
        int unsupportedDigests = 0;
        var failures = new List<string>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, ManifestRecord>? expected = manifest?.Records.ToDictionary(r => r.Id, StringComparer.Ordinal);
        int manifestMismatches = 0;

        try
        {
            using Stream stream = WarcReader.OpenFile(warcPath);
            using var reader = new WarcReader(stream);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                WarcHeader? header = reader.ReadHeader();
                if (header is null)
                {
                    break;
                }

                records++;
                using Stream block = reader.OpenBlock(header);
                RecordDigests digests = RecordDigester.Compute(block, header.ContentLength, header.IsHttpResponse);
                reader.FinishRecord(block);

                string label = $"record {records} ({header.Type}{(header.TargetUri is null ? string.Empty : " " + Clip(header.TargetUri, 80))})";
                if (digests.ShortRead)
                {
                    failures.Add(label + ": block is shorter than its Content-Length");
                }

                CheckDigestHeader(header.Get("WARC-Block-Digest"), digests.Block, label + " block", failures, ref digestChecked, ref unsupportedDigests);
                if (header.Get("WARC-Payload-Digest") is { } payloadHeader)
                {
                    if (digests.Payload is null)
                    {
                        failures.Add(label + ": has a payload digest but no payload could be located");
                    }
                    else
                    {
                        CheckDigestHeader(payloadHeader, digests.Payload, label + " payload", failures, ref digestChecked, ref unsupportedDigests);
                    }
                }

                if (header.RecordId is { } id)
                {
                    string idText = WarcFormat.FormatId(id);
                    seenIds.Add(idText);
                    if (expected is not null)
                    {
                        if (!expected.TryGetValue(idText, out ManifestRecord? entry))
                        {
                            failures.Add(label + ": is not listed in the manifest (added after capture?)");
                        }
                        else if (!string.Equals(entry.BlockSha256, digests.Block.Sha256Hex, StringComparison.OrdinalIgnoreCase)
                              || !string.Equals(entry.BlockSha512, digests.Block.Sha512Hex, StringComparison.OrdinalIgnoreCase)
                              || !string.Equals(entry.PayloadSha256 ?? string.Empty, digests.Payload?.Sha256Hex ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                              || !string.Equals(entry.PayloadSha512 ?? string.Empty, digests.Payload?.Sha512Hex ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                        {
                            manifestMismatches++;
                            failures.Add(label + ": content differs from the digests recorded in the manifest");
                        }
                    }
                }
                else
                {
                    failures.Add(label + ": has no valid WARC-Record-ID");
                }

                if (records % 25 == 0)
                {
                    progress?.Report($"Checked {records} records…");
                }
            }
        }
        catch (WarcFormatException ex)
        {
            failures.Add("Structure error after record " + records.ToString(CultureInfo.InvariantCulture) + ": " + ex.Message);
        }
        catch (InvalidDataException ex)
        {
            failures.Add("The gzip data is damaged after record " + records.ToString(CultureInfo.InvariantCulture) + ": " + ex.Message);
        }
        catch (IOException ex)
        {
            failures.Add("Read error: " + ex.Message);
        }

        if (expected is not null)
        {
            int missing = expected.Keys.Count(id => !seenIds.Contains(id));
            if (missing > 0)
            {
                failures.Add($"{missing} record(s) listed in the manifest are missing from the archive");
            }
            if (manifest!.Warc.RecordCount != records)
            {
                failures.Add($"the manifest lists {manifest.Warc.RecordCount} records but the archive holds {records}");
            }
        }

        if (failures.Count == 0)
        {
            string extra = unsupportedDigests > 0 ? $" ({unsupportedDigests} digest header(s) use an algorithm this tool cannot recompute and were not checked)" : string.Empty;
            report.Add("WARC structure and record digests", unsupportedDigests > 0 ? VerificationStatus.Warning : VerificationStatus.Pass,
                $"{records} record(s) read; {digestChecked} digest header(s) recomputed and identical{extra}.");
            if (manifest is not null)
            {
                report.Add("Records match manifest", VerificationStatus.Pass, "Every record in the archive is listed in the manifest with identical SHA-256 and SHA-512 values.");
            }
        }
        else
        {
            string shown = string.Join("\n", failures.Take(8)) + (failures.Count > 8 ? $"\n… and {failures.Count - 8} more." : string.Empty);
            report.Add("WARC structure and record digests", VerificationStatus.Fail, shown);
            _ = manifestMismatches;
        }
    }

    private static void CheckDigestHeader(string? headerValue, DigestResult computed, string label, List<string> failures, ref int checkedCount, ref int unsupported)
    {
        if (headerValue is null)
        {
            return;
        }
        int colon = headerValue.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0 || !DigestAlgorithmExtensions.TryParse(headerValue[..colon], out DigestAlgorithm algorithm))
        {
            unsupported++;
            return;
        }

        string encoded = headerValue[(colon + 1)..].Trim();
        byte[] actual = computed.Get(algorithm);
        bool match = Base32.TryDecode(encoded, out byte[] decoded) && decoded.AsSpan().SequenceEqual(actual)
                     || string.Equals(encoded, Convert.ToHexStringLower(actual), StringComparison.OrdinalIgnoreCase);
        checkedCount++;
        if (!match)
        {
            failures.Add(label + ": " + algorithm.WarcLabel() + " digest does not match the content");
        }
    }

    private static void CheckAuditLog(VerificationReport report, string directory, Manifest? manifest, DigestResult fileDigests)
    {
        string logName = manifest?.Audit.LogFile is { Length: > 0 } name ? Path.GetFileName(name) : "audit-log.jsonl";
        string logPath = Path.Combine(directory, logName);
        if (!File.Exists(logPath))
        {
            report.Add("Audit log", VerificationStatus.Skipped, "No " + logName + " in this folder (it may be kept elsewhere or was not copied with the archive).");
            return;
        }

        AuditVerification chain = AuditLog.Verify(logPath);
        if (!chain.Ok)
        {
            report.Add("Audit log chain", VerificationStatus.Fail, chain.Error ?? "The chain is broken.");
            return;
        }
        report.Add("Audit log chain", VerificationStatus.Pass, $"{chain.Entries:N0} entries; every entry links to the one before and none was modified. Head: {chain.HeadHash}");

        List<AuditEntry> entries = AuditLog.ReadAll(logPath);
        if (manifest is not null && manifest.Audit.AnchorSeq > 0)
        {
            AuditEntry? anchor = entries.FirstOrDefault(e => e.Seq == manifest.Audit.AnchorSeq);
            report.Add("Manifest anchored in audit log",
                anchor is not null && string.Equals(anchor.Hash, manifest.Audit.AnchorHash, StringComparison.Ordinal) ? VerificationStatus.Pass : VerificationStatus.Fail,
                anchor is null
                    ? $"Audit entry {manifest.Audit.AnchorSeq} named by the manifest does not exist."
                    : string.Equals(anchor.Hash, manifest.Audit.AnchorHash, StringComparison.Ordinal)
                        ? $"The capture-start entry (#{anchor.Seq}, {anchor.Local}) matches the hash stored in the manifest."
                        : $"Audit entry {anchor.Seq} has a different hash from the one stored in the manifest.");
        }

        AuditEntry? complete = entries.LastOrDefault(e =>
            e.Event == "capture.complete"
            && e.Data.TryGetValue("warcSha256", out string? hash)
            && string.Equals(hash, fileDigests.Sha256Hex, StringComparison.OrdinalIgnoreCase));
        report.Add("Completion recorded in audit log",
            complete is not null ? VerificationStatus.Pass : VerificationStatus.Warning,
            complete is not null
                ? $"Audit entry #{complete.Seq} ({complete.Local}) recorded this exact archive hash when the capture finished."
                : "No audit entry records this archive's SHA-256 (the log may belong to another folder, or the archive was not produced here).");
    }

    private static string Clip(string text, int max) => text.Length <= max ? text : text[..max] + "…";
}
