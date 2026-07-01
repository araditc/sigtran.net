namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies a production evidence retention area.
/// </summary>
public enum SigtranReleaseEvidenceRetentionArea
{
    /// <summary>Native SCTP packet, trace, and host evidence.</summary>
    NativeSctp,

    /// <summary>External SIGTRAN peer interoperability evidence.</summary>
    ExternalPeerInterop,

    /// <summary>SCCP, TCAP, MAP, and M3UA protocol comparison evidence.</summary>
    ProtocolInterop,

    /// <summary>Peer traffic performance and resilience evidence.</summary>
    Performance,

    /// <summary>SBOM, signing, timestamp, provenance, and digest evidence.</summary>
    SupplyChain,

    /// <summary>Public API baseline and diff evidence.</summary>
    PublicApi,

    /// <summary>Release workflow dry-run and publication-gate evidence.</summary>
    ReleaseWorkflow,

    /// <summary>Release notes, migration notes, and production readiness report evidence.</summary>
    PublicationDossier
}

/// <summary>
/// Describes one retained evidence path and retention rule.
/// </summary>
public sealed class SigtranReleaseEvidenceRetentionRule
{
    /// <summary>Creates a production evidence retention rule.</summary>
    /// <param name="area">The evidence retention area.</param>
    /// <param name="path">The retained artifact path.</param>
    /// <param name="retentionDays">The retention period in days.</param>
    /// <param name="requiresDigest">Whether the retained path requires digest coverage.</param>
    public SigtranReleaseEvidenceRetentionRule(
        SigtranReleaseEvidenceRetentionArea area,
        string path,
        int retentionDays,
        bool requiresDigest)
    {
        Area = area;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Retention path is required.", nameof(path)) : path;
        RetentionDays = retentionDays <= 0 ? throw new ArgumentOutOfRangeException(nameof(retentionDays), "Retention days must be positive.") : retentionDays;
        RequiresDigest = requiresDigest;
    }

    /// <summary>The evidence retention area.</summary>
    public SigtranReleaseEvidenceRetentionArea Area { get; }

    /// <summary>The retained artifact path.</summary>
    public string Path { get; }

    /// <summary>The retention period in days.</summary>
    public int RetentionDays { get; }

    /// <summary>Whether the retained path requires digest coverage.</summary>
    public bool RequiresDigest { get; }

    /// <summary>Checks whether the rule path is under the release target artifact root.</summary>
    /// <param name="target">The release target lock.</param>
    /// <returns><c>true</c> when the rule path is bound to the target artifact root; otherwise, <c>false</c>.</returns>
    public bool IsBoundTo(SigtranReleaseTargetLock target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return Path.StartsWith(target.ArtifactRoot + "/", StringComparison.Ordinal);
    }
}

/// <summary>
/// Describes the production evidence retention map for one release target.
/// </summary>
public sealed class SigtranReleaseEvidenceRetentionMap
{
    /// <summary>Creates a production evidence retention map.</summary>
    /// <param name="target">The release target lock.</param>
    /// <param name="rules">The evidence retention rules.</param>
    public SigtranReleaseEvidenceRetentionMap(
        SigtranReleaseTargetLock target,
        IReadOnlyList<SigtranReleaseEvidenceRetentionRule> rules)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        ArgumentNullException.ThrowIfNull(rules);
        Rules = rules.Count == 0 ? throw new ArgumentException("At least one retention rule is required.", nameof(rules)) : rules.ToArray();
    }

    /// <summary>The release target lock.</summary>
    public SigtranReleaseTargetLock Target { get; }

    /// <summary>The evidence retention rules.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceRetentionRule> Rules { get; }

    /// <summary>Whether every production evidence area has a retention rule.</summary>
    public bool HasRequiredAreas => Enum.GetValues<SigtranReleaseEvidenceRetentionArea>()
        .All(area => Rules.Any(rule => rule.Area == area));

    /// <summary>Whether every rule is bound to the release target artifact root.</summary>
    public bool UsesTargetArtifactRoot => Rules.All(rule => rule.IsBoundTo(Target));

    /// <summary>Whether every rule keeps evidence for at least one year.</summary>
    public bool MeetsProductionRetentionPeriod => Rules.All(static rule => rule.RetentionDays >= 365);

    /// <summary>Whether every retained path requires digest coverage.</summary>
    public bool RequiresDigestCoverage => Rules.All(static rule => rule.RequiresDigest);

    /// <summary>Whether the retention map is ready for production evidence capture.</summary>
    public bool IsReady => Target.IsLocked
        && HasRequiredAreas
        && UsesTargetArtifactRoot
        && MeetsProductionRetentionPeriod
        && RequiresDigestCoverage;

    /// <summary>Formats a compact retention map summary.</summary>
    /// <returns>The retention map summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceRetentionReady={IsReady} rules={Rules.Count} root={Target.ArtifactRoot}";
    }
}

/// <summary>
/// Provides production evidence retention map helpers.
/// </summary>
public static class SigtranReleaseEvidenceRetentionMaps
{
    /// <summary>Creates the default production evidence retention map for a release target.</summary>
    /// <param name="target">The release target lock.</param>
    /// <returns>The default production evidence retention map.</returns>
    public static SigtranReleaseEvidenceRetentionMap CreateDefault(SigtranReleaseTargetLock target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return new(
            target,
            Enum.GetValues<SigtranReleaseEvidenceRetentionArea>()
                .Select(area => new SigtranReleaseEvidenceRetentionRule(
                    area,
                    $"{target.ArtifactRoot}/{ToPathSegment(area)}",
                    365,
                    requiresDigest: true))
                .ToArray());
    }

    private static string ToPathSegment(SigtranReleaseEvidenceRetentionArea area)
    {
        return area switch
        {
            SigtranReleaseEvidenceRetentionArea.NativeSctp => "native-sctp",
            SigtranReleaseEvidenceRetentionArea.ExternalPeerInterop => "external-peer-interop",
            SigtranReleaseEvidenceRetentionArea.ProtocolInterop => "protocol-interop",
            SigtranReleaseEvidenceRetentionArea.Performance => "performance",
            SigtranReleaseEvidenceRetentionArea.SupplyChain => "supply-chain",
            SigtranReleaseEvidenceRetentionArea.PublicApi => "public-api",
            SigtranReleaseEvidenceRetentionArea.ReleaseWorkflow => "release-workflow",
            SigtranReleaseEvidenceRetentionArea.PublicationDossier => "publication-dossier",
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, "Unknown evidence retention area.")
        };
    }
}
