# WARC Archiver — Web Evidence Capture

A Windows desktop application (C# 14 / .NET 10, WinForms, dark theme, en-US) that preserves a web page **and everything it
loads** as a standards-based **WARC 1.1 archive (ISO 28500)**, fingerprints it with **SHA-256 and SHA-512**, records the capture in a
**tamper-evident audit log**, and can **sign** the evidence with an X.509 certificate. A built-in verifier re-checks everything
independently.

> Why not PDF or a screenshot? They show what a page *looked like*. A WARC keeps the raw HTTP exchange — request and response
> headers, HTML, scripts, stylesheets, images — byte for byte, so the content can be replayed and checked later.

## What you get for each capture

Everything lands in the output folder (`outputDirectory` in `config.json`):

| File | Purpose |
|---|---|
| `20261010-125353_example.com.warc.gz` | The archive: one gzip member per record (`warcinfo`, `request`, `response`, `metadata`). Plain `.warc` if `compressWarc` is `false`. |
| `….manifest.json` | Case reference, operator, notes, local time **with UTC offset**, UTC time, time zone, server `Date` header and clock skew, settings used, every record with offset/length and **both** digests, TLS evidence, failed/skipped resources, audit-log anchor. |
| `….sha256`, `….sha512` | Checksum files in `sha256sum -c` / `sha512sum -c` format (archive + manifest). |
| `….manifest.json.sig` | Only when a certificate was chosen: detached signature over the manifest. |
| `audit-log.jsonl` | One log per output folder. Each entry contains the hash of the previous one (see below). |

All finished files are marked read-only (`security.markFilesReadOnly`).

## Build and run

Requirements: Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build WarcArchiver.slnx -c Release
dotnet run --project src/WarcArchiver.App -c Release

# a small folder to hand to someone else (needs the .NET 10 Desktop Runtime on the target machine)
dotnet publish src/WarcArchiver.App -c Release -r win-x64 --self-contained false -o publish   # or: .\publish.cmd
```

### "You must install .NET Desktop Runtime to run this application"

The small build needs the **.NET Desktop Runtime 10 (x64)** on the PC that runs it. Check with `dotnet --list-runtimes`: it must list
`Microsoft.WindowsDesktop.App 10.0.x`. (The plain ".NET Runtime" and the "ASP.NET Core Runtime" do **not** include it, and an x86
install does not satisfy an x64 program.) Either install it from <https://dotnet.microsoft.com/download/dotnet/10.0> or build a
copy that carries its own runtime and needs nothing installed:

```powershell
.\publish.cmd portable      # about 150 MB, runs anywhere on Windows 10/11 x64
.\publish.cmd               # small build, needs the Desktop Runtime
```

Self-test of the engine (starts a local HTTP+HTTPS test site; works on Windows and Linux):

```
dotnet run --project tests/WarcArchiver.SelfTest -- <scratch-folder>
```

## Using the app

* **Capture** – paste a URL, optionally fill in case reference / operator / notes, choose what to include and (optionally) a signing
  certificate, then *Start capture*. Every request appears live in the table. *Cancel* discards the unfinished archive.
* **Archives** – everything found in the output folder; double-click to verify, copy the SHA-256, filter, open the folder.
* **Verify** – drop a `.warc`, `.warc.gz` or `.manifest.json` onto the page. The report can be exported as text.
* **Audit log** – browse the log, re-compute the chain, copy the *head hash*.
* **Settings** – shows the live `config.json`, opens it in your editor, reloads it, changes the output folder without losing your comments.

## Editing the interface in Visual Studio

Open `MainForm.cs` in the designer: every control, colour, font, margin and event hook is a plain property assignment in
`MainForm.Designer.cs`, so you can move, resize, restyle and save freely — what the designer shows is what runs.

* **Keep `InitializeComponent()` pure.** Code added there (helper methods, loops, `var`, calls into `DarkTheme`) is not
  understood by the designer and is deleted the next time the form is saved, which is what leaves buttons blank and
  layouts collapsed. Put your own code in `MainForm.cs`, after the `InitializeComponent()` call.
* **The five pages are stacked.** In the designer only the Capture page is on top. To work on another page open
  *View → Other Windows → Document Outline*, select e.g. `archivesPage`, right-click → *Bring to Front*. The z-order is
  irrelevant at run time (the sidebar code decides which page is visible).
* **Colours live in the designer file** (palette: background `18,20,24`, surface `26,29,35`, raised `34,38,46`, border `52,58,68`,
  text `230,232,235`, muted `154,163,175`, accent `76,154,255`). `Theme/DarkTheme.cs` only adds what a designer file cannot store:
  the dark status-bar renderer, the dimmed look of disabled buttons, double-buffered grids and dark scroll bars / title bar.
* `Button.Tag` is meaningful: `"nav"` for the sidebar buttons, `"primary"` for the blue call-to-action buttons.
* The *Version* text, the page switching and the certificate list are filled in by code; do not type them in the designer.

## Configuration (`config.json`)

The file sits next to `WarcArchiver.exe` (or in `%LOCALAPPDATA%\WarcArchiver` if that folder is read-only). Comments and trailing
commas are allowed. Out-of-range numbers are clamped. A file with a syntax error is **never overwritten**: the app starts with
defaults, tells you so, and on *Reload* keeps the previous working settings.

| Key | Default | Meaning |
|---|---|---|
| `outputDirectory` | `%USERPROFILE%\Documents\WARC Archives` | Where evidence and the audit log go. `%VARS%` are expanded. |
| `compressWarc` | `true` | `.warc.gz` (per-record gzip) or plain `.warc`. |
| `warcPrimaryDigest` | `"sha256"` | Digest in the WARC headers (`"sha256"` / `"sha512"`). Manifest and checksum files always carry both. |
| `network.userAgent`, `acceptLanguage` | Chrome-like / `en-US` | Sent with every request. |
| `network.connectTimeoutSeconds`, `requestTimeoutSeconds`, `maxRedirects` | 15 / 90 / 10 | Redirects are followed manually and every hop is archived. |
| `network.maxParallelDownloads` | 4 (1–16) | Lower = lighter on your PC and on the site. |
| `network.allowPrivateNetworks` | `false` | Loopback, private, link-local, CGNAT, ULA… are refused, also after redirects. |
| `network.allowInvalidCertificates` | `false` | `true` captures a site with a bad certificate and records the problem as evidence. |
| `network.proxyUrl` | `""` | Optional HTTP proxy (the private-network check cannot see behind a proxy). |
| `limits.*` | 500 resources, 50 MB each, 1000 MB total, CSS depth 2 | Safety limits; hitting one is recorded in the manifest. |
| `defaults.*` | all `true` | Initial state of the Capture-page checkboxes; `operatorName` pre-fills the operator field. |
| `signing.certificateThumbprint` | `""` | Certificate (with private key) in *Current User → Personal* used by default. |
| `security.markFilesReadOnly` | `true` | Read-only attribute on finished files. |

## Checking evidence without this application

The point of an open format is that nobody has to trust this program.

```bash
sha256sum -c 20261010-125353_example.com.sha256          # file fingerprints
sha512sum -c 20261010-125353_example.com.sha512
gzip -t 20261010-125353_example.com.warc.gz               # every gzip member is intact
pip install warcio && warcio check 20261010-125353_example.com.warc.gz   # record digests (WARC-Block/Payload-Digest)
```

Signature: the `.sig` file is JSON (`signature` = base64, `signerCertificateDer` = base64 DER). Verify the signature over the
**exact bytes** of the manifest with the embedded certificate (RSA PKCS#1 v1.5 or ECDSA IEEE-P1363, hash named in `hashAlgorithm`), e.g. with OpenSSL.

## Chain of custody — how the pieces fit

* **Records** carry `WARC-Block-Digest` and `WARC-Payload-Digest` (`sha256:BASE32`). The payload digest is taken over the entity
  body exactly as the server sent it (de-chunked, content-encoding kept), the convention used by warcio and the Wayback Machine.
* **Manifest** repeats both digests for every record and the digests of the WARC file itself.
* **Audit log** – each entry includes a hash over its fields **and the previous entry's hash**. Editing, deleting, inserting or
  re-ordering any entry breaks every later link. The manifest stores the log position and hash at capture time.
  *Copy the head hash from the Audit page and send it to a third party (e-mail, notary): that anchors the entire history at that moment.*
  A damaged log is never overwritten; it is set aside with its SHA-256 recorded in the new log.
* **Signature** – proves that the holder of a certificate vouches for the manifest, which in turn pins every record.
* Verification runs are themselves logged (`verify.run`).

## Security and resource design

* TLS 1.2/1.3 only; certificate errors stop a capture unless you opt out. URLs with credentials are refused; only `http`/`https`.
* Private/loopback/link-local targets are blocked at connection time (after DNS), so DNS tricks and redirects cannot reach them.
* Files are created with `CreateNew` (never overwrite), header/field injection is stripped, config values are clamped, and a
  single-instance mutex prevents two processes from writing to one audit log.
* No browser engine is embedded. Downloads stream through small buffers (spilling to a self-deleting temp file when large), the
  GC runs in workstation non-concurrent mode with `ConserveMemory`, and `InvariantGlobalization` avoids loading ICU.

## Limitations — please read before relying on this in a dispute

* **The DOM is the HTML as served.** Scripts are archived but not executed, so content a page builds with JavaScript after loading
  is not in the archive, and pages that require a login are not reachable. (A headless-browser capture could be added later.)
* **Time is the PC's clock.** The manifest also records the server's `Date` header and the difference, but the signing time is
  self-asserted. For independent proof of *when*, add an RFC 3161 trusted timestamp to the manifest — the natural next step.
* **"SHA-256/SHA-512 signature"** here means cryptographic fingerprints plus an optional X.509 signature over the manifest.
* The signing certificate must be in the Windows *Personal* store (import a `.pfx` there); an expired certificate is flagged in the list.
* Whether an archive is admissible, and what additional procedure (witness, bailiff/huissier, notary) is expected, depends on your
  jurisdiction. This tool produces the technical evidence; it is not legal advice.

## Project layout

```
src/WarcArchiver.Core      engine: WARC writer/reader, HTTP capture, parsing, audit log, signing, verifier  (net10.0)
src/WarcArchiver.App       the WinForms UI; every control is declared in MainForm.Designer.cs              (net10.0-windows)
tests/WarcArchiver.SelfTest  155 end-to-end checks against a local HTTP/HTTPS site
```
