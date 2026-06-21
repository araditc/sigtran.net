namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes an opt-in native SCTP lab profile.
/// </summary>
public sealed class NativeSctpLabProfile
{
    /// <summary>The environment variable that enables native SCTP lab tests.</summary>
    public const string EnableEnvironmentVariable = "SIGTRAN_NATIVE_SCTP_LAB";

    /// <summary>Creates a native SCTP lab profile.</summary>
    /// <param name="enabled">Whether the profile is enabled.</param>
    /// <param name="loopbackEndpoint">The loopback SCTP endpoint.</param>
    /// <param name="requiresExternalPeer">Whether an external peer is required.</param>
    public NativeSctpLabProfile(bool enabled, SctpEndpoint loopbackEndpoint, bool requiresExternalPeer)
    {
        Enabled = enabled;
        LoopbackEndpoint = loopbackEndpoint ?? throw new ArgumentNullException(nameof(loopbackEndpoint));
        RequiresExternalPeer = requiresExternalPeer;
    }

    /// <summary>Whether the profile is enabled.</summary>
    public bool Enabled { get; }

    /// <summary>The loopback SCTP endpoint.</summary>
    public SctpEndpoint LoopbackEndpoint { get; }

    /// <summary>Whether an external peer is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>Formats a compact lab profile summary.</summary>
    /// <returns>The lab profile summary.</returns>
    public string Describe()
    {
        return $"enabled={Enabled} loopback={LoopbackEndpoint} externalPeer={RequiresExternalPeer}";
    }
}

/// <summary>
/// Provides native SCTP lab profile helpers.
/// </summary>
public static class NativeSctpLab
{
    /// <summary>Creates the current native SCTP lab profile from environment variables.</summary>
    /// <returns>The native SCTP lab profile.</returns>
    public static NativeSctpLabProfile CreateFromEnvironment()
    {
        bool enabled = string.Equals(
            Environment.GetEnvironmentVariable(NativeSctpLabProfile.EnableEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

        return new(
            enabled,
            new SctpEndpoint("127.0.0.1", 2905),
            requiresExternalPeer: false);
    }
}
