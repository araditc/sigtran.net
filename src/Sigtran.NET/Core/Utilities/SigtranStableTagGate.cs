namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable tag gate command kinds.
/// </summary>
public enum SigtranStableTagCommandKind
{
    /// <summary>Validate the approved stable release decision.</summary>
    ValidateStableDecision,

    /// <summary>Verify the source commit exists locally.</summary>
    VerifySourceCommit,

    /// <summary>Create the stable annotated Git tag.</summary>
    CreateAnnotatedTag,

    /// <summary>Verify the stable tag points to the expected commit.</summary>
    VerifyTagCommit,

    /// <summary>Push the stable tag to the remote repository.</summary>
    PushTag
}

/// <summary>
/// Describes one stable tag gate command.
/// </summary>
public sealed class SigtranStableTagCommand
{
    /// <summary>Creates a stable tag gate command.</summary>
    /// <param name="kind">The command kind.</param>
    /// <param name="order">The deterministic command order.</param>
    /// <param name="name">The command name.</param>
    /// <param name="commandText">The command text.</param>
    /// <param name="requiresApprovedDecision">Whether the command requires an approved stable decision.</param>
    public SigtranStableTagCommand(
        SigtranStableTagCommandKind kind,
        int order,
        string name,
        string commandText,
        bool requiresApprovedDecision)
    {
        Kind = kind;
        Order = order > 0 ? order : throw new ArgumentOutOfRangeException(nameof(order));
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Command name is required.", nameof(name)) : name;
        CommandText = string.IsNullOrWhiteSpace(commandText) ? throw new ArgumentException("Command text is required.", nameof(commandText)) : commandText;
        RequiresApprovedDecision = requiresApprovedDecision;
    }

    /// <summary>The command kind.</summary>
    public SigtranStableTagCommandKind Kind { get; }

    /// <summary>The deterministic command order.</summary>
    public int Order { get; }

    /// <summary>The command name.</summary>
    public string Name { get; }

    /// <summary>The command text.</summary>
    public string CommandText { get; }

    /// <summary>Whether the command requires an approved stable decision.</summary>
    public bool RequiresApprovedDecision { get; }

    /// <summary>Whether the command contract is complete.</summary>
    public bool IsReady => !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrWhiteSpace(CommandText);
}

/// <summary>
/// Describes the stable tag gate command plan.
/// </summary>
public sealed class SigtranStableTagCommandPlan
{
    /// <summary>Creates a stable tag gate command plan.</summary>
    /// <param name="decision">The approved stable release decision.</param>
    /// <param name="commands">The ordered tag commands.</param>
    public SigtranStableTagCommandPlan(
        SigtranStableCommercialReleaseDecision decision,
        IReadOnlyList<SigtranStableTagCommand> commands)
    {
        Decision = decision ?? throw new ArgumentNullException(nameof(decision));
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one stable tag command is required.", nameof(commands)) : commands.ToArray();
    }

    /// <summary>The approved stable release decision.</summary>
    public SigtranStableCommercialReleaseDecision Decision { get; }

    /// <summary>The ordered tag commands.</summary>
    public IReadOnlyList<SigtranStableTagCommand> Commands { get; }

    /// <summary>Whether command order is deterministic and contiguous.</summary>
    public bool UsesDeterministicOrder => Commands
        .OrderBy(static command => command.Order)
        .Select(static command => command.Order)
        .SequenceEqual(Enumerable.Range(1, Commands.Count));

    /// <summary>Whether every required tag command kind is present.</summary>
    public bool CoversRequiredCommandKinds => Enum.GetValues<SigtranStableTagCommandKind>()
        .All(kind => Commands.Any(command => command.Kind == kind));

    /// <summary>Whether the tag creation command pins the expected tag and commit.</summary>
    public bool CreateTagCommandPinsTarget => Commands
        .Where(static command => command.Kind == SigtranStableTagCommandKind.CreateAnnotatedTag)
        .All(command => command.CommandText.Contains(Decision.Checklist.EvidenceMap.Target.TargetTag, StringComparison.Ordinal)
            && command.CommandText.Contains(Decision.Checklist.EvidenceMap.Target.SourceCommit, StringComparison.Ordinal));

    /// <summary>Whether the push command only pushes the expected stable tag.</summary>
    public bool PushCommandUsesTargetTag => Commands
        .Where(static command => command.Kind == SigtranStableTagCommandKind.PushTag)
        .All(command => command.CommandText.EndsWith(Decision.Checklist.EvidenceMap.Target.TargetTag, StringComparison.Ordinal));

