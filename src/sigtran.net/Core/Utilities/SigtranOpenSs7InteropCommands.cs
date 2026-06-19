namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes OpenSS7/IPSS7 interoperability command requirements.
/// </summary>
public sealed class SigtranOpenSs7InteropCommandSet
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability command set.</summary>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresOpenSs7">Whether OpenSS7/IPSS7 installation is required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture tooling is required.</param>
    public SigtranOpenSs7InteropCommandSet(
        IReadOnlyList<string> commands,
        bool requiresOpenSs7,
        bool requiresPacketCapture)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresOpenSs7 = requiresOpenSs7;
        RequiresPacketCapture = requiresPacketCapture;
    }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether OpenSS7/IPSS7 installation is required.</summary>
    public bool RequiresOpenSs7 { get; }

    /// <summary>Whether packet capture tooling is required.</summary>
    public bool RequiresPacketCapture { get; }
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability command helpers.
/// </summary>
public static class SigtranOpenSs7InteropCommands
{
    /// <summary>Creates the default OpenSS7/IPSS7 interoperability command set.</summary>
    /// <returns>The default OpenSS7/IPSS7 interoperability command set.</returns>
    public static SigtranOpenSs7InteropCommandSet CreateDefault()
    {
        return new(
            [
                "ipss7_config --show || true",
                "tcpdump -i any -w artifacts/openss7/m3ua-asp-to-sg/openss7.pcapng sctp",
                "SIGTRAN_INTEROP_LAB=1 SIGTRAN_INTEROP_PEER=openss7-ipss7 dotnet run --project src/sigtran.net.Tests/sigtran.net.Tests.csproj --configuration Release",
                "dotnet sigtran-trace-compare artifacts/openss7/m3ua-asp-to-sg"
            ],
            requiresOpenSs7: true,
            requiresPacketCapture: true);
    }
}
