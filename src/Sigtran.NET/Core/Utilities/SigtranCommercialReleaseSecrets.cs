namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes why a commercial release secret is required.
/// </summary>
public enum SigtranCommercialReleaseSecretPurpose
{
    /// <summary>The secret authorizes package publication.</summary>
    PackagePublication,

    /// <summary>The secret unlocks package signing material.</summary>
    PackageSigning,

    /// <summary>The secret authorizes provenance or release attestation upload.</summary>
    ProvenanceAttestation
}

/// <summary>
/// Describes one secret required by the commercial release process.
/// </summary>
public sealed class SigtranCommercialReleaseSecretRequirement
{
    /// <summary>Creates a commercial release secret requirement.</summary>
    /// <param name="name">The environment or CI secret name.</param>
    /// <param name="purpose">The operational purpose for the secret.</param>
    /// <param name="requiredForStable">Whether stable publication requires the secret.</param>
    /// <param name="summary">A human-readable requirement summary.</param>
    public SigtranCommercialReleaseSecretRequirement(
        string name,
        SigtranCommercialReleaseSecretPurpose purpose,
        bool requiredForStable,
        string summary)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Secret name is required.", nameof(name)) : name;
        Purpose = purpose;
        RequiredForStable = requiredForStable;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Secret summary is required.", nameof(summary)) : summary;
    }

    /// <summary>The environment or CI secret name.</summary>
    public string Name { get; }

    /// <summary>The operational purpose for the secret.</summary>
    public SigtranCommercialReleaseSecretPurpose Purpose { get; }

    /// <summary>Whether stable publication requires the secret.</summary>
    public bool RequiredForStable { get; }

    /// <summary>A human-readable requirement summary.</summary>
    public string Summary { get; }
}

/// <summary>
/// Describes available commercial release secrets without exposing secret values.
/// </summary>
public sealed class SigtranCommercialReleaseSecretInventory
{
    private readonly HashSet<string> availableSecretNames;

    /// <summary>Creates a commercial release secret inventory.</summary>
    /// <param name="availableSecretNames">The available secret names.</param>
    public SigtranCommercialReleaseSecretInventory(IEnumerable<string> availableSecretNames)
    {
        ArgumentNullException.ThrowIfNull(availableSecretNames);
        this.availableSecretNames = new(availableSecretNames.Where(static name => !string.IsNullOrWhiteSpace(name)), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Checks whether a secret name is available.</summary>
    /// <param name="name">The required secret name.</param>
    /// <returns><c>true</c> when the named secret is available; otherwise, <c>false</c>.</returns>
    public bool Contains(string name)
    {
        return availableSecretNames.Contains(name);
    }
}

/// <summary>
/// Reports readiness for commercial release secrets.
/// </summary>
public sealed class SigtranCommercialReleaseSecretReadiness
{
    /// <summary>Creates a commercial release secret readiness report.</summary>
    /// <param name="requirements">The configured secret requirements.</param>
    /// <param name="missingSecretNames">The missing required secret names.</param>
    public SigtranCommercialReleaseSecretReadiness(
        IReadOnlyList<SigtranCommercialReleaseSecretRequirement> requirements,
        IReadOnlyList<string> missingSecretNames)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(missingSecretNames);
        Requirements = requirements.Count == 0 ? throw new ArgumentException("At least one secret requirement is required.", nameof(requirements)) : requirements.ToArray();
        MissingSecretNames = missingSecretNames.ToArray();
    }

    /// <summary>The configured secret requirements.</summary>
    public IReadOnlyList<SigtranCommercialReleaseSecretRequirement> Requirements { get; }

    /// <summary>The missing required secret names.</summary>
    public IReadOnlyList<string> MissingSecretNames { get; }

    /// <summary>Whether every required secret is available.</summary>
    public bool IsReady => MissingSecretNames.Count == 0;

    /// <summary>Formats a compact secret readiness summary.</summary>
    /// <returns>The secret readiness summary.</returns>
    public string Describe()
    {
        return $"releaseSecretsReady={IsReady} required={Requirements.Count} missing={MissingSecretNames.Count}";
    }
}

/// <summary>
/// Defines the commercial release secret contract.
/// </summary>
public static class SigtranCommercialReleaseSecrets
{
    /// <summary>Creates the default commercial release secret requirements.</summary>
    /// <returns>The default secret requirements.</returns>
    public static IReadOnlyList<SigtranCommercialReleaseSecretRequirement> CreateDefaultRequirements()
    {
        return
        [
            new("NUGET_API_KEY", SigtranCommercialReleaseSecretPurpose.PackagePublication, true, "Authorizes package upload to the package registry."),
            new("SIGNING_CERTIFICATE", SigtranCommercialReleaseSecretPurpose.PackageSigning, true, "Provides trusted package signing material."),
            new("SIGNING_CERTIFICATE_PASSWORD", SigtranCommercialReleaseSecretPurpose.PackageSigning, true, "Unlocks package signing material inside the protected release environment."),
            new("PROVENANCE_ATTESTATION_TOKEN", SigtranCommercialReleaseSecretPurpose.ProvenanceAttestation, true, "Authorizes provenance attestation upload when OIDC is not used.")
        ];
    }

    /// <summary>Evaluates secret readiness from available secret names.</summary>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <returns>The commercial release secret readiness report.</returns>
    public static SigtranCommercialReleaseSecretReadiness Evaluate(IEnumerable<string> availableSecretNames)
    {
        IReadOnlyList<SigtranCommercialReleaseSecretRequirement> requirements = CreateDefaultRequirements();
        SigtranCommercialReleaseSecretInventory inventory = new(availableSecretNames);
        string[] missing = requirements
            .Where(requirement => !inventory.Contains(requirement.Name))
            .Select(static requirement => requirement.Name)
            .ToArray();

        return new(requirements, missing);
    }
}
