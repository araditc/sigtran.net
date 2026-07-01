namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies production evidence execution blocker kinds.
/// </summary>
public enum SigtranReleaseEvidenceExecutionBlockerKind
{
    /// <summary>Readiness preflight blocker.</summary>
    ReadinessPreflight,

    /// <summary>Missing or mismatched execution environment variable.</summary>
    Environment,

    /// <summary>Stage command failure.</summary>
    CommandFailure,

    /// <summary>Native SCTP host capability blocker.</summary>
    NativeSctp,

    /// <summary>External peer availability blocker.</summary>
    ExternalPeer,

    /// <summary>Missing retained artifact blocker.</summary>
    ArtifactRetention,

    /// <summary>Missing digest verification blocker.</summary>
    DigestVerification,

    /// <summary>Missing redaction review blocker.</summary>
    RedactionReview,

    /// <summary>Protected approval blocker.</summary>
    ProtectedApproval,

    /// <summary>Unknown blocker.</summary>
    Unknown
}

/// <summary>
/// Describes one production evidence execution blocker.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionBlocker
{
    /// <summary>Creates a production evidence execution blocker.</summary>
    /// <param name="code">The stable blocker code.</param>
    /// <param name="kind">The blocker kind.</param>
    /// <param name="retryable">Whether the blocker can be retried after correction.</param>
    /// <param name="summary">The blocker summary.</param>
    public SigtranReleaseEvidenceExecutionBlocker(
        string code,
        SigtranReleaseEvidenceExecutionBlockerKind kind,
        bool retryable,
        string summary)
    {
        Code = string.IsNullOrWhiteSpace(code) ? throw new ArgumentException("Blocker code is required.", nameof(code)) : code;
        Kind = kind;
        Retryable = retryable;
        Summary = string.IsNullOrWhiteSpace(summary) ? throw new ArgumentException("Blocker summary is required.", nameof(summary)) : summary;
    }

    /// <summary>The stable blocker code.</summary>
    public string Code { get; }

    /// <summary>The blocker kind.</summary>
    public SigtranReleaseEvidenceExecutionBlockerKind Kind { get; }

    /// <summary>Whether the blocker can be retried after correction.</summary>
    public bool Retryable { get; }

    /// <summary>The blocker summary.</summary>
    public string Summary { get; }
}

/// <summary>
/// Describes the production evidence execution blocker classifier.
/// </summary>
public sealed class SigtranReleaseEvidenceExecutionBlockerClassifier
{
    /// <summary>Creates a production evidence execution blocker classifier.</summary>
    /// <param name="knownBlockers">The known blocker definitions.</param>
    public SigtranReleaseEvidenceExecutionBlockerClassifier(IReadOnlyList<SigtranReleaseEvidenceExecutionBlocker> knownBlockers)
    {
        ArgumentNullException.ThrowIfNull(knownBlockers);
        KnownBlockers = knownBlockers.Count == 0 ? throw new ArgumentException("At least one blocker is required.", nameof(knownBlockers)) : knownBlockers.ToArray();
    }

    /// <summary>The known blocker definitions.</summary>
    public IReadOnlyList<SigtranReleaseEvidenceExecutionBlocker> KnownBlockers { get; }

    /// <summary>Whether blocker codes are unique.</summary>
    public bool HasUniqueCodes => KnownBlockers.Select(static blocker => blocker.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() == KnownBlockers.Count;

    /// <summary>Whether the classifier covers operational blocker kinds expected during execution.</summary>
    public bool CoversOperationalKinds => RequiredKinds.All(kind => KnownBlockers.Any(blocker => blocker.Kind == kind));

    /// <summary>Whether the classifier is ready for execution failure handling.</summary>
    public bool IsReady => HasUniqueCodes
        && CoversOperationalKinds;

    /// <summary>Classifies a blocker code.</summary>
    /// <param name="code">The blocker code.</param>
    /// <returns>The matching blocker definition, or an unknown blocker.</returns>
    public SigtranReleaseEvidenceExecutionBlocker Classify(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Blocker code is required.", nameof(code));
        }

        return KnownBlockers.FirstOrDefault(blocker => string.Equals(blocker.Code, code, StringComparison.OrdinalIgnoreCase))
            ?? new(code, SigtranReleaseEvidenceExecutionBlockerKind.Unknown, retryable: false, "Unknown blocker requires manual triage.");
    }

    /// <summary>Formats a compact blocker classifier summary.</summary>
    /// <returns>The blocker classifier summary.</returns>
    public string Describe()
    {
        return $"productionEvidenceBlockerClassifierReady={IsReady} blockers={KnownBlockers.Count}";
    }

    private static readonly SigtranReleaseEvidenceExecutionBlockerKind[] RequiredKinds =
    [
        SigtranReleaseEvidenceExecutionBlockerKind.ReadinessPreflight,
        SigtranReleaseEvidenceExecutionBlockerKind.Environment,
        SigtranReleaseEvidenceExecutionBlockerKind.CommandFailure,
        SigtranReleaseEvidenceExecutionBlockerKind.NativeSctp,
        SigtranReleaseEvidenceExecutionBlockerKind.ExternalPeer,
        SigtranReleaseEvidenceExecutionBlockerKind.ArtifactRetention,
        SigtranReleaseEvidenceExecutionBlockerKind.DigestVerification,
        SigtranReleaseEvidenceExecutionBlockerKind.RedactionReview,
        SigtranReleaseEvidenceExecutionBlockerKind.ProtectedApproval
    ];
}

/// <summary>
/// Provides production evidence execution blocker classifier helpers.
/// </summary>
public static class SigtranReleaseEvidenceExecutionBlockers
{
    /// <summary>Creates the default production evidence execution blocker classifier.</summary>
    /// <returns>The default blocker classifier.</returns>
    public static SigtranReleaseEvidenceExecutionBlockerClassifier CreateDefault()
    {
        return new(
        [
            new("preflight-not-ready", SigtranReleaseEvidenceExecutionBlockerKind.ReadinessPreflight, false, "Readiness preflight failed before execution."),
            new("environment-missing", SigtranReleaseEvidenceExecutionBlockerKind.Environment, true, "Required execution environment variable is missing."),
            new("environment-mismatch", SigtranReleaseEvidenceExecutionBlockerKind.Environment, true, "Execution environment variable does not match the locked run."),
            new("command-failed", SigtranReleaseEvidenceExecutionBlockerKind.CommandFailure, true, "Execution command failed and needs log review."),
            new("native-sctp-unavailable", SigtranReleaseEvidenceExecutionBlockerKind.NativeSctp, false, "Host does not provide usable native SCTP support."),
            new("external-peer-unavailable", SigtranReleaseEvidenceExecutionBlockerKind.ExternalPeer, true, "External SIGTRAN peer is unavailable or unreachable."),
            new("artifact-missing", SigtranReleaseEvidenceExecutionBlockerKind.ArtifactRetention, true, "Required retained artifact is missing."),
            new("digest-missing", SigtranReleaseEvidenceExecutionBlockerKind.DigestVerification, true, "Required artifact digest is missing."),
            new("redaction-review-missing", SigtranReleaseEvidenceExecutionBlockerKind.RedactionReview, true, "Required redaction review is missing."),
            new("protected-approval-missing", SigtranReleaseEvidenceExecutionBlockerKind.ProtectedApproval, false, "Protected approval is required before execution can continue.")
        ]);
    }
}
