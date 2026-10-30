# WarcSigner 🛡️

A lightweight Windows console application that timestamps web archive manifests with the RFC 3161 protocol.
It is designed to work alongside WARC-Archiver: it obtains a cryptographic timestamp (`.tsr`) from a Time-Stamping Authority (TSA), proving that the manifest existed, unmodified, at a given point in time.

# Key Features

- **Minimal dependencies:** the only dependency is Microsoft's first-party `System.Security.Cryptography.Pkcs` NuGet package (it is not part of the base .NET runtime). No third-party library (BouncyCastle or similar) is used.
- **Layered verification, enforced rather than just displayed:**
  1. Response check: PKI status, message imprint, policy and a **random per-request nonce** (a stale or replayed response is rejected).
  2. Signature check: the CMS signature is valid and bound to the SHA-256 of the manifest.
  3. TSA certificate check: it carries the `id-kp-timeStamping` EKU, was valid at the time of the timestamp, and its chain is trusted (OS trust store, or a root you pin with `--ca`). Revocation is checked when the CRL/OCSP endpoints are reachable.
- **Authenticated (paid / qualified) TSAs:** HTTP Basic login and TLS client certificates (password-protected `.pfx`), configured in an external JSON file. Secrets never go on the command line and are never printed.
- **Safe by default:** nothing is written until every check has passed; an existing `.tsr` is never overwritten; the TSA response is limited to 1 MiB, requests time out after 30 s, and HTTP redirects are refused.
- **Constant memory:** the manifest is hashed as a stream, whatever its size.
- **Automation ready:** exit code `0` on success, `1` on any failure; errors go to stderr.
- **Predictable logs:** English messages, ASCII markers, UTC ISO 8601 times, invariant culture.

# Legal Value (RFC 3161 & eIDAS)

WARC-Archiver guarantees the integrity of the captured bytes, but the local system clock is self-declared.
Sending the SHA-256 fingerprint of the `.manifest.json` to a TSA anchors the manifest to an independent clock.

Be aware of the difference between the two kinds of timestamps under EU Regulation No 910/2014 (eIDAS):

- **Qualified electronic timestamp** (issued by a qualified trust service provider listed in the EU Trusted List): benefits from the presumption of accuracy of date/time and of data integrity (Article 41(2)).
- **Any other timestamp**: it cannot be denied legal effect or admissibility solely because it is not qualified (Article 41(1)), but it does not benefit from that presumption.

WarcSigner **cannot determine whether a TSA is qualified**. The default TSA is a general-purpose public service and must not be assumed to be qualified. For evidence that needs the presumption, use a qualified provider and check it in the EU Trusted List.

### Prerequisites

- Windows 10 / 11 (x64)
- .NET 10 SDK (to build the code)

### Installation & Compilation

Clone this repository, then in the application directory add the Pkcs package (once) and compile in Release mode:

> dotnet add package System.Security.Cryptography.Pkcs

> dotnet build -c Release

The executable is generated inside the `bin/Release/net10.0/` folder.

# How to Use

Command syntax:

> WarcSigner.exe <manifest_path> [tsa_url] [--config <file.json>] [--ca <root.cer>] [--allow-untrusted]

