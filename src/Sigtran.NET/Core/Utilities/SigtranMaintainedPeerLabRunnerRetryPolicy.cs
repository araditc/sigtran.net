using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes retry behavior for one maintained external peer lab runner failure kind.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerRetryRule
{
    /// <summary>Creates a maintained peer lab runner retry rule.</summary>
    /// <param name="failureKind">The failure kind.</param>
    /// <param name="retryable">Whether the failure kind is retryable.</param>
    /// <param name="maxAttempts">The maximum number of attempts.</param>
    /// <param name="delay">The retry delay.</param>
    /// <param name="description">The rule description.</param>
    public SigtranMaintainedPeerLabRunnerRetryRule(
        SigtranMaintainedPeerLabRunnerFailureKind failureKind,
        bool retryable,
        int maxAttempts,
        TimeSpan delay,
        string description)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Maximum attempts must be at least one.");
        }

        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Retry delay cannot be negative.");
        }

        FailureKind = failureKind;
        Retryable = retryable;
        MaxAttempts = maxAttempts;
        Delay = delay;
        Description = string.IsNullOrWhiteSpace(description) ? throw new ArgumentException("Rule description is required.", nameof(description)) : description;
    }

    /// <summary>The failure kind.</summary>
    public SigtranMaintainedPeerLabRunnerFailureKind FailureKind { get; }

    /// <summary>Whether the failure kind is retryable.</summary>
    public bool Retryable { get; }

    /// <summary>The maximum number of attempts.</summary>
    public int MaxAttempts { get; }

    /// <summary>The retry delay.</summary>
    public TimeSpan Delay { get; }

    /// <summary>The rule description.</summary>
    public string Description { get; }

    /// <summary>Formats a compact retry rule summary.</summary>
    /// <returns>The retry rule summary.</returns>
    public string Describe()
    {
        return $"kind={FailureKind} retryable={Retryable} maxAttempts={MaxAttempts} delay={Delay}";
    }
}

/// <summary>
/// Describes retry policy evaluation for a maintained external peer lab runner failure report.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerRetryEvaluation
{
    /// <summary>Creates a maintained peer lab runner retry evaluation.</summary>
    /// <param name="failureReport">The failure report.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <param name="retryableFailures">The retryable failures.</param>
    /// <param name="nonRetryableFailures">The non-retryable failures.</param>
    /// <param name="nextDelay">The next retry delay.</param>
    public SigtranMaintainedPeerLabRunnerRetryEvaluation(
        SigtranMaintainedPeerLabRunnerFailureReport failureReport,
        int attemptNumber,
        IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> retryableFailures,
        IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> nonRetryableFailures,
        TimeSpan nextDelay)
    {
        ArgumentNullException.ThrowIfNull(failureReport);
        ArgumentNullException.ThrowIfNull(retryableFailures);
        ArgumentNullException.ThrowIfNull(nonRetryableFailures);

        if (attemptNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(attemptNumber), "Attempt number must be at least one.");
        }

        FailureReport = failureReport;
        AttemptNumber = attemptNumber;
        RetryableFailures = retryableFailures.ToArray();
        NonRetryableFailures = nonRetryableFailures.ToArray();
        NextDelay = nextDelay;
    }

    /// <summary>The failure report.</summary>
    public SigtranMaintainedPeerLabRunnerFailureReport FailureReport { get; }

    /// <summary>The current attempt number.</summary>
    public int AttemptNumber { get; }

    /// <summary>The retryable failures.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> RetryableFailures { get; }

    /// <summary>The non-retryable failures.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerFailure> NonRetryableFailures { get; }

    /// <summary>The next retry delay.</summary>
    public TimeSpan NextDelay { get; }

    /// <summary>Whether another attempt is allowed.</summary>
    public bool CanRetry => FailureReport.HasFailures
        && RetryableFailures.Count > 0
        && NonRetryableFailures.Count == 0;

    /// <summary>Renders the retry evaluation as Markdown.</summary>
    /// <returns>The Markdown report.</returns>
    public string RenderMarkdown()
    {
        StringBuilder builder = new();
        builder.AppendLine("# Maintained Peer Lab Runner Retry Evaluation");
        builder.AppendLine();
        builder.AppendLine($"Run: `{FailureReport.RunId}`");
        builder.AppendLine($"Attempt: `{AttemptNumber}`");
        builder.AppendLine($"Can retry: `{CanRetry}`");
        builder.AppendLine($"Retryable failures: `{RetryableFailures.Count}`");
        builder.AppendLine($"Non-retryable failures: `{NonRetryableFailures.Count}`");
        builder.AppendLine($"Next delay: `{NextDelay}`");
        return builder.ToString();
    }

    /// <summary>Formats a compact retry evaluation summary.</summary>
    /// <returns>The retry evaluation summary.</returns>
    public string Describe()
    {
        return $"run={FailureReport.RunId} attempt={AttemptNumber} retryable={RetryableFailures.Count} nonRetryable={NonRetryableFailures.Count} canRetry={CanRetry}";
    }
}

