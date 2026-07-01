namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a load-test stage.
/// </summary>
public sealed class SigtranLoadTestStage
{
    /// <summary>Creates a load-test stage.</summary>
    /// <param name="name">The stage name.</param>
    /// <param name="duration">The stage duration.</param>
    /// <param name="targetMessagesPerSecond">The target messages per second.</param>
    public SigtranLoadTestStage(string name, TimeSpan duration, int targetMessagesPerSecond)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Load-test stage name is required.", nameof(name)) : name;
        Duration = duration <= TimeSpan.Zero ? throw new ArgumentOutOfRangeException(nameof(duration)) : duration;
        TargetMessagesPerSecond = targetMessagesPerSecond <= 0 ? throw new ArgumentOutOfRangeException(nameof(targetMessagesPerSecond)) : targetMessagesPerSecond;
    }

    /// <summary>The stage name.</summary>
    public string Name { get; }

    /// <summary>The stage duration.</summary>
    public TimeSpan Duration { get; }

    /// <summary>The target messages per second.</summary>
    public int TargetMessagesPerSecond { get; }
}

/// <summary>
/// Describes a load-test plan.
/// </summary>
public sealed class SigtranLoadTestPlan
{
    /// <summary>Creates a load-test plan.</summary>
    /// <param name="name">The plan name.</param>
    /// <param name="stages">The load-test stages.</param>
    /// <param name="requiresNativeSctp">Whether native SCTP is required.</param>
    /// <param name="requiresExternalPeer">Whether an external peer stack is required.</param>
    public SigtranLoadTestPlan(
        string name,
        IReadOnlyList<SigtranLoadTestStage> stages,
        bool requiresNativeSctp,
        bool requiresExternalPeer)
    {
        ArgumentNullException.ThrowIfNull(stages);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Load-test plan name is required.", nameof(name)) : name;
        Stages = stages.Count == 0 ? throw new ArgumentException("At least one load-test stage is required.", nameof(stages)) : stages.ToArray();
        RequiresNativeSctp = requiresNativeSctp;
        RequiresExternalPeer = requiresExternalPeer;
    }

    /// <summary>The plan name.</summary>
    public string Name { get; }

    /// <summary>The load-test stages.</summary>
    public IReadOnlyList<SigtranLoadTestStage> Stages { get; }

    /// <summary>Whether native SCTP is required.</summary>
    public bool RequiresNativeSctp { get; }

    /// <summary>Whether an external peer stack is required.</summary>
    public bool RequiresExternalPeer { get; }

    /// <summary>Returns the highest target messages per second in the plan.</summary>
    /// <returns>The highest target messages per second.</returns>
    public int GetPeakMessagesPerSecond()
    {
        return Stages.Max(stage => stage.TargetMessagesPerSecond);
    }
}

/// <summary>
/// Provides load-test plan helpers.
/// </summary>
public static class SigtranLoadTestPlans
{
    /// <summary>Creates the default production load-test plan.</summary>
    /// <returns>The default production load-test plan.</returns>
    public static SigtranLoadTestPlan CreateProductionDefault()
    {
        return new(
            "production-load-test",
            [
                new("warmup", TimeSpan.FromMinutes(5), 5000),
                new("sustained", TimeSpan.FromMinutes(30), 25000),
                new("peak", TimeSpan.FromMinutes(10), 50000)
            ],
            requiresNativeSctp: true,
            requiresExternalPeer: true);
    }
}
