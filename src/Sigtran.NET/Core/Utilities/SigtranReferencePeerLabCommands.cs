namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab command kinds.
/// </summary>
public enum SigtranReferencePeerLabCommandKind
{
    /// <summary>Prepare lab directories and configuration.</summary>
    Prepare,

    /// <summary>Start packet capture.</summary>
    Capture,

    /// <summary>Start or verify the reference external peer.</summary>
    StartPeer,

    /// <summary>Run the SDK side of the lab traffic.</summary>
    RunSdk,

    /// <summary>Compare retained traces against expected traffic.</summary>
    Compare,

    /// <summary>Collect and summarize artifacts.</summary>
    Collect
}

/// <summary>
/// Describes one reference external peer lab command.
/// </summary>
public sealed class SigtranReferencePeerLabCommand
{
    /// <summary>Creates a reference external peer lab command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="name">The stable command name.</param>
    /// <param name="commandLine">The command line.</param>
    /// <param name="expectedArtifactKinds">The artifact kinds expected from the command.</param>
    public SigtranReferencePeerLabCommand(
        SigtranReferencePeerLabCommandKind kind,
        string name,
        string commandLine,
        IReadOnlyList<SigtranReferencePeerLabArtifactKind> expectedArtifactKinds)
    {
        ArgumentNullException.ThrowIfNull(expectedArtifactKinds);
        Kind = kind;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandLine = string.IsNullOrWhiteSpace(commandLine) ? throw new ArgumentException("Command line is required.", nameof(commandLine)) : commandLine;
        ExpectedArtifactKinds = expectedArtifactKinds.ToArray();
    }

    /// <summary>The command kind.</summary>
    public SigtranReferencePeerLabCommandKind Kind { get; }

    /// <summary>The stable command name.</summary>
    public string Name { get; }

    /// <summary>The command line.</summary>
    public string CommandLine { get; }

    /// <summary>The artifact kinds expected from the command.</summary>
    public IReadOnlyList<SigtranReferencePeerLabArtifactKind> ExpectedArtifactKinds { get; }

    /// <summary>Formats a compact command summary.</summary>
    /// <returns>The command summary.</returns>
    public string Describe()
    {
        return $"kind={Kind} name={Name} artifacts={ExpectedArtifactKinds.Count}";
    }
}

/// <summary>
/// Describes the reference external peer lab command plan.
/// </summary>
public sealed class SigtranReferencePeerLabCommandPlan
{
    private readonly SigtranReferencePeerLabCommand[] _commands;

