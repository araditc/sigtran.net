namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable release readiness checklist areas.
/// </summary>
public enum SigtranStableReleaseReadinessArea
{
    /// <summary>Stable release target readiness.</summary>
    StableReleaseTarget,

    /// <summary>Production dossier evidence map readiness.</summary>
    ProductionDossierEvidence,

    /// <summary>Reference external peer interoperability readiness.</summary>
    ReferenceExternalPeerInterop,

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
/// Describes one stable release readiness checklist approval.
/// </summary>
public sealed class SigtranStableReleaseReadinessChecklistItem
{
    /// <summary>Creates a stable release readiness checklist approval.</summary>
    /// <param name="area">The readiness area.</param>
    /// <param name="approved">Whether the area has passed review.</param>
    /// <param name="evidencePath">The retained evidence path used for review.</param>
    /// <param name="reviewer">The reviewer identity.</param>
    /// <param name="reviewedAtUtc">The UTC review time.</param>
    /// <param name="notes">The reviewer notes.</param>
    public SigtranStableReleaseReadinessChecklistItem(
        SigtranStableReleaseReadinessArea area,
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
    public SigtranStableReleaseReadinessArea Area { get; }

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
/// Evaluates stable release readiness before stable release decisioning.
/// </summary>
public sealed class SigtranStableReleaseReadinessChecklist
{
    /// <summary>Creates a stable release readiness checklist.</summary>
    /// <param name="evidenceMap">The stable release dossier evidence map.</param>
    /// <param name="items">The readiness checklist items.</param>
    public SigtranStableReleaseReadinessChecklist(
        SigtranStableReleaseDossierEvidenceMap evidenceMap,
        IReadOnlyList<SigtranStableReleaseReadinessChecklistItem> items)
    {
        EvidenceMap = evidenceMap ?? throw new ArgumentNullException(nameof(evidenceMap));
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one readiness checklist item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The stable release dossier evidence map.</summary>
    public SigtranStableReleaseDossierEvidenceMap EvidenceMap { get; }

    /// <summary>The readiness checklist items.</summary>
    public IReadOnlyList<SigtranStableReleaseReadinessChecklistItem> Items { get; }

    /// <summary>Whether the underlying stable release target is ready.</summary>
    public bool HasReadyTarget => EvidenceMap.Target.IsReadyForStableGate;

    /// <summary>Whether the underlying production dossier evidence map is ready.</summary>
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

    /// <summary>Formats a compact stable release readiness checklist summary.</summary>
    /// <returns>The stable release readiness checklist summary.</returns>
    public string Describe()
    {
        return $"stableReleaseReadinessChecklistReady={IsReadyForDecision} version={EvidenceMap.Target.Version} blockers={GetBlockers().Count}";
    }

    private static readonly SigtranStableReleaseReadinessArea[] RequiredAreas =
    [
        SigtranStableReleaseReadinessArea.StableReleaseTarget,
        SigtranStableReleaseReadinessArea.ProductionDossierEvidence,
        SigtranStableReleaseReadinessArea.ReferenceExternalPeerInterop,
        SigtranStableReleaseReadinessArea.NativeSctpHardening,
        SigtranStableReleaseReadinessArea.ProtocolInterop,
        SigtranStableReleaseReadinessArea.PerformanceBenchmark,
        SigtranStableReleaseReadinessArea.SupplyChainRelease,
        SigtranStableReleaseReadinessArea.PublicApiBaseline,
        SigtranStableReleaseReadinessArea.OperationsCompliance,
        SigtranStableReleaseReadinessArea.PublicationDossier
    ];
}

/// <summary>
/// Provides stable release readiness checklist helpers.
/// </summary>
public static class SigtranStableReleaseReadinessChecklists
{
    /// <summary>Creates an approved stable release readiness checklist from a complete evidence map.</summary>
    /// <param name="evidenceMap">The stable release dossier evidence map.</param>
    /// <param name="reviewer">The reviewer identity.</param>
    /// <param name="reviewedAtUtc">The UTC review time.</param>
    /// <returns>The approved stable release readiness checklist.</returns>
    public static SigtranStableReleaseReadinessChecklist CreateApproved(
        SigtranStableReleaseDossierEvidenceMap evidenceMap,
        string reviewer,
        DateTimeOffset reviewedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(evidenceMap);

        return new(evidenceMap,
        [
            Create(SigtranStableReleaseReadinessArea.StableReleaseTarget, evidenceMap.Target.ArtifactRoot, reviewer, reviewedAtUtc, "Stable target lock reviewed."),
            Create(SigtranStableReleaseReadinessArea.ProductionDossierEvidence, evidenceMap.Target.ArtifactRoot + "/production/dossier-evidence-map.md", reviewer, reviewedAtUtc, "Production dossier evidence map reviewed."),
            Create(SigtranStableReleaseReadinessArea.ReferenceExternalPeerInterop, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.ReferenceExternalPeerRun), reviewer, reviewedAtUtc, "Reference external peer evidence reviewed."),
            Create(SigtranStableReleaseReadinessArea.NativeSctpHardening, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.NativeSctpHardening), reviewer, reviewedAtUtc, "Native SCTP hardening evidence reviewed."),
            Create(SigtranStableReleaseReadinessArea.ProtocolInterop, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.ProtocolInterop), reviewer, reviewedAtUtc, "Protocol interoperability evidence reviewed."),
            Create(SigtranStableReleaseReadinessArea.PerformanceBenchmark, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.PerformanceBenchmark), reviewer, reviewedAtUtc, "Performance benchmark evidence reviewed."),
            Create(SigtranStableReleaseReadinessArea.SupplyChainRelease, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.FinalSbom), reviewer, reviewedAtUtc, "Supply-chain release evidence reviewed."),
            Create(SigtranStableReleaseReadinessArea.PublicApiBaseline, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.PublicApiBaselineDiff), reviewer, reviewedAtUtc, "Public API baseline evidence reviewed."),
            Create(SigtranStableReleaseReadinessArea.OperationsCompliance, evidenceMap.Target.ArtifactRoot + "/production/operations-compliance-approval.md", reviewer, reviewedAtUtc, "Operations and compliance approval reviewed."),
            Create(SigtranStableReleaseReadinessArea.PublicationDossier, RequirePath(evidenceMap, SigtranStableReleaseDossierEvidenceKind.ProductionReadinessSnapshot), reviewer, reviewedAtUtc, "Publication dossier evidence reviewed.")
        ]);
    }

    private static SigtranStableReleaseReadinessChecklistItem Create(
        SigtranStableReleaseReadinessArea area,
        string evidencePath,
        string reviewer,
        DateTimeOffset reviewedAtUtc,
        string notes)
    {
        return new(area, approved: true, evidencePath, reviewer, reviewedAtUtc, notes);
    }

    private static string RequirePath(
        SigtranStableReleaseDossierEvidenceMap evidenceMap,
        SigtranStableReleaseDossierEvidenceKind kind)
    {
        return evidenceMap.Items.First(item => item.Kind == kind).RetainedPath;
    }
}
