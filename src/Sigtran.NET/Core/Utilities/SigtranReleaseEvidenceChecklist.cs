namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a production evidence checklist artifact kind.
/// </summary>
public enum SigtranReleaseEvidenceChecklistKind
{
    /// <summary>Packet capture evidence.</summary>
    PacketCapture,

    /// <summary>Peer process log evidence.</summary>
    PeerLog,

    /// <summary>SDK trace evidence.</summary>
    SdkTrace,

    /// <summary>Peer and SDK configuration evidence.</summary>
    Configuration,

    /// <summary>Trace or protocol comparison report evidence.</summary>
    ComparisonReport,

    /// <summary>Software bill of materials evidence.</summary>
    Sbom,

    /// <summary>Package signing verification evidence.</summary>
    SigningVerification,

    /// <summary>Provenance attestation evidence.</summary>
    ProvenanceAttestation,

    /// <summary>Benchmark report evidence.</summary>
    BenchmarkReport,

    /// <summary>Public API diff evidence.</summary>
    PublicApiDiff,

    /// <summary>Release workflow run evidence.</summary>
    ReleaseWorkflowRun,

    /// <summary>Release and migration notes evidence.</summary>
    PublicationNotes,

    /// <summary>Production readiness report evidence.</summary>
    ProductionReadinessSnapshot
}

/// <summary>
/// Describes one production evidence checklist item.
/// </summary>
public sealed class SigtranReleaseEvidenceChecklistItem
{
    /// <summary>Creates a production evidence checklist item.</summary>
    /// <param name="id">The stable checklist item identifier.</param>
    /// <param name="area">The evidence retention area.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="mandatory">Whether the item is mandatory for production readiness.</param>
    /// <param name="summary">The checklist item summary.</param>
    public SigtranReleaseEvidenceChecklistItem(
        string id,
        SigtranReleaseEvidenceRetentionArea area,
        SigtranReleaseEvidenceChecklistKind kind,
        bool mandatory,
        string summary)
    {
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Checklist item id is required.", nameof(id)) : id;
        Area = area;
        Kind = kind;
        Mandatory = mandatory;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Checklist item summary is required.", nameof(summary)) : summary;
    }

    /// <summary>The stable checklist item identifier.</summary>
    public string Id { get; }

