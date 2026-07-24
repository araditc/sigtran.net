namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a technical area that can be supported by retained verification evidence.
/// </summary>
public enum SigtranVerificationArea
{
    /// <summary>Native Linux SCTP transport behavior.</summary>
    NativeLinuxSctp,

    /// <summary>SCTP traffic exchanged with an independently implemented peer.</summary>
    ExternalSctpPeer,

    /// <summary>M3UA traffic decoded and compared with an independent peer.</summary>
    M3uaPeerInteroperability,

    /// <summary>SCCP behavior validated against an independent implementation.</summary>
    SccpPeerInteroperability,

    /// <summary>TCAP behavior validated against an independent implementation.</summary>
    TcapPeerInteroperability,

    /// <summary>MAP behavior validated against an independent implementation.</summary>
    MapPeerInteroperability,

    /// <summary>Capacity and resilience measured under an operator-sized workload.</summary>
    OperatorPerformance,

    /// <summary>Release SBOM generated and retained by the release workflow.</summary>
    ReleaseSbom,

    /// <summary>Release provenance generated and retained by the release workflow.</summary>
    ReleaseProvenance,

    /// <summary>Public API baseline and diff retained by the release workflow.</summary>
    PublicApiBaseline,

    /// <summary>Package signed by an approved identity with a trusted timestamp.</summary>
    TrustedPackageSigning,

    /// <summary>Prerelease package published and restored from the public feed.</summary>
    PrereleasePublication,

    /// <summary>Stable package published and restored from the public feed.</summary>
    StablePublication
}

/// <summary>
/// Describes one retained verification result.
/// </summary>
public sealed class SigtranVerificationRecord
{
    /// <summary>Creates a retained verification result.</summary>
    /// <param name="id">The stable verification run identifier.</param>
    /// <param name="area">The verified technical area.</param>
    /// <param name="artifactReference">The retained artifact or manifest reference.</param>
    /// <param name="observedAtUtc">The UTC verification time.</param>
    /// <param name="passed">Whether the verification passed.</param>
    public SigtranVerificationRecord(
        string id,
        SigtranVerificationArea area,
        string artifactReference,
        DateTimeOffset observedAtUtc,
        bool passed)
    {
        Id = string.IsNullOrWhiteSpace(id)
            ? throw new ArgumentException("Verification id is required.", nameof(id))
            : id;
        Area = area;
        ArtifactReference = string.IsNullOrWhiteSpace(artifactReference)
            ? throw new ArgumentException("Artifact reference is required.", nameof(artifactReference))
            : artifactReference;
        ObservedAtUtc = observedAtUtc.Offset == TimeSpan.Zero
            ? observedAtUtc
            : throw new ArgumentException("Verification time must use UTC.", nameof(observedAtUtc));
        Passed = passed;
    }

    /// <summary>The stable verification run identifier.</summary>
    public string Id { get; }

    /// <summary>The verified technical area.</summary>
    public SigtranVerificationArea Area { get; }

    /// <summary>The retained artifact or manifest reference.</summary>
    public string ArtifactReference { get; }

    /// <summary>The UTC verification time.</summary>
    public DateTimeOffset ObservedAtUtc { get; }

    /// <summary>Whether the verification passed.</summary>
    public bool Passed { get; }
}

/// <summary>
/// Stores retained verification results and evaluates technical evidence requirements.
/// </summary>
public sealed class SigtranVerificationCatalog
{
    private readonly List<SigtranVerificationRecord> _records = [];