    /// <summary>Whether the tag command plan avoids NuGet publication commands.</summary>
    public bool ContainsNoPublishCommand => Commands.All(static command =>
        !command.CommandText.Contains("nuget push", StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether the command plan is ready for stable tag gate evaluation.</summary>
    public bool IsReady => Decision.IsReadyForTagGate
        && UsesDeterministicOrder
        && CoversRequiredCommandKinds
        && CreateTagCommandPinsTarget
        && PushCommandUsesTargetTag
        && ContainsNoPublishCommand
        && Commands.All(static command => command.IsReady && command.RequiresApprovedDecision);

    /// <summary>Formats a compact stable tag command plan summary.</summary>
    /// <returns>The stable tag command plan summary.</returns>
    public string Describe()
    {
        return $"stableTagCommandsReady={IsReady} tag={Decision.Checklist.EvidenceMap.Target.TargetTag} commands={Commands.Count}";
    }
}

/// <summary>
/// Describes stable tag gate evaluation.
/// </summary>
public sealed class SigtranStableTagGateResult
{
    /// <summary>Creates a stable tag gate evaluation result.</summary>
    /// <param name="commandPlan">The stable tag command plan.</param>
    /// <param name="protectedTagPolicyConfirmed">Whether protected tag policy is confirmed.</param>
    /// <param name="existingTagConflict">Whether the stable tag already exists for another target.</param>
    public SigtranStableTagGateResult(
        SigtranStableTagCommandPlan commandPlan,
        bool protectedTagPolicyConfirmed,
        bool existingTagConflict)
    {
        CommandPlan = commandPlan ?? throw new ArgumentNullException(nameof(commandPlan));
        ProtectedTagPolicyConfirmed = protectedTagPolicyConfirmed;
        ExistingTagConflict = existingTagConflict;
    }

    /// <summary>The stable tag command plan.</summary>
    public SigtranStableTagCommandPlan CommandPlan { get; }

    /// <summary>Whether protected tag policy is confirmed.</summary>
    public bool ProtectedTagPolicyConfirmed { get; }

    /// <summary>Whether the stable tag already exists for another target.</summary>
    public bool ExistingTagConflict { get; }

    /// <summary>Whether the stable tag gate can move to protected publication authorization.</summary>
    public bool IsReadyForAuthorization => CommandPlan.IsReady
        && ProtectedTagPolicyConfirmed
        && !ExistingTagConflict;

    /// <summary>Returns stable tag gate blockers.</summary>
    /// <returns>The stable tag gate blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!CommandPlan.IsReady)
        {
            blockers.Add("stable-tag-command-plan-not-ready");
        }

        if (!ProtectedTagPolicyConfirmed)
        {
            blockers.Add("protected-stable-tag-policy-required");
        }

        if (ExistingTagConflict)
        {
            blockers.Add("stable-tag-conflict");
        }

        return blockers;
    }

    /// <summary>Formats a compact stable tag gate summary.</summary>
    /// <returns>The stable tag gate summary.</returns>
    public string Describe()
    {
        return $"stableTagGateReady={IsReadyForAuthorization} tag={CommandPlan.Decision.Checklist.EvidenceMap.Target.TargetTag} blockers={GetBlockers().Count}";
    }
}

/// <summary>
/// Provides stable tag gate helpers.
/// </summary>
public static class SigtranStableTagGates
{
    /// <summary>Creates the stable tag command plan for an approved stable decision.</summary>
    /// <param name="decision">The approved stable release decision.</param>
    /// <returns>The stable tag command plan.</returns>
    public static SigtranStableTagCommandPlan CreateCommandPlan(SigtranStableCommercialReleaseDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        SigtranStableReleaseTarget target = decision.Checklist.EvidenceMap.Target;

        return new(
            decision,
            [
                new(SigtranStableTagCommandKind.ValidateStableDecision, 1, "validate-stable-decision", "test \"${SIGTRAN_STABLE_DECISION_APPROVED:-false}\" = \"true\"", requiresApprovedDecision: true),
                new(SigtranStableTagCommandKind.VerifySourceCommit, 2, "verify-source-commit", $"git rev-parse --verify {target.SourceCommit}^{{commit}}", requiresApprovedDecision: true),
                new(SigtranStableTagCommandKind.CreateAnnotatedTag, 3, "create-annotated-tag", $"git tag -a {target.TargetTag} {target.SourceCommit} -m \"Sigtran.NET {target.Version} stable commercial release\"", requiresApprovedDecision: true),
                new(SigtranStableTagCommandKind.VerifyTagCommit, 4, "verify-tag-commit", $"test \"$(git rev-list -n 1 {target.TargetTag})\" = \"{target.SourceCommit}\"", requiresApprovedDecision: true),
                new(SigtranStableTagCommandKind.PushTag, 5, "push-stable-tag", $"git push origin {target.TargetTag}", requiresApprovedDecision: true)
            ]);
    }

    /// <summary>Evaluates the stable tag gate.</summary>
    /// <param name="decision">The approved stable release decision.</param>
    /// <param name="protectedTagPolicyConfirmed">Whether protected tag policy is confirmed.</param>
    /// <param name="existingTagConflict">Whether the stable tag already exists for another target.</param>
    /// <returns>The stable tag gate result.</returns>
    public static SigtranStableTagGateResult Evaluate(
        SigtranStableCommercialReleaseDecision decision,
        bool protectedTagPolicyConfirmed,
        bool existingTagConflict)
    {
        return new(CreateCommandPlan(decision), protectedTagPolicyConfirmed, existingTagConflict);
    }
}
