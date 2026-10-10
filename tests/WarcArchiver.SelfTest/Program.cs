using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using WarcArchiver.Core.Archive;
using WarcArchiver.Core.Audit;
using WarcArchiver.Core.Config;
using WarcArchiver.Core.Digests;
using WarcArchiver.Core.Http;
using WarcArchiver.Core.Net;
using WarcArchiver.Core.Parsing;
using WarcArchiver.Core.Util;
using WarcArchiver.Core.Verify;
using WarcArchiver.Core.Warc;

namespace WarcArchiver.SelfTest;

/// <summary>
/// End-to-end self-test of the core library: runs a real local web site (HTTP + HTTPS), captures it, verifies the
/// archives, and proves that tampering is detected. Exit code 0 means every check passed.
/// Usage: WarcArchiver.SelfTest [output-folder]
/// </summary>
internal static class Program
{
    static int _passed;
    static int _failed;

    sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    public static async Task<int> Main(string[] args)
    {
        string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Path.GetTempPath(), "warc-selftest-" + Guid.NewGuid().ToString("N")[..8]);
        if (Directory.Exists(root))
        {
            foreach (string f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(f, FileAttributes.Normal);
            }
            Directory.Delete(root, recursive: true);
        }
        Directory.CreateDirectory(root);
        Console.WriteLine("Self-test output folder: " + root);

        UnitTests();
        AuditTests(root);
        ConfigTests(root);

        await using var site = new TestSite();
        await site.StartAsync();
        Console.WriteLine($"Test site: {site.HttpBase}  {site.HttpsBase}");

        await CaptureAndVerifyTests(root, site);
        await SecurityTests(root, site);
        await SigningTests(root, site);
        await LimitTests(root, site);
        await CancellationAndFailureTests(root, site);

