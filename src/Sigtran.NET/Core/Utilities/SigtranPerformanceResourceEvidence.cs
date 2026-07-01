namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes measured CPU, memory, allocation, and GC evidence for a benchmark run.
/// </summary>
public sealed class SigtranPerformanceResourceEvidence
{
    /// <summary>Creates measured resource evidence.</summary>
    /// <param name="averageCpuPercent">The measured average CPU percentage.</param>
    /// <param name="peakCpuPercent">The measured peak CPU percentage.</param>
    /// <param name="peakWorkingSetMegabytes">The measured peak working-set size in megabytes.</param>
    /// <param name="allocatedBytesPerMessage">The measured allocated bytes per message.</param>
    /// <param name="gen2Collections">The measured generation 2 garbage collection count.</param>
    public SigtranPerformanceResourceEvidence(
        double averageCpuPercent,
        double peakCpuPercent,
        int peakWorkingSetMegabytes,
        long allocatedBytesPerMessage,
        int gen2Collections)
    {
        AverageCpuPercent = averageCpuPercent < 0 || averageCpuPercent > 100
            ? throw new ArgumentOutOfRangeException(nameof(averageCpuPercent), "Average CPU percent must be between 0 and 100.")
            : averageCpuPercent;
        PeakCpuPercent = peakCpuPercent < averageCpuPercent || peakCpuPercent > 100
            ? throw new ArgumentOutOfRangeException(nameof(peakCpuPercent), "Peak CPU percent must be greater than or equal to average CPU percent and no more than 100.")
            : peakCpuPercent;
        PeakWorkingSetMegabytes = peakWorkingSetMegabytes <= 0
            ? throw new ArgumentOutOfRangeException(nameof(peakWorkingSetMegabytes), "Peak working set must be positive.")
            : peakWorkingSetMegabytes;
        AllocatedBytesPerMessage = allocatedBytesPerMessage < 0
            ? throw new ArgumentOutOfRangeException(nameof(allocatedBytesPerMessage), "Allocated bytes per message must not be negative.")
            : allocatedBytesPerMessage;
        Gen2Collections = gen2Collections < 0
            ? throw new ArgumentOutOfRangeException(nameof(gen2Collections), "Generation 2 collection count must not be negative.")
            : gen2Collections;
    }

    /// <summary>The measured average CPU percentage.</summary>
    public double AverageCpuPercent { get; }

    /// <summary>The measured peak CPU percentage.</summary>
    public double PeakCpuPercent { get; }

    /// <summary>The measured peak working-set size in megabytes.</summary>
    public int PeakWorkingSetMegabytes { get; }

    /// <summary>The measured allocated bytes per message.</summary>
    public long AllocatedBytesPerMessage { get; }

    /// <summary>The measured generation 2 garbage collection count.</summary>
    public int Gen2Collections { get; }

    /// <summary>Formats a compact resource evidence summary.</summary>
    /// <returns>The resource evidence summary.</returns>
    public string Describe()
    {
        return $"avgCpu={AverageCpuPercent:F2} peakCpu={PeakCpuPercent:F2} workingSetMb={PeakWorkingSetMegabytes} allocBytesPerMessage={AllocatedBytesPerMessage} gen2={Gen2Collections}";
    }
}

/// <summary>
/// Describes the result of checking resource evidence against a resource budget.
/// </summary>
public sealed class SigtranPerformanceResourceBudgetReport
{
    /// <summary>Creates a resource budget report.</summary>
    /// <param name="evidence">The measured resource evidence.</param>
    /// <param name="budget">The expected resource budget.</param>
    public SigtranPerformanceResourceBudgetReport(
        SigtranPerformanceResourceEvidence evidence,
        SigtranResourceBudget budget)
    {
        Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
        Budget = budget ?? throw new ArgumentNullException(nameof(budget));
    }

    /// <summary>The measured resource evidence.</summary>
    public SigtranPerformanceResourceEvidence Evidence { get; }

    /// <summary>The expected resource budget.</summary>
    public SigtranResourceBudget Budget { get; }

    /// <summary>Whether peak CPU is within the configured budget.</summary>
    public bool CpuWithinBudget => Evidence.PeakCpuPercent <= Budget.MaxCpuPercent;

    /// <summary>Whether peak working set is within the configured budget.</summary>
    public bool WorkingSetWithinBudget => Evidence.PeakWorkingSetMegabytes <= Budget.MaxWorkingSetMegabytes;

    /// <summary>Whether measured allocation is within the configured budget.</summary>
    public bool AllocationWithinBudget => Evidence.AllocatedBytesPerMessage <= Budget.MaxAllocatedBytesPerMessage;

    /// <summary>Whether the resource evidence passes all budget checks.</summary>
    public bool Passed => CpuWithinBudget && WorkingSetWithinBudget && AllocationWithinBudget;

    /// <summary>Formats a compact resource budget report summary.</summary>
    /// <returns>The resource budget report summary.</returns>
    public string Describe()
    {
        return $"cpuOk={CpuWithinBudget} workingSetOk={WorkingSetWithinBudget} allocationOk={AllocationWithinBudget} passed={Passed}";
    }
}

/// <summary>
/// Evaluates measured resource evidence against SDK resource budgets.
/// </summary>
public static class SigtranPerformanceResourceEvidenceEvaluator
{
    /// <summary>Evaluates measured resource evidence against the production resource budget.</summary>
    /// <param name="evidence">The measured resource evidence.</param>
    /// <returns>The resource budget report.</returns>
    public static SigtranPerformanceResourceBudgetReport EvaluateProduction(SigtranPerformanceResourceEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new(evidence, SigtranResourceBudgets.CreateProductionDefault());
    }
}
