using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using WarcArchiver.Core.Audit;
using WarcArchiver.Core.Config;
using WarcArchiver.Core.Digests;
using WarcArchiver.Core.Http;
using WarcArchiver.Core.Net;
using WarcArchiver.Core.Parsing;
using WarcArchiver.Core.Signing;
using WarcArchiver.Core.Warc;

namespace WarcArchiver.Core.Archive;

/// <summary>
/// Runs one evidence capture end to end: fetch the page and everything it references over raw, byte-exact HTTP
/// exchanges, write them as WARC 1.1 records, fingerprint the file (SHA-256 + SHA-512), write the manifest and
/// checksum files, optionally sign, and record the whole thing in the tamper-evident audit log.
/// </summary>
public sealed class ArchiveEngine(AppConfig config, AuditLog audit, string configSha256 = "")
{
    public const string ToolName = "WarcArchiver";

    readonly AppConfig _config = config;
    readonly AuditLog _audit = audit;
    readonly HttpFetcher _fetcher = new(config);
    readonly string _configSha256 = configSha256;

    public static string ToolVersion { get; } =
        typeof(ArchiveEngine).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public Task<ArchiveResult> RunAsync(ArchiveRequest request, IProgress<ArchiveEvent>? progress, CancellationToken cancellationToken)
    {
        if (!TryNormalizeUrl(request.Url, out Uri? target, out string? problem))
        {
            _audit.Append("capture.rejected", [new("url", Clip(request.Url, 300)), new("reason", problem!)]);
            return Task.FromResult(ArchiveResult.Failure(problem!));
        }
        return new Session(this, request, target!, progress).RunAsync(cancellationToken);
    }

