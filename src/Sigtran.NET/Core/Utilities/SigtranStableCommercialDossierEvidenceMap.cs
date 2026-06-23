namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies stable commercial dossier evidence required before stable publication.
/// </summary>
public enum SigtranStableCommercialDossierEvidenceKind
{
    /// <summary>Maintained external peer interoperability run evidence.</summary>
    MaintainedExternalPeerRun,

    /// <summary>Native SCTP production hardening evidence.</summary>
    NativeSctpHardening,

    /// <summary>SCCP, TCAP, and MAP interoperability evidence.</summary>
    ProtocolInterop,

    /// <summary>Representative performance and resilience benchmark evidence.</summary>
    PerformanceBenchmark,

    /// <summary>Final software bill of materials evidence.</summary>
    FinalSbom,

    /// <summary>Trusted timestamped package signing verification evidence.</summary>
    PackageSigningVerification,

    /// <summary>Package and SBOM provenance attestation evidence.</summary>
    ProvenanceAttestation,

    /// <summary>Public API baseline and diff evidence.</summary>
    PublicApiBaselineDiff,

    /// <summary>Release workflow run and uploaded artifact evidence.</summary>
    ReleaseWorkflowArtifacts,

    /// <summary>Release notes and migration notes evidence.</summary>
    PublicationNotes,

    /// <summary>Final commercial readiness report evidence.</summary>
    CommercialReadinessReport
}

/// <summary>
/// Describes one retained stable commercial dossier evidence item.
/// </summary>
public sealed class SigtranStableCommercialDossierEvidenceItem
{
    /// <summary>Creates a retained stable commercial dossier evidence item.</summary>
    /// <param name="kind">The evidence kind.</param>
    /// <param name="retainedPath">The retained artifact path.</param>
    /// <param name="sha256">The retained artifact SHA-256 digest.</param>
    /// <param name="required">Whether the item is required for stable publication.</param>
    /// <param name="summary">The evidence summary.</param>
    public SigtranStableCommercialDossierEvidenceItem(
        SigtranStableCommercialDossierEvidenceKind kind,
        string retainedPath,
        string sha256,
        bool required,
        string summary)
    {
        Kind = kind;
        RetainedPath = string.IsNullOrWhiteSpace(retainedPath) ? throw new ArgumentException("Retained path is required.", nameof(retainedPath)) : retainedPath;
        Sha256 = string.IsNullOrWhiteSpace(sha256) ? throw new ArgumentException("SHA-256 digest is required.", nameof(sha256)) : sha256;
        Required = required;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Evidence summary is required.", nameof(summary)) : summary;
    }

    /// <summary>The evidence kind.</summary>
    public SigtranStableCommercialDossierEvidenceKind Kind { get; }

    /// <summary>The retained artifact path.</summary>
    public string RetainedPath { get; }

    /// <summary>The retained artifact SHA-256 digest.</summary>
    public string Sha256 { get; }

    /// <summary>Whether the item is required for stable publication.</summary>
    public bool Required { get; }

    /// <summary>The evidence summary.</summary>
    public string Summary { get; }

    /// <summary>Whether the digest is a valid lowercase or uppercase SHA-256 hex value.</summary>
    public bool HasValidSha256 => Sha256.Length == 64
        && Sha256.All(Uri.IsHexDigit);
}

/// <summary>
/// Maps retained stable commercial dossier evidence to one stable release target.
/// </summary>
public sealed class SigtranStableCommercialDossierEvidenceMap
{
    /// <summary>Creates a stable commercial dossier evidence map.</summary>
    /// <param name="target">The stable release target.</param>
    /// <param name="items">The retained evidence items.</param>
    public SigtranStableCommercialDossierEvidenceMap(
        SigtranStableReleaseTarget target,
        IReadOnlyList<SigtranStableCommercialDossierEvidenceItem> items)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one evidence item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The stable release target.</summary>
    public SigtranStableReleaseTarget Target { get; }

    /// <summary>The retained evidence items.</summary>
    public IReadOnlyList<SigtranStableCommercialDossierEvidenceItem> Items { get; }

    /// <summary>Whether the release target is ready for stable gate evaluation.</summary>
    public bool HasReadyTarget => Target.IsReadyForStableGate;