/// <summary>
/// Describes maintained external peer lab runner retry policy.
/// </summary>
public sealed class SigtranMaintainedPeerLabRunnerRetryPolicy
{
    private readonly SigtranMaintainedPeerLabRunnerRetryRule[] _rules;

    /// <summary>Creates a maintained peer lab runner retry policy.</summary>
    /// <param name="rules">The retry rules.</param>
    public SigtranMaintainedPeerLabRunnerRetryPolicy(IReadOnlyList<SigtranMaintainedPeerLabRunnerRetryRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        _rules = rules.Count == 0 ? throw new ArgumentException("At least one retry rule is required.", nameof(rules)) : rules.ToArray();
    }

    /// <summary>The retry rules.</summary>
    public IReadOnlyList<SigtranMaintainedPeerLabRunnerRetryRule> Rules => _rules.ToArray();

    /// <summary>Returns the retry rule for a failure kind.</summary>
    /// <param name="failureKind">The failure kind.</param>
    /// <returns>The retry rule.</returns>
    public SigtranMaintainedPeerLabRunnerRetryRule GetRule(SigtranMaintainedPeerLabRunnerFailureKind failureKind)
    {
        return _rules.First(rule => rule.FailureKind == failureKind);
    }

    /// <summary>Evaluates whether a failure report can be retried.</summary>
    /// <param name="failureReport">The failure report.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <returns>The retry evaluation.</returns>
    public SigtranMaintainedPeerLabRunnerRetryEvaluation Evaluate(
        SigtranMaintainedPeerLabRunnerFailureReport failureReport,
        int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(failureReport);

        List<SigtranMaintainedPeerLabRunnerFailure> retryable = [];
        List<SigtranMaintainedPeerLabRunnerFailure> nonRetryable = [];
        TimeSpan nextDelay = TimeSpan.Zero;

        foreach (SigtranMaintainedPeerLabRunnerFailure failure in failureReport.BlockingFailures)
        {
            SigtranMaintainedPeerLabRunnerRetryRule rule = GetRule(failure.Kind);
            bool canRetryFailure = rule.Retryable && attemptNumber < rule.MaxAttempts;
            if (canRetryFailure)
            {
                retryable.Add(failure);
                if (rule.Delay > nextDelay)
                {
                    nextDelay = rule.Delay;
                }
            }
            else
            {
                nonRetryable.Add(failure);
            }
        }

        return new(failureReport, attemptNumber, retryable, nonRetryable, nextDelay);
    }

    /// <summary>Creates the default maintained peer lab runner retry policy.</summary>
    /// <returns>The default retry policy.</returns>
    public static SigtranMaintainedPeerLabRunnerRetryPolicy CreateDefault()
    {
        return new(
            [
                new(SigtranMaintainedPeerLabRunnerFailureKind.Preflight, false, 1, TimeSpan.Zero, "Preflight failures require environment or input correction."),
                new(SigtranMaintainedPeerLabRunnerFailureKind.CommandExecution, true, 3, TimeSpan.FromSeconds(5), "Command failures may be transient when peer startup or capture timing is involved."),
                new(SigtranMaintainedPeerLabRunnerFailureKind.ArtifactRetention, false, 1, TimeSpan.Zero, "Missing retained artifacts require runner or storage correction."),
                new(SigtranMaintainedPeerLabRunnerFailureKind.DigestVerification, false, 1, TimeSpan.Zero, "Digest failures require evidence package regeneration or review."),
                new(SigtranMaintainedPeerLabRunnerFailureKind.Provenance, false, 1, TimeSpan.Zero, "Provenance failures require source or runner identity correction."),
                new(SigtranMaintainedPeerLabRunnerFailureKind.Comparison, true, 2, TimeSpan.FromSeconds(10), "Comparison failures may be retried once when peer timing is suspected."),
                new(SigtranMaintainedPeerLabRunnerFailureKind.RunReport, false, 1, TimeSpan.Zero, "Run report failures require report regeneration or operator review.")
            ]);
    }
}
