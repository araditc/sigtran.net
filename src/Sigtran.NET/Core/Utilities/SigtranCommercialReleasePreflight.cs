namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Groups the inputs required before commercial evidence-producing execution can start.
/// </summary>
public sealed class SigtranCommercialReleasePreflightInput
{
    /// <summary>Creates commercial release preflight input.</summary>
    /// <param name="target">The release target lock.</param>
    /// <param name="secrets">The secret readiness report.</param>
    /// <param name="retentionMap">The evidence retention map.</param>
    /// <param name="checklist">The evidence checklist.</param>
    public SigtranCommercialReleasePreflightInput(
        SigtranCommercialReleaseTargetLock target,
        SigtranCommercialReleaseSecretReadiness secrets,
        SigtranCommercialEvidenceRetentionMap retentionMap,
        SigtranCommercialEvidenceChecklist checklist)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        RetentionMap = retentionMap ?? throw new ArgumentNullException(nameof(retentionMap));
        Checklist = checklist ?? throw new ArgumentNullException(nameof(checklist));
    }

    /// <summary>The release target lock.</summary>
    public SigtranCommercialReleaseTargetLock Target { get; }

    /// <summary>The secret readiness report.</summary>
    public SigtranCommercialReleaseSecretReadiness Secrets { get; }

    /// <summary>The evidence retention map.</summary>
    public SigtranCommercialEvidenceRetentionMap RetentionMap { get; }

    /// <summary>The evidence checklist.</summary>
    public SigtranCommercialEvidenceChecklist Checklist { get; }
}

/// <summary>
/// Reports commercial release preflight readiness.
/// </summary>
public sealed class SigtranCommercialReleasePreflightReport
{
    /// <summary>Creates a commercial release preflight report.</summary>
    /// <param name="input">The evaluated preflight input.</param>
    /// <param name="blockers">The preflight blockers.</param>
    public SigtranCommercialReleasePreflightReport(
        SigtranCommercialReleasePreflightInput input,
        IReadOnlyList<string> blockers)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The evaluated preflight input.</summary>
    public SigtranCommercialReleasePreflightInput Input { get; }

    /// <summary>The preflight blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the preflight input is ready for evidence-producing execution.</summary>
    public bool IsReady => Blockers.Count == 0;

    /// <summary>Formats a compact preflight summary.</summary>
    /// <returns>The preflight summary.</returns>
    public string Describe()
    {
        return $"commercialReleasePreflightReady={IsReady} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides commercial release preflight helpers.
/// </summary>
public static class SigtranCommercialReleasePreflightChecks
{
    /// <summary>Evaluates commercial release preflight readiness.</summary>
    /// <param name="input">The preflight input.</param>
    /// <returns>The commercial release preflight report.</returns>
    public static SigtranCommercialReleasePreflightReport Evaluate(SigtranCommercialReleasePreflightInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        List<string> blockers = [];

        if (!input.Target.IsLocked)
        {
            blockers.Add("release-target-not-locked");
        }

        if (!input.Secrets.IsReady)
        {
            blockers.Add("release-secrets-missing");
        }

        if (!input.RetentionMap.IsReady)
        {
            blockers.Add("evidence-retention-map-not-ready");
        }

        if (!ReferenceEquals(input.Target, input.RetentionMap.Target)
            && (input.RetentionMap.Target.Version != input.Target.Version
                || input.RetentionMap.Target.SourceCommit != input.Target.SourceCommit
                || input.RetentionMap.Target.ArtifactRoot != input.Target.ArtifactRoot))
        {
            blockers.Add("retention-map-target-mismatch");
        }

        if (!input.Checklist.IsReady)
        {
            blockers.Add("evidence-checklist-not-ready");
        }

        return new(input, blockers);
    }

    /// <summary>Creates a fully configured release-candidate preflight input.</summary>
    /// <param name="version">The release-candidate package version.</param>
    /// <param name="sourceCommit">The source commit used to produce evidence.</param>
    /// <param name="availableSecretNames">The available release secret names.</param>
    /// <returns>The commercial release preflight input.</returns>
    public static SigtranCommercialReleasePreflightInput CreateReleaseCandidateInput(
        string version,
        string sourceCommit,
        IEnumerable<string> availableSecretNames)
    {
        SigtranCommercialReleaseTargetLock target = SigtranCommercialReleaseTargetLocks.CreateReleaseCandidate(version, sourceCommit);

        return new(
            target,
            SigtranCommercialReleaseSecrets.Evaluate(availableSecretNames),
            SigtranCommercialEvidenceRetentionMaps.CreateDefault(target),
            SigtranCommercialEvidenceChecklists.CreateDefault());
    }
}
