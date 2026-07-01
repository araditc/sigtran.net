namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes filesystem-backed retained file manifest execution.
/// </summary>
public sealed class SigtranReleaseEvidenceFileSystemManifestExecution
{
    /// <summary>Creates filesystem-backed retained file manifest execution.</summary>
    /// <param name="handoff">The promotion handoff that declares retained evidence files.</param>
    /// <param name="observations">The filesystem observations.</param>
    /// <param name="manifest">The retained file manifest created from the observations.</param>
    public SigtranReleaseEvidenceFileSystemManifestExecution(
        SigtranReleaseEvidencePromotionHandoff handoff,
        IReadOnlyList<SigtranReleaseEvidenceFileSystemObservation> observations,
        SigtranReleaseEvidenceRetainedFileManifest manifest)
    {
        Handoff = handoff ?? throw new ArgumentNullException(nameof(handoff));
        ArgumentNullException.ThrowIfNull(observations);
        Observations = observations.Count == 0 ? throw new ArgumentException("At least one filesystem observation is required.", nameof(observations)) : observations.ToArray();
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    /// <summary>The promotion handoff that declares retained evidence files.</summary>
    public SigtranReleaseEvidencePromotionHandoff Handoff { get; }

    /// <summary>The filesystem observations.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceFileSystemObservation> Observations { get; }

    /// <summary>The retained file manifest created from the observations.</summary>
    public SigtranReleaseEvidenceRetainedFileManifest Manifest { get; }

    /// <summary>Whether every handoff item was observed from the filesystem.</summary>
    public bool CoversHandoffItems => Handoff.Items.All(item => Observations.Any(observation =>
        observation.RetainedFile.Kind == item.Kind
        && observation.RetainedFile.RetainedPath == item.RetainedPath
        && string.Equals(observation.RetainedFile.ExpectedSha256, item.Sha256, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Whether all observed files exist on the filesystem.</summary>
    public bool AllObservedFilesExist => Observations.All(static observation => observation.Exists);

    /// <summary>Whether all observed files match the handoff digests.</summary>
    public bool AllObservedDigestsMatch => Observations.All(static observation => observation.DigestMatchesHandoff);

    /// <summary>Whether the filesystem-backed manifest execution is ready for verification reporting.</summary>
    public bool IsReady => CoversHandoffItems
        && AllObservedFilesExist
        && AllObservedDigestsMatch
        && Manifest.IsReady;

    /// <summary>Formats a compact filesystem manifest execution summary.</summary>
    /// <returns>The filesystem manifest execution summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceFileSystemManifestReady={IsReady} observations={Observations.Count} intake={Handoff.Report.Target.IntakeId}";
    }
}

/// <summary>
/// Builds retained production evidence file manifests from filesystem observations.
/// </summary>
public static class SigtranReleaseEvidenceFileSystemManifestBuilder
{
    /// <summary>Builds a retained file manifest by observing every promotion handoff item.</summary>
    /// <param name="handoff">The promotion handoff that declares retained evidence files.</param>
    /// <param name="pathOverrides">Optional local filesystem path overrides keyed by retained path.</param>
    /// <param name="observedAtUtc">An optional UTC observation time.</param>
    /// <returns>The filesystem-backed retained file manifest execution.</returns>
    public static SigtranReleaseEvidenceFileSystemManifestExecution Build(
        SigtranReleaseEvidencePromotionHandoff handoff,
        IReadOnlyDictionary<string, string>? pathOverrides = null,
        DateTimeOffset? observedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        SigtranReleaseEvidenceFileSystemObservation[] observations = handoff.Items
            .Select(item => SigtranReleaseEvidenceFileSystemObserver.Observe(
                item,
                ResolvePath(item, pathOverrides),
                observedAtUtc))
            .ToArray();
        SigtranReleaseEvidenceRetainedFileManifest manifest = new(
            handoff,
            observations.Select(static observation => observation.RetainedFile).ToArray());

        return new(handoff, observations, manifest);
    }

    private static string? ResolvePath(
        SigtranReleaseEvidencePromotionHandoffItem item,
        IReadOnlyDictionary<string, string>? pathOverrides)
    {
        return pathOverrides is not null && pathOverrides.TryGetValue(item.RetainedPath, out string? path)
            ? path
            : null;
    }
}