| Argument | Meaning |
|---|---|
| `manifest_path` | The manifest file to timestamp (required). |
| `tsa_url` | Absolute `http://` or `https://` URL of the TSA. Default: `http://timestamp.digicert.com`, or `tsaUrl` from the configuration file. |
| `--config <file>` | JSON file with the TSA URL, root certificate and credentials (see [Configuration file](#configuration-file-tsa-credentials)). |
| `--ca <file>` | Trust **only** this root certificate (PEM or DER) for the TSA chain. Use it to pin the root of a TSA that is not in the Windows trust store, such as the root published by a qualified provider. |
| `--allow-untrusted` | Save the response even if the TSA chain is not trusted. The token then has no verified trust anchor; not recommended. |

Examples:

- Default public TSA:

> WarcSigner.exe "C:\Archives\20261010-134511_veydunet.com.manifest.json"

- A TSA of your choice, pinning its root certificate:

> WarcSigner.exe "C:\Archives\20261010-134511_veydunet.com.manifest.json" "https://tsa.example.com/rfc3161" --ca "C:\pki\tsa-root.cer"

### Configuration file (TSA credentials)

Commercial and qualified TSAs usually require authentication. Describe the TSA once in a JSON file and pass it with `--config`. Relative paths inside the file are resolved from the folder of the file itself. Comments and trailing commas are tolerated; unknown keys are rejected, so a typo such as `pasword` is an error instead of a silently missing credential.

HTTP Basic login (the password is read from an environment variable):

```json
{
  "tsaUrl": "https://tsa.example.com/rfc3161",
  "rootCertificate": "tsa-root.cer",
  "authentication": {
    "username": "my-account",
    "passwordEnv": "WARCSIGNER_TSA_PASSWORD"
  }
}
```

TLS client certificate (password-protected `.pfx`):

```json
{
  "tsaUrl": "https://tsa.example.com/rfc3161",
  "rootCertificate": "tsa-root.cer",
  "clientCertificate": {
    "pfxPath": "my-client-cert.pfx",
    "passwordEnv": "WARCSIGNER_PFX_PASSWORD"
  }
}
```

Both sections can be combined. A ready-to-copy template is provided in `warcsigner.config.example.json`.

| Key | Meaning |
|---|---|
| `tsaUrl` | TSA endpoint. **Required** when the file contains credentials. |
| `rootCertificate` | Public root certificate to pin (same as `--ca`; `--ca` wins if both are given). Not a `.pfx`. |
| `authentication.username` | HTTP Basic user name (no `:`, no control characters). |
| `authentication.passwordEnv` | Name of the environment variable that holds the password. **Recommended.** |
| `authentication.password` | Password in clear text in the file. Works, but a warning is printed on every run. |
| `clientCertificate.pfxPath` | `.pfx` / `.p12` file with the private key. |
| `clientCertificate.passwordEnv` / `password` | Same two options as above, for the `.pfx` password. |

Set the variable before running (PowerShell):

> $env:WARCSIGNER_TSA_PASSWORD = "your-password"

If you give a username (or a protected `.pfx`) but no password source, WarcSigner asks for the password in the console, with the typed characters hidden. When input is redirected (scripts, CI) it never waits: it stops with an explicit error, so use `passwordEnv` there.

Security rules enforced by the tool:

- **Credentials and client certificates are only sent over `https://`.** A configuration with credentials and an `http://` URL is refused before any connection is made.
- **Credentials are bound to the configured TSA.** A different `tsa_url` on the command line is refused, so credentials cannot be redirected to another server by mistake.
- **HTTP redirects are never followed**, so a TSA cannot forward your login to another host.
- Secrets are never printed, logged or included in error messages.

Keep the file out of version control (add it to `.gitignore`) and restrict it to your account. With a plain-text `password`, anyone who can read the file can use your TSA account.

### Output Files

On success, WarcSigner writes the TSA response next to the manifest, replacing the last extension with `.tsr`:

- 📄 `20261010-134511_veydunet.com.manifest.json` (timestamped file)
- 🛡️ `20261010-134511_veydunet.com.manifest.tsr` (proof)

The `.tsr` is the **unmodified RFC 3161 TimeStampResp** returned by the TSA, so standard tools can verify it independently:

> openssl ts -verify -data 20261010-134511_veydunet.com.manifest.json -in 20261010-134511_veydunet.com.manifest.tsr -CAfile tsa-root.pem

If the `.tsr` already exists, WarcSigner stops without touching it. Keep the manifest, the `.tsr` and the TSA root certificate together.

### Execution Pipeline

1. **Hashing:** streams the manifest through SHA-256.
2. **Request:** builds an RFC 3161 `TimeStampReq` with a random nonce and `certReq` set, so the TSA returns its signing certificate.
3. **Transmission:** HTTP POST with `Content-Type: application/timestamp-query`, 30 s timeout, response capped at 1 MiB, no redirects.
4. **Response verification:** status, message imprint, nonce and policy, no trailing data, then the CMS signature against the manifest hash.
5. **Certificate verification:** `id-kp-timeStamping` EKU, validity at the timestamp time, chain trust (OS store or `--ca`), revocation when reachable. An untrusted chain is a failure unless `--allow-untrusted` is given.
6. **Saving:** the response is written only now, and never over an existing file.

### Exit Codes

| Code | Meaning |
|---|---|
| `0` | Timestamp obtained, verified and saved (with `--allow-untrusted`, a warning states that trust was not established). |
| `1` | Any failure: bad arguments, network error, rejected response, failed verification, untrusted chain, existing output file. |

### Limitations

- The tool does not check whether the TSA is an eIDAS qualified provider.
- Revocation checking is best effort: if no CRL/OCSP answer can be obtained the run continues with a warning. For long-term validation, archive the revocation data (or use a long-term format) in addition to the `.tsr`.
- Only HTTP Basic and TLS client certificates are supported. TSAs that authenticate with an API key in a custom header, a bearer token or OAuth are not.
- Plain `http://` TSA endpoints are normal for RFC 3161 (the token is self-authenticating), but the exchange itself is not encrypted; prefer `https://` when the TSA offers it.
