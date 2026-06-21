namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes external peer interoperability command requirements.
/// </summary>
public sealed class SigtranExternalPeerInteropCommandSet
{
    /// <summary>Creates an external peer interoperability command set.</summary>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresExternalPeer">Whether an external peer package is required.</param>
    /// <param name="requiresPacketCapture">Whether packet capture tooling is required.</param>
    /// <param name="requiresSdkTrace">Whether SDK trace capture is required.</param>
    /// <param name="requiresComparisonReport">Whether trace comparison report generation is required.</param>
    /// <param name="requiredEnvironmentVariables">The required environment variables.</param>
    public SigtranExternalPeerInteropCommandSet(
        IReadOnlyList<string> commands,
        bool requiresExternalPeer,
        bool requiresPacketCapture,
        bool requiresSdkTrace = true,
        bool requiresComparisonReport = true,
        IReadOnlyList<string>? requiredEnvironmentVariables = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresExternalPeer = requiresExternalPeer;
        RequiresPacketCapture = requiresPacketCapture;
        RequiresSdkTrace = requiresSdkTrace;
        RequiresComparisonReport = requiresComparisonReport;
        RequiredEnvironmentVariables = requiredEnvironmentVariables is null || requiredEnvironmentVariables.Count == 0
            ? ["SIGTRAN_EXTERNAL_PEER_ID", "SIGTRAN_EXTERNAL_PEER_PACKAGE", "SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT"]
            : requiredEnvironmentVariables.ToArray();
    }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether an external peer package is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>Whether packet capture tooling is required.</summary>
    public bool RequiresPacketCapture { get; }

    /// <summary>Whether SDK trace capture is required.</summary>
    public bool RequiresSdkTrace { get; }

    /// <summary>Whether trace comparison report generation is required.</summary>
    public bool RequiresComparisonReport { get; }

    /// <summary>The required environment variables.</summary>
    public IReadOnlyList<string> RequiredEnvironmentVariables { get; }

    /// <summary>Whether the command set covers the commercial external peer lab workflow.</summary>
    public bool IsCommercialLabCommandSet => RequiresExternalPeer
        && RequiresPacketCapture
        && RequiresSdkTrace
        && RequiresComparisonReport
        && RequiredEnvironmentVariables.Count >= 3;
}

/// <summary>
/// Provides external peer interoperability command helpers.
/// </summary>
public static class SigtranExternalPeerInteropCommands
{
    /// <summary>Creates the default external peer interoperability command set.</summary>
    /// <returns>The default external peer interoperability command set.</returns>
    public static SigtranExternalPeerInteropCommandSet CreateDefault()
    {
        return new(
            [
                "export SIGTRAN_EXTERNAL_PEER_ID=${SIGTRAN_EXTERNAL_PEER_ID:-external-sigtran-peer}",
                "test -n \"$SIGTRAN_EXTERNAL_PEER_PACKAGE\"",
                "export SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT=${SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT:-artifacts/external-peer}",
                "mkdir -p \"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT/pcap\" \"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT/logs\" \"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT/trace\" \"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT/comparison\"",
                "tcpdump -i ${SIGTRAN_CAPTURE_INTERFACE:-any} -w \"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT/pcap/m3ua-asp-to-sg.pcapng\" sctp",
                "SIGTRAN_INTEROP_LAB=1 SIGTRAN_INTEROP_PEER=$SIGTRAN_EXTERNAL_PEER_ID SIGTRAN_INTEROP_TRACE_ROOT=\"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT/trace\" dotnet run --project src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj --configuration Release",
                "dotnet sigtran-trace-compare \"$SIGTRAN_EXTERNAL_PEER_ARTIFACT_ROOT\""
            ],
            requiresExternalPeer: true,
            requiresPacketCapture: true);
    }
}
