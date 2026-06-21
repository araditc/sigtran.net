namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes the package-neutral commercial roadmap realignment result.
/// </summary>
public sealed class SigtranCommercialRoadmapRealignmentReport
{
    /// <summary>Creates a commercial roadmap realignment report.</summary>
    /// <param name="completedUnitCount">The completed work unit count.</param>
    /// <param name="capabilities">The completed capability ids.</param>
    /// <param name="packageNeutralNamingReady">Whether public contract names are package-neutral.</param>
    /// <param name="maintainedPeerPolicyReady">Whether the maintained peer policy is available.</param>
    /// <param name="externalPeerExecutionFoundationReady">Whether external peer execution foundation is ready.</param>
    /// <param name="commercialGateAligned">Whether commercial gates use external peer terminology.</param>
    /// <param name="externalPeerCommercialInteropReady">Whether current external peer commercial interop evidence is ready.</param>
    /// <param name="blockers">The current blocker identifiers.</param>
    public SigtranCommercialRoadmapRealignmentReport(
        int completedUnitCount,
        IReadOnlyList<string> capabilities,
        bool packageNeutralNamingReady,
        bool maintainedPeerPolicyReady,
        bool externalPeerExecutionFoundationReady,
        bool commercialGateAligned,
        bool externalPeerCommercialInteropReady,
        IReadOnlyList<string> blockers)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(blockers);

        CompletedUnitCount = completedUnitCount;
        Capabilities = capabilities.ToArray();
        PackageNeutralNamingReady = packageNeutralNamingReady;
        MaintainedPeerPolicyReady = maintainedPeerPolicyReady;
        ExternalPeerExecutionFoundationReady = externalPeerExecutionFoundationReady;
        CommercialGateAligned = commercialGateAligned;
        ExternalPeerCommercialInteropReady = externalPeerCommercialInteropReady;
        Blockers = blockers.ToArray();
    }

    /// <summary>The completed work unit count.</summary>
    public int CompletedUnitCount { get; }

    /// <summary>The completed capability ids.</summary>
    public IReadOnlyList<string> Capabilities { get; }

    /// <summary>Whether public contract names are package-neutral.</summary>
    public bool PackageNeutralNamingReady { get; }

    /// <summary>Whether the maintained peer policy is available.</summary>
    public bool MaintainedPeerPolicyReady { get; }

    /// <summary>Whether external peer execution foundation is ready.</summary>
    public bool ExternalPeerExecutionFoundationReady { get; }

    /// <summary>Whether commercial gates use external peer terminology.</summary>
    public bool CommercialGateAligned { get; }

    /// <summary>Whether current external peer commercial interop evidence is ready.</summary>
    public bool ExternalPeerCommercialInteropReady { get; }

    /// <summary>The current blocker identifiers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the package-neutral roadmap realignment foundation is complete.</summary>
    public bool FoundationReady => Capabilities.Count == CompletedUnitCount
        && PackageNeutralNamingReady
        && MaintainedPeerPolicyReady
        && ExternalPeerExecutionFoundationReady
        && CommercialGateAligned;

    /// <summary>Whether the realigned commercial release gate can pass today.</summary>
    public bool CommercialReleaseReady => FoundationReady
        && ExternalPeerCommercialInteropReady
        && Blockers.Count == 0;

    /// <summary>Formats a compact realignment status summary.</summary>
    /// <returns>The realignment status summary.</returns>
    public string Describe()
    {
        return $"commercialRoadmapRealignmentReady={FoundationReady} commercialReleaseReady={CommercialReleaseReady} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides package-neutral commercial roadmap realignment status.
/// </summary>
public static class SigtranCommercialRoadmapRealignmentStatus
{
    private static readonly string[] Capabilities =
    [
        "package-neutral-source-contracts",
        "external-peer-profile-model",
        "maintained-peer-selection-policy",
        "external-peer-lab-environment-contract",
        "digest-covered-external-peer-artifacts",
        "external-peer-run-commands",
        "external-peer-commercial-readiness",
        "commercial-release-gate-alignment",
        "public-label-migration",
        "documentation"
    ];

    private static readonly string[] PublicContractNames =
    [
        nameof(SigtranExternalPeerInteropStatus),
        nameof(SigtranExternalPeerCommercialReadiness),
        nameof(SigtranMaintainedPeerSelectionPolicy),
        nameof(SigtranCommercialReleaseExecutionReadiness),
        nameof(SigtranCommercialEvidenceGate),
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
    public const string StatusLabel = "Commercial Roadmap Realignment";

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
            || name.Contains("commercial", StringComparison.OrdinalIgnoreCase)
            || name.Contains("maintained", StringComparison.OrdinalIgnoreCase)
            || name.Contains("evidence", StringComparison.OrdinalIgnoreCase)
            || name.Contains("package-neutral", StringComparison.OrdinalIgnoreCase)
            || name.Contains("public-label", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "documentation", StringComparison.OrdinalIgnoreCase));

    /// <summary>Creates the current package-neutral commercial roadmap realignment report.</summary>
    /// <returns>The current realignment report.</returns>
    public static SigtranCommercialRoadmapRealignmentReport GetReport()
    {
        SigtranExternalPeerCommercialReadinessReport commercialPeer = SigtranExternalPeerCommercialReadiness.CreateCurrent();
        SigtranCommercialReleaseExecutionReadinessReport release = SigtranCommercialReleaseExecutionReadiness.CreateCurrent();

        bool maintainedPeerPolicyReady = SigtranMaintainedPeerSelectionPolicy.CreateDefault().GetCriteria().Count == 6
            && SigtranInteropPeerProfiles.CreateExternalPeerSignallingGateway().IsMaintainedCommercialCandidate;
        bool commercialGateAligned = release.Items.Any(static item => item.Name == "external-peer-interop")
            && release.Items.All(static item => !item.Name.Contains("openss7", StringComparison.OrdinalIgnoreCase))
            && release.Items.All(static item => !item.Name.Contains("ipss7", StringComparison.OrdinalIgnoreCase));

        List<string> blockers = [];
        if (!PackageNeutralNamingReady)
        {
            blockers.Add("package-neutral-naming-required");
        }

        if (!maintainedPeerPolicyReady)
        {
            blockers.Add("maintained-peer-policy-required");
        }

        if (!SigtranExternalPeerInteropStatus.FoundationReady)
        {
            blockers.Add("external-peer-execution-foundation-required");
        }

        if (!commercialGateAligned)
        {
            blockers.Add("commercial-gate-alignment-required");
        }

        blockers.AddRange(commercialPeer.Blockers);

        return new(
            CompletedUnitCount,
            Capabilities,
            PackageNeutralNamingReady,
            maintainedPeerPolicyReady,
            SigtranExternalPeerInteropStatus.FoundationReady,
            commercialGateAligned,
            commercialPeer.CommercialInteropReady,
            blockers);
    }

    /// <summary>Formats a compact realignment status summary.</summary>
    /// <returns>The realignment status summary.</returns>
    public static string Describe()
    {
        SigtranCommercialRoadmapRealignmentReport report = GetReport();
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={report.FoundationReady} commercialReleaseReady={report.CommercialReleaseReady}";
    }
}
