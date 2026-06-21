namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies maintained external peer lab command kinds.
/// </summary>
public enum SigtranMaintainedPeerLabCommandKind
{
    /// <summary>Prepare lab directories and configuration.</summary>
    Prepare,

    /// <summary>Start packet capture.</summary>
    Capture,

    /// <summary>Start or verify the maintained external peer.</summary>
    StartPeer,

    /// <summary>Run the SDK side of the lab traffic.</summary>
    RunSdk,

    /// <summary>Compare retained traces against expected traffic.</summary>
    Compare,

    /// <summary>Collect and summarize artifacts.</summary>
    Collect
}

/// <summary>
/// Describes one maintained external peer lab command.
/// </summary>
public sealed class SigtranMaintainedPeerLabCommand
{
    /// <summary>Creates a maintained external peer lab command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="name">The stable command name.</param>
    /// <param name="commandLine">The command line.</param>
    /// <param name="expectedArtifactKinds">The artifact kinds expected from the command.</param>
    public SigtranMaintainedPeerLabCommand(
        SigtranMaintainedPeerLabCommandKind kind,
        string name,
        string commandLine,
        IReadOnlyList<SigtranMaintainedPeerLabArtifactKind> expectedArtifactKinds)
    {
        ArgumentNullException.ThrowIfNull(expectedArtifactKinds);
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandLine = string.IsNullOrWhiteSpace(commandLine) ? throw new ArgumentException("Command line is required.", nameof(commandLine)) : commandLine;
        ExpectedArtifactKinds = expectedArtifactKinds.ToArray();
    }

    /// <summary>The command kind.</summary>
    public SigtranMaintainedPeerLabCommandKind Kind { get; }

    /// <summary>The stable command name.</summary>
    public string Name { get; }

    /// <summary>The command line.</summary>
    public string CommandLine { get; }

    /// <summary>The artifact kinds expected from the command.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabArtifactKind> ExpectedArtifactKinds { get; }

    /// <summary>Formats a compact command summary.</summary>
    /// <returns>The command summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} name={Name} artifacts={ExpectedArtifactKinds.Count}";
    }
}

/// <summary>
/// Describes the maintained external peer lab command plan.
/// </summary>
public sealed class SigtranMaintainedPeerLabCommandPlan
{
    private readonly SigtranMaintainedPeerLabCommand[] _commands;

