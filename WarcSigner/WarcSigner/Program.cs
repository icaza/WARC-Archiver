using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class Program
{
    // The real DigiCert RFC 3161 endpoint (digicert.com itself is just their website).
    const string DefaultTsaUrl = "http://timestamp.digicert.com";

    // id-kp-timeStamping: RFC 3161 requires the TSA certificate to carry this EKU.
    const string TimeStampingEkuOid = "1.3.6.1.5.5.7.3.8";

    // A TimeStampResp is a few KB (token + certificates). 1 MiB is generous and
    // prevents a hostile or misconfigured server from filling memory.
    const long MaxResponseBytes = 1024 * 1024;

    const long MaxConfigBytes = 64 * 1024;

    static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);
    static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    sealed record Options(string ManifestPath, string? TsaUrl, bool AllowUntrusted, string? CaPath, string? ConfigPath);

    sealed record ChainResult(
        bool Trusted,
        bool RevocationUnknownOnly,
        List<string> Path,
        List<string> Problems);

    // ---------------------------------------------------------------------
    // External configuration file (JSON)
    // ---------------------------------------------------------------------

    class SecretSource
    {
        // Plain-text password in the file (works, but a warning is printed).
        public string? Password { get; set; }

        // Name of an environment variable holding the password (recommended).
        public string? PasswordEnv { get; set; }
    }

    sealed class BasicAuthConfig : SecretSource
    {
        public string? Username { get; set; }
    }

    sealed class ClientCertificateConfig : SecretSource
    {
        public string? PfxPath { get; set; }
    }

    sealed class ConfigFile
    {
        public string? TsaUrl { get; set; }
        public string? RootCertificate { get; set; }
        public BasicAuthConfig? Authentication { get; set; }
        public ClientCertificateConfig? ClientCertificate { get; set; }
    }

    static async Task<int> Main(string[] args)
    {
        // Console setup must never prevent the tool from running (no console attached, redirected handles...).
        try { Console.OutputEncoding = new UTF8Encoding(false); } catch (IOException) { }
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        if (!TryParseArgs(args, out Options? options) || options is null)
        {
            PrintUsage();
            return 1;
        }

        try
        {
            return await RunAsync(options);
        }
        catch (Exception ex)
        {
            return Fail(DescribeException(ex));
        }
    }

    static async Task<int> RunAsync(Options opt)
    {
        // =================================================================
        // 1. CONFIGURATION AND INPUTS (nothing is written until everything is verified)
        // =================================================================
        ConfigFile? config = null;
        string configDir = Directory.GetCurrentDirectory();
        if (opt.ConfigPath is not null)
        {
            config = LoadConfig(opt.ConfigPath, out configDir);
        }

        bool hasCredentials = config?.Authentication is not null || config?.ClientCertificate is not null;

        // Credentials are bound to the TSA named in the file: they can never follow a typo or an
        // override on the command line to another server.
        if (hasCredentials && string.IsNullOrWhiteSpace(config?.TsaUrl))
        {
            return Fail("A configuration file that contains credentials must define 'tsaUrl'.");
        }
        if (config?.TsaUrl is not null && opt.TsaUrl is not null && !SameEndpoint(config.TsaUrl, opt.TsaUrl))
        {
            return Fail("The tsa_url given on the command line differs from 'tsaUrl' in the configuration file. " +
                        "Remove one of them (credentials are bound to the configured TSA).");
        }

        string tsaUrlText = opt.TsaUrl ?? config?.TsaUrl ?? DefaultTsaUrl;
        if (!Uri.TryCreate(tsaUrlText, UriKind.Absolute, out Uri? tsaUri) ||
            (tsaUri.Scheme != Uri.UriSchemeHttp && tsaUri.Scheme != Uri.UriSchemeHttps))
        {
            return Fail($"Invalid TSA URL (an absolute http:// or https:// URL is required): {tsaUrlText}");
        }
        if (hasCredentials && tsaUri.Scheme != Uri.UriSchemeHttps)
        {
            return Fail("Credentials and client certificates are only sent over https://; the TSA URL is not https.");
        }

        string manifestPath = Path.GetFullPath(opt.ManifestPath);
        if (!File.Exists(manifestPath))
        {
            return Fail($"The manifest file could not be found: {manifestPath}");
        }

        string outputPath = Path.ChangeExtension(manifestPath, ".tsr");
        if (string.Equals(outputPath, manifestPath, StringComparison.OrdinalIgnoreCase))
        {
            return Fail("The input file already has a .tsr extension; refusing to overwrite it.");
        }
        if (File.Exists(outputPath))
        {
            return Fail($"Output file already exists, refusing to overwrite existing evidence: {outputPath}");
        }

        // Trust anchor: --ca wins over the configuration file.
        string? caPath = opt.CaPath is not null
            ? Path.GetFullPath(opt.CaPath)
            : config?.RootCertificate is not null ? Path.GetFullPath(config.RootCertificate, configDir) : null;

        X509Certificate2Collection? customRoots = null;
        if (caPath is not null)
        {
            if (!File.Exists(caPath))
            {
                return Fail($"The CA certificate file could not be found: {caPath}");
            }
            try
            {
                customRoots = new X509Certificate2Collection { X509CertificateLoader.LoadCertificateFromFile(caPath) };
            }
            catch (CryptographicException)
            {
                return Fail($"Cannot read the CA certificate '{Path.GetFileName(caPath)}'. It must be a public certificate " +
                            "(.cer, .crt or .pem); a password-protected .pfx/.p12 is not a root certificate file.");
            }
        }

        // TSA authentication (HTTP Basic) and TLS client certificate, if configured.
        AuthenticationHeaderValue? authorization = null;
        string? userName = null;
        if (config?.Authentication is { } basic)
        {
            userName = basic.Username;
            if (string.IsNullOrEmpty(userName) || userName.Contains(':') || userName.Any(char.IsControl))
            {
                return Fail("'authentication.username' is missing or invalid (HTTP Basic forbids an empty name, control characters and ':').");
            }
            string password = ResolveSecret(basic, "authentication")
                              ?? PromptPassword($"Password for '{Sanitize(userName)}' on {tsaUri.Host}: ");
            authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
        }

        using X509Certificate2? clientCertificate = config?.ClientCertificate is { } clientConfig
            ? LoadClientCertificate(clientConfig, configDir)
            : null;

        // =================================================================
        // 2. HASH THE MANIFEST (streaming: constant memory whatever the size)
        // =================================================================
        Console.WriteLine($"[1/5] Hashing manifest: {Path.GetFileName(manifestPath)}");
        byte[] hash;
        await using (var input = new FileStream(
            manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81920, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            hash = await SHA256.HashDataAsync(input);
        }
        Console.WriteLine($"      SHA-256: {Convert.ToHexString(hash).ToLowerInvariant()}");

        // =================================================================
        // 3. BUILD THE REQUEST (random nonce => a response cannot be replayed)
        // =================================================================
        byte[] nonce = RandomNumberGenerator.GetBytes(8);
        nonce[0] &= 0x7F; // keep it a positive 64-bit integer, accepted by every TSA

        Rfc3161TimestampRequest request = Rfc3161TimestampRequest.CreateFromHash(
            hash,
            HashAlgorithmName.SHA256,
            nonce: nonce,
            requestSignerCertificates: true);

        // =================================================================
        // 4. NETWORK EXCHANGE
        // =================================================================
        Console.WriteLine($"[2/5] Requesting a timestamp from: {tsaUri}");
        if (tsaUri.Scheme == Uri.UriSchemeHttp)
        {
            Console.WriteLine("      [INFO] Plain HTTP: the token is signed, so it stays tamper-evident, but the exchange is not encrypted.");
        }
        if (userName is not null)
        {
            Console.WriteLine($"      [INFO] HTTP Basic authentication as '{Sanitize(userName)}'.");
        }
        if (clientCertificate is not null)
        {
            Console.WriteLine($"      [INFO] TLS client certificate: {Sanitize(clientCertificate.Subject)}");
        }
        byte[] encodedResponse = await SendRequestAsync(tsaUri, request.Encode(), authorization, clientCertificate);

        // =================================================================
        // 5. VERIFY THE RESPONSE
        // =================================================================
        Console.WriteLine("[3/5] Verifying the response (status, imprint, nonce, signature)...");

        // Checks PKIStatus, message imprint, nonce and policy against our request.
        // It does NOT verify the CMS signature nor the certificate trust.
        Rfc3161TimestampToken token = request.ProcessResponse(encodedResponse, out int bytesConsumed);
        if (bytesConsumed != encodedResponse.Length)
        {
            return Fail("Unexpected trailing data after the TSA response; refusing to store it.");
        }

        // Verifies the CMS signature and that the token seals exactly this hash.
        if (!token.VerifySignatureForHash(hash, HashAlgorithmName.SHA256, out X509Certificate2? signer, null) || signer is null)
        {
            return Fail("Signature verification failed: the token does not seal this manifest, or the TSA did not return its signing certificate.");
        }

        Rfc3161TimestampTokenInfo info = token.TokenInfo;
        DateTimeOffset genTime = info.Timestamp;

        Console.WriteLine("[OK]  Signature valid and bound to the manifest hash.");
        Console.WriteLine($"      Timestamp (UTC): {genTime.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"      Serial number  : {Convert.ToHexString(info.GetSerialNumber().Span)}");
        Console.WriteLine($"      TSA policy OID : {info.PolicyId.Value}");
        Console.WriteLine($"      TSA signer     : {Sanitize(signer.Subject)}");
        Console.WriteLine($"      Signer SHA-256 : {signer.GetCertHashString(HashAlgorithmName.SHA256)}");

        TimeSpan skew = DateTimeOffset.UtcNow - genTime;
        if (skew.Duration() > MaxClockSkew)
        {
            Console.WriteLine($"      [WARN] The TSA time differs from this computer's clock by {skew.Duration().TotalMinutes:F1} min. Check the local clock.");
        }

        // Hard requirements, independent of --allow-untrusted:
        if (genTime < new DateTimeOffset(signer.NotBefore) || genTime > new DateTimeOffset(signer.NotAfter))
        {
            return Fail("The timestamp lies outside the validity period of the TSA certificate.");
        }
        if (!HasTimeStampingEku(signer))
        {
            return Fail("The signing certificate is not a time-stamping certificate (missing id-kp-timeStamping EKU).");
        }

        // =================================================================
        // 6. TRUST CHAIN (enforced, not just displayed)
        // =================================================================
        Console.WriteLine("[4/5] Validating the TSA certificate chain...");
        ChainResult chain = EvaluateChain(signer, token.AsSignedCms().Certificates, customRoots, genTime);

        foreach (string element in chain.Path)
        {
            Console.WriteLine($"      chain: {element}");
        }

        bool trusted = chain.Trusted;
        if (chain.Trusted)
        {
            Console.WriteLine(customRoots is null
                ? "[OK]  Chain is valid and rooted in the operating system trust store."
                : "[OK]  Chain is valid and rooted in the CA certificate you supplied (--ca / rootCertificate).");
        }
        else if (chain.RevocationUnknownOnly)
        {
            trusted = true;
            Console.WriteLine("[WARN] Chain is valid, but revocation status could not be determined (offline or no CRL/OCSP).");
        }
        else
        {
            foreach (string problem in chain.Problems)
            {
                Console.Error.WriteLine($"      problem: {problem}");
            }
            if (!opt.AllowUntrusted)
            {
                return Fail("The TSA certificate chain is not trusted, nothing was written. " +
                            "Pin the TSA root with --ca <root.cer> (or 'rootCertificate' in the configuration), " +
                            "or use --allow-untrusted to accept it at your own risk.");
            }
            Console.WriteLine("[WARN] Chain NOT trusted, accepted because of --allow-untrusted. The token has no verified trust anchor.");
        }

        // =================================================================
        // 7. WRITE THE EVIDENCE (only now; never overwrites)
        // =================================================================
        Console.WriteLine($"[5/5] Saving the TSA response: {Path.GetFileName(outputPath)}");
        await WriteNewFileAsync(outputPath, encodedResponse);

        Console.WriteLine(trusted
            ? "\n[DONE] Timestamp response verified and saved."
            : "\n[DONE] Timestamp response saved, but the TSA trust was NOT established.");
        Console.WriteLine("[NOTE] This tool cannot tell whether the TSA is an eIDAS *qualified* provider; check the EU Trusted List.");
        return 0;
    }

    // ---------------------------------------------------------------------
    // Configuration and secrets
    // ---------------------------------------------------------------------

    static ConfigFile LoadConfig(string path, out string baseDirectory)
    {
        var info = new FileInfo(path);
        if (!info.Exists)
        {
            throw new FileNotFoundException($"Configuration file not found: {info.FullName}");
        }
        if (info.Length > MaxConfigBytes)
        {
            throw new InvalidDataException($"Configuration file is too large (limit {MaxConfigBytes / 1024} KB).");
        }
        baseDirectory = info.DirectoryName ?? Directory.GetCurrentDirectory();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            // A typo such as "pasword" must be an error, not a silently missing credential.
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

        try
        {
            using FileStream stream = info.OpenRead();
            return JsonSerializer.Deserialize<ConfigFile>(stream, options)
                   ?? throw new InvalidDataException("The configuration file is empty.");
        }
        catch (JsonException ex)
        {
            // ex.Message can expose internal type names; the JSON path and line are what the user needs.
            string where = ex.Path is { Length: > 0 } ? $" at '{Sanitize(ex.Path)}'" : string.Empty;
            string line = ex.LineNumber is { } n ? $" (line {n + 1})" : string.Empty;
            throw new InvalidDataException(
                $"Invalid configuration file{where}{line}: unknown key, wrong value type or JSON syntax error. " +
                "Allowed keys: tsaUrl, rootCertificate, authentication{username,password,passwordEnv}, clientCertificate{pfxPath,password,passwordEnv}.");
        }
    }

    // Returns the secret from the file or the environment, or null when the user must be asked.
    static string? ResolveSecret(SecretSource source, string section)
    {
        if (source.Password is not null && source.PasswordEnv is not null)
        {
            throw new InvalidDataException($"'{section}': set either 'password' or 'passwordEnv', not both.");
        }
        if (source.Password is not null)
        {
            Console.Error.WriteLine($"[WARN] '{section}': plain-text password in the configuration file. Prefer 'passwordEnv' and keep the file out of version control.");
            return source.Password;
        }
        if (source.PasswordEnv is not null)
        {
            string? value = Environment.GetEnvironmentVariable(source.PasswordEnv);
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException($"'{section}': environment variable '{source.PasswordEnv}' is not set or is empty.");
            }
            return value;
        }
        return null;
    }

    static X509Certificate2 LoadClientCertificate(ClientCertificateConfig config, string configDirectory)
    {
        if (string.IsNullOrWhiteSpace(config.PfxPath))
        {
            throw new InvalidDataException("'clientCertificate.pfxPath' is required.");
        }
        string pfxPath = Path.GetFullPath(config.PfxPath, configDirectory);
        if (!File.Exists(pfxPath))
        {
            throw new FileNotFoundException($"Client certificate file not found: {pfxPath}");
        }

        string? password = ResolveSecret(config, "clientCertificate");
        X509Certificate2 certificate;
        if (password is null)
        {
            try
            {
                certificate = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, (string?)null);
            }
            catch (CryptographicException)
            {
                // Most likely password-protected: ask for it (fails cleanly when input is redirected).
                certificate = X509CertificateLoader.LoadPkcs12FromFile(
                    pfxPath, PromptPassword($"Password for {Path.GetFileName(pfxPath)}: "));
            }
        }
        else
        {
            certificate = X509CertificateLoader.LoadPkcs12FromFile(pfxPath, password);
        }

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();
            throw new InvalidDataException("The client certificate file does not contain a private key.");
        }
        return certificate;
    }

    // Masked prompt on stderr. Refuses to hang when there is nobody to answer (CI, pipes).
    static string PromptPassword(string prompt)
    {
        if (Console.IsInputRedirected)
        {
            throw new InvalidOperationException(
                "A password is required but input is redirected. Set 'passwordEnv' in the configuration file.");
        }

        Console.Error.Write(prompt);
        var password = new StringBuilder();
        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0) password.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
        }
        Console.Error.WriteLine();
        return password.ToString();
    }

    static bool SameEndpoint(string a, string b)
    {
        return Uri.TryCreate(a, UriKind.Absolute, out Uri? ua)
            && Uri.TryCreate(b, UriKind.Absolute, out Uri? ub)
            && Uri.Compare(ua, ub, UriComponents.HttpRequestUrl, UriFormat.UriEscaped, StringComparison.OrdinalIgnoreCase) == 0;
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    static async Task<byte[]> SendRequestAsync(Uri tsaUri, byte[] body, AuthenticationHeaderValue? authorization, X509Certificate2? clientCertificate)
    {
        using var handler = new SocketsHttpHandler
        {
            // A redirected POST is silently turned into a GET (and the body dropped), and a redirect could
            // forward credentials to another host: fail loudly instead.
            AllowAutoRedirect = false,
            ConnectTimeout = TimeSpan.FromSeconds(10),
        };
        if (clientCertificate is not null)
        {
            handler.SslOptions.ClientCertificates = new X509CertificateCollection { clientCertificate };
        }

        using var http = new HttpClient(handler)
        {
            Timeout = HttpTimeout,
            MaxResponseContentBufferSize = MaxResponseBytes,
        };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("WarcSigner/1.0");
        http.DefaultRequestHeaders.Accept.ParseAdd("application/timestamp-reply");

        using var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");

        using var message = new HttpRequestMessage(HttpMethod.Post, tsaUri) { Content = content };
        message.Headers.Authorization = authorization;

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(message);
        }
        catch (HttpRequestException ex) when (clientCertificate is null && tsaUri.Scheme == Uri.UriSchemeHttps
                                              && ex.InnerException is IOException)
        {
            throw new HttpRequestException(
                "The TLS connection was closed during the handshake. If this TSA requires a client certificate, " +
                "configure 'clientCertificate' (see --config).", ex);
        }

        using (response)
        {
            return await ReadResponseAsync(response);
        }
    }

    static async Task<byte[]> ReadResponseAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            throw new HttpRequestException(
                $"The TSA refused the request ({(int)response.StatusCode} {response.StatusCode}). " +
                "It may require authentication (see --config) or the credentials were rejected.");
        }
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    static ChainResult EvaluateChain(
        X509Certificate2 signer,
        X509Certificate2Collection tokenCertificates,
        X509Certificate2Collection? customRoots,
        DateTimeOffset verificationTime)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
        chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(10);
        chain.ChainPolicy.VerificationTime = verificationTime.UtcDateTime; // valid when the stamp was issued
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(TimeStampingEkuOid));

        // Intermediates shipped inside the token must be usable for chain building.
        chain.ChainPolicy.ExtraStore.AddRange(tokenCertificates);

        if (customRoots is not null)
        {
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(customRoots);
        }

        bool built = chain.Build(signer);

        var path = new List<string>();
        foreach (X509ChainElement element in chain.ChainElements)
        {
            path.Add($"{Sanitize(element.Certificate.Subject)} [{element.Certificate.GetCertHashString(HashAlgorithmName.SHA256)[..16]}...]");
        }

        var problems = new List<string>();
        foreach (X509ChainStatus status in chain.ChainStatus)
        {
            problems.Add($"{status.Status}: {Sanitize(status.StatusInformation.Trim())}");
        }

        const X509ChainStatusFlags soft =
            X509ChainStatusFlags.RevocationStatusUnknown | X509ChainStatusFlags.OfflineRevocation;
        bool softOnly = !built && chain.ChainStatus.All(s => (s.Status & ~soft) == X509ChainStatusFlags.NoError);

        return new ChainResult(built, softOnly, path, problems);
    }

    static bool HasTimeStampingEku(X509Certificate2 certificate)
    {
        foreach (X509Extension extension in certificate.Extensions)
        {
            if (extension is X509EnhancedKeyUsageExtension eku)
            {
                foreach (Oid oid in eku.EnhancedKeyUsages)
                {
                    if (oid.Value == TimeStampingEkuOid) return true;
                }
            }
        }
        return false;
    }

    // CreateNew fails atomically if the file exists, so existing evidence can never be overwritten.
    static async Task WriteNewFileAsync(string path, byte[] data)
    {
        var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        try
        {
            await using (stream)
            {
                await stream.WriteAsync(data);
                stream.Flush(flushToDisk: true);
            }
        }
        catch
        {
            // We created this file just above, so removing a partial write is safe.
            try { File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    // Strings coming from a remote server or certificate must not be able to inject terminal control sequences.
    static string Sanitize(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (char c in value) sb.Append(char.IsControl(c) ? '?' : c);
        return sb.ToString();
    }

    static string DescribeException(Exception ex)
    {
        string text = $"{ex.GetType().Name}: {Sanitize(ex.Message)}";
        if (ex.InnerException is not null)
        {
            text += $" ({Sanitize(ex.InnerException.Message)})";
        }
        return text;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine($"[FAIL] {message}");
        return 1;
    }

    static bool TryParseArgs(string[] args, out Options? options)
    {
        options = null;
        var positional = new List<string>();
        bool allowUntrusted = false;
        string? caPath = null;
        string? configPath = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--allow-untrusted":
                    allowUntrusted = true;
                    break;
                case "--ca":
                    if (i + 1 >= args.Length) return false;
                    caPath = args[++i];
                    break;
                case "--config":
                    if (i + 1 >= args.Length) return false;
                    configPath = args[++i];
                    break;
                default:
                    if (args[i].StartsWith("--", StringComparison.Ordinal)) return false;
                    positional.Add(args[i]);
                    break;
            }
        }

        if (positional.Count < 1 || positional.Count > 2) return false;
        options = new Options(positional[0], positional.Count > 1 ? positional[1] : null, allowUntrusted, caPath, configPath);
        return true;
    }

    static void PrintUsage()
    {
        Console.Error.WriteLine("Usage:   WarcSigner <manifest_path> [tsa_url] [--config <file.json>] [--ca <root.cer>] [--allow-untrusted]");
        Console.Error.WriteLine($"Default TSA: {DefaultTsaUrl}");
        Console.Error.WriteLine("  --config <file>    JSON file with the TSA URL, root certificate and credentials (see README).");
        Console.Error.WriteLine("  --ca <file>        Trust only this root certificate (PEM or DER) for the TSA chain.");
        Console.Error.WriteLine("  --allow-untrusted  Save the token even if the TSA chain is not trusted (not recommended).");
        Console.Error.WriteLine("Exit codes: 0 = success, 1 = any failure.");
    }
}
