namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable commercial readiness checklist areas.
/// </summary>
public enum SigtranStableCommercialReadinessArea
{
    /// <summary>Stable release target readiness.</summary>
    StableReleaseTarget,

    /// <summary>Commercial dossier evidence map readiness.</summary>
    CommercialDossierEvidence,

    /// <summary>Maintained external peer interoperability readiness.</summary>
    MaintainedExternalPeerInterop,

    /// <summary>Native SCTP production hardening readiness.</summary>
    NativeSctpHardening,

    /// <summary>SCCP, TCAP, and MAP interoperability readiness.</summary>
    ProtocolInterop,

    /// <summary>Representative performance benchmark readiness.</summary>
    PerformanceBenchmark,

    /// <summary>Supply-chain release evidence readiness.</summary>
    SupplyChainRelease,

    /// <summary>Public API baseline readiness.</summary>
    PublicApiBaseline,

    /// <summary>Operations and compliance readiness.</summary>
    OperationsCompliance,

    /// <summary>Publication dossier readiness.</summary>
    PublicationDossier
}

/// <summary>
/// Describes one stable commercial readiness checklist approval.
/// </summary>
public sealed class SigtranStableCommercialReadinessChecklistItem
{
    /// <summary>Creates a stable commercial readiness checklist approval.</summary>
    /// <param name="area">The readiness area.</param>
    /// <param name="approved">Whether the area has passed review.</param>
    /// <param name="evidencePath">The retained evidence path used for review.</param>
    /// <param name="reviewer">The reviewer identity.</param>
    /// <param name="reviewedAtUtc">The UTC review time.</param>
    /// <param name="notes">The reviewer notes.</param>
    public SigtranStableCommercialReadinessChecklistItem(
        SigtranStableCommercialReadinessArea area,
        bool approved,
        string evidencePath,
        string reviewer,
        DateTimeOffset? reviewedAtUtc,
        string notes)
    {
        Area = area;
        Approved = approved;
        EvidencePath = string.IsNullOrWhiteSpace(evidencePath) ? throw new ArgumentException("Evidence path is required.", nameof(evidencePath)) : evidencePath;
        Reviewer = string.IsNullOrWhiteSpace(reviewer) ? throw new ArgumentException("Reviewer is required.", nameof(reviewer)) : reviewer;
        ReviewedAtUtc = reviewedAtUtc is null
            ? null
            : reviewedAtUtc.Value.Offset == TimeSpan.Zero ? reviewedAtUtc.Value : reviewedAtUtc.Value.ToUniversalTime();
        Notes = string.IsNullOrWhiteSpace(notes) ? throw new ArgumentException("Review notes are required.", nameof(notes)) : notes;
    }

    /// <summary>The readiness area.</summary>
    public SigtranStableCommercialReadinessArea Area { get; }

    /// <summary>Whether the area has passed review.</summary>
    public bool Approved { get; }

    /// <summary>The retained evidence path used for review.</summary>
    public string EvidencePath { get; }

    /// <summary>The reviewer identity.</summary>
    public string Reviewer { get; }

    /// <summary>The UTC review time.</summary>
    public DateTimeOffset? ReviewedAtUtc { get; }

    /// <summary>The reviewer notes.</summary>
    public string Notes { get; }

    /// <summary>Whether the review has a UTC timestamp.</summary>
    public bool HasUtcReviewTime => ReviewedAtUtc.HasValue
        && ReviewedAtUtc.Value.Offset == TimeSpan.Zero;

    /// <summary>Whether the item is approved and reviewable.</summary>
    public bool IsReady => Approved
        && HasUtcReviewTime;
}