    /// <summary>Creates a maintained external peer lab command plan.</summary>
    /// <param name="configuration">The lab configuration.</param>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="commands">The ordered commands.</param>
    public SigtranMaintainedPeerLabCommandPlan(
        SigtranMaintainedPeerLabConfiguration configuration,
        SigtranMaintainedPeerLabArtifactPlan artifactPlan,
        IReadOnlyList<SigtranMaintainedPeerLabCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(commands);

        Configuration = configuration;
        ArtifactPlan = artifactPlan;
        _commands = commands.Count == 0 ? throw new ArgumentException("At least one lab command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The lab configuration.</summary>
    public SigtranMaintainedPeerLabConfiguration Configuration { get; }

    /// <summary>The artifact plan.</summary>
    public SigtranMaintainedPeerLabArtifactPlan ArtifactPlan { get; }

    /// <summary>The ordered commands.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabCommand> Commands => _commands.ToArray();

    /// <summary>Whether the command plan contains every required command kind.</summary>
    public bool CoversRequiredCommandKinds => RequiredKinds.All(kind => _commands.Any(command => command.Kind == kind));

    /// <summary>Formats a compact command plan summary.</summary>
    /// <returns>The command plan summary.</returns>
    public string Describe()
    {
        return $"peer={Configuration.PeerName} run={ArtifactPlan.RunId} commands={_commands.Length} ready={CoversRequiredCommandKinds}";
    }

    private static readonly SigtranMaintainedPeerLabCommandKind[] RequiredKinds =
    [
        SigtranMaintainedPeerLabCommandKind.Prepare,
        SigtranMaintainedPeerLabCommandKind.Capture,
        SigtranMaintainedPeerLabCommandKind.StartPeer,
        SigtranMaintainedPeerLabCommandKind.RunSdk,
        SigtranMaintainedPeerLabCommandKind.Compare,
        SigtranMaintainedPeerLabCommandKind.Collect
    ];
}

/// <summary>
/// Provides maintained external peer lab command plan helpers.
/// </summary>
public static class SigtranMaintainedPeerLabCommandPlans
{
    /// <summary>Creates the default maintained external peer lab command plan.</summary>
    /// <param name="configuration">The lab configuration.</param>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <returns>The default command plan.</returns>
    public static SigtranMaintainedPeerLabCommandPlan CreateDefault(
        SigtranMaintainedPeerLabConfiguration configuration,
        SigtranMaintainedPeerLabArtifactPlan artifactPlan)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(artifactPlan);

        string configPath = FindArtifactPath(artifactPlan, SigtranMaintainedPeerLabArtifactKind.PeerConfiguration);
        string pcapPath = FindArtifactPath(artifactPlan, SigtranMaintainedPeerLabArtifactKind.PacketCapture);
        string peerLogPath = FindArtifactPath(artifactPlan, SigtranMaintainedPeerLabArtifactKind.PeerLog);
        string sdkTracePath = FindArtifactPath(artifactPlan, SigtranMaintainedPeerLabArtifactKind.SdkTrace);
        string comparisonPath = FindArtifactPath(artifactPlan, SigtranMaintainedPeerLabArtifactKind.ComparisonReport);
        string reportPath = FindArtifactPath(artifactPlan, SigtranMaintainedPeerLabArtifactKind.RunReport);

        return new(
            configuration,
            artifactPlan,
            [
                new(SigtranMaintainedPeerLabCommandKind.Prepare, "prepare-artifacts", $"mkdir -p {artifactPlan.ArtifactRoot}/pcap {artifactPlan.ArtifactRoot}/logs {artifactPlan.ArtifactRoot}/config {artifactPlan.ArtifactRoot}/trace {artifactPlan.ArtifactRoot}/comparison {artifactPlan.ArtifactRoot}/reports && env > {configPath}", [SigtranMaintainedPeerLabArtifactKind.PeerConfiguration]),
                new(SigtranMaintainedPeerLabCommandKind.Capture, "capture-sctp", $"tcpdump -i lo -w {pcapPath} 'sctp or port {configuration.LocalSctpPort} or port {configuration.RemoteSctpPort}'", [SigtranMaintainedPeerLabArtifactKind.PacketCapture]),
                new(SigtranMaintainedPeerLabCommandKind.StartPeer, "start-maintained-peer", $"external-peer-runner --config {configPath} --log {peerLogPath}", [SigtranMaintainedPeerLabArtifactKind.PeerLog]),
                new(SigtranMaintainedPeerLabCommandKind.RunSdk, "run-sdk-traffic", $"dotnet run --project samples/maintained-peer-lab -- --config {configPath} --trace {sdkTracePath}", [SigtranMaintainedPeerLabArtifactKind.SdkTrace]),
                new(SigtranMaintainedPeerLabCommandKind.Compare, "compare-traces", $"sigtran-trace-compare --sdk {sdkTracePath} --pcap {pcapPath} --out {comparisonPath}", [SigtranMaintainedPeerLabArtifactKind.ComparisonReport]),
                new(SigtranMaintainedPeerLabCommandKind.Collect, "collect-run-report", $"sigtran-lab-report --artifacts {artifactPlan.ArtifactRoot} --out {reportPath}", [SigtranMaintainedPeerLabArtifactKind.RunReport])
            ]);
    }

    private static string FindArtifactPath(SigtranMaintainedPeerLabArtifactPlan artifactPlan, SigtranMaintainedPeerLabArtifactKind kind)
    {
        return artifactPlan.Items.First(item => item.Kind == kind).Path;
    }
}
