namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes the package-neutral API naming alignment result.
/// </summary>
public sealed class SigtranApiNamingAlignmentSnapshot
{
    /// <summary>Creates an API naming alignment snapshot.</summary>
    /// <param name="completedUnitCount">The completed work unit count.</param>
    /// <param name="capabilities">The completed capability ids.</param>
    /// <param name="packageNeutralNamingReady">Whether public contract names are package-neutral.</param>
    /// <param name="referencePeerPolicyReady">Whether the reference peer policy is available.</param>
    /// <param name="externalPeerExecutionFoundationReady">Whether external peer execution foundation is ready.</param>
    /// <param name="productionGateAligned">Whether production gates use external peer terminology.</param>
    /// <param name="externalPeerProductionInteropReady">Whether current external peer production interop evidence is ready.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranApiNamingAlignmentSnapshot(
        int completedUnitCount,
        IReadOnlyList<string> capabilities,
        bool packageNeutralNamingReady,
        bool referencePeerPolicyReady,
        bool externalPeerExecutionFoundationReady,
        bool productionGateAligned,
        bool externalPeerProductionInteropReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(blockers);

        CompletedUnitCount = completedUnitCount;
        Capabilities = capabilities.ToArray();
        PackageNeutralNamingReady = packageNeutralNamingReady;
        ReferencePeerPolicyReady = referencePeerPolicyReady;
        ExternalPeerExecutionFoundationReady = externalPeerExecutionFoundationReady;
        ProductionGateAligned = productionGateAligned;
        ExternalPeerProductionInteropReady = externalPeerProductionInteropReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed work unit count.</summary>
    public int CompletedUnitCount { get; }

    /// <summary>The completed capability ids.</summary>
    public IReadOnlyList<string> Capabilities { get; }

    /// <summary>Whether public contract names are package-neutral.</summary>
    public bool PackageNeutralNamingReady { get; }

    /// <summary>Whether the reference peer policy is available.</summary>
    public bool ReferencePeerPolicyReady { get; }

    /// <summary>Whether external peer execution foundation is ready.</summary>
    public bool ExternalPeerExecutionFoundationReady { get; }

    /// <summary>Whether production gates use external peer terminology.</summary>
    public bool ProductionGateAligned { get; }

    /// <summary>Whether current external peer production interop evidence is ready.</summary>
    public bool ExternalPeerProductionInteropReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the package-neutral API naming alignment foundation is complete.</summary>
    public bool FoundationReady => Capabilities.Count == CompletedUnitCount
        && PackageNeutralNamingReady
        && ReferencePeerPolicyReady
        && ExternalPeerExecutionFoundationReady
        && ProductionGateAligned;

    /// <summary>Whether the realigned production release gate can pass today.</summary>
    public bool ReleaseReady => FoundationReady
        && ExternalPeerProductionInteropReady
        && Blockers.Count == 0;

