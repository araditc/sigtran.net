namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes verification required for one commercial evidence execution artifact.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionVerificationItem
{
    /// <summary>Creates a commercial evidence execution verification item.</summary>
    /// <param name="artifactPath">The retained artifact path.</param>
    /// <param name="kind">The checklist artifact kind.</param>
    /// <param name="requiresDigest">Whether digest verification is required.</param>
    /// <param name="requiresRedactionReview">Whether redaction review is required.</param>
    public SigtranCommercialEvidenceExecutionVerificationItem(
        string artifactPath,
        SigtranCommercialEvidenceChecklistKind kind,
        bool requiresDigest,
        bool requiresRedactionReview)
    {
        ArtifactPath = string.IsNullOrWhiteSpace(artifactPath) ? throw new ArgumentException("Artifact path is required.", nameof(artifactPath)) : artifactPath;
        Kind = kind;
        RequiresDigest = requiresDigest;
        RequiresRedactionReview = requiresRedactionReview;
    }

    /// <summary>The retained artifact path.</summary>
    public string ArtifactPath { get; }

    /// <summary>The checklist artifact kind.</summary>
    public SigtranCommercialEvidenceChecklistKind Kind { get; }

    /// <summary>Whether digest verification is required.</summary>
    public bool RequiresDigest { get; }

    /// <summary>Whether redaction review is required.</summary>
    public bool RequiresRedactionReview { get; }

    /// <summary>Whether this item satisfies verification requirements for its artifact kind.</summary>
    public bool IsReady => RequiresDigest
        && (!IsTraceBearingKind(Kind) || RequiresRedactionReview);

    /// <summary>Checks whether an artifact kind carries traffic, logs, or configuration.</summary>
    /// <param name="kind">The artifact kind.</param>
    /// <returns><c>true</c> when redaction review is required for the kind; otherwise, <c>false</c>.</returns>
    public static bool IsTraceBearingKind(SigtranCommercialEvidenceChecklistKind kind)
    {
        return kind is SigtranCommercialEvidenceChecklistKind.PacketCapture
            or SigtranCommercialEvidenceChecklistKind.PeerLog
            or SigtranCommercialEvidenceChecklistKind.SdkTrace
            or SigtranCommercialEvidenceChecklistKind.Configuration
            or SigtranCommercialEvidenceChecklistKind.ComparisonReport
            or SigtranCommercialEvidenceChecklistKind.BenchmarkReport;
    }
}

/// <summary>
/// Describes the commercial evidence execution digest and redaction verification plan.
/// </summary>
public sealed class SigtranCommercialEvidenceExecutionVerificationPlan
{
    /// <summary>Creates a commercial evidence execution verification plan.</summary>
    /// <param name="manifest">The artifact manifest.</param>
    /// <param name="items">The verification items.</param>
    public SigtranCommercialEvidenceExecutionVerificationPlan(
        SigtranCommercialEvidenceExecutionArtifactManifest manifest,
        IReadOnlyList<SigtranCommercialEvidenceExecutionVerificationItem> items)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        ArgumentNullException.ThrowIfNull(items);
        Items = items.Count == 0 ? throw new ArgumentException("At least one verification item is required.", nameof(items)) : items.ToArray();
    }

    /// <summary>The artifact manifest.</summary>
    public SigtranCommercialEvidenceExecutionArtifactManifest Manifest { get; }

    /// <summary>The verification items.</summary>
    public IReadOnlyList<SigtranCommercialEvidenceExecutionVerificationItem> Items { get; }

    /// <summary>Whether every artifact has a verification item.</summary>
    public bool CoversArtifacts => Manifest.Artifacts.All(artifact => Items.Any(item => item.ArtifactPath == artifact.Path));

    /// <summary>Whether every artifact requires digest verification.</summary>
    public bool RequiresDigestForAllArtifacts => Items.All(static item => item.RequiresDigest);

    /// <summary>Whether trace-bearing artifacts require redaction review.</summary>
    public bool RequiresRedactionForTraceArtifacts => Items
        .Where(static item => SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(item.Kind))
        .All(static item => item.RequiresRedactionReview);

    /// <summary>Whether the verification plan is ready for evidence review.</summary>
    public bool IsReady => Manifest.IsReady
        && CoversArtifacts
        && RequiresDigestForAllArtifacts
        && RequiresRedactionForTraceArtifacts
        && Items.All(static item => item.IsReady);

    /// <summary>Formats a compact verification plan summary.</summary>
    /// <returns>The verification plan summary.</returns>
    public string Describe()
    {
        return $"commercialEvidenceVerificationReady={IsReady} items={Items.Count}";
    }
}

/// <summary>
/// Provides commercial evidence execution verification plan helpers.
/// </summary>
public static class SigtranCommercialEvidenceExecutionVerifications
{
    /// <summary>Creates the default digest and redaction verification plan.</summary>
    /// <param name="manifest">The artifact manifest.</param>
    /// <returns>The default verification plan.</returns>
    public static SigtranCommercialEvidenceExecutionVerificationPlan CreateDefault(SigtranCommercialEvidenceExecutionArtifactManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return new(
            manifest,
            manifest.Artifacts
                .Select(static artifact => new SigtranCommercialEvidenceExecutionVerificationItem(
                    artifact.Path,
                    artifact.Kind,
                    requiresDigest: true,
                    requiresRedactionReview: SigtranCommercialEvidenceExecutionVerificationItem.IsTraceBearingKind(artifact.Kind)))
                .ToArray());
    }
}
