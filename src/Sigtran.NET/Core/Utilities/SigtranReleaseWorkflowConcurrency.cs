namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes release workflow concurrency policy.
/// </summary>
public sealed class SigtranReleaseWorkflowConcurrencyPolicy
{
    /// <summary>Creates a release workflow concurrency policy.</summary>
    /// <param name="groupExpression">The concurrency group expression.</param>
    /// <param name="cancelInProgress">Whether in-progress workflows are cancelled.</param>
    public SigtranReleaseWorkflowConcurrencyPolicy(string groupExpression, bool cancelInProgress)
    {
        GroupExpression = string.IsNullOrWhiteSpace(groupExpression) ? throw new ArgumentException("Group expression is required.", nameof(groupExpression)) : groupExpression;
        CancelInProgress = cancelInProgress;
    }

    /// <summary>The concurrency group expression.</summary>
    public string GroupExpression { get; }

    /// <summary>Whether in-progress workflows are cancelled.</summary>
    public bool CancelInProgress { get; }

    /// <summary>Whether the policy prevents overlapping release runs for the same ref.</summary>
    public bool PreventsOverlappingReleaseRuns => GroupExpression.Contains("github.ref", StringComparison.Ordinal)
        && !CancelInProgress;
}

/// <summary>
/// Provides release workflow concurrency policies.
/// </summary>
public static class SigtranReleaseWorkflowConcurrency
{
    /// <summary>Creates the default release workflow concurrency policy.</summary>
    /// <returns>The default release workflow concurrency policy.</returns>
    public static SigtranReleaseWorkflowConcurrencyPolicy CreateDefault()
    {
        return new("release-${{ github.ref }}", cancelInProgress: false);
    }
}
