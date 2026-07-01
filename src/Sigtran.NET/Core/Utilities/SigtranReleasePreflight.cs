namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Groups the inputs required before production evidence-producing execution can start.
/// </summary>
public sealed class SigtranReleasePreflightInput
{
    /// <summary>Creates production release preflight input.</summary>
    /// <param name="target">The release target lock.</param>
    /// <param name="secrets">The secret readiness report.</param>
    /// <param name="retentionMap">The evidence retention map.</param>
    /// <param name="checklist">The evidence checklist.</param>
    public SigtranReleasePreflightInput(
        SigtranReleaseTargetLock target,
        SigtranReleaseSecretReadiness secrets,
        SigtranReleaseEvidenceRetentionMap retentionMap,
        SigtranReleaseEvidenceChecklist checklist)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
        RetentionMap = retentionMap ?? throw new ArgumentNullException(nameof(retentionMap));
        Checklist = checklist ?? throw new ArgumentNullException(nameof(checklist));
    }

    /// <summary>The release target lock.</summary>
    public SigtranReleaseTargetLock Target { get; }

    /// <summary>The secret readiness report.</summary>
    public SigtranReleaseSecretReadiness Secrets { get; }

    /// <summary>The evidence retention map.</summary>
    public SigtranReleaseEvidenceRetentionMap RetentionMap { get; }

    /// <summary>The evidence checklist.</summary>
    public SigtranReleaseEvidenceChecklist Checklist { get; }
}

/// <summary>
/// Reports production release preflight readiness.
/// </summary>
public sealed class SigtranReleasePreflightReport
{
    /// <summary>Creates a production release preflight report.</summary>
    /// <param name="input">The evaluated preflight input.</param>
    /// <param name="blockers">The preflight blockers.</param>
    public SigtranReleasePreflightReport(
        SigtranReleasePreflightInput input,
        IReadOnlyList<string> blockers)
    {
        Input = input ?? throw new ArgumentNullException(nameof(input));
        ArgumentNullException.ThrowIfNull(blockers);
        Blockers = blockers.ToArray();
    }

    /// <summary>The evaluated preflight input.</summary>
    public SigtranReleasePreflightInput Input { get; }

    /// <summary>The preflight blockers.</summary>
    public IReadOnlyList<string> Blockers { get; }

    /// <summary>Whether the preflight input is ready for evidence-producing execution.</summary>
    public bool IsReady => Blockers.Count == 0;

    /// <summary>Formats a compact preflight summary.</summary>
    /// <returns>The preflight summary.</returns>
    public string Describe()
    {
        return $"productionReleasePreflightReady={IsReady} blockers={Blockers.Count}";
    }
}

/// <summary>
/// Provides production release preflight helpers.
/// </summary>
public static class SigtranReleasePreflightChecks
{
    /// <summary>Evaluates production release preflight readiness.</summary>
    /// <param name="input">The preflight input.</param>
    /// <returns>The production release preflight report.</returns>
    public static SigtranReleasePreflightReport Evaluate(SigtranReleasePreflightInput input)
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
    /// <returns>The production release preflight input.</returns>
    public static SigtranReleasePreflightInput CreatePrereleaseInput(
        string version,
        string sourceCommit,
        IEnumerable<string> availableSecretNames)
    {
        SigtranReleaseTargetLock target = SigtranReleaseTargetLocks.CreatePrerelease(version, sourceCommit);

        return new(
            target,
            SigtranReleaseSecrets.Evaluate(availableSecretNames),
            SigtranReleaseEvidenceRetentionMaps.CreateDefault(target),
            SigtranReleaseEvidenceChecklists.CreateDefault());
    }
}
