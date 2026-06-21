namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes transport configuration defaults.
/// </summary>
public sealed class SigtranTransportConfiguration
{
    /// <summary>Creates transport configuration defaults.</summary>
    /// <param name="kind">The transport kind.</param>
    /// <param name="requiresPpid">Whether PPID configuration is required.</param>
    /// <param name="requiresStreamPolicy">Whether stream policy configuration is required.</param>
    /// <param name="requiresReconnectPolicy">Whether reconnect policy configuration is required.</param>
    public SigtranTransportConfiguration(
        string kind,
        bool requiresPpid,
        bool requiresStreamPolicy,
        bool requiresReconnectPolicy)
    {
        Kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("Transport kind is required.", nameof(kind)) : kind;
        RequiresPpid = requiresPpid;
        RequiresStreamPolicy = requiresStreamPolicy;
        RequiresReconnectPolicy = requiresReconnectPolicy;
    }

    /// <summary>The transport kind.</summary>
    public string Kind { get; }

    /// <summary>Whether PPID configuration is required.</summary>
    public bool RequiresPpid { get; }

    /// <summary>Whether stream policy configuration is required.</summary>
    public bool RequiresStreamPolicy { get; }

    /// <summary>Whether reconnect policy configuration is required.</summary>
    public bool RequiresReconnectPolicy { get; }

    /// <summary>Whether the transport configuration is suitable for SIGTRAN production use.</summary>
    public bool IsSigtranReady => Kind == "native-sctp"
        && RequiresPpid
        && RequiresStreamPolicy
        && RequiresReconnectPolicy;
}

/// <summary>
/// Provides transport configuration helpers.
/// </summary>
public static class SigtranTransportConfigurations
{
    /// <summary>Creates the default native SCTP transport configuration.</summary>
    /// <returns>The default native SCTP transport configuration.</returns>
    public static SigtranTransportConfiguration CreateNativeSctpDefault()
    {
        return new(
            "native-sctp",
            requiresPpid: true,
            requiresStreamPolicy: true,
            requiresReconnectPolicy: true);
    }
}
