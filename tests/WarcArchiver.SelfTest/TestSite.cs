using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WarcArchiver.SelfTest;

/// <summary>A tiny local web site that exercises every code path of the capture engine.</summary>
internal sealed class TestSite : IAsyncDisposable
{
    static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    WebApplication? _app;

    public string HttpBase { get; private set; } = string.Empty;

    public string HttpsBase { get; private set; } = string.Empty;

    public static byte[] BigPayload { get; } = MakeBig();

    /// <summary>SHA-256 (hex) of the HTTPS server certificate, to compare with the TLS evidence stored in the archive.</summary>
    public string ServerCertSha256 { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(san.Build());
        using X509Certificate2 ephemeral = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        X509Certificate2 serverCert = X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pfx, "t"), "t");
        ServerCertSha256 = Convert.ToHexStringLower(serverCert.GetCertHash(HashAlgorithmName.SHA256));

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(k =>
        {
            k.Listen(IPAddress.Loopback, 0);
            k.Listen(IPAddress.Loopback, 0, o =>
            {
                o.Protocols = HttpProtocols.Http1;
                o.UseHttps(serverCert);
            });
        });
        _app = builder.Build();
        MapRoutes(_app);
        await _app.StartAsync();

        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
        HttpBase = addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal)).TrimEnd('/');
        HttpsBase = addresses.First(a => a.StartsWith("https://", StringComparison.Ordinal)).TrimEnd('/');
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    static byte[] MakeBig()
    {
        var data = new byte[3 * 1024 * 1024];
        new Random(12345).NextBytes(data);
        return data;
    }

    static void MapRoutes(WebApplication app)
    {
        app.MapGet("/", async ctx =>
        {
            ctx.Response.ContentType = "text/html; charset=utf-8";
            await ctx.Response.WriteAsync("""
                <!doctype html><html><head><meta charset="utf-8"><title>Test page</title>
                <!-- <img src="/in-comment.png"> -->
                <link rel="stylesheet" href="/style.css">
                <link rel="icon" href="/logo.png">
                <script src="/app.js"></script>
                <script src="/chunked.js"></script>
                <script>var s = "<img src='/in-script.png'>";</script>
                <style>.x{background:url('/inline-bg.png')}</style></head>
                <body>
                <img src="/logo.png" srcset="/logo.png 1x, /logo2.png?a=1&amp;b=2 2x" alt="x">
                <iframe src="/frame.html"></iframe>
                <div style="background:url(/style-attr.png)"></div>
                <a href="/other-page.html">not fetched</a>
                <img src="data:image/png;base64,AAAA">
                <img src="/missing.png">
                <img src="/redir">
                <img src="/big.png">
                </body></html>
                """);
        });

        app.MapGet("/style.css", async ctx =>
        {
            ctx.Response.ContentType = "text/css";
            ctx.Response.Headers.ContentEncoding = "gzip";
            await using var gz = new GZipStream(ctx.Response.Body, CompressionLevel.Fastest, leaveOpen: true);
            await gz.WriteAsync(Encoding.UTF8.GetBytes(
                "@import \"imported.css\";\nbody{background:url(bg.png)}\n@font-face{src:url(\"font.woff2\") format(\"woff2\")}\n/* url(/in-css-comment.png) */"));
        });
        app.MapGet("/imported.css", async ctx =>
        {
            ctx.Response.ContentType = "text/css";
            await ctx.Response.WriteAsync("h1{background:url(/deep.png)}");
        });
        app.MapGet("/app.js", async ctx =>
        {
            ctx.Response.ContentType = "application/javascript";
            await ctx.Response.WriteAsync("console.log('app loaded');");
        });
        app.MapGet("/chunked.js", async ctx =>
        {
            ctx.Response.ContentType = "application/javascript";
            await ctx.Response.WriteAsync("console.log('part one');");
            await ctx.Response.Body.FlushAsync();
            await ctx.Response.WriteAsync("console.log('part two');");
        });
        app.MapGet("/frame.html", async ctx =>
        {
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync("<html><body>frame</body></html>");
        });
        app.MapGet("/redir", ctx =>
        {
            ctx.Response.Redirect("/redir-target.png");
            return Task.CompletedTask;
        });
        app.MapGet("/big.png", async ctx =>
        {
            ctx.Response.ContentType = "image/png";
            ctx.Response.ContentLength = BigPayload.Length;
            await ctx.Response.Body.WriteAsync(BigPayload);
        });
        app.MapGet("/slow", async ctx =>
        {
            ctx.Response.ContentType = "text/html";
            await Task.Delay(TimeSpan.FromSeconds(20), ctx.RequestAborted);
        });
        app.MapGet("/missing.png", ctx =>
        {
            ctx.Response.StatusCode = 404;
            return ctx.Response.WriteAsync("not found");
        });
        app.MapGet("/not-html", async ctx =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync("{\"ok\":true}");
        });

        foreach (string path in new[] { "/logo.png", "/logo2.png", "/bg.png", "/deep.png", "/inline-bg.png", "/style-attr.png", "/redir-target.png" })
        {
            app.MapGet(path, async ctx =>
            {
                ctx.Response.ContentType = "image/png";
                await ctx.Response.Body.WriteAsync(Png);
            });
        }
        app.MapGet("/font.woff2", async ctx =>
        {
            ctx.Response.ContentType = "font/woff2";
            await ctx.Response.Body.WriteAsync(new byte[] { 1, 2, 3, 4, 5 });
        });
    }
}
