namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes package governance requirements for commercial release.
/// </summary>
public sealed class SigtranPackageGovernancePolicy
{
    /// <summary>Creates a package governance policy.</summary>
    /// <param name="requiresLicense">Whether a license is required.</param>
    /// <param name="requiresReadme">Whether a package README is required.</param>
    /// <param name="requiresRepositoryMetadata">Whether repository metadata is required.</param>
    /// <param name="requiresSymbols">Whether symbols are required.</param>
    /// <param name="requiresPackageSigning">Whether package signing is required.</param>
    /// <param name="requiresSbom">Whether an SBOM is required.</param>
    public SigtranPackageGovernancePolicy(
        bool requiresLicense,
        bool requiresReadme,
        bool requiresRepositoryMetadata,
        bool requiresSymbols,
        bool requiresPackageSigning,
        bool requiresSbom)
    {
        RequiresLicense = requiresLicense;
        RequiresReadme = requiresReadme;
        RequiresRepositoryMetadata = requiresRepositoryMetadata;
        RequiresSymbols = requiresSymbols;
        RequiresPackageSigning = requiresPackageSigning;
        RequiresSbom = requiresSbom;
    }

    /// <summary>Whether a license is required.</summary>
    public bool RequiresLicense { get; }

    /// <summary>Whether a package README is required.</summary>
    public bool RequiresReadme { get; }

    /// <summary>Whether repository metadata is required.</summary>
    public bool RequiresRepositoryMetadata { get; }

    /// <summary>Whether symbols are required.</summary>
    public bool RequiresSymbols { get; }

    /// <summary>Whether package signing is required.</summary>
    public bool RequiresPackageSigning { get; }

    /// <summary>Whether an SBOM is required.</summary>
    public bool RequiresSbom { get; }

    /// <summary>Whether all commercial governance requirements are satisfied by the current package configuration.</summary>
    public bool IsSatisfiedByCurrentPackage => RequiresLicense && RequiresReadme && RequiresRepositoryMetadata && RequiresSymbols && !RequiresPackageSigning && !RequiresSbom;

    /// <summary>Formats a compact governance summary.</summary>
    /// <returns>The governance summary.</returns>
    public string Describe()
    {
        return $"license={RequiresLicense} readme={RequiresReadme} repository={RequiresRepositoryMetadata} symbols={RequiresSymbols} signing={RequiresPackageSigning} sbom={RequiresSbom} currentSatisfied={IsSatisfiedByCurrentPackage}";
    }
}

/// <summary>
/// Provides package governance policies.
/// </summary>
public static class SigtranPackageGovernance
{
    /// <summary>Creates the current package governance policy.</summary>
    /// <returns>The current package governance policy.</returns>
    public static SigtranPackageGovernancePolicy CreateCurrentPolicy()
    {
        return new(
            requiresLicense: true,
            requiresReadme: true,
            requiresRepositoryMetadata: true,
            requiresSymbols: true,
            requiresPackageSigning: false,
            requiresSbom: false);
    }

    /// <summary>Creates the target commercial package governance policy.</summary>
    /// <returns>The target commercial package governance policy.</returns>
    public static SigtranPackageGovernancePolicy CreateCommercialTargetPolicy()
    {
        return new(
            requiresLicense: true,
            requiresReadme: true,
            requiresRepositoryMetadata: true,
            requiresSymbols: true,
            requiresPackageSigning: true,
            requiresSbom: true);
    }
}