        Console.WriteLine();
        Console.WriteLine($"RESULT: {_passed} passed, {_failed} failed");
        return _failed == 0 ? 0 : 1;
    }

    // ------------------------------------------------------------------ unit tests

    static void UnitTests()
    {
        Section("Unit tests");

        Check(Base32.Encode("foobar"u8, true) == "MZXW6YTBOI======", "Base32 matches the RFC 4648 test vector");
        Check(Base32.TryDecode("MZXW6YTBOI======", out byte[] decoded) && Encoding.ASCII.GetString(decoded) == "foobar", "Base32 decodes it back");

        Check(DigestSet.OfBytes("abc"u8).Sha256Hex == "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", "SHA-256 of 'abc' is correct");
        Check(DigestSet.OfBytes("abc"u8).Sha512Hex.StartsWith("ddaf35a193617aba", StringComparison.Ordinal), "SHA-512 of 'abc' is correct");

        // Chunked decoding, fed one byte at a time to prove it is boundary-independent.
        var output = new MemoryStream();
        var chunked = new ChunkedDecoder(span => output.Write(span));
        foreach (byte b in "4\r\nWiki\r\n5;ext=1\r\npedia\r\n0\r\nX-Trailer: 1\r\n\r\n"u8.ToArray())
        {
            chunked.Feed([b]);
        }
        Check(Encoding.ASCII.GetString(output.ToArray()) == "Wikipedia" && chunked.IsComplete, "Chunked decoder reassembles a body split at every byte");

        var bad = new ChunkedDecoder(_ => { });
        bad.Feed("zz\r\nxx"u8);
        Check(bad.IsMalformed, "Chunked decoder flags malformed input without throwing");

        byte[] interim = "HTTP/1.1 103 Early Hints\r\nLink: </a.css>\r\n\r\nHTTP/1.1 200 OK\r\nContent-Type: Text/HTML; charset=ISO-8859-1\r\nTransfer-Encoding: chunked\r\n\r\nbody"u8.ToArray();
        HttpHead? head = HttpHead.TryParseFinal(interim);
        Check(head is { StatusCode: 200, IsChunked: true, MediaType: "text/html", Charset: "ISO-8859-1" } && Encoding.ASCII.GetString(interim, head.HeadLength, 4) == "body",
            "HTTP head parser skips 1xx interim responses and finds the body start");
        Check(HttpHead.TryParse("GET / HTTP/1.1\r\nHost: x\r\n\r\n"u8) is { IsResponse: false, Method: "GET" }, "HTTP head parser reads request heads");
        Check(HttpHead.TryParse("HTTP/1.1 200 OK\r\nA: b"u8) is null, "Incomplete head is rejected");

        foreach (string ok in new[] { "8.8.8.8", "1.1.1.1", "172.32.0.1", "2606:4700:4700::1111" })
        {
            Check(NetworkPolicy.IsPubliclyRoutable(IPAddress.Parse(ok)), "Network policy allows public address " + ok);
        }
        foreach (string blocked in new[] { "127.0.0.1", "10.1.2.3", "192.168.1.5", "172.16.0.1", "169.254.169.254", "100.64.0.1", "0.0.0.0", "255.255.255.255", "::1", "fe80::1", "fc00::1", "::ffff:10.0.0.1", "2001:db8::1", "64:ff9b::7f00:1" })
        {
            Check(!NetworkPolicy.IsPubliclyRoutable(IPAddress.Parse(blocked)), "Network policy blocks " + blocked);
        }

        Check(ArchiveEngine.TryNormalizeUrl("example.com/a#frag", out Uri? u1, out _) && u1!.AbsoluteUri == "https://example.com/a", "URL without scheme gets https:// and loses its fragment");
        Check(!ArchiveEngine.TryNormalizeUrl("ftp://example.com", out _, out _), "ftp:// is rejected");
        Check(!ArchiveEngine.TryNormalizeUrl("https://user:secret@example.com/", out _, out _), "URL with credentials is rejected");
        Check(!ArchiveEngine.TryNormalizeUrl("javascript:alert(1)", out _, out _), "javascript: is rejected");
        Check(!ArchiveEngine.TryNormalizeUrl("   ", out _, out _), "Empty URL is rejected");

        // HTML extraction.
        const string html = """
            <html><head><base href="/sub/"><!-- <img src="comment.png"> -->
            <link rel="stylesheet" href="a.css"><link rel="shortcut icon" href="/fav.ico">
            <link rel="preload" as="font" href="f.woff2"><link rel="canonical" href="/not-this">
            <script src="s.js"></script><script>var x = "<img src='in-script.png'>";</script>
            <style>@import url("imp.css"); .a{background:URL( 'bg.png' )}</style></head>
            <body background="body.png"><img src=unquoted.png srcset="s1.png 1x, s2.png?x=1&amp;y=2 2x">
            <video poster="p.png"><source src="v.mp4" type="video/mp4"></video>
            <img src="data:image/gif;base64,R0lGOD"><img src="javascript:alert(1)"><img src="//cdn.example.org/c.png">
            <img src="http://user:pw@evil.example/x.png"><a href="page.html">link</a></body></html>
            """;
        var refs = HtmlResourceExtractor.Extract(html, new Uri("https://example.com/dir/page"));
        string[] urls = refs.Select(r => r.Url.AbsoluteUri).ToArray();
        foreach (string expected in new[]
        {
            "https://example.com/sub/a.css", "https://example.com/fav.ico", "https://example.com/sub/f.woff2", "https://example.com/sub/s.js",
            "https://example.com/sub/imp.css", "https://example.com/sub/bg.png", "https://example.com/sub/body.png", "https://example.com/sub/unquoted.png",
            "https://example.com/sub/s1.png", "https://example.com/sub/s2.png?x=1&y=2", "https://example.com/sub/p.png", "https://example.com/sub/v.mp4",
            "https://cdn.example.org/c.png",
        })
        {
            Check(urls.Contains(expected), "HTML extractor finds " + expected);
        }
        foreach (string forbidden in new[] { "comment.png", "in-script.png", "not-this", "page.html", "data:", "javascript:", "evil.example" })
        {
            Check(!urls.Any(x => x.Contains(forbidden, StringComparison.Ordinal)), "HTML extractor ignores " + forbidden);
        }
        Check(refs.First(r => r.Url.AbsoluteUri.EndsWith("f.woff2", StringComparison.Ordinal)).Kind == ResourceKind.Font, "preload as=font is classified as a font");

        // WARC writer/reader round trip with both layouts.
        foreach (bool compress in new[] { false, true })
        {
            string path = Path.Combine(Path.GetTempPath(), "warc-rt-" + Guid.NewGuid().ToString("N") + (compress ? ".warc.gz" : ".warc"));
            try
            {
                using (var fs = new FileStream(path, FileMode.CreateNew))
                using (var writer = new WarcWriter(fs, compress, DigestAlgorithm.Sha256))
                {
                    byte[] http = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: 5\r\n\r\nhello"u8.ToArray();
                    writer.WriteRecord(new WarcRecordSpec { Type = WarcTypes.Response, ContentType = WarcFormat.HttpResponseContentType, IsHttpResponse = true, TargetUri = "http://x/" }, http);
                    writer.WriteRecord(new WarcRecordSpec { Type = WarcTypes.Metadata, ContentType = WarcFormat.FieldsContentType }, WarcWriter.BuildFields([new("a", "b\r\nInjected: yes")]));
                    writer.Complete();
                }
                using Stream s = WarcReader.OpenFile(path);
                using var reader = new WarcReader(s);
                int count = 0;
                bool payloadOk = false;
                bool injectionBlocked = false;
                while (reader.ReadHeader() is { } h)
                {
                    count++;
                    using Stream block = reader.OpenBlock(h);
                    var d = RecordDigester.Compute(block, h.ContentLength, h.IsHttpResponse);
                    reader.FinishRecord(block);
                    if (h.IsHttpResponse)
                    {
                        payloadOk = d.Payload?.Sha256Hex == DigestSet.OfBytes("hello"u8).Sha256Hex
                                    && h.Get("WARC-Payload-Digest") == "sha256:" + Base32.Encode(DigestSet.OfBytes("hello"u8).Sha256);
                    }
                    else
                    {
                        injectionBlocked = h.ContentLength == "a: b  Injected: yes\r\n"u8.Length;
                    }
                }
                Check(count == 2 && payloadOk, $"WARC round trip ({(compress ? "gz" : "plain")}): 2 records, payload digest correct");
                Check(injectionBlocked, $"WARC fields cannot be used to inject extra lines ({(compress ? "gz" : "plain")})");
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    // ------------------------------------------------------------------ audit log + config

    static void AuditTests(string root)
    {
        Section("Audit log");
        string dir = Path.Combine(root, "audit");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "audit-log.jsonl");

        var log = new AuditLog(path);
        log.Append("one", [new("k", "v")]);
        log.Append("two", [new("unicode", "é€ 日本語 \"quotes\" \n newline")]);
        log.Append("three");
        Check(AuditLog.Verify(path) is { Ok: true, Entries: 3 }, "Audit chain of 3 entries verifies");

        var reopened = new AuditLog(path);
        reopened.Append("four");
        Check(AuditLog.Verify(path) is { Ok: true, Entries: 4 } && reopened.LastSeq == 4, "Chain continues correctly after the log is reopened");

        string[] lines = File.ReadAllLines(path);
        File.WriteAllLines(path, [lines[0], lines[1].Replace("two", "TWO"), lines[2], lines[3]]);
        Check(AuditLog.Verify(path) is { Ok: false, FailedSeq: 2 }, "Editing an entry is detected at that entry");

        File.WriteAllLines(path, [lines[0], lines[2], lines[3]]);
        Check(AuditLog.Verify(path) is { Ok: false }, "Deleting an entry is detected");

        File.WriteAllLines(path, [lines[0], lines[2], lines[1], lines[3]]);
        Check(AuditLog.Verify(path) is { Ok: false }, "Re-ordering entries is detected");

        File.WriteAllText(path, string.Join('\n', lines.Take(3)) + "\n{\"seq\":4,\"truncated");
        var recovered = new AuditLog(path);
        string[] quarantined = Directory.GetFiles(dir, "audit-log.jsonl.corrupt-*");
        Check(quarantined.Length == 1 && AuditLog.Verify(path) is { Ok: true, Entries: 1 } && recovered.LastSeq == 1,
            "A damaged log is set aside (not overwritten) and a fresh verifiable chain starts");
        Check(recovered.QuarantinedFile is not null && Path.GetFileName(recovered.QuarantinedFile) == Path.GetFileName(quarantined[0]), "The application is told which file was set aside");
        Check(AuditLog.ReadAll(path)[0].Event == "audit.restart" && AuditLog.ReadAll(path)[0].Data.ContainsKey("quarantinedSha256"), "The restart entry records the hash of the damaged file");
    }

    static void ConfigTests(string root)
    {
        Section("Configuration");
        string path = Path.Combine(root, "cfg", "config.json");

        ConfigLoadResult created = ConfigStore.Load(path);
        Check(File.Exists(path) && created.Warning is not null && created.Config.Network.MaxParallelDownloads == 4, "Missing config.json is created with defaults");

        File.WriteAllText(path, """
            {
              // comments and trailing commas are allowed
              "outputDirectory": "%TEMP%/x",
              "warcPrimaryDigest": "SHA-512",
              "network": { "maxParallelDownloads": 9999, "connectTimeoutSeconds": -5, "userAgent": "A\r\nInjected: 1", },
              "limits": { "maxResources": 0 },
            }
            """);
        ConfigLoadResult edited = ConfigStore.Load(path);
        Check(edited.Warning is null && edited.Config.PrimaryDigest == DigestAlgorithm.Sha512, "User-edited config.json with comments is read");
        Check(edited.Config.Network.MaxParallelDownloads == 16 && edited.Config.Network.ConnectTimeoutSeconds == 3 && edited.Config.Limits.MaxResources == 1, "Out-of-range values are clamped");
        Check(!edited.Config.Network.UserAgent.Contains('\r') && !edited.Config.Network.UserAgent.Contains('\n'), "Control characters are stripped from header values");

        string broken = "{ \"network\": { \"maxRedirects\": ";
        File.WriteAllText(path, broken);
        ConfigLoadResult invalid = ConfigStore.Load(path);
        Check(invalid.Warning is not null && File.ReadAllText(path) == broken && invalid.Config.Network.MaxRedirects == 10, "Invalid JSON falls back to defaults and leaves the user's file untouched");

        // The config.json that ships with the application must load cleanly and describe exactly the built-in defaults.
        string? shipped = FindShippedConfig();
        if (shipped is null)
        {
            Console.WriteLine("  SKIP  Shipped config.json not found next to the sources");
        }
        else
        {
            string copy = Path.Combine(root, "cfg-shipped", "config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(shipped, copy, overwrite: true);
            ConfigLoadResult loaded = ConfigStore.Load(copy);
            var defaults = new AppConfig();
            defaults.Normalize();
            Check(loaded.Warning is null, "The shipped config.json parses without warnings");
            Check(
                JsonSerializer.Serialize(loaded.Config, ConfigStore.JsonOptions) == JsonSerializer.Serialize(defaults, ConfigStore.JsonOptions),
                "The shipped config.json matches the built-in defaults");
        }
    }

    static string? FindShippedConfig()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            string candidate = Path.Combine(dir.FullName, "src", "WarcArchiver.App", "config.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    // ------------------------------------------------------------------ capture + verify

    static async Task CaptureAndVerifyTests(string root, TestSite site)
    {
        Section("Capture over HTTP (gzip layout) and verification");
        (AppConfig cfg, string dir) = NewConfig(root, "cap-gz");
        var events = new ConcurrentQueue<ArchiveEvent>();
        ArchiveResult result = await CaptureAsync(cfg, site.HttpBase + "/", progress: new SyncProgress<ArchiveEvent>(events.Enqueue));
        Check(result.Success, "Capture succeeds" + (result.Error is null ? string.Empty : ": " + result.Error));
        if (!result.Success)
        {
            return;
        }

        Check(result.MainStatus == 200 && result.WarcPath!.EndsWith(".warc.gz", StringComparison.Ordinal), "Main page HTTP 200, output is .warc.gz");
        Check(events.Any(e => e.Kind == ArchiveEventKind.Stage) && events.Count(e => e.Kind == ArchiveEventKind.Resource) >= 15, "Progress events were reported");
        Check(File.GetAttributes(result.WarcPath!).HasFlag(FileAttributes.ReadOnly) && File.GetAttributes(result.ManifestPath!).HasFlag(FileAttributes.ReadOnly), "Finished files are marked read-only");

        Manifest manifest = ReadManifest(result.ManifestPath!);
        string[] responseUris = manifest.Records.Where(r => r.Type == "response").Select(r => r.Uri!).ToArray();
        string B(string path) => site.HttpBase + path;
        foreach (string expected in new[]
        {
            "/", "/style.css", "/logo.png", "/app.js", "/chunked.js", "/inline-bg.png", "/logo2.png?a=1&b=2", "/frame.html", "/style-attr.png",
            "/missing.png", "/redir", "/redir-target.png", "/big.png", "/bg.png", "/font.woff2", "/imported.css", "/deep.png",
        })
        {
            Check(responseUris.Contains(B(expected)), "Archived " + expected);
        }
        foreach (string forbidden in new[] { "/in-comment.png", "/in-script.png", "/in-css-comment.png", "/other-page.html" })
        {
            Check(!responseUris.Contains(B(forbidden)), "Did not fetch " + forbidden);
        }

        Check(manifest.Records.Count(r => r.Type == "warcinfo") == 1, "Exactly one warcinfo record");
        Check(manifest.Records.Count(r => r.Type == "request") == responseUris.Length + 0 && manifest.Records.Count(r => r.Type == "metadata") == responseUris.Length,
            "Every response has a request record and a metadata record");
        Check(manifest.Issues.Any(i => i.Type == "warning" && i.Url.EndsWith("/missing.png", StringComparison.Ordinal)), "HTTP 404 of a sub-resource is recorded as a warning (and still archived)");
        Check(manifest.Capture.ServerDateHeader is not null && manifest.Capture.ClockSkewSeconds is not null && Math.Abs(manifest.Capture.ClockSkewSeconds.Value) < 5, "Server Date header and clock skew are recorded");
        Check(manifest.Capture.StartedLocal.Length > 20 && manifest.Capture.StartedUtc.EndsWith('Z') && manifest.Capture.TimeZoneId.Length > 0, "Local time (with offset) and UTC are both recorded");

        // Look inside the archive with the reader: byte-exact wire data, chunked handling, payload digests.
        var records = ReadAllRecords(result.WarcPath!);
        (WarcHeader header, byte[] body) mainResponse = records.First(r => r.header.Type == "response" && r.header.TargetUri == B("/"));
        string mainText = Encoding.UTF8.GetString(mainResponse.body);
        Check(mainText.StartsWith("HTTP/1.1 200 OK\r\n", StringComparison.Ordinal) && mainText.Contains("Content-Type: text/html", StringComparison.Ordinal), "Response record holds the raw HTTP response head");
        (WarcHeader header, byte[] body) mainRequest = records.First(r => r.header.Type == "request" && r.header.TargetUri == B("/"));
        string requestText = Encoding.UTF8.GetString(mainRequest.body);
        Check(requestText.StartsWith("GET / HTTP/1.1\r\n", StringComparison.Ordinal) && requestText.Contains("Connection: close", StringComparison.Ordinal) && requestText.Contains("Accept-Encoding: gzip", StringComparison.Ordinal),
            "Request record holds the exact bytes the client sent");
        Check(mainRequest.header.Get("WARC-Concurrent-To") == mainResponse.header.Get("WARC-Record-ID"), "Request record is linked to its response (WARC-Concurrent-To)");
        Check(mainResponse.header.Get("WARC-IP-Address") == "127.0.0.1", "Server IP address is recorded");

        var chunkedRecord = records.First(r => r.header.Type == "response" && r.header.TargetUri == B("/chunked.js"));
        Check(Encoding.UTF8.GetString(chunkedRecord.body).Contains("Transfer-Encoding: chunked", StringComparison.OrdinalIgnoreCase), "Chunked response is stored raw, chunk framing included");
        ManifestRecord chunkedEntry = manifest.Records.First(r => r.Uri == B("/chunked.js") && r.Type == "response");
        Check(chunkedEntry.PayloadSha256 == DigestSet.OfBytes("console.log('part one');console.log('part two');"u8).Sha256Hex, "Payload digest of a chunked response covers the de-chunked body");

        var gzRecord = records.First(r => r.header.Type == "response" && r.header.TargetUri == B("/style.css"));
        HttpHead gzHead = HttpHead.TryParseFinal(gzRecord.body)!;
        ManifestRecord gzEntry = manifest.Records.First(r => r.Uri == B("/style.css") && r.Type == "response");
        var gzEntity = new MemoryStream();
        if (gzHead.IsChunked)
        {
            new ChunkedDecoder(span => gzEntity.Write(span)).Feed(gzRecord.body.AsSpan(gzHead.HeadLength));
        }
        else
        {
            gzEntity.Write(gzRecord.body.AsSpan(gzHead.HeadLength));
        }
        Check(gzHead.ContentEncoding == "gzip" && gzHead.IsChunked && gzEntry.PayloadSha256 == DigestSet.OfBytes(gzEntity.ToArray()).Sha256Hex,
            "gzip+chunked response is stored as received; payload digest covers the still-compressed entity body");

        ManifestRecord bigEntry = manifest.Records.First(r => r.Uri == B("/big.png") && r.Type == "response");
        Check(bigEntry.PayloadSha256 == DigestSet.OfBytes(TestSite.BigPayload).Sha256Hex && bigEntry.Truncated is null, "A 3 MB resource (spilled to a temp file) is archived intact");

        var metadata = records.First(r => r.header.Type == "metadata" && r.header.TargetUri == B("/redir-target.png"));
        Check(Encoding.UTF8.GetString(metadata.body).Contains("redirected-from: " + B("/redir"), StringComparison.Ordinal), "Redirect hop is recorded in metadata");

        // Verification of the untouched archive.
        var report = await new ArchiveVerifier().VerifyAsync(result.WarcPath!, null, CancellationToken.None);
        Console.WriteLine(report.ToText());
        Check(report.Passed, "Verifier: untouched archive PASSES");
        Check(report.Checks.Any(c => c.Name == "Records match manifest" && c.Status == VerificationStatus.Pass), "Verifier: records match the manifest");
        Check(report.Checks.Any(c => c.Name == "Manifest anchored in audit log" && c.Status == VerificationStatus.Pass), "Verifier: manifest is anchored in the audit log");
        Check(report.Checks.Any(c => c.Name == "Completion recorded in audit log" && c.Status == VerificationStatus.Pass), "Verifier: completion is recorded in the audit log");
        Check((await new ArchiveVerifier().VerifyAsync(result.ManifestPath!, null, CancellationToken.None)).Passed, "Verifier accepts the .manifest.json as input");

        // Tamper tests on copies.
        string t1 = CopyDir(dir, Path.Combine(root, "tamper-gz-flip"));
        string warc = Path.Combine(t1, Path.GetFileName(result.WarcPath!));
        byte[] raw = File.ReadAllBytes(warc);
        raw[raw.Length / 2] ^= 0xFF;
        File.WriteAllBytes(warc, raw);
        var flipped = await new ArchiveVerifier().VerifyAsync(warc, null, CancellationToken.None);
        Check(!flipped.Passed && flipped.Checks.Any(c => c.Name == "Archive matches manifest" && c.Status == VerificationStatus.Fail), "Tamper: one flipped byte in the .warc.gz is detected");

        string t2 = CopyDir(dir, Path.Combine(root, "tamper-manifest"));
        string manifestCopy = Path.Combine(t2, Path.GetFileName(result.ManifestPath!));
        File.WriteAllText(manifestCopy, File.ReadAllText(manifestCopy).Replace("\"mainStatus\": 200", "\"mainStatus\": 404", StringComparison.Ordinal));
        var manifestTampered = await new ArchiveVerifier().VerifyAsync(Path.Combine(t2, Path.GetFileName(result.WarcPath!)), null, CancellationToken.None);
        Check(!manifestTampered.Passed && manifestTampered.Checks.Any(c => c.Name.StartsWith("Checksum file", StringComparison.Ordinal) && c.Status == VerificationStatus.Fail), "Tamper: an edited manifest is caught by the checksum files");

        string t3 = CopyDir(dir, Path.Combine(root, "tamper-audit"));
        string logCopy = Path.Combine(t3, "audit-log.jsonl");
        File.WriteAllText(logCopy, File.ReadAllText(logCopy).Replace("capture.complete", "capture.compl3te", StringComparison.Ordinal));
        var auditTampered = await new ArchiveVerifier().VerifyAsync(Path.Combine(t3, Path.GetFileName(result.WarcPath!)), null, CancellationToken.None);
        Check(!auditTampered.Passed && auditTampered.Checks.Any(c => c.Name == "Audit log chain" && c.Status == VerificationStatus.Fail), "Tamper: an edited audit log is detected");

        // Uncompressed layout: flip a byte inside a body so the record structure stays valid but its digest does not.
        Section("Capture (plain .warc layout) and record-level tamper detection");
        (AppConfig plainCfg, string plainDir) = NewConfig(root, "cap-plain", c => c.CompressWarc = false);
        ArchiveResult plain = await CaptureAsync(plainCfg, site.HttpBase + "/");
        Check(plain.Success && plain.WarcPath!.EndsWith(".warc", StringComparison.Ordinal), "Uncompressed capture succeeds");
        Check((await new ArchiveVerifier().VerifyAsync(plain.WarcPath!, null, CancellationToken.None)).Passed, "Verifier: untouched .warc PASSES");

        string t4 = CopyDir(plainDir, Path.Combine(root, "tamper-plain-body"));
        string plainWarc = Path.Combine(t4, Path.GetFileName(plain.WarcPath!));
        byte[] plainBytes = File.ReadAllBytes(plainWarc);
        int at = IndexOf(plainBytes, "part one"u8);
        plainBytes[at + 5] = (byte)'0';
        File.WriteAllBytes(plainWarc, plainBytes);
        var bodyTampered = await new ArchiveVerifier().VerifyAsync(plainWarc, null, CancellationToken.None);
        Check(!bodyTampered.Passed && bodyTampered.Checks.Any(c => c.Name == "WARC structure and record digests" && c.Status == VerificationStatus.Fail),
            "Tamper: one changed character inside a response body fails that record's own WARC digest");

        Section("Non-HTML main resource and resource-kind filters");
        (AppConfig jsonCfg, _) = NewConfig(root, "cap-json");
        ArchiveResult json = await CaptureAsync(jsonCfg, site.HttpBase + "/not-html");
        Check(json.Success && ReadManifest(json.ManifestPath!).Records.Count(r => r.Type == "response") == 1, "A JSON main resource is archived on its own (no link scanning)");

        (AppConfig filterCfg, _) = NewConfig(root, "cap-filter");
        ArchiveResult filtered = await CaptureAsync(filterCfg, site.HttpBase + "/", req: new ArchiveRequestOptions { Images = false, Scripts = false });
        string[] filteredUris = ReadManifest(filtered.ManifestPath!).Records.Where(r => r.Type == "response").Select(r => r.Uri!).ToArray();
        Check(filtered.Success && !filteredUris.Any(u => u.EndsWith(".png", StringComparison.Ordinal) || u.EndsWith(".js", StringComparison.Ordinal)) && filteredUris.Contains(B("/style.css")),
            "Unticking images and scripts skips them but keeps stylesheets");

        Section("Two captures in one second never overwrite each other");
        (AppConfig dupCfg, string dupDir) = NewConfig(root, "cap-dup");
        ArchiveResult a = await CaptureAsync(dupCfg, site.HttpBase + "/not-html");
        ArchiveResult b = await CaptureAsync(dupCfg, site.HttpBase + "/not-html");
        Check(a.Success && b.Success && a.WarcPath != b.WarcPath && Directory.GetFiles(dupDir, "*.warc.gz").Length == 2, "Colliding file names get a numeric suffix");
        Check(AuditLog.Verify(Path.Combine(dupDir, "audit-log.jsonl")) is { Ok: true, Entries: 4 }, "Two captures leave a verifiable 4-entry audit chain");
    }

    // ------------------------------------------------------------------ security

    static async Task SecurityTests(string root, TestSite site)
    {
        Section("Network and TLS security");

        (AppConfig strictNet, string strictDir) = NewConfig(root, "sec-private", c => c.Network.AllowPrivateNetworks = false);
        ArchiveResult blocked = await CaptureAsync(strictNet, site.HttpBase + "/");
        Check(!blocked.Success && blocked.Error!.Contains("Blocked by network policy", StringComparison.Ordinal), "Loopback address is refused by default" + (blocked.Error is null ? string.Empty : " (" + blocked.Error + ")"));
        Check(Directory.GetFiles(strictDir, "*.warc*").Length == 0, "A failed capture leaves no partial archive behind");
        Check(AuditLog.ReadAll(Path.Combine(strictDir, "audit-log.jsonl")).Any(e => e.Event == "capture.failed"), "The failure is written to the audit log");

        (AppConfig strictTls, _) = NewConfig(root, "sec-tls-strict");
        ArchiveResult rejected = await CaptureAsync(strictTls, site.HttpsBase + "/");
        Check(!rejected.Success && rejected.Error!.Contains("certificate", StringComparison.OrdinalIgnoreCase), "Self-signed certificate is rejected by default" + (rejected.Error is null ? string.Empty : " (" + rejected.Error + ")"));

        (AppConfig lenientTls, _) = NewConfig(root, "sec-tls-lenient", c => c.Network.AllowInvalidCertificates = true);
        ArchiveResult accepted = await CaptureAsync(lenientTls, site.HttpsBase + "/");
        Check(accepted.Success, "With allowInvalidCertificates the page is captured anyway" + (accepted.Error is null ? string.Empty : ": " + accepted.Error));
        if (accepted.Success)
        {
            var records = ReadAllRecords(accepted.WarcPath!);
            string meta = Encoding.UTF8.GetString(records.First(r => r.header.Type == "metadata" && r.header.TargetUri == site.HttpsBase + "/").body);
            Check(meta.Contains("tls-peer-sha256: " + site.ServerCertSha256, StringComparison.Ordinal), "TLS evidence: the server certificate fingerprint is stored in the archive");
            Check(meta.Contains("tls-policy-errors: RemoteCertificateChainErrors", StringComparison.Ordinal), "TLS evidence: the certificate problem itself is recorded");
            Check(meta.Contains("tls-peer-subject: CN=localhost", StringComparison.Ordinal) && meta.Contains("tls-peer-certificate-der-base64: ", StringComparison.Ordinal), "TLS evidence: subject and full certificate are stored");
            Check(accepted.Issues.Count == 0 || accepted.Issues.All(i => i.Type != "failed"), "No sub-resource failed over HTTPS");
            string request = Encoding.UTF8.GetString(records.First(r => r.header.Type == "request" && r.header.TargetUri == site.HttpsBase + "/").body);
            Check(request.StartsWith("GET / HTTP/1.1\r\n", StringComparison.Ordinal), "The decrypted HTTPS request is what is stored (not ciphertext)");
            Check((await new ArchiveVerifier().VerifyAsync(accepted.WarcPath!, null, CancellationToken.None)).Passed, "Verifier: HTTPS archive PASSES");
        }
    }

    static async Task SigningTests(string root, TestSite site)
    {
        Section("Digital signatures");
        foreach (string kind in new[] { "RSA", "ECDSA" })
        {
            using X509Certificate2 cert = MakeSigningCertificate(kind);
            (AppConfig cfg, string dir) = NewConfig(root, "sign-" + kind.ToLowerInvariant(), c => c.WarcPrimaryDigest = kind == "RSA" ? "sha256" : "sha512");
            ArchiveResult signed = await CaptureAsync(cfg, site.HttpBase + "/not-html", cert);
            Check(signed.Success && signed.Signed && File.Exists(signed.SignaturePath!), kind + ": capture is signed");
            var report = await new ArchiveVerifier().VerifyAsync(signed.WarcPath!, null, CancellationToken.None);
            Check(report.Passed && report.Checks.Any(c => c.Name == "Digital signature" && c.Status == VerificationStatus.Pass), kind + ": signature verifies");

            string copy = CopyDir(dir, Path.Combine(root, "sign-tamper-" + kind.ToLowerInvariant()));
            string manifestCopy = Path.Combine(copy, Path.GetFileName(signed.ManifestPath!));
            File.WriteAllText(manifestCopy, File.ReadAllText(manifestCopy).Replace("\"mainStatus\": 200", "\"mainStatus\": 500", StringComparison.Ordinal));
            var tampered = await new ArchiveVerifier().VerifyAsync(Path.Combine(copy, Path.GetFileName(signed.WarcPath!)), null, CancellationToken.None);
            Check(tampered.Checks.Any(c => c.Name == "Digital signature" && c.Status == VerificationStatus.Fail), kind + ": signature check fails when the manifest is edited");

            // Re-sign the edited manifest with a different key: the signature is valid but it is not the original signer.
            if (kind == "RSA")
            {
                using X509Certificate2 other = MakeSigningCertificate("RSA");
                var sigFile = JsonSerializer.Deserialize<WarcArchiver.Core.Signing.SignatureFile>(File.ReadAllText(signed.SignaturePath! ), ManifestJson.Options)!;
                Check(sigFile.SignerThumbprintSha256 == Convert.ToHexStringLower(cert.GetCertHash(HashAlgorithmName.SHA256)) && sigFile.SignerThumbprintSha256 != Convert.ToHexStringLower(other.GetCertHash(HashAlgorithmName.SHA256)),
                    "The signature file identifies the signing certificate by SHA-256 thumbprint");
            }
        }
    }

    static async Task LimitTests(string root, TestSite site)
    {
        Section("Size and count limits");
        (AppConfig cfg, _) = NewConfig(root, "limit-size", c => c.Limits.MaxResourceMegabytes = 1);
        ArchiveResult result = await CaptureAsync(cfg, site.HttpBase + "/");
        Check(result.Success, "Capture with a 1 MB per-resource limit succeeds");
        Manifest manifest = ReadManifest(result.ManifestPath!);
        ManifestRecord big = manifest.Records.First(r => r.Type == "response" && r.Uri!.EndsWith("/big.png", StringComparison.Ordinal));
        Check(big.Truncated == "length" && big.BlockLength <= 1024 * 1024, "The oversized resource is cut at the limit and marked WARC-Truncated: length");
        Check(manifest.Issues.Any(i => i.Message.Contains("incompletely", StringComparison.Ordinal) && i.Url.EndsWith("/big.png", StringComparison.Ordinal)), "The truncation is listed in the manifest issues");
        Check((await new ArchiveVerifier().VerifyAsync(result.WarcPath!, null, CancellationToken.None)).Passed, "A archive containing a truncated resource still verifies");

        (AppConfig countCfg, _) = NewConfig(root, "limit-count", c => c.Limits.MaxResources = 3);
        ArchiveResult counted = await CaptureAsync(countCfg, site.HttpBase + "/");
        Check(counted.Success && counted.ResourcesSkipped > 0 && counted.Issues.Any(i => i.Type == "skipped"), "The resource-count limit skips the rest and says so");

        (AppConfig depthCfg, _) = NewConfig(root, "limit-depth", c => c.Limits.MaxCssDepth = 1);
        ArchiveResult depth = await CaptureAsync(depthCfg, site.HttpBase + "/");
        string[] uris = ReadManifest(depth.ManifestPath!).Records.Where(r => r.Type == "response").Select(r => r.Uri!).ToArray();
        Check(uris.Contains(site.HttpBase + "/imported.css") && !uris.Contains(site.HttpBase + "/deep.png"), "maxCssDepth=1 follows @import once but not the imported file's own references");
    }

    static async Task CancellationAndFailureTests(string root, TestSite site)
    {
        Section("Cancellation and failures");
        (AppConfig cfg, string dir) = NewConfig(root, "cancel");
        using var cts = new CancellationTokenSource();
        Task<ArchiveResult> running = CaptureAsync(cfg, site.HttpBase + "/slow", ct: cts.Token);
        await Task.Delay(700);
        cts.Cancel();
        ArchiveResult cancelled = await running;
        Check(cancelled.Cancelled && !cancelled.Success, "Cancelling a running capture reports Cancelled");
        Check(Directory.GetFiles(dir, "*.warc*").Length == 0, "Cancelling removes the partial archive");
        Check(AuditLog.ReadAll(Path.Combine(dir, "audit-log.jsonl")).Select(e => e.Event).SequenceEqual(["capture.start", "capture.cancelled"]), "Start and cancellation are both in the audit log");

        (AppConfig refusedCfg, string refusedDir) = NewConfig(root, "refused");
        ArchiveResult refused = await CaptureAsync(refusedCfg, "http://127.0.0.1:1/");
        Check(!refused.Success && refused.Error is not null && Directory.GetFiles(refusedDir, "*.warc*").Length == 0, "A refused connection fails cleanly: " + refused.Error);

        (AppConfig slowCfg, _) = NewConfig(root, "timeout", c => c.Network.RequestTimeoutSeconds = 5);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        ArchiveResult timedOut = await CaptureAsync(slowCfg, site.HttpBase + "/slow");
        Check(!timedOut.Success && timedOut.Error!.Contains("timed out", StringComparison.OrdinalIgnoreCase) && sw.Elapsed < TimeSpan.FromSeconds(15), "The request timeout is enforced: " + timedOut.Error);
    }

    // ------------------------------------------------------------------ helpers

    sealed class ArchiveRequestOptions
    {
        public bool Scripts { get; init; } = true;

        public bool Images { get; init; } = true;
    }

    static (AppConfig Config, string Dir) NewConfig(string root, string name, Action<AppConfig>? tweak = null)
    {
        string dir = Path.Combine(root, name);
        Directory.CreateDirectory(dir);
        var cfg = new AppConfig { OutputDirectory = dir };
        cfg.Network.AllowPrivateNetworks = true;
        cfg.Network.RequestTimeoutSeconds = 30;
        tweak?.Invoke(cfg);
        cfg.Normalize();
        return (cfg, dir);
    }

    static async Task<ArchiveResult> CaptureAsync(
        AppConfig cfg, string url, X509Certificate2? cert = null, IProgress<ArchiveEvent>? progress = null, CancellationToken ct = default, ArchiveRequestOptions? req = null)
    {
        var audit = new AuditLog(Path.Combine(cfg.ResolveOutputDirectory(), "audit-log.jsonl"));
        var engine = new ArchiveEngine(cfg, audit, "selftest");
        req ??= new ArchiveRequestOptions();
        return await engine.RunAsync(
            new ArchiveRequest
            {
                Url = url,
                CaseReference = "CASE-2026-001",
                Operator = "Self-test",
                Notes = "Automated test capture",
                IncludeScripts = req.Scripts,
                IncludeImages = req.Images,
                SigningCertificate = cert,
            },
            progress,
            ct);
    }

    static Manifest ReadManifest(string path) =>
        JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path), ManifestJson.Options)!;

    static List<(WarcHeader header, byte[] body)> ReadAllRecords(string path)
    {
        var list = new List<(WarcHeader, byte[])>();
        using Stream stream = WarcReader.OpenFile(path);
        using var reader = new WarcReader(stream);
        while (reader.ReadHeader() is { } header)
        {
            using Stream block = reader.OpenBlock(header);
            var ms = new MemoryStream();
            block.CopyTo(ms);
            reader.FinishRecord(block);
            list.Add((header, ms.ToArray()));
        }
        return list;
    }

    static X509Certificate2 MakeSigningCertificate(string kind)
    {
        CertificateRequest request = kind == "RSA"
            ? new CertificateRequest("CN=WarcArchiver Test Signer (RSA)", RSA.Create(2048), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
            : new CertificateRequest("CN=WarcArchiver Test Signer (ECDSA)", ECDsa.Create(ECCurve.NamedCurves.nistP256), HashAlgorithmName.SHA256);
        using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx, "t"), "t");
    }

    static string CopyDir(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
        {
            string target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, overwrite: true);
            File.SetAttributes(target, FileAttributes.Normal);
        }
        return destination;
    }

    static int IndexOf(byte[] haystack, ReadOnlySpan<byte> needle) => haystack.AsSpan().IndexOf(needle);

    static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine("== " + title);
    }

    static void Check(bool condition, string name)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine("  PASS  " + name);
        }
        else
        {
            _failed++;
            Console.WriteLine("  FAIL  " + name);
        }
    }
}
