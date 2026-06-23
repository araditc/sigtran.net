namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable commercial release decision outcomes.
/// </summary>
public enum SigtranStableCommercialReleaseDecisionKind
{
    /// <summary>Stable commercial publication is blocked.</summary>
    Blocked,

    /// <summary>Stable commercial publication is approved for the next gate.</summary>
    Approved
}

/// <summary>
/// Describes a stable commercial release decision.
/// </summary>
public sealed class SigtranStableCommercialReleaseDecision
{
    /// <summary>Creates a stable commercial release decision.</summary>
    /// <param name="checklist">The reviewed stable commercial readiness checklist.</param>
    /// <param name="kind">The decision kind.</param>
    /// <param name="decidedBy">The decision maker identity.</param>
    /// <param name="decidedAtUtc">The UTC decision time.</param>
    /// <param name="reasons">The decision reasons.</param>
    public SigtranStableCommercialReleaseDecision(
        SigtranStableCommercialReadinessChecklist checklist,
        SigtranStableCommercialReleaseDecisionKind kind,
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

    /// <summary>The reviewed stable commercial readiness checklist.</summary>
    public SigtranStableCommercialReadinessChecklist Checklist { get; }

    /// <summary>The decision kind.</summary>
    public SigtranStableCommercialReleaseDecisionKind Kind { get; }

    /// <summary>The decision maker identity.</summary>
    public string DecidedBy { get; }

    /// <summary>The UTC decision time.</summary>
    public DateTimeOffset DecidedAtUtc { get; }

    /// <summary>The decision reasons.</summary>
    public IReadOnlyList<string> Reasons { get; }

    /// <summary>Whether the decision time is UTC.</summary>
    public bool HasUtcDecisionTime => DecidedAtUtc.Offset == TimeSpan.Zero;

    /// <summary>Whether the decision approves stable commercial publication for the next gate.</summary>
    public bool ApprovesStablePublication => Kind == SigtranStableCommercialReleaseDecisionKind.Approved
        && Checklist.IsReadyForDecision;

    /// <summary>Whether the decision is ready for stable tag gate evaluation.</summary>
    public bool IsReadyForTagGate => ApprovesStablePublication
        && HasUtcDecisionTime;

    /// <summary>Formats a compact stable commercial release decision summary.</summary>
    /// <returns>The stable commercial release decision summary.</returns>
    public string Describe()
    {
        return $"stableCommercialReleaseDecision={Kind} version={Checklist.EvidenceMap.Target.Version} tagGateReady={IsReadyForTagGate} reasons={Reasons.Count}";
    }
}

/// <summary>
/// Provides stable commercial release decision helpers.
/// </summary>
public static class SigtranStableCommercialReleaseDecisions
{
    /// <summary>Creates a stable commercial release decision from the reviewed readiness checklist.</summary>
    /// <param name="checklist">The reviewed stable commercial readiness checklist.</param>
    /// <param name="decidedBy">The decision maker identity.</param>
    /// <param name="decidedAtUtc">The UTC decision time.</param>
    /// <returns>The stable commercial release decision.</returns>
    public static SigtranStableCommercialReleaseDecision Decide(
        SigtranStableCommercialReadinessChecklist checklist,
        string decidedBy,
        DateTimeOffset decidedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        if (checklist.IsReadyForDecision)
        {
            return new(
                checklist,
                SigtranStableCommercialReleaseDecisionKind.Approved,
                decidedBy,
                decidedAtUtc,
                ["stable-commercial-readiness-approved"]);
        }

        IReadOnlyList<string> blockers = checklist.GetBlockers();
        return new(
            checklist,
            SigtranStableCommercialReleaseDecisionKind.Blocked,
            decidedBy,
            decidedAtUtc,
            blockers.Count == 0 ? ["stable-commercial-readiness-required"] : blockers);
    }
}
