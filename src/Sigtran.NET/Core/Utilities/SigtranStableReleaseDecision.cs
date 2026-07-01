namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable release release decision outcomes.
/// </summary>
public enum SigtranStableReleaseDecisionKind
{
    /// <summary>Stable release publication is blocked.</summary>
    Blocked,

    /// <summary>Stable release publication is approved for the next gate.</summary>
    Approved
}

/// <summary>
/// Describes a stable release release decision.
/// </summary>
public sealed class SigtranStableReleaseDecision
{
    /// <summary>Creates a stable release release decision.</summary>
    /// <param name="checklist">The reviewed stable release readiness checklist.</param>
    /// <param name="kind">The decision kind.</param>
    /// <param name="decidedBy">The decision maker identity.</param>
    /// <param name="decidedAtUtc">The UTC decision time.</param>
    /// <param name="reasons">The decision reasons.</param>
    public SigtranStableReleaseDecision(
        SigtranStableReleaseReadinessChecklist checklist,
        SigtranStableReleaseDecisionKind kind,
        string decidedBy,
        DateTimeOffset decidedAtUtc,
        IReadOnlyList<string> reasons)
    {
        Checklist = checklist ?? throw new ArgumentNullException(nameof(checklist));
        ArgumentNullException.ThrowIfNull(reasons);
        Kind = kind;
        DecidedBy = string.IsNullOrWhiteSpace(decidedBy) ? throw new ArgumentException("Decision maker is required.", nameof(decidedBy)) : decidedBy;
        DecidedAtUtc = decidedAtUtc.Offset == TimeSpan.Zero ? decidedAtUtc : decidedAtUtc.ToUniversalTime();
        Reasons = reasons.Count == 0 ? throw new ArgumentException("At least one decision reason is required.", nameof(reasons)) : reasons.ToArray();
    }

    /// <summary>The reviewed stable release readiness checklist.</summary>
    public SigtranStableReleaseReadinessChecklist Checklist { get; }

    /// <summary>The decision kind.</summary>
    public SigtranStableReleaseDecisionKind Kind { get; }

    /// <summary>The decision maker identity.</summary>
    public string DecidedBy { get; }

    /// <summary>The UTC decision time.</summary>
    public DateTimeOffset DecidedAtUtc { get; }

    /// <summary>The decision reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Whether the decision time is UTC.</summary>
    public bool HasUtcDecisionTime => DecidedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the decision approves stable release publication for the next gate.</summary>
    public bool ApprovesStablePublication => Kind == SigtranStableReleaseDecisionKind.Approved
        && Checklist.IsReadyForDecision;

    /// <summary>Whether the decision is ready for stable tag gate evaluation.</summary>
    public bool IsReadyForTagGate => ApprovesStablePublication
        && HasUtcDecisionTime;

    /// <summary>Formats a compact stable release release decision summary.</summary>
    /// <returns>The stable release release decision summary.</returns>
    public string Describe()
    {
        return $"stableReleaseDecision={Kind} version={Checklist.EvidenceMap.Target.Version} tagGateReady={IsReadyForTagGate} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Provides stable release release decision helpers.
/// </summary>
public static class SigtranStableReleaseDecisions
{
    /// <summary>Creates a stable release release decision from the reviewed readiness checklist.</summary>
    /// <param name="checklist">The reviewed stable release readiness checklist.</param>
    /// <param name="decidedBy">The decision maker identity.</param>
    /// <param name="decidedAtUtc">The UTC decision time.</param>
    /// <returns>The stable release release decision.</returns>
    public static SigtranStableReleaseDecision Decide(
        SigtranStableReleaseReadinessChecklist checklist,
        string decidedBy,
        DateTimeOffset decidedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        if (checklist.IsReadyForDecision)
        {
            return new(
                checklist,
                SigtranStableReleaseDecisionKind.Approved,
                decidedBy,
                decidedAtUtc,
                ["stable-release-readiness-approved"]);
        }

        IReadOnlyList<string> blockers = checklist.GetBlockers();
        return new(
            checklist,
            SigtranStableReleaseDecisionKind.Blocked,
            decidedBy,
            decidedAtUtc,
            blockers.Count == 0 ? ["stable-release-readiness-required"] : blockers);
    }
}