    /// <summary>The evidence retention area.</summary>
    public SigtranReleaseEvidenceRetentionArea Area { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranReleaseEvidenceChecklistKind Kind { get; }

    /// <summary>Whether the item is mandatory for production readiness.</summary>
    public bool Mandatory { get; }

    /// <summary>The checklist item summary.</summary>
    public string Summary { get; }
}

/// <summary>
/// Describes the production evidence checklist.
/// </summary>
public sealed class SigtranReleaseEvidenceChecklist
{
    /// <summary>Creates a production evidence checklist.</summary>
    /// <param name="items">The checklist items.</param>
    public SigtranReleaseEvidenceChecklist(IReadOnlyList<SigtranReleaseEvidenceChecklistItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one checklist item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The checklist items.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceChecklistItem> Items { get; }

    /// <summary>Whether checklist item identifiers are unique.</summary>
    public bool HasUniqueIds => Items.Select(static item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Items.Count;

    /// <summary>Whether every production evidence retention area has a mandatory checklist item.</summary>
    public bool CoversRetentionAreas => Enum.GetValues<SigtranReleaseEvidenceRetentionArea>()
        .All(area => Items.Any(item => item.Area == area && item.Mandatory));

    /// <summary>Whether every essential evidence artifact kind is represented.</summary>
    public bool ContainsEssentialArtifacts => RequiredKinds
        .All(kind => Items.Any(item => item.Kind == kind && item.Mandatory));

    /// <summary>Whether the checklist is complete enough to drive production evidence capture.</summary>
    public bool IsReady => HasUniqueIds
        && CoversRetentionAreas
        && ContainsEssentialArtifacts;

    /// <summary>Formats a compact checklist summary.</summary>
    /// <returns>The checklist summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceChecklistReady={IsReady} items={Items.Count}";
    }

    private static readonly SigtranReleaseEvidenceChecklistKind[] RequiredKinds =
    [
        SigtranReleaseEvidenceChecklistKind.PacketCapture,
        SigtranReleaseEvidenceChecklistKind.PeerLog,
        SigtranReleaseEvidenceChecklistKind.SdkTrace,
        SigtranReleaseEvidenceChecklistKind.Configuration,
        SigtranReleaseEvidenceChecklistKind.ComparisonReport,
        SigtranReleaseEvidenceChecklistKind.Sbom,
        SigtranReleaseEvidenceChecklistKind.SigningVerification,
        SigtranReleaseEvidenceChecklistKind.ProvenanceAttestation,
        SigtranReleaseEvidenceChecklistKind.BenchmarkReport,
        SigtranReleaseEvidenceChecklistKind.PublicApiDiff,
        SigtranReleaseEvidenceChecklistKind.ReleaseWorkflowRun,
        SigtranReleaseEvidenceChecklistKind.PublicationNotes,
        SigtranReleaseEvidenceChecklistKind.ProductionReadinessSnapshot
    ];
}

/// <summary>
/// Provides production evidence checklist helpers.
/// </summary>
public static class SigtranReleaseEvidenceChecklists
{
    /// <summary>Creates the default production evidence checklist.</summary>
    /// <returns>The default production evidence checklist.</returns>
    public static SigtranReleaseEvidenceChecklist CreateDefault()
    {
        return new(
        [
            new("native-sctp-pcap", SigtranReleaseEvidenceRetentionArea.NativeSctp, SigtranReleaseEvidenceChecklistKind.PacketCapture, true, "Retain native Linux SCTP packet capture."),
            new("external-peer-log", SigtranReleaseEvidenceRetentionArea.ExternalPeerInterop, SigtranReleaseEvidenceChecklistKind.PeerLog, true, "Retain external peer execution logs."),
            new("sdk-trace", SigtranReleaseEvidenceRetentionArea.ExternalPeerInterop, SigtranReleaseEvidenceChecklistKind.SdkTrace, true, "Retain SDK trace output for the interop run."),
            new("peer-config", SigtranReleaseEvidenceRetentionArea.ExternalPeerInterop, SigtranReleaseEvidenceChecklistKind.Configuration, true, "Retain peer and SDK configuration used for the run."),
            new("protocol-comparison", SigtranReleaseEvidenceRetentionArea.ProtocolInterop, SigtranReleaseEvidenceChecklistKind.ComparisonReport, true, "Retain byte and trace comparison report."),
            new("final-sbom", SigtranReleaseEvidenceRetentionArea.SupplyChain, SigtranReleaseEvidenceChecklistKind.Sbom, true, "Retain final SPDX SBOM output."),
            new("signing-verification", SigtranReleaseEvidenceRetentionArea.SupplyChain, SigtranReleaseEvidenceChecklistKind.SigningVerification, true, "Retain trusted timestamped signing verification."),
            new("provenance-attestation", SigtranReleaseEvidenceRetentionArea.SupplyChain, SigtranReleaseEvidenceChecklistKind.ProvenanceAttestation, true, "Retain package and SBOM provenance attestations."),
            new("peer-benchmark-report", SigtranReleaseEvidenceRetentionArea.Performance, SigtranReleaseEvidenceChecklistKind.BenchmarkReport, true, "Retain warmup, sustained, peak, failover, latency, CPU, and memory benchmark report."),
            new("public-api-diff", SigtranReleaseEvidenceRetentionArea.PublicApi, SigtranReleaseEvidenceChecklistKind.PublicApiDiff, true, "Retain public API baseline and diff report."),
            new("release-workflow-run", SigtranReleaseEvidenceRetentionArea.ReleaseWorkflow, SigtranReleaseEvidenceChecklistKind.ReleaseWorkflowRun, true, "Retain dry-run and gated publication workflow evidence."),
            new("publication-notes", SigtranReleaseEvidenceRetentionArea.PublicationDossier, SigtranReleaseEvidenceChecklistKind.PublicationNotes, true, "Retain release notes and migration notes."),
            new("production-readiness-report", SigtranReleaseEvidenceRetentionArea.PublicationDossier, SigtranReleaseEvidenceChecklistKind.ProductionReadinessSnapshot, true, "Retain final production readiness report.")
        ]);
    }
}