    /// <summary>Whether retained paths are unique across the dossier.</summary>
    public bool HasUniqueRetainedPaths => Items
        .Select(static item => item.RetainedPath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count() == Items.Count;

    /// <summary>Whether every required evidence kind is present.</summary>
    public bool HasRequiredEvidenceKinds => MissingRequiredKinds().Count == 0;

    /// <summary>Whether every retained item has a valid SHA-256 digest.</summary>
    public bool HasValidDigests => Items.All(static item => item.HasValidSha256);

    /// <summary>Whether every retained path is under the stable target artifact root.</summary>
    public bool PathsStayUnderArtifactRoot => Items.All(item =>
        item.RetainedPath.StartsWith(Target.ArtifactRoot + "/", StringComparison.Ordinal));

    /// <summary>Whether the evidence map can drive stable readiness checklist evaluation.</summary>
    public bool IsReadyForChecklist => HasReadyTarget
        && HasUniqueRetainedPaths
        && HasRequiredEvidenceKinds
        && HasValidDigests
        && PathsStayUnderArtifactRoot;

    /// <summary>Returns missing required evidence kinds.</summary>
    /// <returns>The missing required evidence kinds.</returns>
    public IReadOnlyList<SigtranStableCommercialDossierEvidenceKind> MissingRequiredKinds()
    {
        return RequiredKinds
            .Where(kind => !Items.Any(item => item.Required && item.Kind == kind))
            .ToArray();
    }

    /// <summary>Formats a compact stable commercial dossier evidence map summary.</summary>
    /// <returns>The stable commercial dossier evidence map summary.</returns>
    public string Describe()
    {
        return $"stableDossierEvidenceMapReady={IsReadyForChecklist} version={Target.Version} items={Items.Count} missing={MissingRequiredKinds().Count}";
    }

    private static readonly SigtranStableCommercialDossierEvidenceKind[] RequiredKinds =
    [
        SigtranStableCommercialDossierEvidenceKind.MaintainedExternalPeerRun,
        SigtranStableCommercialDossierEvidenceKind.NativeSctpHardening,
        SigtranStableCommercialDossierEvidenceKind.ProtocolInterop,
        SigtranStableCommercialDossierEvidenceKind.PerformanceBenchmark,
        SigtranStableCommercialDossierEvidenceKind.FinalSbom,
        SigtranStableCommercialDossierEvidenceKind.PackageSigningVerification,
        SigtranStableCommercialDossierEvidenceKind.ProvenanceAttestation,
        SigtranStableCommercialDossierEvidenceKind.PublicApiBaselineDiff,
        SigtranStableCommercialDossierEvidenceKind.ReleaseWorkflowArtifacts,
        SigtranStableCommercialDossierEvidenceKind.PublicationNotes,
        SigtranStableCommercialDossierEvidenceKind.CommercialReadinessReport
    ];
}

/// <summary>
/// Provides stable commercial dossier evidence map helpers.
/// </summary>
public static class SigtranStableCommercialDossierEvidenceMaps
{
    /// <summary>Creates a complete stable commercial dossier evidence map for a retained artifact root.</summary>
    /// <param name="target">The stable release target.</param>
    /// <param name="sha256">The digest to assign to generated evidence items.</param>
    /// <returns>The stable commercial dossier evidence map.</returns>
    public static SigtranStableCommercialDossierEvidenceMap CreateComplete(
        SigtranStableReleaseTarget target,
        string sha256)
    {
        ArgumentNullException.ThrowIfNull(target);

        return new(target,
        [
            Create(target, SigtranStableCommercialDossierEvidenceKind.MaintainedExternalPeerRun, "interop/maintained-peer-run-report.md", sha256, "Retained maintained external peer PCAP, logs, traces, configuration, comparison, and run report."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.NativeSctpHardening, "native-sctp/production-hardening-report.md", sha256, "Retained Linux SCTP stream, PPID, lifecycle, reconnect, backpressure, cancellation, multi-homing, and recovery evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.ProtocolInterop, "protocol/sccp-tcap-map-comparison.md", sha256, "Retained SCCP, TCAP, and MAP vector and trace comparison evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.PerformanceBenchmark, "performance/peer-benchmark-report.md", sha256, "Retained warmup, sustained, peak, failover, latency, CPU, and memory benchmark evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.FinalSbom, "supply-chain/sbom.spdx.json", sha256, "Retained final SPDX SBOM evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.PackageSigningVerification, "supply-chain/signing-verification.md", sha256, "Retained trusted timestamped package signing verification evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.ProvenanceAttestation, "supply-chain/provenance.intoto.jsonl", sha256, "Retained package and SBOM provenance attestation evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.PublicApiBaselineDiff, "api/public-api-diff.md", sha256, "Retained public API baseline and diff evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.ReleaseWorkflowArtifacts, "release/workflow-run.md", sha256, "Retained release workflow run and uploaded artifact evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.PublicationNotes, "release/publication-notes.md", sha256, "Retained release notes and migration notes evidence."),
            Create(target, SigtranStableCommercialDossierEvidenceKind.CommercialReadinessReport, "commercial/final-commercial-readiness-report.md", sha256, "Retained final commercial readiness report evidence.")
        ]);
    }

    private static SigtranStableCommercialDossierEvidenceItem Create(
        SigtranStableReleaseTarget target,
        SigtranStableCommercialDossierEvidenceKind kind,
        string relativePath,
        string sha256,
        string summary)
    {
        return new(kind, $"{target.ArtifactRoot}/{relativePath}", sha256, required: true, summary);
    }
}
