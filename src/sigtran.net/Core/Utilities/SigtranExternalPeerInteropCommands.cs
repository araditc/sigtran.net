namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes OpenSS7/IPSS7 interoperability command requirements.
/// </summary>
public sealed class SigtranExternalPeerInteropCommandSet
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability command set.</summary>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresExternalPeer">Whether OpenSS7/IPSS7 installation is required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture tooling is required.</param>
    public SigtranExternalPeerInteropCommandSet(
        IReadOnlyList<string> commands,
        bool requiresExternalPeer,
        bool requiresPacketCapture)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresExternalPeer = requiresExternalPeer;
        RequiresPacketCapture = requiresPacketCapture;
    }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether OpenSS7/IPSS7 installation is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>Whether packet capture tooling is required.</summary>
    public bool RequiresPacketCapture { get; }
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability command helpers.
/// </summary>
public static class SigtranExternalPeerInteropCommands
{
    /// <summary>Creates the default OpenSS7/IPSS7 interoperability command set.</summary>
    /// <returns>The default OpenSS7/IPSS7 interoperability command set.</returns>
    public static SigtranExternalPeerInteropCommandSet CreateDefault()
    {
        return new(
            [
                "ipss7_config --show || true",
                "tcpdump -i any -w artifacts/external-peer/m3ua-asp-to-sg/openss7.pcapng sctp",
                "SIGTRAN_INTEROP_LAB=1 SIGTRAN_INTEROP_PEER=external-sigtran-peer dotnet run --project src/sigtran.net.Tests/sigtran.net.Tests.csproj --configuration Release",
                "dotnet sigtran-trace-compare artifacts/external-peer/m3ua-asp-to-sg"
            ],
            requiresExternalPeer: true,
            requiresPacketCapture: true);
    }
}
