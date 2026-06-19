namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the opt-in CI profile for external interoperability lab runs.
/// </summary>
public sealed class SigtranInteropLabCiProfile
{
    /// <summary>Creates an interoperability lab CI profile.</summary>
    /// <param name="enableVariable">The variable that enables lab execution.</param>
    /// <param name="artifactRootVariable">The artifact root variable.</param>
    /// <param name="peerVariable">The peer selector variable.</param>
    /// <param name="nativeSctpVariable">The native SCTP lab variable.</param>
    /// <param name="commands">The commands to run after the lab environment is prepared.</param>
    public SigtranInteropLabCiProfile(
        string enableVariable,
        string artifactRootVariable,
        string peerVariable,
        string nativeSctpVariable,
        IReadOnlyList<string> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        EnableVariable = string.IsNullOrWhiteSpace(enableVariable) ? throw new ArgumentException("Enable variable is required.", nameof(enableVariable)) : enableVariable;
        ArtifactRootVariable = string.IsNullOrWhiteSpace(artifactRootVariable) ? throw new ArgumentException("Artifact root variable is required.", nameof(artifactRootVariable)) : artifactRootVariable;
        PeerVariable = string.IsNullOrWhiteSpace(peerVariable) ? throw new ArgumentException("Peer variable is required.", nameof(peerVariable)) : peerVariable;
        NativeSctpVariable = string.IsNullOrWhiteSpace(nativeSctpVariable) ? throw new ArgumentException("Native SCTP variable is required.", nameof(nativeSctpVariable)) : nativeSctpVariable;
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The variable that enables lab execution.</summary>
    public string EnableVariable { get; }

    /// <summary>The artifact root variable.</summary>
    public string ArtifactRootVariable { get; }

    /// <summary>The peer selector variable.</summary>
    public string PeerVariable { get; }

    /// <summary>The native SCTP lab variable.</summary>
    public string NativeSctpVariable { get; }

    /// <summary>The commands to run after the lab environment is prepared.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>The required variable names.</summary>
    public IReadOnlyList<string> RequiredVariables => [EnableVariable, ArtifactRootVariable, PeerVariable];

    /// <summary>Returns whether the environment enables the external lab profile.</summary>
    /// <param name="environment">The environment variables.</param>
    /// <returns>True when the lab profile is enabled; otherwise false.</returns>
    public bool IsEnabled(IReadOnlyDictionary<string, string> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        return environment.TryGetValue(EnableVariable, out string? value)
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Provides the official external interoperability lab CI profile.
/// </summary>
public static class SigtranInteropLabCiProfiles
{
    /// <summary>Creates the default external interoperability lab CI profile.</summary>
    /// <returns>The default external interoperability lab CI profile.</returns>
    public static SigtranInteropLabCiProfile CreateDefault()
    {
        return new(
            "SIGTRAN_INTEROP_LAB",
            "SIGTRAN_INTEROP_LAB_ARTIFACT_ROOT",
            "SIGTRAN_INTEROP_PEER",
            "SIGTRAN_NATIVE_SCTP_LAB",
            [
                "dotnet build src/sigtran.net.sln --configuration Release",
                "dotnet run --project src/sigtran.net.Tests/sigtran.net.Tests.csproj --configuration Release",
                "dotnet pack src/sigtran.net/sigtran.net.csproj --configuration Release --no-build"
            ]);
    }
}
