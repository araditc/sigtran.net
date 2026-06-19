namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the protocol interoperability vector CI profile.
/// </summary>
public sealed class SigtranProtocolInteropCiProfile
{
    /// <summary>Creates a protocol interoperability CI profile.</summary>
    /// <param name="enableVariable">The enable variable.</param>
    /// <param name="vectorRootVariable">The external vector root variable.</param>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresExternalVectors">Whether external vectors are required.</param>
    public SigtranProtocolInteropCiProfile(
        string enableVariable,
        string vectorRootVariable,
        IReadOnlyList<string> commands,
        bool requiresExternalVectors)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        VectorRootVariable = string.IsNullOrWhiteSpace(vectorRootVariable) ? throw new ArgumentException("Vector root variable is required.", nameof(vectorRootVariable)) : vectorRootVariable;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresExternalVectors = requiresExternalVectors;
    }

    /// <summary>The enable variable.</summary>
    public string EnableVariable { get; }

    /// <summary>The external vector root variable.</summary>
    public string VectorRootVariable { get; }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether external vectors are required.</summary>
    public bool RequiresExternalVectors { get; }

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
/// Provides protocol interoperability CI profile helpers.
/// </summary>
public static class SigtranProtocolInteropCi
{
    /// <summary>Creates the default protocol interoperability CI profile.</summary>
    /// <returns>The default protocol interoperability CI profile.</returns>
    public static SigtranProtocolInteropCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_PROTOCOL_INTEROP",
            "SIGTRAN_PROTOCOL_VECTOR_ROOT",
            SigtranProtocolInteropCommands.CreateDefault().Commands,
            requiresExternalVectors: true);
    }
}
