# WARC Archiver — Web archiving with evidentiary value

### Part of the Ostium toolset

WARC-Archiver is one of the Ostium tools — see also
[Ostium OSINT Browser](https://github.com/icaza/Ostium-Osint-Browser).

**Language:** [Français](README_fr.md) · English (en-US)

A Windows application that **preserves a web page and everything it loads**
as a **WARC 1.1 (ISO 28500)** archive, **seals it with two cryptographic digests (SHA-256 and SHA-512)**, records
the capture in a **hash-chained audit log** and can **sign** it with an X.509 certificate. A built-in verifier — and third-party
tools — let anyone re-check everything, without having to trust the program.

> **Why not a PDF or a screenshot?** They show what a page *looked like*. A WARC archive preserves the HTTP exchange
> itself — request and response, raw headers, HTML, scripts, stylesheets, images — byte for byte, together with the date, the server's IP address
> and the TLS certificate presented at that moment. The content can be re-read, replayed and **mathematically checked** years later.

## Table of contents

1. [What the application does](#1-what-the-application-does)
2. [Why this archive can be trusted](#2-why-this-archive-can-be-trusted)
3. [Legal framework (France / European Union)](#3-legal-framework-france--european-union)
4. [Recommended protocol to strengthen evidentiary value](#4-recommended-protocol-to-strengthen-evidentiary-value)
5. [Verifying an archive without the application](#5-verifying-an-archive-without-the-application)
6. [Limits — read before relying on it in a dispute](#6-limits--read-before-relying-on-it-in-a-dispute)
7. [Installation and launch](#7-installation-and-launch)
8. [Using the interface](#8-using-the-interface)
9. [Configuration (`config.json`)](#9-configuration-configjson)
10. [Security and resource footprint](#10-security-and-resource-footprint)
11. [For developers](#11-for-developers)
12. [Glossary](#12-glossary)

---

## 1. What the application does

### 1.1 How a capture runs

You enter the address of a page, optionally a case reference, your name and a note, then click **Start capture**.

1. **Address check.** Only `http://` and `https://` addresses are accepted. An address containing a user name or
   password is refused (the credentials would end up in the archive).
2. **Audit log entry** (`capture.start`): URL, archive name, case reference, operator, digest of the `config.json`
   in use. This entry serves as the capture's *anchor* (see 1.5).
3. **Fetching the page.** A single `GET` request over HTTP/1.1, on a dedicated connection. The bytes exchanged with the server are recorded
   as they are, before any interpretation (no automatic decompression, no cache, no cookies). **Redirects** are followed one
   at a time and **every hop is archived**.
4. **Parsing the page** to find what it loads: scripts, stylesheets, images (including `srcset`, `<picture>`, video posters),
   fonts, media, frames (`iframe`), embedded objects; then, inside stylesheets, `@import` rules and `url(...)` references (depth is configurable).
5. **Fetching the resources** in parallel (4 simultaneous downloads by default), each handled with the same care as the page itself.
   A failure, a skipped resource (limit reached) or an incomplete response does not make the capture fail: they are **noted in the manifest**.
6. **Writing the WARC archive.** For each exchange: a `response` record, a `request` record and a `metadata` record; at the start, a `warcinfo` record.
7. **Sealing**: SHA-256 and SHA-512 digests of the archive, the manifest and every record; `.sha256` and `.sha512` check files.
8. **Optional signing** of the manifest with a certificate from your Windows certificate store.
9. **Audit log entry** (`capture.complete`) with the final digests, then the files are set to **read-only** (`security.markFilesReadOnly` setting).

A cancelled or failed capture **deletes the unfinished archive** (no partial file is left that could pass for complete); the cancellation or failure is still recorded in the log.

### 1.2 What is captured

| Item | Where to find it in the archive |
|---|---|
| **Raw HTTP request** (request line, headers sent) | `request` record |
| **Raw HTTP response** (status, headers received, body exactly as the server sent it — compressed if it was) | `response` record |
| **HTML** of the page (the "DOM" *as served*, see [limits](#6-limits--read-before-relying-on-it-in-a-dispute)) | `response` of the main page |
| **Scripts, stylesheets, images, fonts, media, frames** | one `response` per resource |
| **Server IP address**, addresses returned by DNS | `WARC-IP-Address` header and `metadata` record |
| **Server TLS certificate** (subject, issuer, serial number, validity, fingerprint, chain, full certificate in DER/base64), validation errors if any | `metadata` record (`tls-*`) |
| **Date and time** of each exchange (UTC and local time with offset), duration, time zone | `WARC-Date` and `metadata` |
| **Server `Date` header** of the main page and its **offset from the local clock** | manifest (`capture.serverDateHeader`, `capture.clockSkewSeconds`) |
| **Software and version, `User-Agent`**; **.NET, operating system, settings used** | `warcinfo` and `metadata` (software, `User-Agent`); manifest (everything else) |
| **Case reference, operator, notes** | `warcinfo` and manifest |

### 1.3 Files produced

Everything is written to the output folder (`outputDirectory` in `config.json`). Archive names are timestamped (local time); an existing file
is **never** overwritten.

| File | Purpose |
|---|---|
| `20261010-125353_example.com.warc.gz` | The archive: one gzip member per record (`warcinfo`, `request`, `response`, `metadata`). Plain `.warc` file if `compressWarc` is `false`. |
| `….manifest.json` | The **technical record**: see 1.4. |
| `….sha256`, `….sha512` | Digests of the archive **and** the manifest, in the format used by `sha256sum -c` / `sha512sum -c`. |
| `….manifest.json.sig` | Only if a certificate was chosen: detached signature of the manifest (RSA PKCS#1 v1.5 or ECDSA), with the signer's public certificate. |
| `audit-log.jsonl` | The case's audit log (one per output folder): see 1.5. |

### 1.4 The manifest

A readable JSON file written next to the archive. It contains:

* **`tool`** — name, version, .NET version, operating system;
* **`case`** — case reference, operator, notes;
* **`capture`** — requested URL and final URL, HTTP status, start and end **in local time with UTC offset and in UTC**, time zone, server `Date` header,
  clock offset, number of resources archived / failed / skipped, volume;
* **`settings`** — what was included, `User-Agent`, limits, security options, **SHA-256 digest of the `config.json`** in use;
* **`warc`** — name, size, SHA-256 and SHA-512 of the archive, number of records;
* **`audit`** — number and digest of the log entry written at the start of the capture;
* **`issues`** — everything that went wrong or was not done: failed, skipped or truncated resource, page that answered 404, redirect not followed…;
* **`records`** — for each record: identifier, type, URL, date, position and length in the file, status, media type, IP, and **SHA-256 + SHA-512 of the block and of the payload**.

Real excerpt (simplified):

```json
{
  "schema": "warc-archiver-manifest/1",
  "tool": { "name": "WarcArchiver", "version": "1.0.0", "runtime": ".NET 10.0.12", "operatingSystem": "…" },
  "case": { "reference": "CASE-2026-001", "operator": "…", "notes": "…" },
  "capture": {
    "targetUrl": "https://example.com/", "finalUrl": "https://example.com/", "mainStatus": 200,
    "startedUtc": "2026-10-10T16:42:49.271Z", "startedLocal": "2026-10-10T18:42:49.271+02:00",
    "timeZoneId": "Europe/Paris", "utcOffset": "+02:00",
    "serverDateHeader": "Sat, 10 Oct 2026 16:42:49 GMT", "clockSkewSeconds": -0.3
  },
  "warc": { "fileName": "….warc.gz", "version": "WARC/1.1", "bytes": 3174041,
            "sha256": "9058…4378", "sha512": "52c2…f9cd", "recordCount": 52 },
  "audit": { "logFile": "audit-log.jsonl", "anchorSeq": 1, "anchorHash": "ba69…a433" },
  "issues": [ { "type": "warning", "url": "…/missing.png", "message": "The server answered HTTP 404." } ],
  "records": [ { "id": "<urn:uuid:…>", "type": "response", "uri": "…", "httpStatus": 200, "ipAddress": "…",
                 "blockSha256": "…", "blockSha512": "…", "payloadSha256": "…", "payloadSha512": "…" } ]
}
```

### 1.5 The audit log

`audit-log.jsonl` holds one JSON line per event: `capture.start`, `capture.complete`, `capture.cancelled`, `capture.failed`, `capture.rejected`,
`verify.run`, `audit.restart`. Each line carries a sequence number, the date (UTC and local), the event data, **the SHA-256 digest of the
previous line** and its own digest. Modifying, deleting, inserting or reordering a line **breaks every following link**, which the *Audit log* page
and the verifier detect. A damaged log is never overwritten: it is set aside (`audit-log.jsonl.corrupt-…`) and its digest is written
into the new log.

> **What to do:** copy the **head hash** (*Copy head hash* button) and send it to a third party (e-mail to your lawyer, deposit with a notary or a *commissaire de justice*…).
> It **anchors the whole history at that date**: without an external anchor, someone with access to the folder could, in theory, recompute the entire log (see [limits](#6-limits--read-before-relying-on-it-in-a-dispute)).

### 1.6 Built-in verification

The **Verify** page (drag and drop a `.warc`, `.warc.gz` or `.manifest.json`) re-checks, with **its own WARC reader**:

1. the SHA-256 / SHA-512 digests of the archive file, compared with the manifest;
2. the `.sha256` and `.sha512` files (archive **and** manifest);
3. the WARC structure and **every record**: `WARC-Block-Digest` and `WARC-Payload-Digest` recomputed, lengths, identifiers;
4. **record-by-record** agreement with the manifest (nothing added, nothing removed, same SHA-256 and SHA-512);
5. the digital signature, if there is one (has the manifest changed since it was signed?);
6. the audit log: chain recomputed, anchor entry consistent with the manifest, end-of-capture entry carrying the archive's exact digest.

The report (PASS / FAIL / WARNING / SKIPPED / INFO) can be exported as text and used as an annex. Every verification is itself written to the log (`verify.run`).

---

## 2. Why this archive can be trusted

The design principle: **ask nobody to take anything on faith**. Every claim in the archive can be re-checked by a third party (opposing party, expert,
judge) with tools of their own choosing. The guarantees rest on seven pillars.

| # | What is expected of digital evidence | What the application does | How to check it yourself |
|---|---|---|---|
| 1 | **Fidelity** — what is presented is what was received | Records the actual bytes exchanged with the server (request and response), with no decompression or reformatting; one connection per request; HTTP/1.1 only | Read the `.warc.gz` with a compliant WARC reader (re-reading tested during development with `warcio`) |
| 2 | **Integrity** — nothing has been modified since | Dual SHA-256 + SHA-512 digests at three levels: archive file, manifest, every record (block and payload). Files set to read-only | `sha256sum -c`, `sha512sum -c`, `warcio check` (section 5) |
| 3 | **Dating** — when? | Local time with UTC offset, UTC, time zone, duration; **server `Date` header** and its offset from the local clock, as an independent reference | Compare `startedUtc` and `serverDateHeader` in the manifest |
| 4 | **Traceability** — who, what, with what, under which conditions | Case reference, operator, notes; software, .NET and system versions; settings and `config.json` digest; IP address, DNS, TLS certificate | Read `warcinfo`, the `metadata` records and the manifest |
| 5 | **Chain of custody** — is the history intact? | Hash-chained audit log, anchor entry in the manifest, verifications themselves logged, head hash exportable to a third party | *Audit log* page; independent recomputation (the formula is public: section 5) |
| 6 | **Attribution** — who does the document come from? | Detached X.509 signature (RSA or ECDSA) of the manifest, which pins the archive and every record; public certificate included | `openssl dgst -verify …` (section 5) |
| 7 | **Transparency and adversarial review** — can the opposing party check it? | Open format (ISO 28500); plain JSON manifest; **all failures are recorded** (failed or skipped resources, truncated responses); verifier independent of the writing code | Read the manifest; re-run the verification with another tool |

### What reinforces these guarantees

* **A recognized preservation format.** WARC is the ISO 28500 standard, used by national libraries and the Internet Archive. The archives produced here are
  read back without errors by `warcio` (a third-party library), and a deliberately altered archive is rejected by `warcio` as well as by the built-in verifier.
* **Two hash functions.** SHA-256 and SHA-512 are computed in parallel; forging an archive without either one revealing it is not realistic.
* **A capture process with no interpretation.** No embedded browser that would rewrite the page, no automatic decompression, no cookies, no
  cache: what is recorded is what went over the connection (recording happens on the decrypted stream, at the TLS level).
* **Evidence of network provenance.** The full TLS certificate, its chain, the IP address and the addresses returned by DNS are kept. The server contacted can be
  identified, and any anomalies (invalid certificate, unexpected redirect) are visible.
* **No cosmetic cover-up of incidents.** A page that returns an error is archived as it is ("proof that the page answered 404" is evidence too); a
  size limit that is reached is marked `WARC-Truncated` and reported in the manifest.
* **A safe, non-intrusive process.** TLS 1.2/1.3 only, certificates verified by default, private/local addresses refused (including after a redirect), no
  credentials accepted in the URL, files created without overwriting, a single process at a time on the log (section 10).
* **Tests.** The engine is covered by 155 automated end-to-end checks (`tests/WarcArchiver.SelfTest`) against a local HTTP/HTTPS site: capture, read-back,
  record-by-record tamper detection, RSA and ECDSA signatures (including failure after the manifest is modified), audit log,
  limits, cancellation, network security.

### What the archive establishes — and what it does not

**It establishes** that at a given date, from a given machine (software, version, system, clock), the named operator requested address `X` and received from a server
at IP address `Y`, presenting certificate `Z`, **exactly these bytes**, and that they have not changed since (digests) — which any third party can re-check.

**On its own it does not establish:** that the content is *true*; that every visitor saw the same thing (personalized, geolocated or A/B-tested content); what a browser would have
displayed after running JavaScript; an enforceable date without a timestamp from a trusted third party; the operator's civil identity beyond the certificate they use.
These points are covered in section 4 (good practice) and section 6 (limits).

---

## 3. Legal framework (France / European Union)

> **Disclaimer.** This section is for information; it is **not legal advice**. The admissibility and probative force of a document are matters for the judge's assessment
> and depend on the jurisdiction, the nature of the dispute and the circumstances. The application is **neither certified nor qualified** (not ANSSI, not eIDAS, not NF Z67-147).
> Have your use validated by a lawyer or a *commissaire de justice* before any significant dispute.

**Principle: in France, evidence is most often free.** Proving that content was online on a given date is, in most cases, proving a *fact*: proof may
be provided by any means (Civil Code (*Code civil*) art. 1358; in commercial matters, Commercial Code (*Code de commerce*) art. L110-3; in criminal matters, Code of Criminal Procedure (*Code de procédure pénale*) art. 427). There is no mandatory legal
"label" for a web capture: the judge assesses the **reliability** of the document, and the opposing party may challenge it. This is precisely where this tool matters:
it provides **technical, documented and verifiable** elements rather than a mere image.

| Text | In substance | Link with the application |
|---|---|---|
| Civil Code, art. 1366 | An electronic document has the same probative force as a paper one, provided that **the person it comes from is duly identified** and that it is **created and kept under conditions that guarantee its integrity**. | Identification: operator, case reference, optional X.509 signature, audit log. Integrity: dual digests at three levels, read-only files, verifier, recommended retention practice (section 4). |
| Civil Code, art. 1367; decree no. 2017-1416 | A **qualified** electronic signature enjoys a presumption of reliability. | The application's signature is a **simple signature**: as it stands, it is not a qualified signature. It remains an element of attribution that the judge is free to weigh. |
| Regulation (EU) No 910/2014 (eIDAS), art. 25 | An electronic signature cannot be denied admissibility as evidence in legal proceedings **solely on the grounds** that it is electronic or that it is not qualified. | The optional signature of the manifest cannot be set aside *on that ground alone*. |
| Regulation (EU) No 910/2014, art. 41 | A **qualified electronic time stamp** enjoys a presumption of accuracy of the date and time and of integrity of the data. | **Not built in.** Strongly recommended as a complement to date the capture in an enforceable way (section 4, step 6). See the supplementary application [WarcSigner](https://github.com/icaza/WARC-Archiver/WarcSigner) |
| Report by a *commissaire de justice* (formerly *huissier*); AFNOR standard NF Z67-147 | Reinforced probative force of a report drawn up by a public officer, with a standardized procedure for reports on the Internet. | The application **does not replace** such a report. It **complements** it (fast collection, complete preservation, later checking) or prepares it. For capture in accordance with the **AFNOR NF Z67-147** standard, see the application [Evidence Collector](https://github.com/icaza/EvidenceCollector) |
| ISO/IEC 27037 | Guidelines for the identification, collection, acquisition and preservation of digital evidence: documentation, traceability, reproducibility, minimal alteration. | The design is **inspired by** it (log, manifest, digests, process without interpretation). No certification is claimed. |
| GDPR (RGPD) | An archived page may contain personal data. | Retention for the purpose of establishing, exercising or defending a legal claim may be justified; define the purpose, retention period and access. |
| For information, outside the EU | United States: rules 902(13) and 902(14) of the *Federal Rules of Evidence* (authentication of electronic data, notably by hash value, on certification by a qualified person). | Same logic: the digest is what supports authentication. |

**Fairness of collection.** The application only accesses what is **publicly accessible without authentication**: no cookies, no credentials, no protection bypassed.
It does not consult `robots.txt` (the capture is a one-off act by the operator; the choice is recorded in the `warcinfo`). It does not get around login walls,
paywalls or access restrictions.

**What the judge or expert will look at, and what the application gives them:**

| Likely question | Answer the archive allows you to document |
|---|---|
| Could the content have been modified since? | Digests of the file, of every record and of the manifest; signature; chained log |
| Is the capture complete? | List of resources, failures and limits in `issues`; `WARC-Truncated` |
| Was the server contacted the right one? | IP, DNS, TLS certificate and chain kept |
| When was it made? | Local time + UTC + time zone, server `Date`, clock offset; third-party time stamp if you add one |
| Who made it, and how? | Operator, tool version, system, settings, `config.json` digest |
| Is the process reproducible and checkable? | Open format, independent verification, commands in section 5 |

---

## 4. Recommended protocol to strengthen evidentiary value

The tool produces solid technical elements; **how you use it** does the rest.

1. **Prepare the workstation.** Up-to-date system, **synchronized** clock (`w32tm /query /status`, or *Settings → Time & language → Date & time → Sync now*),
   no unnecessary VPN or proxy (otherwise, note it). Close unneeded programs.
2. **Fill in the capture details.** Case reference, operator name (can be set once and for all via `defaults.operatorName`), a note on why the capture is made.
3. **Choose a signing certificate** (*Capture* page → *Sign the manifest with*). First import your `.pfx` certificate into *Current User → Personal*.
   Prefer a certificate issued by a recognized authority (ideally a qualified one) over a self-signed certificate.
4. **Capture, then verify immediately** (*Verify this archive*) and **export the verification report**.
5. **Anchor the log.** *Audit log* page → *Copy head hash* → send it by e-mail (to yourself, to your lawyer) or have it deposited with a *commissaire de justice* or a notary.
6. **Time-stamp through a trusted third party** (recommended). Obtain an RFC 3161 token for the **manifest** (which pins the whole archive) from a **qualified**
   trust service provider (EU trusted list). With OpenSSL:
   ```bash
   openssl ts -query -data 20261010-125353_example.com.manifest.json -sha256 -cert -out request.tsq
   curl -H "Content-Type: application/timestamp-query" --data-binary @request.tsq https://<TIMESTAMP-SERVICE-URL> -o response.tsr
   openssl ts -verify -in response.tsr -queryfile request.tsq -CAfile <provider-certificate-chain>.pem
   ```
   Keep `request.tsq`, `response.tsr` and the provider's certificate chain together with the archive See the supplementary application [WarcSigner](https://github.com/icaza/WARC-Archiver/WarcSigner).
7. **Retain properly.** Keep the archive, manifest, `.sha256`/`.sha512` files, signature, **audit log** and verification report together. Make **several copies**
   (including one offline, on write-once media if possible), note the SHA-256 digest in a separate document and **always work on a copy**, never on the original.
8. **For anything important,** back it up with a **report by a *commissaire de justice***. The archive remains useful: it preserves more (every resource, the raw exchanges) and can be re-checked.
9. **Hand over the document** together with this documentation, the tool version, the verification report and the independent-check commands (section 5), so that the opposing party
   and the expert can redo everything.

---

## 5. Verifying an archive without the application

The point of an open format is that nobody has to trust this program.

```bash
sha256sum -c 20261010-125353_example.com.sha256      # digests of the archive and the manifest
sha512sum -c 20261010-125353_example.com.sha512
gzip -t 20261010-125353_example.com.warc.gz           # every gzip member is intact
pip install warcio && warcio check 20261010-125353_example.com.warc.gz   # digests of every record
```

On Windows (PowerShell), without installing anything:

```powershell
Get-FileHash .\20261010-125353_example.com.warc.gz -Algorithm SHA256   # compare with the .sha256 file and the manifest
Get-FileHash .\20261010-125353_example.com.warc.gz -Algorithm SHA512
```

**Signature** (`.sig` file: JSON containing `signature` in base64, `signerCertificateDer` in base64, `hashAlgorithm` and `signatureAlgorithm`).
Example for an **RSA** signature with OpenSSL (case `"signatureAlgorithm": "RSA-PKCS1"` and `"hashAlgorithm": "SHA256"`; otherwise change `-sha256` to `-sha512`):

```powershell
# OpenSSL ships with, for example, Git for Windows; in PowerShell 5.1, do not use ">" to write a PEM file (it would be saved as UTF-16), hence "-out".
$s = Get-Content .\20261010-125353_example.com.manifest.json.sig -Raw | ConvertFrom-Json
[IO.File]::WriteAllBytes((Join-Path $PWD "signer.cer"), [Convert]::FromBase64String($s.signerCertificateDer))
[IO.File]::WriteAllBytes((Join-Path $PWD "signature.bin"),  [Convert]::FromBase64String($s.signature))
openssl x509 -inform DER -in signer.cer -pubkey -noout -out signer.pub
openssl dgst -sha256 -verify signer.pub -signature signature.bin 20261010-125353_example.com.manifest.json
# → "Verified OK"; any modification of the manifest gives "Verification failure"
```

For an **ECDSA** signature, the signature is in raw "r‖s" format (IEEE P1363): convert it to DER before giving it to OpenSSL, or use the built-in verifier.
Also remember to check **the certificate itself** (issuer, validity, `signerThumbprintSha256` fingerprint).

**Audit log.** The chaining formula is simple: an entry's digest is the SHA-256 of the concatenation (separated by line feeds) of the sequence number, the UTC date, the local
date, the event, the data (keys sorted, one line per item in the format `length(key):key=length(value):value`, lengths in UTF-16 code units as in .NET, text encoded as UTF-8) and
the previous digest (64 zeros for the first entry). Anyone can recompute it (see `src/WarcArchiver.Core/Audit/AuditLog.cs`, method `ComputeHash`); this formula was
recomputed with an independent script on a log produced by the application (identical result).

---

## 6. Limits — read before relying on it in a dispute

* **The "DOM" is the HTML as served.** Scripts are archived but **not executed**: content that a page builds with JavaScript after it loads (`fetch`/XHR calls, client-side rendering) **is not in the archive**.
  A capture through a headless browser could be added.
* **No authentication.** Pages that require a login (private social networks, customer areas) cannot be reached; there are no cookies and no session.
* **What *you* received, not necessarily what everyone sees.** A site may vary its content by IP address, language, `User-Agent` or time. The settings used are recorded so that this can be explained.
* **The time is your PC's.** The manifest adds the server's `Date` header and the measured offset, but **the time stamp remains self-declared** (as does the signing time, `signedUtc`).
  For independent dating, add an RFC 3161 time stamp (section 4, step 6).
* **The audit log is not tamper-proof on its own.** It is *resistant to apparent tampering*, not unforgeable: since the hash is not keyed, someone who controls the whole folder could recompute
  the entire chain. This is why **external anchoring** of the head hash (section 1.5) matters. Likewise, the "read-only" attribute is only a safeguard: the digests and the signature are what protect.
* **"SHA-256/SHA-512 signature"** means here: cryptographic digests + an *optional* X.509 signature of the manifest. It is not a qualified electronic signature within the meaning of eIDAS.
* **The signing certificate** must be in the Windows *Personal* store (import a `.pfx`); an expired certificate is flagged in the list.
* **Network.** HTTP/1.1 only; one `GET` request per resource; your machine's DNS (the addresses obtained are recorded, but DNS is not signed). Downloads exceeding the limits are truncated and marked as such;
  `robots.txt` is not consulted.
* **Personal data and third-party rights.** The archive is designed for evidence, not for republication. Comply with the GDPR and copyright law when using and retaining it.
* **No legal advice.** The admissibility of a document, the complementary procedure expected (witness, *commissaire de justice*, notary) and its probative force depend on your jurisdiction and the dispute. The tool provides the **technical** evidence.

---

## 7. Installation and launch

Requirements: Windows 10/11 x64 and the [.NET 10 SDK](https://dotnet.microsoft.com/download) to build.

```powershell
dotnet build WarcArchiver.slnx -c Release
dotnet run --project src/WarcArchiver.App -c Release
```

**Ready-to-copy folder** (`publish\`):

```powershell
.\publish.cmd            # small version: the target computer must have the ".NET Desktop Runtime 10" (x64)
.\publish.cmd portable   # self-contained version (about 150 MB): works on any Windows 10/11 x64, nothing to install
```

Copy **the entire folder** (`WarcArchiver.exe`, `WarcArchiver.dll`, `WarcArchiver.runtimeconfig.json`, `config.json`…), not just the executable, for the small version.

### "You must install .NET Desktop Runtime to run this application"

The small version needs the **.NET Desktop Runtime 10 (x64)** on the PC that runs it. Check with `dotnet --list-runtimes`: the list must contain `Microsoft.WindowsDesktop.App 10.0.x`.
(The plain ".NET Runtime" and the "ASP.NET Core Runtime" do **not** contain it, and an x86 installation is not suitable for an x64 program.) Install it from
<https://dotnet.microsoft.com/download/dotnet/10.0> or build the self-contained version with `.\publish.cmd portable`.

**Engine test** (starts a local HTTP + HTTPS test site; works on Windows and Linux):

```
dotnet run --project tests/WarcArchiver.SelfTest -- "working-folder"
```

---

## 8. Using the interface

The interface is in English (en-US), dark theme, with navigation through the left-hand menu.

* **Capture** — paste the URL; fill in case reference, operator and notes if needed; choose what to include (scripts, stylesheets, images, fonts/media/frames,
  resources from other domains) and an optional signing certificate; click *Start capture*. Every request appears live in the table (time, status, type, URL, size, SHA-256, duration, remark).
  *Cancel* aborts the capture and deletes the unfinished archive. When it finishes: *Open folder*, *Verify this archive*, *Copy SHA-256*.
* **Archives** — everything in the output folder, with filtering and sorting; double-click to verify, copy the SHA-256, open the folder.
* **Verify** — drop a `.warc`, `.warc.gz` or `.manifest.json` onto the page. The report can be exported as text.
* **Audit log** — browse the log, recompute the chain (*Verify chain*), copy the **head hash** (*Copy head hash*), show the file.
* **Settings** — shows the active `config.json`, opens it in your editor, reloads it (*Reload*), changes the output folder **without losing your comments**.

Only one long operation (capture, verification, log check) at a time: this spares resources and keeps the log's reader and writer from crossing paths.

---

## 9. Configuration (`config.json`)

The file sits next to `WarcArchiver.exe` (or in `%LOCALAPPDATA%\WarcArchiver` if that folder is read-only). `//` comments and trailing commas are allowed. Out-of-range values are
**brought back into a safe range**. A file with a syntax error is **never overwritten**: the application starts with the default values, tells you, and on *Reload* keeps the previous valid settings.
The digest of the file in use is written to the log and to the manifest of every capture.

| Key | Default | Meaning |
|---|---|---|
| `outputDirectory` | `%USERPROFILE%\Documents\WARC Archives` | Folder for archives, manifests and the log. `%VARIABLES%` are expanded. |
| `compressWarc` | `true` | `.warc.gz` (one gzip member per record) or plain `.warc`. |
| `warcPrimaryDigest` | `"sha256"` | Digest written in the WARC headers (`"sha256"` or `"sha512"`). The manifest and the check files **always carry both**. |
| `network.userAgent`, `network.acceptLanguage` | close to Chrome / `en-US,en;q=0.9` | Sent with every request (recorded in the archive). |
| `network.connectTimeoutSeconds`, `network.requestTimeoutSeconds`, `network.maxRedirects` | 15 / 90 / 10 | Redirects are followed one at a time and every hop is archived. |
| `network.maxParallelDownloads` | 4 (1–16) | Lower = lighter on your PC and on the site. |
| `network.allowPrivateNetworks` | `false` | Loopback, private, link-local, CGNAT, ULA… addresses **refused**, including after a redirect. |
| `network.allowInvalidCertificates` | `false` | `true` allows capturing a site with an invalid certificate; the problem is then **recorded as evidence**. |
| `network.proxyUrl` | `""` | Optional HTTP proxy (the private-network protection cannot see behind a proxy). |
| `limits.maxResources`, `maxResourceMegabytes`, `maxTotalMegabytes` | 500 / 50 / 1000 | Safeguards; a limit that is reached is written to the manifest. |
| `limits.maxCssDepth` | 2 | Levels of `@import` / `url()` followed in stylesheets. |
| `limits.maxParseMegabytes`, `maxMemoryKilobytesPerBuffer` | 8 / 256 | Maximum size parsed; memory buffer per download (beyond that: temporary file). |
| `defaults.*` | all `true` | Initial state of the *Capture* page checkboxes; `operatorName` pre-fills the operator field. |
| `signing.certificateThumbprint` | `""` | SHA-1 thumbprint of a certificate (with private key) in *Current User → Personal*, used by default. |
| `security.markFilesReadOnly` | `true` | Read-only attribute on finished files. |

---

## 10. Security and resource footprint

**Security**

* TLS 1.2/1.3 only; a certificate error stops the capture unless you allow it (in which case the error is recorded).
* Only `http`/`https` addresses are accepted; URLs containing credentials are refused.
* Non-public addresses (loopback, private network, link-local, cloud metadata…) are blocked **at connection time, after DNS resolution**, so neither a rigged DNS nor a redirect can
  make the application probe your network.
* Files created with `CreateNew` (no overwriting), header fields sanitized against injection, configuration values bounded, an invalid configuration never overwritten.
* A single-instance lock prevents two processes from writing to the same audit log.

**Resources**

* No embedded browser engine; downloads go through small buffers (overflow goes to an automatically deleted temporary file).
* Garbage collector in non-concurrent workstation mode with `ConserveMemory`; `InvariantGlobalization` avoids loading ICU.
* Four parallel downloads by default, adjustable.

---

## 11. For developers

```
src/WarcArchiver.Core        engine: WARC writing/reading, HTTP capture, parsing, audit log, signing, verifier   (net10.0)
src/WarcArchiver.App         WinForms interface; every control is declared in MainForm.Designer.cs               (net10.0-windows)
tests/WarcArchiver.SelfTest  155 end-to-end checks against a local HTTP/HTTPS site
```

## 12. Glossary

| Term | Meaning |
|---|---|
| **WARC** | *Web ARChive*, a format for preserving web exchanges standardized as ISO 28500. A file contains "records" (`warcinfo`, `request`, `response`, `metadata`) laid end to end, here each in its own gzip member. |
| **Digest (hash)** | A fixed-length value computed from some content (SHA-256: 256 bits, SHA-512: 512 bits). Changing a single bit of the content completely changes the digest. |
| **Block / payload** | A record's *block* is its entire content (HTTP headers + body); the *payload* is just the response body, as the server sent it ("chunked" framing removed, compression kept). |
| **Manifest** | A technical record in JSON that summarizes the capture and pins the archive by its digests. |
| **Audit log** | A register of the application's actions in which every line contains the digest of the previous one. |
| **Anchoring** | Writing the head hash of the log (or of the manifest) with a third party, which fixes the state of the history at that date. |
| **Detached X.509 signature** | A digital signature of a file made with the private key of a certificate; the `.sig` file contains the signature and the public certificate, without modifying the signed file. |
| **RFC 3161 time stamp** | A token issued by a trusted third party (time-stamping authority) attesting that a digest existed at a given moment; *qualified* if it comes from an eIDAS-qualified provider. |
| **Clock offset (`clockSkewSeconds`)** | Difference between the time given by the server (`Date` header) and the PC's UTC clock at the time of the request (includes network latency and rounding to the second). |
| **eIDAS** | European Regulation No 910/2014 on electronic identification and trust services (signatures, seals, time stamps). |
| **Commissaire de justice** | A French public and ministerial officer, created by merging the *huissiers de justice* (bailiffs) and the *commissaires-priseurs judiciaires* (judicial auctioneers); can draw up reports, including on the Internet. |

## 📜 License

MIT