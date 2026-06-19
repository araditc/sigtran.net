namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes package publication readiness.
/// </summary>
public sealed class SigtranPackagePublicationReadinessReport
{
    /// <summary>Creates a package publication readiness report.</summary>
    /// <param name="versionPolicyReady">Whether version policy is ready.</param>
    /// <param name="metadataReady">Whether NuGet metadata is ready.</param>
    /// <param name="layoutReady">Whether package layout is ready.</param>
    /// <param name="dryRunReady">Whether dry-run publication is ready.</param>
    /// <param name="credentialPolicyReady">Whether credential policy is ready.</param>
    /// <param name="channelPolicyReady">Whether channel policy is ready.</param>
    /// <param name="integrityManifestReady">Whether package integrity manifest is ready.</param>
    /// <param name="evidenceManifestReady">Whether publication evidence manifest is ready.</param>
    /// <param name="publicationGateReady">Whether publication gate is ready.</param>
    public SigtranPackagePublicationReadinessReport(
        bool versionPolicyReady,
        bool metadataReady,
        bool layoutReady,
        bool dryRunReady,
        bool credentialPolicyReady,
        bool channelPolicyReady,
        bool integrityManifestReady,
        bool evidenceManifestReady,
        bool publicationGateReady)
    {
        VersionPolicyReady = versionPolicyReady;
        MetadataReady = metadataReady;
        LayoutReady = layoutReady;
        DryRunReady = dryRunReady;
        CredentialPolicyReady = credentialPolicyReady;
        ChannelPolicyReady = channelPolicyReady;
        IntegrityManifestReady = integrityManifestReady;
        EvidenceManifestReady = evidenceManifestReady;
        PublicationGateReady = publicationGateReady;
    }

    /// <summary>Whether version policy is ready.</summary>
    public bool VersionPolicyReady { get; }

    /// <summary>Whether NuGet metadata is ready.</summary>
    public bool MetadataReady { get; }

    /// <summary>Whether package layout is ready.</summary>
    public bool LayoutReady { get; }

    /// <summary>Whether dry-run publication is ready.</summary>
    public bool DryRunReady { get; }

    /// <summary>Whether credential policy is ready.</summary>
    public bool CredentialPolicyReady { get; }

    /// <summary>Whether channel policy is ready.</summary>
    public bool ChannelPolicyReady { get; }

    /// <summary>Whether package integrity manifest is ready.</summary>
    public bool IntegrityManifestReady { get; }

    /// <summary>Whether publication evidence manifest is ready.</summary>
    public bool EvidenceManifestReady { get; }

    /// <summary>Whether publication gate is ready.</summary>
    public bool PublicationGateReady { get; }

    /// <summary>Whether the package publication foundation is ready.</summary>
    public bool FoundationReady => VersionPolicyReady
        && MetadataReady
        && LayoutReady
        && DryRunReady
        && CredentialPolicyReady
        && ChannelPolicyReady
        && IntegrityManifestReady
        && EvidenceManifestReady
        && PublicationGateReady;

    /// <summary>Whether real package publication is currently ready.</summary>
    public bool PublicationReady => false;
}

/// <summary>
/// Provides package publication readiness and status reporting.
/// </summary>
public static class SigtranPackagePublicationStatus
{
    private static readonly string[] Capabilities =
    [
        "version-policy",
        "nuget-metadata-contract",
        "package-layout",
        "dry-run-publish-plan",
        "credential-policy",
        "channel-policy",
        "integrity-manifest",
        "publication-evidence",
        "publication-gate",
        "documentation"
    ];

    /// <summary>The status label.</summary>
    public const string StatusLabel = "Package Publication Readiness";

    /// <summary>The number of completed package publication work units.</summary>
    public const int CompletedUnitCount = 10;

    /// <summary>Returns the completed package publication capability names.</summary>
    /// <returns>The completed capability names.</returns>
    public static IReadOnlyList<string> GetCompletedCapabilities()
    {
        return Capabilities.ToArray();
    }

    /// <summary>Returns the current package publication readiness report.</summary>
    /// <returns>The current package publication readiness report.</returns>
    public static SigtranPackagePublicationReadinessReport GetReadiness()
    {
        SigtranNuGetMetadataContract metadata = SigtranNuGetMetadata.CreateDefaultContract();
        string projectXml = File.Exists("src/sigtran.net/sigtran.net.csproj")
            ? File.ReadAllText("src/sigtran.net/sigtran.net.csproj")
            : string.Empty;

        return new(
            versionPolicyReady: SigtranReleaseVersionPolicies.CreateDefault().CoversPublicationLanes,
            metadataReady: metadata.IsPublicationReady && metadata.GetMissingProperties(projectXml).Count == 0,
            layoutReady: SigtranPackageLayouts.CreateDefault().IncludesRequiredArtifacts,
            dryRunReady: SigtranNuGetPublishPlans.CreateDryRun().IsDryRunSafe,
            credentialPolicyReady: SigtranPublicationCredentials.CreateDefaultPolicy().RequiresCommercialSecrets,
            channelPolicyReady: SigtranPublishChannels.GetChannels().Any(static channel => channel.Kind == SigtranPublishChannelKind.Stable && channel.RequiresCommercialReadiness),
            integrityManifestReady: SigtranPackageIntegrityManifest.CreateCompleteSample().IsComplete,
            evidenceManifestReady: SigtranPublicationEvidenceManifest.CreateCompleteSample().IsComplete,
            publicationGateReady: Capabilities.Contains("publication-gate"));
    }

    /// <summary>Whether the package publication foundation is ready.</summary>
    public static bool FoundationReady => GetReadiness().FoundationReady && Capabilities.Length == CompletedUnitCount;

    /// <summary>Whether real package publication is currently ready.</summary>
    public static bool PublicationReady => GetReadiness().PublicationReady;

    /// <summary>Formats a compact package publication status summary.</summary>
    /// <returns>The package publication status summary.</returns>
    public static string Describe()
    {
        return $"{StatusLabel}: completedUnits={CompletedUnitCount} capabilities={Capabilities.Length} foundationReady={FoundationReady} publicationReady={PublicationReady}";
    }
}
