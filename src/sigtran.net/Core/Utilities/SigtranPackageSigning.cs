namespace sigtran.net.Core.Utilities;

/// <summary>
/// Identifies a package signing mode.
/// </summary>
public enum SigtranPackageSigningMode
{
    /// <summary>No package signing is performed.</summary>
    None,

    /// <summary>Author signing is required.</summary>
    Author,

    /// <summary>Repository signing is required.</summary>
    Repository
}

/// <summary>
/// Describes the package signing plan for a governed release.
/// </summary>
public sealed class SigtranPackageSigningPlan
{
    /// <summary>Creates a package signing plan.</summary>
    /// <param name="mode">The signing mode.</param>
    /// <param name="certificateSubject">The expected certificate subject.</param>
    /// <param name="timestampAuthorityUrl">The timestamp authority URL.</param>
    /// <param name="isRequiredForCommercialRelease">Whether signing is required for commercial release.</param>
    public SigtranPackageSigningPlan(
        SigtranPackageSigningMode mode,
        string certificateSubject,
        string timestampAuthorityUrl,
        bool isRequiredForCommercialRelease)
    {
        Mode = mode;
        CertificateSubject = string.IsNullOrWhiteSpace(certificateSubject) ? throw new ArgumentException("Certificate subject is required.", nameof(certificateSubject)) : certificateSubject;
        TimestampAuthorityUrl = string.IsNullOrWhiteSpace(timestampAuthorityUrl) ? throw new ArgumentException("Timestamp authority URL is required.", nameof(timestampAuthorityUrl)) : timestampAuthorityUrl;
        IsRequiredForCommercialRelease = isRequiredForCommercialRelease;
    }

    /// <summary>The signing mode.</summary>
    public SigtranPackageSigningMode Mode { get; }

    /// <summary>The expected certificate subject.</summary>
    public string CertificateSubject { get; }

    /// <summary>The timestamp authority URL.</summary>
    public string TimestampAuthorityUrl { get; }

    /// <summary>Whether signing is required for commercial release.</summary>
    public bool IsRequiredForCommercialRelease { get; }

    /// <summary>Whether the plan has signing material references.</summary>
    public bool HasSigningMaterialReferences => Mode != SigtranPackageSigningMode.None
        && CertificateSubject.Length > 0
        && TimestampAuthorityUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats a compact signing summary.</summary>
    /// <returns>The signing summary.</returns>
    public string Describe()
    {
        return $"mode={Mode} certificate={CertificateSubject} timestamp={TimestampAuthorityUrl} required={IsRequiredForCommercialRelease}";
    }
}

/// <summary>
/// Provides package signing planning helpers.
/// </summary>
public static class SigtranPackageSigning
{
    /// <summary>Creates the default commercial package signing plan.</summary>
    /// <returns>The default commercial package signing plan.</returns>
    public static SigtranPackageSigningPlan CreateDefaultPlan()
    {
        return new(
            SigtranPackageSigningMode.Author,
            "SIGTRAN.NET release signing",
            "https://timestamp.digicert.com",
            isRequiredForCommercialRelease: true);
    }
}
