namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes why a production release secret is required.
/// </summary>
public enum SigtranReleaseSecretPurpose
{
    /// <summary>The secret authorizes package publication.</summary>
    PackagePublication,

    /// <summary>The secret unlocks package signing material.</summary>
    PackageSigning,

    /// <summary>The secret authorizes provenance or release attestation upload.</summary>
    ProvenanceAttestation
}

/// <summary>
/// Describes one secret required by the production release process.
/// </summary>
public sealed class SigtranReleaseSecretRequirement
{
    /// <summary>Creates a production release secret requirement.</summary>
    /// <param name="name">The environment or CI secret name.</param>
    /// <param name="purpose">The operational purpose for the secret.</param>
    /// <param name="requiredForStable">Whether stable publication requires the secret.</param>
    /// <param name="summary">A human-readable requirement summary.</param>
    public SigtranReleaseSecretRequirement(
        string name,
        SigtranReleaseSecretPurpose purpose,
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
    public SigtranReleaseSecretPurpose Purpose { get; }

    /// <summary>Whether stable publication requires the secret.</summary>
    public bool RequiredForStable { get; }

    /// <summary>A human-readable requirement summary.</summary>
    public string Summary { get; }
}

/// <summary>
/// Describes available production release secrets without exposing secret values.
/// </summary>
public sealed class SigtranReleaseSecretInventory
{
    private readonly HashSet<string> availableSecretNames;

    /// <summary>Creates a production release secret inventory.</summary>
    /// <param name="availableSecretNames">The available secret names.</param>
    public SigtranReleaseSecretInventory(IEnumerable<string> availableSecretNames)
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
/// Reports readiness for production release secrets.
/// </summary>
public sealed class SigtranReleaseSecretReadiness
{
    /// <summary>Creates a production release secret readiness report.</summary>
    /// <param name="requirements">The configured secret requirements.</param>
    /// <param name="missingSecretNames">The missing required secret names.</param>
    public SigtranReleaseSecretReadiness(
        IReadOnlyList<SigtranReleaseSecretRequirement> requirements,
        IReadOnlyList<string> missingSecretNames)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        ArgumentNullException.ThrowIfNull(missingSecretNames);
        Requirements = requirements.Count == 0 ? throw new ArgumentException("At least one secret requirement is required.", nameof(requirements)) : requirements.ToArray();
        MissingSecretNames = missingSecretNames.ToArray();
    }

    /// <summary>The configured secret requirements.</summary>
    public IReadOnlyList<SigtranReleaseSecretRequirement> Requirements { get; }

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
/// Defines the production release secret contract.
/// </summary>
public static class SigtranReleaseSecrets
{
    /// <summary>Creates the default production release secret requirements.</summary>
    /// <returns>The default secret requirements.</returns>
    public static IReadOnlyList<SigtranReleaseSecretRequirement> CreateDefaultRequirements()
    {
        return
        [
            new("NUGET_API_KEY", SigtranReleaseSecretPurpose.PackagePublication, true, "Authorizes package upload to the package registry."),
            new("SIGNING_CERTIFICATE", SigtranReleaseSecretPurpose.PackageSigning, true, "Provides trusted package signing material."),
            new("SIGNING_CERTIFICATE_PASSWORD", SigtranReleaseSecretPurpose.PackageSigning, true, "Unlocks package signing material inside the protected release environment."),
            new("PROVENANCE_ATTESTATION_TOKEN", SigtranReleaseSecretPurpose.ProvenanceAttestation, true, "Authorizes provenance attestation upload when OIDC is not used.")
        ];
    }

    /// <summary>Evaluates secret readiness from available secret names.</summary>
    /// <param name="availableSecretNames">The available secret names.</param>
    /// <returns>The production release secret readiness report.</returns>
    public static SigtranReleaseSecretReadiness Evaluate(IEnumerable<string> availableSecretNames)
    {
        IReadOnlyList<SigtranReleaseSecretRequirement> requirements = CreateDefaultRequirements();
        SigtranReleaseSecretInventory inventory = new(availableSecretNames);
        string[] missing = requirements
            .Where(requirement => !inventory.Contains(requirement.Name))
            .Select(static requirement => requirement.Name)
            .ToArray();

        return new(requirements, missing);
    }
}
