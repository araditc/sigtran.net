namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes open-source license compliance obligations.
/// </summary>
public sealed class SigtranLicenseCompliancePolicy
{
    /// <summary>Creates a license compliance policy.</summary>
    /// <param name="projectLicense">The project license expression.</param>
    /// <param name="requiresThirdPartyNotices">Whether third-party notices are required.</param>
    /// <param name="requiresDependencyReview">Whether dependency license review is required.</param>
    /// <param name="allowsCommercialUse">Whether commercial use is allowed by the project license.</param>
    public SigtranLicenseCompliancePolicy(
        string projectLicense,
        bool requiresThirdPartyNotices,
        bool requiresDependencyReview,
        bool allowsCommercialUse)
    {
        ProjectLicense = string.IsNullOrWhiteSpace(projectLicense) ? throw new ArgumentException("Project license is required.", nameof(projectLicense)) : projectLicense;
        RequiresThirdPartyNotices = requiresThirdPartyNotices;
        RequiresDependencyReview = requiresDependencyReview;
        AllowsCommercialUse = allowsCommercialUse;
    }

    /// <summary>The project license expression.</summary>
    public string ProjectLicense { get; }

    /// <summary>Whether third-party notices are required.</summary>
    public bool RequiresThirdPartyNotices { get; }

    /// <summary>Whether dependency license review is required.</summary>
    public bool RequiresDependencyReview { get; }

    /// <summary>Whether commercial use is allowed by the project license.</summary>
    public bool AllowsCommercialUse { get; }

    /// <summary>Whether the license policy is ready for commercial adoption.</summary>
    public bool IsCommercialReady => ProjectLicense == "Apache-2.0"
        && RequiresThirdPartyNotices
        && RequiresDependencyReview
        && AllowsCommercialUse;
}

/// <summary>
/// Provides license compliance policy helpers.
/// </summary>
public static class SigtranLicenseCompliance
{
    /// <summary>Creates the current license compliance policy.</summary>
    /// <returns>The current license compliance policy.</returns>
    public static SigtranLicenseCompliancePolicy CreateCurrentPolicy()
    {
        return new(
            "Apache-2.0",
            requiresThirdPartyNotices: true,
            requiresDependencyReview: true,
            allowsCommercialUse: true);
    }
}
