namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes package signing execution evidence.
/// </summary>
public sealed class SigtranPackageSigningExecutionEvidence
{
    /// <summary>Creates package signing execution evidence.</summary>
    /// <param name="signedPackagePath">The signed package path.</param>
    /// <param name="signedPackageSha256">The signed package SHA-256 digest.</param>
    /// <param name="signLogPath">The signing log path.</param>
    /// <param name="verificationLogPath">The verification log path.</param>
    /// <param name="signingSucceeded">Whether signing produced a signed package.</param>
    /// <param name="verificationPassed">Whether package verification passed.</param>
    /// <param name="timestamped">Whether the package signature is timestamped.</param>
    /// <param name="trustedCertificate">Whether the signing certificate chains to a trusted provider.</param>
    public SigtranPackageSigningExecutionEvidence(
        string signedPackagePath,
        string signedPackageSha256,
        string signLogPath,
        string verificationLogPath,
        bool signingSucceeded,
        bool verificationPassed,
        bool timestamped,
        bool trustedCertificate)
    {
        SignedPackagePath = string.IsNullOrWhiteSpace(signedPackagePath) ? throw new ArgumentException("Signed package path is required.", nameof(signedPackagePath)) : signedPackagePath;
        SignedPackageSha256 = string.IsNullOrWhiteSpace(signedPackageSha256) ? throw new ArgumentException("Signed package digest is required.", nameof(signedPackageSha256)) : signedPackageSha256;
        SignLogPath = string.IsNullOrWhiteSpace(signLogPath) ? throw new ArgumentException("Signing log path is required.", nameof(signLogPath)) : signLogPath;
        VerificationLogPath = string.IsNullOrWhiteSpace(verificationLogPath) ? throw new ArgumentException("Verification log path is required.", nameof(verificationLogPath)) : verificationLogPath;
        SigningSucceeded = signingSucceeded;
        VerificationPassed = verificationPassed;
        Timestamped = timestamped;
        TrustedCertificate = trustedCertificate;
    }

    /// <summary>The signed package path.</summary>
    public string SignedPackagePath { get; }

    /// <summary>The signed package SHA-256 digest.</summary>
    public string SignedPackageSha256 { get; }

    /// <summary>The signing log path.</summary>
    public string SignLogPath { get; }

    /// <summary>The verification log path.</summary>
    public string VerificationLogPath { get; }

    /// <summary>Whether signing produced a signed package.</summary>
    public bool SigningSucceeded { get; }

    /// <summary>Whether package verification passed.</summary>
    public bool VerificationPassed { get; }

    /// <summary>Whether the package signature is timestamped.</summary>
    public bool Timestamped { get; }

    /// <summary>Whether the signing certificate chains to a trusted provider.</summary>
    public bool TrustedCertificate { get; }

    /// <summary>Whether the signed package can support commercial promotion.</summary>
    public bool SupportsCommercialPromotion => SigningSucceeded
        && VerificationPassed
        && Timestamped
        && TrustedCertificate
        && SignedPackageSha256.Length == 64;
}

/// <summary>
/// Provides package signing execution evidence helpers.
/// </summary>
public static class SigtranPackageSigningExecution
{
    /// <summary>Creates package signing execution evidence from retained artifact digests.</summary>
    /// <param name="signedPackageSha256">The retained signed package SHA-256 digest.</param>
    /// <returns>The package signing execution evidence.</returns>
    public static SigtranPackageSigningExecutionEvidence CreateFromSignedPackageDigest(string signedPackageSha256)
    {
        return new(
            "artifacts/signing/Sigtran.Net.1.0.0.nupkg",
            signedPackageSha256,
            "artifacts/signing/sign-package.log",
            "artifacts/signing/verify-package.log",
            signingSucceeded: true,
            verificationPassed: false,
            timestamped: false,
            trustedCertificate: false);
    }
}
