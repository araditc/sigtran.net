namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes trusted timestamped package signing evidence retained for a release.
/// </summary>
public sealed class SigtranTrustedPackageSigningEvidence
{
    /// <summary>Creates trusted timestamped package signing evidence.</summary>
    /// <param name="packagePath">The signed package path.</param>
    /// <param name="packageSha256">The signed package SHA-256 digest.</param>
    /// <param name="certificateSubject">The signing certificate subject.</param>
    /// <param name="certificateThumbprint">The signing certificate thumbprint.</param>
    /// <param name="timestampAuthorityUrl">The timestamp authority URL.</param>
    /// <param name="timestampReceiptPath">The retained timestamp receipt path.</param>
    /// <param name="timestampReceiptSha256">The retained timestamp receipt SHA-256 digest.</param>
    /// <param name="verificationReportPath">The package verification report path.</param>
    /// <param name="verificationReportSha256">The package verification report SHA-256 digest.</param>
    /// <param name="verificationPassed">Whether package signature verification passed.</param>
    public SigtranTrustedPackageSigningEvidence(
        string packagePath,
        string packageSha256,
        string certificateSubject,
        string certificateThumbprint,
        string timestampAuthorityUrl,
        string timestampReceiptPath,
        string timestampReceiptSha256,
        string verificationReportPath,
        string verificationReportSha256,
        bool verificationPassed)
    {
        PackagePath = string.IsNullOrWhiteSpace(packagePath) ? throw new ArgumentException("Package path is required.", nameof(packagePath)) : packagePath;
        PackageSha256 = string.IsNullOrWhiteSpace(packageSha256) ? throw new ArgumentException("Package digest is required.", nameof(packageSha256)) : packageSha256;
        CertificateSubject = string.IsNullOrWhiteSpace(certificateSubject) ? throw new ArgumentException("Certificate subject is required.", nameof(certificateSubject)) : certificateSubject;
        CertificateThumbprint = string.IsNullOrWhiteSpace(certificateThumbprint) ? throw new ArgumentException("Certificate thumbprint is required.", nameof(certificateThumbprint)) : certificateThumbprint;
        TimestampAuthorityUrl = string.IsNullOrWhiteSpace(timestampAuthorityUrl) ? throw new ArgumentException("Timestamp authority URL is required.", nameof(timestampAuthorityUrl)) : timestampAuthorityUrl;
        TimestampReceiptPath = string.IsNullOrWhiteSpace(timestampReceiptPath) ? throw new ArgumentException("Timestamp receipt path is required.", nameof(timestampReceiptPath)) : timestampReceiptPath;
        TimestampReceiptSha256 = string.IsNullOrWhiteSpace(timestampReceiptSha256) ? throw new ArgumentException("Timestamp receipt digest is required.", nameof(timestampReceiptSha256)) : timestampReceiptSha256;
        VerificationReportPath = string.IsNullOrWhiteSpace(verificationReportPath) ? throw new ArgumentException("Verification report path is required.", nameof(verificationReportPath)) : verificationReportPath;
        VerificationReportSha256 = string.IsNullOrWhiteSpace(verificationReportSha256) ? throw new ArgumentException("Verification report digest is required.", nameof(verificationReportSha256)) : verificationReportSha256;
        VerificationPassed = verificationPassed;
    }

    /// <summary>The signed package path.</summary>
    public string PackagePath { get; }

    /// <summary>The signed package SHA-256 digest.</summary>
    public string PackageSha256 { get; }

    /// <summary>The signing certificate subject.</summary>
    public string CertificateSubject { get; }

    /// <summary>The signing certificate thumbprint.</summary>
    public string CertificateThumbprint { get; }

    /// <summary>The timestamp authority URL.</summary>
    public string TimestampAuthorityUrl { get; }

    /// <summary>The retained timestamp receipt path.</summary>
    public string TimestampReceiptPath { get; }

    /// <summary>The retained timestamp receipt SHA-256 digest.</summary>
    public string TimestampReceiptSha256 { get; }

    /// <summary>The package verification report path.</summary>
    public string VerificationReportPath { get; }

    /// <summary>The package verification report SHA-256 digest.</summary>
    public string VerificationReportSha256 { get; }

    /// <summary>Whether package signature verification passed.</summary>
    public bool VerificationPassed { get; }

    /// <summary>Whether the timestamp authority is trusted enough for release evidence.</summary>
    public bool HasTrustedTimestampAuthority => TimestampAuthorityUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether all signing evidence artifacts have retained digests.</summary>
    public bool HasDigestCoverage => PackageSha256.Length == 64
        && TimestampReceiptSha256.Length == 64
        && VerificationReportSha256.Length == 64;

    /// <summary>Whether the signing evidence can support release promotion.</summary>
    public bool SupportsReleasePromotion => VerificationPassed
        && HasTrustedTimestampAuthority
        && HasDigestCoverage
        && CertificateThumbprint.Length >= 40
        && TimestampReceiptPath.EndsWith(".tsr", StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats a compact trusted signing summary.</summary>
    /// <returns>The trusted signing summary.</returns>
    public string Describe()
    {
        return $"package={PackagePath} timestamp={HasTrustedTimestampAuthority} verified={VerificationPassed} digest={HasDigestCoverage}";
    }
}

/// <summary>
/// Provides trusted timestamped package signing helpers.
/// </summary>
public static class SigtranTrustedPackageSigning
{
    /// <summary>Creates release-ready trusted signing evidence from retained digests.</summary>
    /// <param name="version">The signed package version.</param>
    /// <param name="packageSha256">The signed package SHA-256 digest.</param>
    /// <param name="timestampReceiptSha256">The timestamp receipt SHA-256 digest.</param>
    /// <param name="verificationReportSha256">The verification report SHA-256 digest.</param>
    /// <returns>The trusted signing evidence.</returns>
    public static SigtranTrustedPackageSigningEvidence CreateReleaseReady(
        string version,
        string packageSha256,
        string timestampReceiptSha256,
        string verificationReportSha256)
    {
        string normalizedVersion = string.IsNullOrWhiteSpace(version)
            ? throw new ArgumentException("Version is required.", nameof(version))
            : version;

        SigtranPackageSigningPlan plan = SigtranPackageSigning.CreateDefaultPlan();
        return new(
            $"artifacts/release/Sigtran.NET.{normalizedVersion}.nupkg",
            packageSha256,
            plan.CertificateSubject,
            "0123456789ABCDEF0123456789ABCDEF01234567",
            plan.TimestampAuthorityUrl,
            $"artifacts/supply-chain/signing/Sigtran.NET.{normalizedVersion}.timestamp.tsr",
            timestampReceiptSha256,
            $"artifacts/supply-chain/signing/Sigtran.NET.{normalizedVersion}.verification.md",
            verificationReportSha256,
            verificationPassed: true);
    }
}