    /// <summary>Creates a reference external peer lab command plan.</summary>
    /// <param name="configuration">The lab configuration.</param>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <param name="commands">The ordered commands.</param>
    public SigtranReferencePeerLabCommandPlan(
        SigtranReferencePeerLabConfiguration configuration,
        SigtranReferencePeerLabArtifactPlan artifactPlan,
        IReadOnlyList<SigtranReferencePeerLabCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(artifactPlan);
        ArgumentNullException.ThrowIfNull(commands);

        Configuration = configuration;
        ArtifactPlan = artifactPlan;
        _commands = commands.Count == 0 ? throw new ArgumentException("At least one lab command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The lab configuration.</summary>
    public SigtranReferencePeerLabConfiguration Configuration { get; }

    /// <summary>The artifact plan.</summary>
    public SigtranReferencePeerLabArtifactPlan ArtifactPlan { get; }

    /// <summary>The ordered commands.</summary>
    public IReadOnlyList<SigtranReferencePeerLabCommand> Commands => _commands.ToArray();

    /// <summary>Whether the command plan contains every required command kind.</summary>
    public bool CoversRequiredCommandKinds => RequiredKinds.All(kind => _commands.Any(command => command.Kind == kind));

    /// <summary>Formats a compact command plan summary.</summary>
    /// <returns>The command plan summary.</returns>
    public string Describe()
    {
        return $"peer={Configuration.PeerName} run={ArtifactPlan.RunId} commands={_commands.Length} ready={CoversRequiredCommandKinds}";
    }

    private static readonly SigtranReferencePeerLabCommandKind[] RequiredKinds =
    [
        SigtranReferencePeerLabCommandKind.Prepare,
        SigtranReferencePeerLabCommandKind.Capture,
        SigtranReferencePeerLabCommandKind.StartPeer,
        SigtranReferencePeerLabCommandKind.RunSdk,
        SigtranReferencePeerLabCommandKind.Compare,
        SigtranReferencePeerLabCommandKind.Collect
    ];
}

/// <summary>
/// Provides reference external peer lab command plan helpers.
/// </summary>
public static class SigtranReferencePeerLabCommandPlans
{
    /// <summary>Creates the default reference external peer lab command plan.</summary>
    /// <param name="configuration">The lab configuration.</param>
    /// <param name="artifactPlan">The artifact plan.</param>
    /// <returns>The default command plan.</returns>
    public static SigtranReferencePeerLabCommandPlan CreateDefault(
        SigtranReferencePeerLabConfiguration configuration,
        SigtranReferencePeerLabArtifactPlan artifactPlan)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(artifactPlan);

        string configPath = FindArtifactPath(artifactPlan, SigtranReferencePeerLabArtifactKind.PeerConfiguration);
        string pcapPath = FindArtifactPath(artifactPlan, SigtranReferencePeerLabArtifactKind.PacketCapture);
        string peerLogPath = FindArtifactPath(artifactPlan, SigtranReferencePeerLabArtifactKind.PeerLog);
        string sdkTracePath = FindArtifactPath(artifactPlan, SigtranReferencePeerLabArtifactKind.SdkTrace);
        string comparisonPath = FindArtifactPath(artifactPlan, SigtranReferencePeerLabArtifactKind.ComparisonReport);
        string reportPath = FindArtifactPath(artifactPlan, SigtranReferencePeerLabArtifactKind.RunReport);

        return new(
            configuration,
            artifactPlan,
            [
                new(SigtranReferencePeerLabCommandKind.Prepare, "prepare-artifacts", $"mkdir -p {artifactPlan.ArtifactRoot}/pcap {artifactPlan.ArtifactRoot}/logs {artifactPlan.ArtifactRoot}/config {artifactPlan.ArtifactRoot}/trace {artifactPlan.ArtifactRoot}/comparison {artifactPlan.ArtifactRoot}/reports && env > {configPath}", [SigtranReferencePeerLabArtifactKind.PeerConfiguration]),
                new(SigtranReferencePeerLabCommandKind.Capture, "capture-sctp", $"tcpdump -i lo -w {pcapPath} 'sctp or port {configuration.LocalSctpPort} or port {configuration.RemoteSctpPort}'", [SigtranReferencePeerLabArtifactKind.PacketCapture]),
                new(SigtranReferencePeerLabCommandKind.StartPeer, "start-reference-peer", $"external-peer-runner --config {configPath} --log {peerLogPath}", [SigtranReferencePeerLabArtifactKind.PeerLog]),
                new(SigtranReferencePeerLabCommandKind.RunSdk, "run-sdk-traffic", $"dotnet run --project samples/reference-peer-lab -- --config {configPath} --trace {sdkTracePath}", [SigtranReferencePeerLabArtifactKind.SdkTrace]),
                new(SigtranReferencePeerLabCommandKind.Compare, "compare-traces", $"sigtran-trace-compare --sdk {sdkTracePath} --pcap {pcapPath} --out {comparisonPath}", [SigtranReferencePeerLabArtifactKind.ComparisonReport]),
                new(SigtranReferencePeerLabCommandKind.Collect, "collect-run-report", $"sigtran-lab-report --artifacts {artifactPlan.ArtifactRoot} --out {reportPath}", [SigtranReferencePeerLabArtifactKind.RunReport])
            ]);
    }

    private static string FindArtifactPath(SigtranReferencePeerLabArtifactPlan artifactPlan, SigtranReferencePeerLabArtifactKind kind)
    {
        return artifactPlan.Items.First(item => item.Kind == kind).Path;
    }
}