    /// <summary>Accepts http/https URLs only, adds https:// when the scheme is missing, and rejects embedded credentials.</summary>
    public static bool TryNormalizeUrl(string? text, out Uri? uri, out string? problem)
    {
        uri = null;
        problem = null;
        string raw = (text ?? string.Empty).Trim();
        if (raw.Length == 0)
        {
            problem = "Enter the address of the page to archive.";
            return false;
        }
        if (raw.Length > 2048)
        {
            problem = "The URL is longer than 2048 characters.";
            return false;
        }
        if (!raw.Contains("://", StringComparison.Ordinal))
        {
            raw = "https://" + raw;
        }
        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? parsed) || string.IsNullOrEmpty(parsed.Host))
        {
            problem = "That is not a valid web address.";
            return false;
        }
        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps)
        {
            problem = "Only http:// and https:// addresses can be archived.";
            return false;
        }
        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            problem = "Addresses containing a user name or password are refused, because credentials would be stored in the archive.";
            return false;
        }
        uri = new Uri(parsed.GetLeftPart(UriPartial.Query));
        return true;
    }

    static string Clip(string value, int max) => value.Length <= max ? value : value[..max] + "…";

    sealed record ResourceOutcome(DecodedBody? Body, Uri FinalUri);

    sealed class Session(ArchiveEngine engine, ArchiveRequest request, Uri target, IProgress<ArchiveEvent>? progress)
    {
        readonly ArchiveEngine _engine = engine;
        readonly ArchiveRequest _request = request;
        readonly Uri _target = target;
        readonly IProgress<ArchiveEvent>? _progress = progress;

        readonly object _stateLock = new();
        readonly object _writeLock = new();
        readonly HashSet<string> _seen = new(StringComparer.Ordinal);
        readonly List<ManifestIssue> _issues = [];

        WarcWriter? _writer;
        DateTimeOffset _startedLocal;
        Uri _finalUri = target;
        int _mainStatus;
        string? _serverDate;
        double? _clockSkew;
        List<ResourceRef> _pageReferences = [];
        string? _mainError;

        int _discovered;
        int _completed;
        int _started;
        int _archived;
        int _failed;
        int _skipped;
        long _bytes;

        private AppConfig Config => _engine._config;

        public async Task<ArchiveResult> RunAsync(CancellationToken cancellationToken)
        {
            _startedLocal = DateTimeOffset.Now;
            string outputDirectory = Config.ResolveOutputDirectory();
            Directory.CreateDirectory(outputDirectory);

            string extension = Config.CompressWarc ? ".warc.gz" : ".warc";
            string baseName = _startedLocal.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "_" + SafeHostName(_target);
            (string warcPath, FileStream warcStream) = CreateUniqueFile(outputDirectory, baseName, extension);
            string stem = Path.GetFileName(warcPath)[..^extension.Length];

            AuditEntry anchor = _engine._audit.Append("capture.start",
            [
                new("url", _target.AbsoluteUri),
                new("archive", Path.GetFileName(warcPath)),
                new("caseReference", Clip(_request.CaseReference, 200)),
                new("operator", Clip(_request.Operator, 200)),
                new("configSha256", _engine._configSha256),
                new("signing", _request.SigningCertificate is null ? "none" : "x509"),
            ]);

            try
            {
                _writer = new WarcWriter(warcStream, Config.CompressWarc, Config.PrimaryDigest);
                WriteWarcInfo(Path.GetFileName(warcPath));

                Stage("Fetching the page…", discovered: 1, completed: 0);
                lock (_stateLock)
                {
                    _seen.Add(_target.AbsoluteUri);
                }

                if (!await CaptureMainAsync(cancellationToken).ConfigureAwait(false))
                {
                    return Abort(warcPath, _mainError ?? "The page could not be fetched.", cancelled: false);
                }

                Stage("Fetching referenced resources…", discovered: 1 + _pageReferences.Count, completed: 1);
                await CaptureResourcesAsync(cancellationToken).ConfigureAwait(false);

                Stage("Finalizing the archive…", discovered: _discovered, completed: _completed);
                return await FinalizeAsync(outputDirectory, warcPath, stem, anchor).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return Abort(warcPath, "Capture cancelled.", cancelled: true);
            }
            catch (Exception ex)
            {
                return Abort(warcPath, "Capture failed: " + ex.Message, cancelled: false);
            }
        }

        ArchiveResult Abort(string warcPath, string reason, bool cancelled)
        {
            _writer?.Dispose();
            _writer = null;
            try
            {
                File.Delete(warcPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A leftover partial file is harmless; the audit entry below records that the capture did not complete.
            }

            _engine._audit.Append(cancelled ? "capture.cancelled" : "capture.failed",
            [
                new("url", _target.AbsoluteUri),
                new("archive", Path.GetFileName(warcPath)),
                new("reason", Clip(reason, 500)),
            ]);
            return new ArchiveResult { Cancelled = cancelled, Error = reason };
        }

        // ----------------------------------------------------------------- main document

        async Task<bool> CaptureMainAsync(CancellationToken cancellationToken)
        {
            Uri current = _target;
            Uri? redirectedFrom = null;

            for (int hop = 0; ; hop++)
            {
                using CapturedExchange exchange = await _engine._fetcher.FetchAsync(current, null, cancellationToken).ConfigureAwait(false);
                WarcRecordInfo? response = WriteExchange(exchange, ResourceKind.Document, redirectedFrom);
                ReportResource(exchange, ResourceKind.Document, response, completed: 1);

                if (!exchange.HasResponse)
                {
                    _mainError = exchange.Error ?? "The server did not send a response.";
                    return false;
                }

                if (hop == 0)
                {
                    NoteServerClock(exchange);
                }

                if (IsRedirect(exchange.StatusCode) && exchange.Head!.Location is { Length: > 0 } location)
                {
                    Uri? next = UrlResolver.Resolve(current, location);
                    if (next is null)
                    {
                        AddIssue("warning", current, "Redirect to an unsupported location (" + Clip(location, 120) + ") was archived but not followed.");
                    }
                    else if (hop >= Config.Network.MaxRedirects)
                    {
                        _mainError = "Too many redirects (limit " + Config.Network.MaxRedirects.ToString(CultureInfo.InvariantCulture) + ").";
                        return false;
                    }
                    else
                    {
                        lock (_stateLock)
                        {
                            _seen.Add(next.AbsoluteUri);
                        }
                        redirectedFrom = current;
                        current = next;
                        continue;
                    }
                }

                _finalUri = current;
                _mainStatus = exchange.StatusCode;
                if (exchange.Truncated is not null)
                {
                    AddIssue("warning", current, "The page itself was incomplete (" + exchange.Truncated + "): " + (exchange.Error ?? "response cut short."));
                }
                if (exchange.StatusCode >= 400)
                {
                    AddIssue("warning", current, "The server answered HTTP " + exchange.StatusCode.ToString(CultureInfo.InvariantCulture) + "; the response was archived as it was received.");
                }

                if (exchange.Head!.MediaType is "text/html" or "application/xhtml+xml")
                {
                    DecodedBody? body = DecodeForParsing(exchange, current);
                    if (body is not null)
                    {
                        _pageReferences = FilterNew(HtmlResourceExtractor.Extract(body.Text, current));
                    }
                }
                return true;
            }
        }

        // ----------------------------------------------------------------- sub-resources

        async Task CaptureResourcesAsync(CancellationToken cancellationToken)
        {
            List<ResourceRef> level = _pageReferences;
            int parallelism = Config.Network.MaxParallelDownloads;

            for (int depth = 1; level.Count > 0; depth++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int currentDepth = depth;
                var children = new ConcurrentBag<ResourceRef>();
                Interlocked.Add(ref _discovered, level.Count);

                await Parallel.ForEachAsync(
                    level,
                    new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cancellationToken },
                    async (reference, token) =>
                    {
                        if (!TryBegin(reference))
                        {
                            return;
                        }

                        ResourceOutcome outcome = await ArchiveResourceAsync(reference, currentDepth, token).ConfigureAwait(false);
                        Interlocked.Increment(ref _completed);

                        if (outcome.Body is not null)
                        {
                            foreach (ResourceRef child in FilterNew(CssResourceExtractor.Extract(outcome.Body.Text, outcome.FinalUri)))
                            {
                                children.Add(child);
                            }
                        }
                    }).ConfigureAwait(false);

                level = [.. children];
            }
        }

        bool TryBegin(ResourceRef reference)
        {
            long maxTotal = Config.Limits.MaxTotalMegabytes * 1024L * 1024L;
            string? reason = null;
            if (Interlocked.Increment(ref _started) > Config.Limits.MaxResources)
            {
                reason = "Resource limit reached (" + Config.Limits.MaxResources.ToString(CultureInfo.InvariantCulture) + ").";
            }
            else if (Interlocked.Read(ref _bytes) >= maxTotal)
            {
                reason = "Total size limit reached (" + Config.Limits.MaxTotalMegabytes.ToString(CultureInfo.InvariantCulture) + " MB).";
            }

            if (reason is null)
            {
                return true;
            }

            Interlocked.Increment(ref _skipped);
            Interlocked.Increment(ref _completed);
            AddIssue("skipped", reference.Url, reason);
            Report(new ArchiveEvent
            {
                Kind = ArchiveEventKind.Resource,
                Completed = _completed,
                Discovered = _discovered,
                Resource = new ResourceEntry
                {
                    Status = "SKIPPED",
                    Kind = reference.Kind,
                    Url = reference.Url.AbsoluteUri,
                    Note = reason,
                    IsProblem = true,
                },
            });
            return false;
        }

        async Task<ResourceOutcome> ArchiveResourceAsync(ResourceRef reference, int depth, CancellationToken cancellationToken)
        {
            Uri current = reference.Url;
            Uri? redirectedFrom = null;

            for (int hop = 0; ; hop++)
            {
                using CapturedExchange exchange = await _engine._fetcher.FetchAsync(current, _finalUri, cancellationToken).ConfigureAwait(false);
                WarcRecordInfo? response = WriteExchange(exchange, reference.Kind, redirectedFrom);
                ReportResource(exchange, reference.Kind, response, completed: _completed);

                if (!exchange.HasResponse)
                {
                    Interlocked.Increment(ref _failed);
                    AddIssue("failed", current, exchange.Error ?? "No response.");
                    return new ResourceOutcome(null, current);
                }

                Interlocked.Add(ref _bytes, exchange.Response.Length);

                if (IsRedirect(exchange.StatusCode) && exchange.Head!.Location is { Length: > 0 } location)
                {
                    Uri? next = UrlResolver.Resolve(current, location);
                    bool fresh;
                    lock (_stateLock)
                    {
                        fresh = next is not null && _seen.Add(next.AbsoluteUri);
                    }
                    if (next is null || !fresh || hop >= Config.Network.MaxRedirects)
                    {
                        Interlocked.Increment(ref _archived);
                        return new ResourceOutcome(null, current);
                    }
                    redirectedFrom = current;
                    current = next;
                    continue;
                }

                Interlocked.Increment(ref _archived);
                if (exchange.StatusCode >= 400)
                {
                    AddIssue("warning", current, "The server answered HTTP " + exchange.StatusCode.ToString(CultureInfo.InvariantCulture) + ".");
                }
                if (exchange.Truncated is not null)
                {
                    AddIssue("warning", current, "Archived incompletely (" + exchange.Truncated + "): " + (exchange.Error ?? "response cut short."));
                }

                DecodedBody? body = null;
                if (reference.Kind == ResourceKind.Stylesheet
                    && depth <= Config.Limits.MaxCssDepth
                    && exchange.StatusCode is >= 200 and < 300
                    && exchange.Head!.MediaType is "text/css" or "text/plain" or null)
                {
                    body = DecodeForParsing(exchange, current);
                }
                return new ResourceOutcome(body, current);
            }
        }

        DecodedBody? DecodeForParsing(CapturedExchange exchange, Uri uri)
        {
            long limit = Config.Limits.MaxParseMegabytes * 1024L * 1024L;
            if (exchange.Response.Length > limit)
            {
                AddIssue("warning", uri, "Too large to scan for further links (" + Config.Limits.MaxParseMegabytes.ToString(CultureInfo.InvariantCulture) + " MB parse limit); it was archived but its references were not followed.");
                return null;
            }
            DecodedBody? body = HttpBodyDecoder.TryDecode(exchange.Response, limit);
            if (body is null)
            {
                AddIssue("warning", uri, "The body could not be decoded, so its references were not followed.");
            }
            return body;
        }

        // ----------------------------------------------------------------- WARC records

        void WriteWarcInfo(string fileName)
        {
            var fields = new List<KeyValuePair<string, string>>
            {
                new("software", ToolName + "/" + ToolVersion),
                new("format", "WARC File Format 1.1"),
                new("conformsTo", WarcFormat.SpecUrl),
                new("description", "Evidence capture of " + _target.AbsoluteUri),
                new("http-header-user-agent", Config.Network.UserAgent),
                new("robots", "ignore"),
                new("x-capture-start-utc", _startedLocal.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)),
                new("x-capture-start-local", _startedLocal.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture)),
                new("x-capture-time-zone", TimeZoneInfo.Local.Id),
            };
            if (_request.Operator.Length > 0)
            {
                fields.Add(new("operator", _request.Operator));
            }
            if (_request.CaseReference.Length > 0)
            {
                fields.Add(new("isPartOf", _request.CaseReference));
            }
            if (_request.Notes.Length > 0)
            {
                fields.Add(new("x-case-notes", _request.Notes));
            }

            var spec = new WarcRecordSpec
            {
                Type = WarcTypes.WarcInfo,
                ContentType = WarcFormat.FieldsContentType,
                DateUtc = _startedLocal.UtcDateTime,
                Filename = fileName,
            };
            _writer!.WriteRecord(spec, WarcWriter.BuildFields(fields));
        }

        /// <summary>Writes the response, request and metadata records of one exchange. Returns the response record, if any.</summary>
        WarcRecordInfo? WriteExchange(CapturedExchange exchange, ResourceKind kind, Uri? redirectedFrom)
        {
            lock (_writeLock)
            {
                string uri = exchange.Uri.AbsoluteUri;
                string? ip = exchange.RemoteIp?.ToString();
                WarcRecordInfo? response = null;
                Guid? responseId = null;

                if (exchange.HasResponse)
                {
                    responseId = Guid.NewGuid();
                    response = _writer!.WriteRecord(
                        new WarcRecordSpec
                        {
                            Type = WarcTypes.Response,
                            ContentType = WarcFormat.HttpResponseContentType,
                            Id = responseId,
                            DateUtc = exchange.StartUtc,
                            TargetUri = uri,
                            IpAddress = ip,
                            Truncated = exchange.Truncated,
                            IsHttpResponse = true,
                            HttpStatus = exchange.StatusCode,
                            MediaType = exchange.Head!.MediaType,
                        },
                        exchange.Response);
                }

                if (exchange.Request.Length > 0)
                {
                    _writer!.WriteRecord(
                        new WarcRecordSpec
                        {
                            Type = WarcTypes.Request,
                            ContentType = WarcFormat.HttpRequestContentType,
                            DateUtc = exchange.StartUtc,
                            TargetUri = uri,
                            IpAddress = ip,
                            ConcurrentTo = responseId,
                        },
                        exchange.Request);
                }

                _writer!.WriteRecord(
                    new WarcRecordSpec
                    {
                        Type = WarcTypes.Metadata,
                        ContentType = WarcFormat.FieldsContentType,
                        DateUtc = exchange.StartUtc,
                        TargetUri = uri,
                        ConcurrentTo = responseId,
                    },
                    WarcWriter.BuildFields(BuildMetadata(exchange, kind, redirectedFrom)));

                return response;
            }
        }

        static List<KeyValuePair<string, string>> BuildMetadata(CapturedExchange ex, ResourceKind kind, Uri? redirectedFrom)
        {
            var fields = new List<KeyValuePair<string, string>>();
            void Add(string name, string? value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    fields.Add(new(name, value));
                }
            }

            var startLocal = new DateTimeOffset(DateTime.SpecifyKind(ex.StartUtc, DateTimeKind.Utc)).ToLocalTime();
            Add("software", ToolName + "/" + ToolVersion);
            Add("capture-start-utc", ex.StartUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture));
            Add("capture-start-local", startLocal.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture));
            Add("capture-time-zone", TimeZoneInfo.Local.Id);
            Add("capture-duration-ms", ((long)ex.Duration.TotalMilliseconds).ToString(CultureInfo.InvariantCulture));
            Add("resource-kind", kind.ToString());
            Add("redirected-from", redirectedFrom?.AbsoluteUri);
            if (ex.HasResponse)
            {
                Add("http-status", ex.StatusCode.ToString(CultureInfo.InvariantCulture));
                Add("http-date-header", ex.Head!.Get("Date"));
            }
            Add("remote-ip", ex.RemoteIp?.ToString());
            if (ex.ResolvedAddresses.Count > 0)
            {
                Add("dns-resolved-addresses", string.Join(", ", ex.ResolvedAddresses));
            }
            Add("truncated", ex.Truncated);
            Add("error", ex.Error);

            if (ex.Tls is { } tls)
            {
                Add("tls-peer-subject", tls.PeerSubject);
                Add("tls-peer-issuer", tls.PeerIssuer);
                Add("tls-peer-serial", tls.PeerSerial);
                Add("tls-peer-valid-from", tls.NotBeforeUtc?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
                Add("tls-peer-valid-to", tls.NotAfterUtc?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
                Add("tls-peer-sha256", tls.PeerSha256Hex);
                Add("tls-policy-errors", tls.PolicyErrors);
                for (int i = 0; i < tls.Chain.Count; i++)
                {
                    Add("tls-chain-" + (i + 1).ToString(CultureInfo.InvariantCulture), tls.Chain[i]);
                }
                Add("tls-peer-certificate-der-base64", tls.PeerDerBase64);
            }
            return fields;
        }

        // ----------------------------------------------------------------- finalization

        async Task<ArchiveResult> FinalizeAsync(string outputDirectory, string warcPath, string stem, AuditEntry anchor)
        {
            WarcWriter writer = _writer!;
            IReadOnlyList<WarcRecordInfo> records = writer.Records;
            DigestResult warcDigests = writer.Complete();
            _writer = null;

            DateTimeOffset finishedLocal = DateTimeOffset.Now;
            Manifest manifest = BuildManifest(warcPath, warcDigests, records, finishedLocal, anchor);
            byte[] manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestJson.Options);
            string manifestPath = Path.Combine(outputDirectory, stem + ".manifest.json");
            await File.WriteAllBytesAsync(manifestPath, manifestBytes).ConfigureAwait(false);
            DigestResult manifestDigests = DigestSet.OfBytes(manifestBytes);

            string? signaturePath = null;
            if (_request.SigningCertificate is { } certificate)
            {
                signaturePath = ManifestSigner.Sign(manifestPath, certificate, Config.PrimaryDigest);
            }

            string warcName = Path.GetFileName(warcPath);
            string manifestName = Path.GetFileName(manifestPath);
            string sha256Path = Path.Combine(outputDirectory, stem + ".sha256");
            string sha512Path = Path.Combine(outputDirectory, stem + ".sha512");
            await File.WriteAllTextAsync(sha256Path,
                $"{warcDigests.Sha256Hex}  {warcName}\n{manifestDigests.Sha256Hex}  {manifestName}\n").ConfigureAwait(false);
            await File.WriteAllTextAsync(sha512Path,
                $"{warcDigests.Sha512Hex}  {warcName}\n{manifestDigests.Sha512Hex}  {manifestName}\n").ConfigureAwait(false);

            if (Config.Security.MarkFilesReadOnly)
            {
                foreach (string path in new[] { warcPath, manifestPath, sha256Path, sha512Path }.Concat(signaturePath is null ? [] : [signaturePath]))
                {
                    TrySetReadOnly(path);
                }
            }

            _engine._audit.Append("capture.complete",
            [
                new("url", _target.AbsoluteUri),
                new("archive", warcName),
                new("warcSha256", warcDigests.Sha256Hex),
                new("warcSha512", warcDigests.Sha512Hex),
                new("manifestSha256", manifestDigests.Sha256Hex),
                new("signed", signaturePath is null ? "no" : "yes"),
                new("records", records.Count.ToString(CultureInfo.InvariantCulture)),
                new("resourcesArchived", _archived.ToString(CultureInfo.InvariantCulture)),
                new("resourcesFailed", _failed.ToString(CultureInfo.InvariantCulture)),
            ]);

            Stage("Done.", discovered: _discovered, completed: _completed);
            return new ArchiveResult
            {
                Success = true,
                WarcPath = warcPath,
                ManifestPath = manifestPath,
                SignaturePath = signaturePath,
                ChecksumPath = sha256Path,
                WarcDigests = warcDigests,
                ManifestSha256 = manifestDigests.Sha256Hex,
                Signed = signaturePath is not null,
                FinalUrl = _finalUri.AbsoluteUri,
                MainStatus = _mainStatus,
                ResourcesArchived = _archived,
                ResourcesFailed = _failed,
                ResourcesSkipped = _skipped,
                ResponseBytes = _bytes,
                Issues = _issues.ToArray(),
            };
        }

        Manifest BuildManifest(string warcPath, DigestResult warcDigests, IReadOnlyList<WarcRecordInfo> records, DateTimeOffset finishedLocal, AuditEntry anchor)
        {
            AppConfig cfg = Config;
            return new Manifest
            {
                Tool = new ManifestTool
                {
                    Name = ToolName,
                    Version = ToolVersion,
                    Runtime = RuntimeInformation.FrameworkDescription,
                    OperatingSystem = RuntimeInformation.OSDescription,
                },
                Case = new ManifestCase
                {
                    Reference = _request.CaseReference,
                    Operator = _request.Operator,
                    Notes = _request.Notes,
                },
                Capture = new ManifestCapture
                {
                    TargetUrl = _target.AbsoluteUri,
                    FinalUrl = _finalUri.AbsoluteUri,
                    MainStatus = _mainStatus,
                    StartedUtc = _startedLocal.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                    StartedLocal = _startedLocal.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
                    FinishedUtc = finishedLocal.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                    FinishedLocal = finishedLocal.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture),
                    TimeZoneId = TimeZoneInfo.Local.Id,
                    UtcOffset = _startedLocal.ToString("zzz", CultureInfo.InvariantCulture),
                    ServerDateHeader = _serverDate,
                    ClockSkewSeconds = _clockSkew,
                    ResourcesArchived = _archived,
                    ResourcesFailed = _failed,
                    ResourcesSkipped = _skipped,
                    ResponseBytes = _bytes,
                },
                Settings = new ManifestSettings
                {
                    IncludeScripts = _request.IncludeScripts,
                    IncludeStylesheets = _request.IncludeStylesheets,
                    IncludeImages = _request.IncludeImages,
                    IncludeOtherAssets = _request.IncludeOtherAssets,
                    IncludeThirdParty = _request.IncludeThirdParty,
                    UserAgent = cfg.Network.UserAgent,
                    MaxRedirects = cfg.Network.MaxRedirects,
                    MaxResources = cfg.Limits.MaxResources,
                    MaxResourceMegabytes = cfg.Limits.MaxResourceMegabytes,
                    AllowPrivateNetworks = cfg.Network.AllowPrivateNetworks,
                    AllowInvalidCertificates = cfg.Network.AllowInvalidCertificates,
                    ConfigSha256 = _engine._configSha256,
                },
                Warc = new ManifestWarc
                {
                    FileName = Path.GetFileName(warcPath),
                    Version = WarcFormat.Version,
                    Compressed = cfg.CompressWarc,
                    Bytes = warcDigests.Length,
                    Sha256 = warcDigests.Sha256Hex,
                    Sha512 = warcDigests.Sha512Hex,
                    PrimaryDigest = cfg.PrimaryDigest.WarcLabel(),
                    RecordCount = records.Count,
                },
                Audit = new ManifestAudit
                {
                    LogFile = Path.GetFileName(_engine._audit.Path),
                    AnchorSeq = anchor.Seq,
                    AnchorHash = anchor.Hash,
                },
                Issues = [.. _issues],
                Records = records.Select(ToManifestRecord).ToList(),
            };
        }

        static ManifestRecord ToManifestRecord(WarcRecordInfo r) => new()
        {
            Id = WarcFormat.FormatId(r.Id),
            Type = r.Type,
            Uri = r.TargetUri,
            DateUtc = WarcFormat.FormatDate(r.DateUtc),
            Offset = r.Offset,
            StoredLength = r.StoredLength,
            BlockLength = r.BlockLength,
            HttpStatus = r.HttpStatus,
            MediaType = r.MediaType,
            IpAddress = r.IpAddress,
            Truncated = r.Truncated,
            BlockSha256 = r.Block.Sha256Hex,
            BlockSha512 = r.Block.Sha512Hex,
            PayloadSha256 = r.Payload?.Sha256Hex,
            PayloadSha512 = r.Payload?.Sha512Hex,
        };

        // ----------------------------------------------------------------- helpers

        List<ResourceRef> FilterNew(IEnumerable<ResourceRef> candidates)
        {
            var accepted = new List<ResourceRef>();
            lock (_stateLock)
            {
                foreach (ResourceRef candidate in candidates)
                {
                    if (IsWanted(candidate) && _seen.Add(candidate.Url.AbsoluteUri))
                    {
                        accepted.Add(candidate);
                    }
                }
            }
            return accepted;
        }

        bool IsWanted(ResourceRef reference)
        {
            bool kindWanted = reference.Kind switch
            {
                ResourceKind.Script => _request.IncludeScripts,
                ResourceKind.Stylesheet => _request.IncludeStylesheets,
                ResourceKind.Image => _request.IncludeImages,
                _ => _request.IncludeOtherAssets,
            };
            return kindWanted && (_request.IncludeThirdParty || SameSite(reference.Url, _target));
        }

        static bool SameSite(Uri a, Uri b)
        {
            string hostA = a.IdnHost.ToLowerInvariant();
            string hostB = b.IdnHost.ToLowerInvariant();
            return hostA == hostB || SiteKey(hostA) == SiteKey(hostB);
        }

        static string SiteKey(string host)
        {
            if (IPAddress.TryParse(host, out _))
            {
                return host;
            }
            string[] labels = host.Split('.');
            if (labels.Length <= 2)
            {
                return host;
            }
            int take = labels[^1].Length == 2 && labels[^2] is "co" or "com" or "org" or "net" or "gov" or "ac" or "edu" ? 3 : 2;
            return string.Join('.', labels[^Math.Min(take, labels.Length)..]);
        }

        void NoteServerClock(CapturedExchange exchange)
        {
            string? date = exchange.Head?.Get("Date");
            if (date is null)
            {
                return;
            }
            _serverDate = date;
            if (DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed))
            {
                _clockSkew = Math.Round((parsed.UtcDateTime - exchange.StartUtc).TotalSeconds, 1);
            }
        }

        void AddIssue(string type, Uri url, string message)
        {
            lock (_stateLock)
            {
                _issues.Add(new ManifestIssue { Type = type, Url = url.AbsoluteUri, Message = message });
            }
        }

        void Stage(string message, int discovered, int completed) =>
            Report(new ArchiveEvent { Kind = ArchiveEventKind.Stage, Message = message, Discovered = discovered, Completed = completed });

        void ReportResource(CapturedExchange exchange, ResourceKind kind, WarcRecordInfo? response, int completed)
        {
            string status = exchange.HasResponse
                ? exchange.StatusCode.ToString(CultureInfo.InvariantCulture)
                : "ERROR";
            bool problem = !exchange.HasResponse || exchange.StatusCode >= 400 || exchange.Truncated is not null;
            string note = exchange.Error ?? (exchange.Tls is { HasPolicyErrors: true } tls ? "Certificate problem: " + tls.PolicyErrors : string.Empty);
            if (note.Length == 0 && exchange.HasResponse && IsRedirect(exchange.StatusCode))
            {
                note = "Redirect → " + Clip(exchange.Head!.Location ?? string.Empty, 120);
            }

            Report(new ArchiveEvent
            {
                Kind = ArchiveEventKind.Resource,
                Completed = completed,
                Discovered = Math.Max(_discovered, 1),
                Resource = new ResourceEntry
                {
                    Status = status,
                    Kind = kind,
                    Url = exchange.Uri.AbsoluteUri,
                    Bytes = exchange.Response.Length,
                    Sha256 = response is null ? string.Empty : (response.Payload ?? response.Block).Sha256Hex,
                    DurationMs = (int)exchange.Duration.TotalMilliseconds,
                    Note = note,
                    IsProblem = problem || exchange.Tls is { HasPolicyErrors: true },
                },
            });
        }

        void Report(ArchiveEvent evt) => _progress?.Report(evt);

        static bool IsRedirect(int status) => status is 301 or 302 or 303 or 307 or 308;

        static void TrySetReadOnly(string path)
        {
            try
            {
                File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Best effort: the digests and signature, not file attributes, are what protect the evidence.
            }
        }

        static (string Path, FileStream Stream) CreateUniqueFile(string directory, string baseName, string extension)
        {
            for (int i = 0; i < 100; i++)
            {
                string name = i == 0 ? baseName : baseName + "-" + (i + 1).ToString(CultureInfo.InvariantCulture);
                string path = Path.Combine(directory, name + extension);
                try
                {
                    var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 81920, FileOptions.SequentialScan);
                    return (path, stream);
                }
                catch (IOException) when (File.Exists(path))
                {
                    // Name taken; try the next suffix. Existing evidence is never overwritten.
                }
            }
            throw new IOException("Could not create a unique archive file name in " + directory + ".");
        }

        static string SafeHostName(Uri uri)
        {
            var sb = new StringBuilder();
            foreach (char c in uri.IdnHost.ToLowerInvariant())
            {
                sb.Append(char.IsAsciiLetterOrDigit(c) || c == '-' || c == '.' ? c : '_');
                if (sb.Length >= 60)
                {
                    break;
                }
            }
            return sb.Length == 0 ? "page" : sb.ToString();
        }
    }
}