    /// <summary>Adds a retained verification result.</summary>
    /// <param name="record">The verification result.</param>
    public void Add(SigtranVerificationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (_records.Any(existing =>
                existing.Area == record.Area
                && string.Equals(existing.Id, record.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Verification '{record.Id}' already exists for area '{record.Area}'.");
        }

        _records.Add(record);
    }

    /// <summary>Returns the retained results in insertion order.</summary>
    /// <returns>The retained verification results.</returns>
    public IReadOnlyList<SigtranVerificationRecord> Snapshot()
    {
        return _records.ToArray();
    }

    /// <summary>Returns whether at least one passing result exists for an area.</summary>
    /// <param name="area">The technical area.</param>
    /// <returns>True when passing retained evidence exists; otherwise false.</returns>
    public bool HasPassingEvidence(SigtranVerificationArea area)
    {
        return _records.Any(record => record.Area == area && record.Passed);
    }

    /// <summary>Returns the areas that do not have passing retained evidence.</summary>
    /// <param name="requiredAreas">The required technical areas.</param>
    /// <returns>The missing technical areas.</returns>
    public IReadOnlyList<SigtranVerificationArea> GetMissingAreas(
        IReadOnlyList<SigtranVerificationArea> requiredAreas)
    {
        ArgumentNullException.ThrowIfNull(requiredAreas);
        return requiredAreas
            .Distinct()
            .Where(area => !HasPassingEvidence(area))
            .ToArray();
    }
}

/// <summary>
/// Provides the verification evidence retained for the current SDK line.
/// </summary>
public static class SigtranVerificationCatalogs
{
    private static readonly DateTimeOffset ExternalPeerObservedAt =
        new(2026, 6, 27, 11, 19, 32, TimeSpan.Zero);

    private static readonly DateTimeOffset ReleaseObservedAt =
        new(2026, 6, 27, 13, 6, 23, TimeSpan.Zero);

    private static readonly DateTimeOffset NativeSctpObservedAt =
        new(2026, 7, 1, 10, 39, 51, TimeSpan.Zero);

    /// <summary>Creates the catalog backed by the retained repository evidence manifests.</summary>
    /// <returns>The current retained verification catalog.</returns>
    public static SigtranVerificationCatalog CreateCurrent()
    {
        SigtranVerificationCatalog catalog = new();

        catalog.Add(new(
            "phase45-native-sctp-20260701T103951Z",
            SigtranVerificationArea.NativeLinuxSctp,
            "docs/evidence/PHASE45_NATIVE_SCTP_20260701T103951Z.json",
            NativeSctpObservedAt,
            passed: true));
        catalog.Add(new(
            "commercial-external-peer-20260627T111932Z",
            SigtranVerificationArea.ExternalSctpPeer,
            "docs/evidence/COMMERCIAL_EVIDENCE_20260627.json",
            ExternalPeerObservedAt,
            passed: true));
        catalog.Add(new(
            "commercial-external-peer-20260627T111932Z",
            SigtranVerificationArea.M3uaPeerInteroperability,
            "docs/evidence/COMMERCIAL_EVIDENCE_20260627.json",
            ExternalPeerObservedAt,
            passed: true));
        catalog.Add(new(
            "release-workflow-28289987418",
            SigtranVerificationArea.ReleaseSbom,
            "docs/evidence/RELEASE_WORKFLOW_DRY_RUN_28289987418.json",
            ReleaseObservedAt,
            passed: true));
        catalog.Add(new(
            "release-workflow-28289987418",
            SigtranVerificationArea.ReleaseProvenance,
            "docs/evidence/RELEASE_WORKFLOW_DRY_RUN_28289987418.json",
            ReleaseObservedAt,
            passed: true));
        catalog.Add(new(
            "nuget-prerelease-28290586511",
            SigtranVerificationArea.PublicApiBaseline,
            "docs/evidence/NUGET_PRERELEASE_PUBLISH_28290586511.json",
            ReleaseObservedAt,
            passed: true));
        catalog.Add(new(
            "nuget-prerelease-28290586511",
            SigtranVerificationArea.PrereleasePublication,
            "docs/evidence/NUGET_PRERELEASE_PUBLISH_28290586511.json",
            ReleaseObservedAt,
            passed: true));

        return catalog;
    }
}