    /// <summary>Formats a compact realignment status summary.</summary>
    /// <returns>The realignment status summary.</returns>
    public string Describe()
    {
        return $"apiNamingAlignmentReady={FoundationReady} releaseReady={ReleaseReady} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides package-neutral API naming alignment status.
/// </summary>
public static class SigtranApiNamingAlignmentStatus
{
    private static readonly string[] Capabilities =
    [
        "package-neutral-source-contracts",
        "external-peer-profile-model",
        "reference-peer-selection-policy",
        "external-peer-lab-environment-contract",
        "digest-covered-external-peer-artifacts",
        "external-peer-run-commands",
        "external-peer-production-readiness",
        "production-release-gate-alignment",
        "public-label-migration",
        "documentation"
    ];

    private static readonly string[] PublicContractNames =
    [
        nameof(SigtranExternalPeerInteropStatus),
        nameof(SigtranExternalPeerProductionReadiness),
        nameof(SigtranReferencePeerSelectionPolicy),
        nameof(SigtranReleaseExecutionReadiness),
        nameof(SigtranReleaseEvidenceGate),
        StatusLabel,
        SigtranExternalPeerInteropStatus.StatusLabel
    ];

    private static readonly string[] PackageSpecificNameGuardCategories =
    [
        "peer-product-type-name",
        "peer-product-label",
        "peer-product-ci-variable"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "API Naming Alignment";

    /// <summary>The number of completed realignment work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed realignment capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns the public contract names checked by the package-neutral naming guard.</summary>
    /// <returns>The public contract names checked by the naming guard.</returns>
    public static IReadOnlyList<string> GetPublicContractNames()
    {
        return [.. PublicContractNames, .. Capabilities];
    }

    /// <summary>Returns the package-specific naming categories guarded by the realignment status.</summary>
    /// <returns>The guarded package-specific naming categories.</returns>
    public static IReadOnlyList<string> GetPackageSpecificNameGuardCategories()
    {
        return PackageSpecificNameGuardCategories.ToArray();
    }

    /// <summary>Whether the realignment public names are package-neutral.</summary>
    public static bool PackageNeutralNamingReady => GetPublicContractNames()
        .All(static name => name.Contains("external", StringComparison.OrdinalIgnoreCase)
            || name.Contains("production", StringComparison.OrdinalIgnoreCase)
            || name.Contains("reference", StringComparison.OrdinalIgnoreCase)
            || name.Contains("evidence", StringComparison.OrdinalIgnoreCase)
            || name.Contains("release", StringComparison.OrdinalIgnoreCase)
            || name.Contains("api", StringComparison.OrdinalIgnoreCase)
            || name.Contains("naming", StringComparison.OrdinalIgnoreCase)
            || name.Contains("package-neutral", StringComparison.OrdinalIgnoreCase)
            || name.Contains("public-label", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "documentation", StringComparison.OrdinalIgnoreCase));

    /// <summary>Creates the current package-neutral API naming alignment report.</summary>
    /// <returns>The current realignment report.</returns>
    public static SigtranApiNamingAlignmentSnapshot GetReport()
    {
        SigtranExternalPeerProductionReadinessSnapshot productionPeer = SigtranExternalPeerProductionReadiness.CreateCurrent();
        SigtranReleaseExecutionReadinessSnapshot release = SigtranReleaseExecutionReadiness.CreateCurrent();

        bool referencePeerPolicyReady = SigtranReferencePeerSelectionPolicy.CreateDefault().GetCriteria().Count == 6
            && SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway().IsReferencePeerCandidate;
        bool productionGateAligned = release.Items.Any(static item => item.Name == "external-peer-interop")
            && release.Items.All(static item => !item.Name.Contains("openss7", StringComparison.OrdinalIgnoreCase))
            && release.Items.All(static item => !item.Name.Contains("ipss7", StringComparison.OrdinalIgnoreCase));

        List<string> blockers = [];
        if (!PackageNeutralNamingReady)
        {
            blockers.Add("package-neutral-naming-required");
        }

        if (!referencePeerPolicyReady)
        {
            blockers.Add("reference-peer-policy-required");
        }

        if (!SigtranExternalPeerInteropStatus.FoundationReady)
        {
            blockers.Add("external-peer-execution-foundation-required");
        }

        if (!productionGateAligned)
        {
            blockers.Add("production-gate-alignment-required");
        }

        blockers.AddRange(productionPeer.Blockers);

        return new(
            CompletedUnitCount,
            Capabilities,
            PackageNeutralNamingReady,
            referencePeerPolicyReady,
            SigtranExternalPeerInteropStatus.FoundationReady,
            productionGateAligned,
            productionPeer.ProductionInteropReady,
            blockers);
    }

    /// <summary>Formats a compact realignment status summary.</summary>
    /// <returns>The realignment status summary.</returns>
    public static string Describe()
    {
        SigtranApiNamingAlignmentSnapshot report = GetReport();
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={report.FoundationReady} releaseReady={report.ReleaseReady}";
    }
}