/// <summary>
/// Evaluates stable commercial readiness before stable release decisioning.
/// </summary>
public sealed class SigtranStableCommercialReadinessChecklist
{
    /// <summary>Creates a stable commercial readiness checklist.</summary>
    /// <param name="evidenceMap">The stable commercial dossier evidence map.</param>
    /// <param name="items">The readiness checklist items.</param>
    public SigtranStableCommercialReadinessChecklist(
        SigtranStableCommercialDossierEvidenceMap evidenceMap,
        IReadOnlyList<SigtranStableCommercialReadinessChecklistItem> items)
    {
        EvidenceMap = evidenceMap ?? throw new ArgumentNullException(nameof(evidenceMap));
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one readiness checklist item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The stable commercial dossier evidence map.</summary>
    public SigtranStableCommercialDossierEvidenceMap EvidenceMap { get; }

    /// <summary>The readiness checklist items.</summary>
    public IReadOnlyList<SigtranStableCommercialReadinessChecklistItem> Items { get; }

    /// <summary>Whether the underlying stable release target is ready.</summary>
    public bool HasReadyTarget => EvidenceMap.Target.IsReadyForStableGate;

    /// <summary>Whether the underlying commercial dossier evidence map is ready.</summary>
    public bool HasReadyEvidenceMap => EvidenceMap.IsReadyForChecklist;

    /// <summary>Whether readiness areas are unique.</summary>
    public bool HasUniqueAreas => Items
        .Select(static item => item.Area)
        .Distinct()
        .Count() == Items.Count;

    /// <summary>Whether every required readiness area is covered.</summary>
    public bool CoversRequiredAreas => RequiredAreas.All(area => Items.Any(item => item.Area == area));

    /// <summary>Whether every checklist item has passed review.</summary>
    public bool AllItemsApproved => Items.All(static item => item.IsReady);

    /// <summary>Whether the checklist can move to stable release decisioning.</summary>
    public bool IsReadyForDecision => HasReadyTarget
        && HasReadyEvidenceMap
        && HasUniqueAreas
        && CoversRequiredAreas
        && AllItemsApproved;

    /// <summary>Returns checklist blockers for stable release decisioning.</summary>
    /// <returns>The readiness blockers.</returns>
    public IReadOnlyList<string> GetBlockers()
    {
        List<string> blockers = [];

        if (!HasReadyTarget)
        {
            blockers.Add("stable-release-target-not-ready");
        }

        if (!HasReadyEvidenceMap)
        {
            blockers.Add("stable-dossier-evidence-map-not-ready");
        }

        if (!HasUniqueAreas)
        {
            blockers.Add("stable-readiness-areas-not-unique");
        }

        if (!CoversRequiredAreas)
        {
            blockers.Add("stable-readiness-required-areas-missing");
        }

        blockers.AddRange(Items
            .Where(static item => !item.IsReady)
            .Select(static item => $"stable-readiness-{item.Area}-not-approved"));

        return blockers;
    }

    /// <summary>Formats a compact stable commercial readiness checklist summary.</summary>
    /// <returns>The stable commercial readiness checklist summary.</returns>
    public string Describe()
    {
        return $"stableCommercialReadinessChecklistReady={IsReadyForDecision} version={EvidenceMap.Target.Version} blockers={GetBlockers().Count}";
    }

    private static readonly SigtranStableCommercialReadinessArea[] RequiredAreas =
    [
        SigtranStableCommercialReadinessArea.StableReleaseTarget,
        SigtranStableCommercialReadinessArea.CommercialDossierEvidence,
        SigtranStableCommercialReadinessArea.MaintainedExternalPeerInterop,
        SigtranStableCommercialReadinessArea.NativeSctpHardening,
        SigtranStableCommercialReadinessArea.ProtocolInterop,
        SigtranStableCommercialReadinessArea.PerformanceBenchmark,
        SigtranStableCommercialReadinessArea.SupplyChainRelease,
        SigtranStableCommercialReadinessArea.PublicApiBaseline,
        SigtranStableCommercialReadinessArea.OperationsCompliance,
        SigtranStableCommercialReadinessArea.PublicationDossier
    ];
}

/// <summary>
/// Provides stable commercial readiness checklist helpers.
/// </summary>
public static class SigtranStableCommercialReadinessChecklists
{
    /// <summary>Creates an approved stable commercial readiness checklist from a complete evidence map.</summary>
    /// <param name="evidenceMap">The stable commercial dossier evidence map.</param>
    /// <param name="reviewer">The reviewer identity.</param>
    /// <param name="reviewedAtUtc">The UTC review time.</param>
    /// <returns>The approved stable commercial readiness checklist.</returns>
    public static SigtranStableCommercialReadinessChecklist CreateApproved(
        SigtranStableCommercialDossierEvidenceMap evidenceMap,
        string reviewer,
        DateTimeOffset reviewedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(evidenceMap);

        return new(evidenceMap,
        [
            Create(SigtranStableCommercialReadinessArea.StableReleaseTarget, evidenceMap.Target.ArtifactRoot, reviewer, reviewedAtUtc, "Stable target lock reviewed."),
            Create(SigtranStableCommercialReadinessArea.CommercialDossierEvidence, evidenceMap.Target.ArtifactRoot + "/commercial/dossier-evidence-map.md", reviewer, reviewedAtUtc, "Commercial dossier evidence map reviewed."),
            Create(SigtranStableCommercialReadinessArea.MaintainedExternalPeerInterop, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.MaintainedExternalPeerRun), reviewer, reviewedAtUtc, "Maintained external peer evidence reviewed."),
            Create(SigtranStableCommercialReadinessArea.NativeSctpHardening, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.NativeSctpHardening), reviewer, reviewedAtUtc, "Native SCTP hardening evidence reviewed."),
            Create(SigtranStableCommercialReadinessArea.ProtocolInterop, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.ProtocolInterop), reviewer, reviewedAtUtc, "Protocol interoperability evidence reviewed."),
            Create(SigtranStableCommercialReadinessArea.PerformanceBenchmark, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.PerformanceBenchmark), reviewer, reviewedAtUtc, "Performance benchmark evidence reviewed."),
            Create(SigtranStableCommercialReadinessArea.SupplyChainRelease, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.FinalSbom), reviewer, reviewedAtUtc, "Supply-chain release evidence reviewed."),
            Create(SigtranStableCommercialReadinessArea.PublicApiBaseline, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.PublicApiBaselineDiff), reviewer, reviewedAtUtc, "Public API baseline evidence reviewed."),
            Create(SigtranStableCommercialReadinessArea.OperationsCompliance, evidenceMap.Target.ArtifactRoot + "/commercial/operations-compliance-approval.md", reviewer, reviewedAtUtc, "Operations and compliance approval reviewed."),
            Create(SigtranStableCommercialReadinessArea.PublicationDossier, RequirePath(evidenceMap, SigtranStableCommercialDossierEvidenceKind.CommercialReadinessReport), reviewer, reviewedAtUtc, "Publication dossier evidence reviewed.")
        ]);
    }

    private static SigtranStableCommercialReadinessChecklistItem Create(
        SigtranStableCommercialReadinessArea area,
        string evidencePath,
        string reviewer,
        DateTimeOffset reviewedAtUtc,
        string notes)
    {
        return new(area, approved: true, evidencePath, reviewer, reviewedAtUtc, notes);
    }

    private static string RequirePath(
        SigtranStableCommercialDossierEvidenceMap evidenceMap,
        SigtranStableCommercialDossierEvidenceKind kind)
    {
        return evidenceMap.Items.First(item => item.Kind == kind).RetainedPath;
    }
}
