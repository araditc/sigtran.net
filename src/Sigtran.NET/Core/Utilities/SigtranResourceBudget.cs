namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes runtime resource budgets for performance validation.
/// </summary>
public sealed class SigtranResourceBudget
{
    /// <summary>Creates a resource budget.</summary>
    /// <param name="maxAllocatedBytesPerMessage">The maximum allocated bytes per message.</param>
    /// <param name="maxWorkingSetMegabytes">The maximum working-set size in megabytes.</param>
    /// <param name="maxCpuPercent">The maximum CPU percentage target.</param>
    /// <param name="requiresAllocationTracking">Whether allocation tracking is required.</param>
    public SigtranResourceBudget(
        long maxAllocatedBytesPerMessage,
        int maxWorkingSetMegabytes,
        int maxCpuPercent,
        bool requiresAllocationTracking)
    {
        MaxAllocatedBytesPerMessage = maxAllocatedBytesPerMessage < 0 ? throw new ArgumentOutOfRangeException(nameof(maxAllocatedBytesPerMessage)) : maxAllocatedBytesPerMessage;
        MaxWorkingSetMegabytes = maxWorkingSetMegabytes <= 0 ? throw new ArgumentOutOfRangeException(nameof(maxWorkingSetMegabytes)) : maxWorkingSetMegabytes;
        MaxCpuPercent = maxCpuPercent <= 0 || maxCpuPercent > 100 ? throw new ArgumentOutOfRangeException(nameof(maxCpuPercent)) : maxCpuPercent;
        RequiresAllocationTracking = requiresAllocationTracking;
    }

    /// <summary>The maximum allocated bytes per message.</summary>
    public long MaxAllocatedBytesPerMessage { get; }

    /// <summary>The maximum working-set size in megabytes.</summary>
    public int MaxWorkingSetMegabytes { get; }

    /// <summary>The maximum CPU percentage target.</summary>
    public int MaxCpuPercent { get; }

    /// <summary>Whether allocation tracking is required.</summary>
    public bool RequiresAllocationTracking { get; }

    /// <summary>Whether the resource budget is suitable for production benchmarking.</summary>
    public bool IsProductionBenchmarkBudget => RequiresAllocationTracking
        && MaxWorkingSetMegabytes >= 512
        && MaxCpuPercent <= 90;
}

/// <summary>
/// Provides resource budget helpers.
/// </summary>
public static class SigtranResourceBudgets
{
    /// <summary>Creates the default production resource budget.</summary>
    /// <returns>The default production resource budget.</returns>
    public static SigtranResourceBudget CreateProductionDefault()
    {
        return new(
            maxAllocatedBytesPerMessage: 0,
            maxWorkingSetMegabytes: 1024,
            maxCpuPercent: 85,
            requiresAllocationTracking: true);
    }
}
