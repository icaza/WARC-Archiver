using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using WarcArchiver.Core.Config;
using WarcArchiver.Core.Http;

namespace WarcArchiver.Core.Net;

/// <summary>
/// Performs single HTTP/1.1 GET requests and returns the byte-exact wire capture. Each request gets its own
/// connection (and handler) so the captured bytes map one-to-one onto a request/response pair, TLS evidence is
/// attributable, and nothing is reused or cached between captures.
/// </summary>
public sealed class HttpFetcher
{
    const string AcceptHeader = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8";

    readonly AppConfig _config;
    readonly IWebProxy? _proxy;

    public HttpFetcher(AppConfig config)
    {
        _config = config;
        if (!string.IsNullOrWhiteSpace(config.Network.ProxyUrl)
            && Uri.TryCreate(config.Network.ProxyUrl, UriKind.Absolute, out var proxyUri)
            && proxyUri.Scheme is "http" or "https")
        {
            _proxy = new WebProxy(proxyUri);
        }
    }

    public long MaxResourceBytes => _config.Limits.MaxResourceMegabytes * 1024L * 1024L;

    public async Task<CapturedExchange> FetchAsync(Uri uri, Uri? referrer, CancellationToken cancellationToken)
    {
        var net = _config.Network;
        var exchange = new CapturedExchange(uri, _config.Limits.MaxMemoryKilobytesPerBuffer * 1024)
        {
            StartUtc = DateTime.UtcNow,
        };
        var tls = new TlsEvidence();
        RecordingStream? recorder = null;
        var stopwatch = Stopwatch.StartNew();

        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            UseProxy = _proxy is not null,
            Proxy = _proxy,
            ConnectTimeout = TimeSpan.FromSeconds(net.ConnectTimeoutSeconds),
            MaxResponseHeadersLength = 64,
            SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                ApplicationProtocols = [SslApplicationProtocol.Http11],
                RemoteCertificateValidationCallback = (_, certificate, chain, errors) =>
                {
                    CaptureTls(tls, certificate, chain, errors);
                    return errors == SslPolicyErrors.None || net.AllowInvalidCertificates;
                },
            },
            PlaintextStreamFilter = (context, _) =>
            {
                recorder = new RecordingStream(context.PlaintextStream, exchange.Request, exchange.Response, MaxResourceBytes);
                return ValueTask.FromResult<Stream>(recorder);
            },
        };

        if (_proxy is null)
        {
            handler.ConnectCallback = (context, token) => ConnectAsync(context, exchange, token);
        }

        using var client = new HttpClient(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(net.RequestTimeoutSeconds));

        try
        {
            using var request = BuildRequest(uri, referrer);
            using HttpResponseMessage response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            await using Stream body = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            await body.CopyToAsync(Stream.Null, 81920, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            exchange.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            exchange.Error = Describe(ex, tls, net.AllowInvalidCertificates, timeout.IsCancellationRequested);
            if (recorder?.LimitExceeded == true)
            {
                exchange.Truncated = "length";
            }
            else if (timeout.IsCancellationRequested)
            {
                exchange.Truncated = "time";
            }
            else if (exchange.Response.Length > 0)
            {
                exchange.Truncated = "disconnect";
            }
        }

        stopwatch.Stop();
        exchange.Duration = stopwatch.Elapsed;
        if (tls.PeerSubject is not null || tls.PeerSha256Hex is not null)
        {
            exchange.Tls = tls;
        }
        exchange.Head = ParseHead(exchange);
        return exchange;
    }

    HttpRequestMessage BuildRequest(Uri uri, Uri? referrer)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri)
        {
            Version = HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };
        request.Headers.TryAddWithoutValidation("User-Agent", _config.Network.UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", AcceptHeader);
        request.Headers.TryAddWithoutValidation("Accept-Language", _config.Network.AcceptLanguage);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        request.Headers.ConnectionClose = true;
        if (referrer is not null && !(referrer.Scheme == Uri.UriSchemeHttps && uri.Scheme == Uri.UriSchemeHttp))
        {
            request.Headers.Referrer = new Uri(referrer.GetLeftPart(UriPartial.Query));
        }
        return request;
    }

    async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CapturedExchange exchange, CancellationToken token)
    {
        DnsEndPoint endpoint = context.DnsEndPoint;
        IPAddress[] addresses = IPAddress.TryParse(endpoint.Host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(endpoint.Host, token).ConfigureAwait(false);

        exchange.ResolvedAddresses.AddRange(addresses.Select(a => a.ToString()));
        if (addresses.Length == 0)
        {
            throw new IOException("DNS returned no addresses for " + endpoint.Host + ".");
        }

        Exception? lastFailure = null;
        IPAddress? blocked = null;
        foreach (IPAddress address in addresses)
        {
            if (!_config.Network.AllowPrivateNetworks && !NetworkPolicy.IsPubliclyRoutable(address))
            {
                blocked ??= address;
                continue;
            }

            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                connectTimeout.CancelAfter(TimeSpan.FromSeconds(_config.Network.ConnectTimeoutSeconds));
                await socket.ConnectAsync(new IPEndPoint(address, endpoint.Port), connectTimeout.Token).ConfigureAwait(false);
                exchange.RemoteIp = address;
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is SocketException || (ex is OperationCanceledException && !token.IsCancellationRequested))
            {
                socket.Dispose();
                lastFailure = ex;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        if (blocked is not null && lastFailure is null)
        {
            throw new NetworkPolicyException(
                $"Blocked by network policy: {endpoint.Host} resolves to {blocked}, which is not a public internet address. " +
                "Set network.allowPrivateNetworks to true in config.json to archive internal pages.");
        }
        throw new IOException("Could not connect to " + endpoint.Host + ":" + endpoint.Port + ".", lastFailure);
    }

    static void CaptureTls(TlsEvidence tls, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        tls.PolicyErrors = errors.ToString();
        if (certificate is not null)
        {
            using var leaf = new X509Certificate2(certificate);
            tls.PeerSubject = leaf.Subject;
            tls.PeerIssuer = leaf.Issuer;
            tls.PeerSerial = leaf.SerialNumber;
            tls.NotBeforeUtc = leaf.NotBefore.ToUniversalTime();
            tls.NotAfterUtc = leaf.NotAfter.ToUniversalTime();
            tls.PeerSha256Hex = Convert.ToHexStringLower(leaf.GetCertHash(HashAlgorithmName.SHA256));
            tls.PeerDerBase64 = Convert.ToBase64String(leaf.RawData);
        }
        if (chain is not null)
        {
            tls.Chain.Clear();
            foreach (X509ChainElement element in chain.ChainElements)
            {
                tls.Chain.Add(element.Certificate.Subject + " [sha256 " +
                    Convert.ToHexStringLower(element.Certificate.GetCertHash(HashAlgorithmName.SHA256)) + "]");
            }
        }
    }

    static HttpHead? ParseHead(CapturedExchange exchange)
    {
        if (exchange.Response.Length == 0)
        {
            return null;
        }
        int count = (int)Math.Min(exchange.Response.Length, 65536 + 8);
        byte[] prefix = new byte[count];
        exchange.Response.Position = 0;
        exchange.Response.ReadExactly(prefix);
        exchange.Response.Position = 0;
        return HttpHead.TryParseFinal(prefix);
    }

    static string Describe(Exception ex, TlsEvidence tls, bool allowInvalid, bool timedOut)
    {
        if (timedOut)
        {
            return "The request timed out.";
        }

        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            switch (e)
            {
                case NetworkPolicyException policy:
                    return policy.Message;
                case AuthenticationException auth:
                    string detail = tls.HasPolicyErrors && !allowInvalid
                        ? $"The server certificate was rejected ({tls.PolicyErrors})."
                        : "TLS handshake failed: " + auth.Message;
                    return detail;
                case SocketException socket:
                    return "Network error: " + socket.SocketErrorCode + ".";
            }
        }

        Exception root = ex;
        while (root.InnerException is not null)
        {
            root = root.InnerException;
        }
        return root.Message;
    }
}
