namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a commercial evidence checklist artifact kind.
/// </summary>
public enum SigtranCommercialEvidenceChecklistKind
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

    /// <summary>Commercial readiness report evidence.</summary>
    CommercialReadinessReport
}

/// <summary>
/// Describes one commercial evidence checklist item.
/// </summary>
public sealed class SigtranCommercialEvidenceChecklistItem
{
    /// <summary>Creates a commercial evidence checklist item.</summary>
    /// <param name="id">The stable checklist item identifier.</param>
    /// <param name="area">The evidence retention area.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="mandatory">Whether the item is mandatory for commercial readiness.</param>
    /// <param name="summary">The checklist item summary.</param>
    public SigtranCommercialEvidenceChecklistItem(
        string id,
        SigtranCommercialEvidenceRetentionArea area,
        SigtranCommercialEvidenceChecklistKind kind,
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
    public SigtranCommercialEvidenceRetentionArea Area { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>Whether the item is mandatory for commercial readiness.</summary>
    public bool Mandatory { get; }

    /// <summary>The checklist item summary.</summary>
    public string Summary { get; }
}

/// <summary>
/// Describes the commercial evidence checklist.
/// </summary>
public sealed class SigtranCommercialEvidenceChecklist
{
    /// <summary>Creates a commercial evidence checklist.</summary>
    /// <param name="items">The checklist items.</param>
    public SigtranCommercialEvidenceChecklist(IReadOnlyList<SigtranCommercialEvidenceChecklistItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one checklist item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The checklist items.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceChecklistItem> Items { get; }

    /// <summary>Whether checklist item identifiers are unique.</summary>
    public bool HasUniqueIds => Items.Select(static item => item.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() == Items.Count;

    /// <summary>Whether every commercial evidence retention area has a mandatory checklist item.</summary>
    public bool CoversRetentionAreas => Enum.GetValues<SigtranCommercialEvidenceRetentionArea>()
        .All(area => Items.Any(item => item.Area == area && item.Mandatory));

    /// <summary>Whether every essential evidence artifact kind is represented.</summary>
    public bool ContainsEssentialArtifacts => RequiredKinds
        .All(kind => Items.Any(item => item.Kind == kind && item.Mandatory));

    /// <summary>Whether the checklist is complete enough to drive commercial evidence capture.</summary>
    public bool IsReady => HasUniqueIds
        && CoversRetentionAreas
        && ContainsEssentialArtifacts;

    /// <summary>Formats a compact checklist summary.</summary>
    /// <returns>The checklist summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceChecklistReady={IsReady} items={Items.Count}";
    }

    private static readonly SigtranCommercialEvidenceChecklistKind[] RequiredKinds =
    [
        SigtranCommercialEvidenceChecklistKind.PacketCapture,
        SigtranCommercialEvidenceChecklistKind.PeerLog,
        SigtranCommercialEvidenceChecklistKind.SdkTrace,
        SigtranCommercialEvidenceChecklistKind.Configuration,
        SigtranCommercialEvidenceChecklistKind.ComparisonReport,
        SigtranCommercialEvidenceChecklistKind.Sbom,
        SigtranCommercialEvidenceChecklistKind.SigningVerification,
        SigtranCommercialEvidenceChecklistKind.ProvenanceAttestation,
        SigtranCommercialEvidenceChecklistKind.BenchmarkReport,
        SigtranCommercialEvidenceChecklistKind.PublicApiDiff,
        SigtranCommercialEvidenceChecklistKind.ReleaseWorkflowRun,
        SigtranCommercialEvidenceChecklistKind.PublicationNotes,
        SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport
    ];
}

/// <summary>
/// Provides commercial evidence checklist helpers.
/// </summary>
public static class SigtranCommercialEvidenceChecklists
{
    /// <summary>Creates the default commercial evidence checklist.</summary>
    /// <returns>The default commercial evidence checklist.</returns>
    public static SigtranCommercialEvidenceChecklist CreateDefault()
    {
        return new(
        [
            new("native-sctp-pcap", SigtranCommercialEvidenceRetentionArea.NativeSctp, SigtranCommercialEvidenceChecklistKind.PacketCapture, true, "Retain native Linux SCTP packet capture."),
            new("external-peer-log", SigtranCommercialEvidenceRetentionArea.ExternalPeerInterop, SigtranCommercialEvidenceChecklistKind.PeerLog, true, "Retain external peer execution logs."),
            new("sdk-trace", SigtranCommercialEvidenceRetentionArea.ExternalPeerInterop, SigtranCommercialEvidenceChecklistKind.SdkTrace, true, "Retain SDK trace output for the interop run."),
            new("peer-config", SigtranCommercialEvidenceRetentionArea.ExternalPeerInterop, SigtranCommercialEvidenceChecklistKind.Configuration, true, "Retain peer and SDK configuration used for the run."),
            new("protocol-comparison", SigtranCommercialEvidenceRetentionArea.ProtocolInterop, SigtranCommercialEvidenceChecklistKind.ComparisonReport, true, "Retain byte and trace comparison report."),
            new("final-sbom", SigtranCommercialEvidenceRetentionArea.SupplyChain, SigtranCommercialEvidenceChecklistKind.Sbom, true, "Retain final SPDX SBOM output."),
            new("signing-verification", SigtranCommercialEvidenceRetentionArea.SupplyChain, SigtranCommercialEvidenceChecklistKind.SigningVerification, true, "Retain trusted timestamped signing verification."),
            new("provenance-attestation", SigtranCommercialEvidenceRetentionArea.SupplyChain, SigtranCommercialEvidenceChecklistKind.ProvenanceAttestation, true, "Retain package and SBOM provenance attestations."),
            new("peer-benchmark-report", SigtranCommercialEvidenceRetentionArea.Performance, SigtranCommercialEvidenceChecklistKind.BenchmarkReport, true, "Retain warmup, sustained, peak, failover, latency, CPU, and memory benchmark report."),
            new("public-api-diff", SigtranCommercialEvidenceRetentionArea.PublicApi, SigtranCommercialEvidenceChecklistKind.PublicApiDiff, true, "Retain public API baseline and diff report."),
            new("release-workflow-run", SigtranCommercialEvidenceRetentionArea.ReleaseWorkflow, SigtranCommercialEvidenceChecklistKind.ReleaseWorkflowRun, true, "Retain dry-run and gated publication workflow evidence."),
            new("publication-notes", SigtranCommercialEvidenceRetentionArea.PublicationDossier, SigtranCommercialEvidenceChecklistKind.PublicationNotes, true, "Retain release notes and migration notes."),
            new("commercial-readiness-report", SigtranCommercialEvidenceRetentionArea.PublicationDossier, SigtranCommercialEvidenceChecklistKind.CommercialReadinessReport, true, "Retain final commercial readiness report.")
        ]);
    }
}
