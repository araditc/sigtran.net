namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a release rollback step.
/// </summary>
public sealed class SigtranRollbackStep
{
    /// <summary>Creates a rollback step.</summary>
    /// <param name="order">The one-based step order.</param>
    /// <param name="action">The rollback action.</param>
    public SigtranRollbackStep(int order, string action)
    {
        Order = order <= 0 ? throw new ArgumentOutOfRangeException(nameof(order), "Order must be positive.") : order;
        Action = string.IsNullOrWhiteSpace(action) ? throw new ArgumentException("Action is required.", nameof(action)) : action;
    }

    /// <summary>The one-based step order.</summary>
    public int Order { get; }

    /// <summary>The rollback action.</summary>
    public string Action { get; }
}

/// <summary>
/// Describes the release rollback plan.
/// </summary>
public sealed class SigtranRollbackPlan
{
    /// <summary>Creates a rollback plan.</summary>
    /// <param name="id">The stable plan id.</param>
    /// <param name="steps">The ordered rollback steps.</param>
    public SigtranRollbackPlan(string id, IReadOnlyList<SigtranRollbackStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Plan id is required.", nameof(id)) : id;
        Steps = steps.Count == 0 ? throw new ArgumentException("At least one rollback step is required.", nameof(steps)) : steps.OrderBy(static step => step.Order).ToArray();
    }

    /// <summary>The stable plan id.</summary>
    public string Id { get; }

    /// <summary>The ordered rollback steps.</summary>
    public IReadOnlyList<SigtranRollbackStep> Steps { get; }
}

/// <summary>
/// Provides release rollback planning helpers.
/// </summary>
public static class SigtranRollbackPlans
{
    /// <summary>Creates the default package rollback plan.</summary>
    /// <returns>The default package rollback plan.</returns>
    public static SigtranRollbackPlan CreateDefaultPackageRollback()
    {
        return new(
            "package-rollback",
            [
                new SigtranRollbackStep(1, "Stop publishing the affected package version."),
                new SigtranRollbackStep(2, "Communicate the affected version and recommended previous version."),
                new SigtranRollbackStep(3, "Preserve release artifact manifest and provenance."),
                new SigtranRollbackStep(4, "Publish a corrected prerelease or patch when gates pass.")
            ]);
    }
}
