namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the supply-chain CI profile.
/// </summary>
public sealed class SigtranSupplyChainCiProfile
{
    /// <summary>Creates a supply-chain CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="artifactRootVariable">The artifact root variable.</param>
    /// <param name="requiredSecrets">The required secret names.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresSigningSecrets">Whether signing secrets are required.</param>
    public SigtranSupplyChainCiProfile(
        string enableVariable,
        string artifactRootVariable,
        IReadOnlyList<string> requiredSecrets,
        IReadOnlyList<string> commands,
        bool requiresSigningSecrets)
    {
        ArgumentNullException.ThrowIfNull(requiredSecrets);
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        ArtifactRootVariable = string.IsNullOrWhiteSpace(artifactRootVariable) ? throw new ArgumentException("Artifact root variable is required.", nameof(artifactRootVariable)) : artifactRootVariable;
        RequiredSecrets = requiredSecrets.ToArray();
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresSigningSecrets = requiresSigningSecrets;
    }

    /// <summary>The enable variable.</summary>
    public string EnableVariable { get; }

    /// <summary>The artifact root variable.</summary>
    public string ArtifactRootVariable { get; }

    /// <summary>The required secret names.</summary>
    public IReadOnlyList<string> RequiredSecrets { get; }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether signing secrets are required.</summary>
    public bool RequiresSigningSecrets { get; }

    /// <summary>Returns whether the profile is enabled by environment variables.</summary>
    /// <param name="environment">The environment variables.</param>
    /// <returns>True when enabled; otherwise false.</returns>
    public bool IsEnabled(IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.TryGetValue(EnableVariable, out string? value)
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Provides supply-chain CI profile helpers.
/// </summary>
public static class SigtranSupplyChainCi
{
    /// <summary>Creates the default supply-chain CI profile.</summary>
    /// <returns>The default supply-chain CI profile.</returns>
    public static SigtranSupplyChainCiProfile CreateDefault()
    {
        SigtranSupplyChainAutomationPlan plan = SigtranSupplyChainAutomation.CreateDefaultPlan();
        return new(
            "SIGTRAN_SUPPLY_CHAIN",
            "SIGTRAN_SUPPLY_CHAIN_ARTIFACT_ROOT",
            ["SIGNING_CERTIFICATE", "SIGNING_CERTIFICATE_PASSWORD"],
            plan.GetCommands(),
            requiresSigningSecrets: true);
    }
}
