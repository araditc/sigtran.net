namespace Sigtran.NET.Core.Utilities;

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
    /// <param name="isRequiredForRelease">Whether signing is required for production release.</param>
    public SigtranPackageSigningPlan(
        SigtranPackageSigningMode mode,
        string certificateSubject,
        string timestampAuthorityUrl,
        bool isRequiredForRelease)
    {
        Mode = mode;
        CertificateSubject = string.IsNullOrWhiteSpace(certificateSubject) ? throw new ArgumentException("Certificate subject is required.", nameof(certificateSubject)) : certificateSubject;
        TimestampAuthorityUrl = string.IsNullOrWhiteSpace(timestampAuthorityUrl) ? throw new ArgumentException("Timestamp authority URL is required.", nameof(timestampAuthorityUrl)) : timestampAuthorityUrl;
        IsRequiredForRelease = isRequiredForRelease;
    }

    /// <summary>The signing mode.</summary>
    public SigtranPackageSigningMode Mode { get; }

    /// <summary>The expected certificate subject.</summary>
    public string CertificateSubject { get; }

    /// <summary>The timestamp authority URL.</summary>
    public string TimestampAuthorityUrl { get; }

    /// <summary>Whether signing is required for production release.</summary>
    public bool IsRequiredForRelease { get; }

    /// <summary>Whether the plan has signing material references.</summary>
    public bool HasSigningMaterialReferences => Mode != SigtranPackageSigningMode.None
        && CertificateSubject.Length > 0
        && TimestampAuthorityUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase);

    /// <summary>Formats a compact signing summary.</summary>
    /// <returns>The signing summary.</returns>
    public string Describe()
    {
        return $"mode={Mode} certificate={CertificateSubject} timestamp={TimestampAuthorityUrl} required={IsRequiredForRelease}";
    }
}

/// <summary>
/// Provides package signing planning helpers.
/// </summary>
public static class SigtranPackageSigning
{
    /// <summary>Creates the default production package signing plan.</summary>
    /// <returns>The default production package signing plan.</returns>
    public static SigtranPackageSigningPlan CreateDefaultPlan()
    {
        return new(
            SigtranPackageSigningMode.Author,
            "SIGTRAN.NET release signing",
            "http://timestamp.sectigo.com",
            isRequiredForRelease: true);
    }
}
