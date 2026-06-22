namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a commercial evidence file verification command kind.
/// </summary>
public enum SigtranCommercialEvidenceFileVerificationCommandKind
{
    /// <summary>Observe retained evidence files.</summary>
    ObserveRetainedFiles,

    /// <summary>Compute retained file digests.</summary>
    ComputeFileDigests,

    /// <summary>Compare observed digests with promotion handoff digests.</summary>
    CompareHandoffDigests,

    /// <summary>Write the file verification report.</summary>
    WriteVerificationReport,

    /// <summary>Write the retention ledger.</summary>
    WriteRetentionLedger,

    /// <summary>Create the integrity seal.</summary>
    CreateIntegritySeal,

    /// <summary>Create the publication attachment manifest.</summary>
    CreatePublicationAttachments,

    /// <summary>Evaluate the verified promotion gate.</summary>
    EvaluatePromotionGate
}

/// <summary>
/// Describes one commercial evidence file verification command.
/// </summary>
public sealed class SigtranCommercialEvidenceFileVerificationCommand
{
    /// <summary>Creates a commercial evidence file verification command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="order">The deterministic command order.</param>
    /// <param name="name">The command name.</param>
    /// <param name="commandText">The command text.</param>
    /// <param name="producesArtifact">Whether the command produces a retained artifact.</param>
    /// <param name="requiresApproval">Whether the command requires explicit release approval.</param>
    public SigtranCommercialEvidenceFileVerificationCommand(
        SigtranCommercialEvidenceFileVerificationCommandKind kind,
        int order,
        string name,
        string commandText,
        bool producesArtifact,
        bool requiresApproval)
    {
        Kind = kind;
        Order = order > 0 ? order : throw new ArgumentOutOfRangeException(nameof(order));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException("Command text is required.", nameof(commandText)) : commandText;
        ProducesArtifact = producesArtifact;
        RequiresApproval = requiresApproval;
    }

    /// <summary>The command kind.</summary>
    public SigtranCommercialEvidenceFileVerificationCommandKind Kind { get; }

    /// <summary>The deterministic command order.</summary>
    public int Order { get; }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string CommandText { get; }

    /// <summary>Whether the command produces a retained artifact.</summary>
    public bool ProducesArtifact { get; }

    /// <summary>Whether the command requires explicit release approval.</summary>
    public bool RequiresApproval { get; }

    /// <summary>Whether the command contract is complete.</summary>
    public bool IsReady => Order > 0
        && !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(CommandText);
}

/// <summary>
/// Describes the commercial evidence file verification command plan.
/// </summary>
public sealed class SigtranCommercialEvidenceFileVerificationCommandPlan
{
    /// <summary>Creates a commercial evidence file verification command plan.</summary>
    /// <param name="artifactRoot">The retained evidence artifact root.</param>
    /// <param name="commands">The ordered verification commands.</param>
    public SigtranCommercialEvidenceFileVerificationCommandPlan(
        string artifactRoot,
        IReadOnlyList<SigtranCommercialEvidenceFileVerificationCommand> commands)
    {
        ArtifactRoot = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one file verification command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The retained evidence artifact root.</summary>
    public string ArtifactRoot { get; }

    /// <summary>The ordered verification commands.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceFileVerificationCommand> Commands { get; }

    /// <summary>Whether command order is deterministic and contiguous.</summary>
    public bool UsesDeterministicOrder => Commands
        .OrderBy(static command => command.Order)
        .Select(static command => command.Order)
        .SequenceEqual(Enumerable.Range(1, Commands.Count));

    /// <summary>Whether every required command kind is represented.</summary>
    public bool CoversRequiredCommandKinds => Enum.GetValues<SigtranCommercialEvidenceFileVerificationCommandKind>()
        .All(kind => Commands.Any(command => command.Kind == kind));

    /// <summary>Whether retained verification artifact-producing commands are present.</summary>
    public bool ProducesRequiredArtifacts => Commands.Any(static command => command.Kind == SigtranCommercialEvidenceFileVerificationCommandKind.WriteVerificationReport && command.ProducesArtifact)
        && Commands.Any(static command => command.Kind == SigtranCommercialEvidenceFileVerificationCommandKind.WriteRetentionLedger && command.ProducesArtifact)
        && Commands.Any(static command => command.Kind == SigtranCommercialEvidenceFileVerificationCommandKind.CreateIntegritySeal && command.ProducesArtifact)
        && Commands.Any(static command => command.Kind == SigtranCommercialEvidenceFileVerificationCommandKind.CreatePublicationAttachments && command.ProducesArtifact);

    /// <summary>Whether explicit approval is reserved for the promotion gate command.</summary>
    public bool ApprovalReservedForPromotionGate => Commands
        .Where(static command => command.RequiresApproval)
        .All(static command => command.Kind == SigtranCommercialEvidenceFileVerificationCommandKind.EvaluatePromotionGate);

    /// <summary>Whether the command plan is ready for workflow materialization.</summary>
    public bool IsReady => UsesDeterministicOrder
        && CoversRequiredCommandKinds
        && ProducesRequiredArtifacts
        && ApprovalReservedForPromotionGate
        && Commands.All(static command => command.IsReady);

    /// <summary>Formats a compact command plan summary.</summary>
    /// <returns>The command plan summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceFileVerificationCommandsReady={IsReady} commands={Commands.Count} artifactRoot={ArtifactRoot}";
    }
}

/// <summary>
/// Provides commercial evidence file verification command plan helpers.
/// </summary>
public static class SigtranCommercialEvidenceFileVerificationCommands
{
    /// <summary>Creates the default commercial evidence file verification command plan.</summary>
    /// <param name="artifactRoot">The retained evidence artifact root.</param>
    /// <returns>The default commercial evidence file verification command plan.</returns>
    public static SigtranCommercialEvidenceFileVerificationCommandPlan CreateDefault(string artifactRoot)
    {
        string root = string.IsNullOrWhiteSpace(artifactRoot) ? throw new ArgumentException("Artifact root is required.", nameof(artifactRoot)) : artifactRoot;

        return new(
            root,
            [
                new(SigtranCommercialEvidenceFileVerificationCommandKind.ObserveRetainedFiles, 1, "observe-retained-files", $"sigtran evidence observe-files --artifact-root {root}", producesArtifact: false, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.ComputeFileDigests, 2, "compute-file-digests", $"sigtran evidence compute-digests --artifact-root {root}", producesArtifact: true, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.CompareHandoffDigests, 3, "compare-handoff-digests", $"sigtran evidence compare-digests --artifact-root {root}", producesArtifact: true, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.WriteVerificationReport, 4, "write-verification-report", $"sigtran evidence write-verification-report --artifact-root {root}", producesArtifact: true, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.WriteRetentionLedger, 5, "write-retention-ledger", $"sigtran evidence write-retention-ledger --artifact-root {root}", producesArtifact: true, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.CreateIntegritySeal, 6, "create-integrity-seal", $"sigtran evidence create-integrity-seal --artifact-root {root}", producesArtifact: true, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.CreatePublicationAttachments, 7, "create-publication-attachments", $"sigtran evidence create-publication-attachments --artifact-root {root}", producesArtifact: true, requiresApproval: false),
                new(SigtranCommercialEvidenceFileVerificationCommandKind.EvaluatePromotionGate, 8, "evaluate-promotion-gate", $"sigtran evidence evaluate-promotion-gate --artifact-root {root}", producesArtifact: true, requiresApproval: true)
            ]);
    }
}
