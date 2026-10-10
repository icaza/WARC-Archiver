using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using WarcArchiver.Core.Archive;
using WarcArchiver.Core.Digests;

namespace WarcArchiver.Core.Signing;

public sealed class SignatureFile
{
    public string Schema { get; set; } = "warc-archiver-signature/1";

    public string SignedFile { get; set; } = string.Empty;

    public string SignedFileSha256 { get; set; } = string.Empty;

    /// <summary>"SHA256" or "SHA512" — the hash used inside the signature algorithm.</summary>
    public string HashAlgorithm { get; set; } = "SHA256";

    /// <summary>"RSA-PKCS1" (RSASSA-PKCS1-v1_5) or "ECDSA-P1363" (raw r||s signature bytes).</summary>
    public string SignatureAlgorithm { get; set; } = string.Empty;

    public string Signature { get; set; } = string.Empty;

    public string SignerCertificateDer { get; set; } = string.Empty;

    public string SignerSubject { get; set; } = string.Empty;

    public string SignerThumbprintSha256 { get; set; } = string.Empty;

    /// <summary>Signing time according to the signer's own clock. This value is self-asserted, not independently timestamped.</summary>
    public string SignedUtc { get; set; } = string.Empty;
}

public sealed record SignatureCheck(bool Valid, string Detail, string? Subject, string? ThumbprintSha256, bool? ChainTrusted);

/// <summary>
/// Detached digital signature over the manifest (which itself carries the SHA-256/SHA-512 of the archive). Uses the
/// private key of a certificate the operator already owns; the matching public certificate travels in the .sig file.
/// </summary>
public static class ManifestSigner
{
    public static string SignaturePathFor(string manifestPath) => manifestPath + ".sig";

    public static string Sign(string manifestPath, X509Certificate2 certificate, DigestAlgorithm hash)
    {
        byte[] data = File.ReadAllBytes(manifestPath);
        HashAlgorithmName name = hash.HashName();

        byte[] signature;
        string algorithm;
        using (RSA? rsa = certificate.GetRSAPrivateKey())
        {
            if (rsa is not null)
            {
                signature = rsa.SignData(data, name, RSASignaturePadding.Pkcs1);
                algorithm = "RSA-PKCS1";
            }
            else
            {
                using ECDsa ecdsa = certificate.GetECDsaPrivateKey()
                    ?? throw new CryptographicException("The selected certificate has no usable RSA or ECDSA private key.");
                signature = ecdsa.SignData(data, name);
                algorithm = "ECDSA-P1363";
            }
        }

        var file = new SignatureFile
        {
            SignedFile = Path.GetFileName(manifestPath),
            SignedFileSha256 = Convert.ToHexStringLower(SHA256.HashData(data)),
            HashAlgorithm = hash == DigestAlgorithm.Sha512 ? "SHA512" : "SHA256",
            SignatureAlgorithm = algorithm,
            Signature = Convert.ToBase64String(signature),
            SignerCertificateDer = Convert.ToBase64String(certificate.RawData),
            SignerSubject = certificate.Subject,
            SignerThumbprintSha256 = Convert.ToHexStringLower(certificate.GetCertHash(HashAlgorithmName.SHA256)),
            SignedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
        };

        string path = SignaturePathFor(manifestPath);
        File.WriteAllText(path, JsonSerializer.Serialize(file, ManifestJson.Options));
        return path;
    }

    public static SignatureCheck Verify(string manifestPath, string signaturePath)
    {
        SignatureFile? file;
        try
        {
            file = JsonSerializer.Deserialize<SignatureFile>(File.ReadAllText(signaturePath), ManifestJson.Options);
        }
        catch (JsonException)
        {
            file = null;
        }
        if (file is null || string.IsNullOrEmpty(file.Signature) || string.IsNullOrEmpty(file.SignerCertificateDer))
        {
            return new SignatureCheck(false, "The signature file is unreadable or incomplete.", null, null, null);
        }

        byte[] data = File.ReadAllBytes(manifestPath);
        string actualHash = Convert.ToHexStringLower(SHA256.HashData(data));
        if (!string.Equals(actualHash, file.SignedFileSha256, StringComparison.OrdinalIgnoreCase))
        {
            return new SignatureCheck(false, "The manifest was changed after it was signed (its SHA-256 no longer matches the signature file).", file.SignerSubject, file.SignerThumbprintSha256, null);
        }

        try
        {
            using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(file.SignerCertificateDer));
            HashAlgorithmName name = file.HashAlgorithm == "SHA512" ? HashAlgorithmName.SHA512 : HashAlgorithmName.SHA256;
            byte[] signature = Convert.FromBase64String(file.Signature);

            bool valid;
            if (file.SignatureAlgorithm == "RSA-PKCS1")
            {
                using RSA rsa = certificate.GetRSAPublicKey() ?? throw new CryptographicException("The signer certificate has no RSA public key.");
                valid = rsa.VerifyData(data, signature, name, RSASignaturePadding.Pkcs1);
            }
            else if (file.SignatureAlgorithm == "ECDSA-P1363")
            {
                using ECDsa ecdsa = certificate.GetECDsaPublicKey() ?? throw new CryptographicException("The signer certificate has no ECDSA public key.");
                valid = ecdsa.VerifyData(data, signature, name);
            }
            else
            {
                return new SignatureCheck(false, "Unknown signature algorithm: " + file.SignatureAlgorithm, file.SignerSubject, file.SignerThumbprintSha256, null);
            }

            if (!valid)
            {
                return new SignatureCheck(false, "The cryptographic signature does not match the manifest.", certificate.Subject, file.SignerThumbprintSha256, null);
            }

            using var chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority;
            bool trusted = chain.Build(certificate) && chain.ChainStatus.All(s => s.Status == X509ChainStatusFlags.NoError);

            string detail = $"Valid signature by {certificate.Subject}; certificate valid {certificate.NotBefore:yyyy-MM-dd} to {certificate.NotAfter:yyyy-MM-dd}; " +
                            (trusted ? "chains to a certificate authority this computer trusts." : "the certificate is not chained to a trusted authority on this computer (normal for self-issued certificates).") +
                            $" Signing time claimed by the signer: {file.SignedUtc}.";
            return new SignatureCheck(true, detail, certificate.Subject, file.SignerThumbprintSha256, trusted);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return new SignatureCheck(false, "The signature could not be checked: " + ex.Message, file.SignerSubject, file.SignerThumbprintSha256, null);
        }
    }
}
