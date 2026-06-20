namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes benchmark execution evidence.
/// </summary>
public sealed class SigtranBenchmarkExecutionEvidence
{
    /// <summary>Creates benchmark execution evidence.</summary>
    /// <param name="reportPath">The benchmark report path.</param>
    /// <param name="reportSha256">The benchmark report SHA-256 digest.</param>
    /// <param name="durationMilliseconds">The workload duration in milliseconds.</param>
    /// <param name="passedChecks">The number of passed workload checks.</param>
    /// <param name="smokeOnly">Whether the benchmark is a smoke benchmark only.</param>
    public SigtranBenchmarkExecutionEvidence(
        string reportPath,
        string reportSha256,
        long durationMilliseconds,
        int passedChecks,
        bool smokeOnly)
    {
        ReportPath = string.IsNullOrWhiteSpace(reportPath) ? throw new ArgumentException("Benchmark report path is required.", nameof(reportPath)) : reportPath;
        ReportSha256 = string.IsNullOrWhiteSpace(reportSha256) ? throw new ArgumentException("Benchmark report digest is required.", nameof(reportSha256)) : reportSha256;
        DurationMilliseconds = durationMilliseconds;
        PassedChecks = passedChecks;
        SmokeOnly = smokeOnly;
    }

    /// <summary>The benchmark report path.</summary>
    public string ReportPath { get; }

    /// <summary>The benchmark report SHA-256 digest.</summary>
    public string ReportSha256 { get; }

    /// <summary>The workload duration in milliseconds.</summary>
    public long DurationMilliseconds { get; }

    /// <summary>The number of passed workload checks.</summary>
    public int PassedChecks { get; }

    /// <summary>Whether the benchmark is a smoke benchmark only.</summary>
    public bool SmokeOnly { get; }

    /// <summary>Whether this evidence can support commercial performance promotion.</summary>
    public bool SupportsCommercialPerformancePromotion => !SmokeOnly
        && ReportSha256.Length == 64
        && DurationMilliseconds > 0
        && PassedChecks > 0;
}

/// <summary>
/// Provides benchmark execution evidence helpers.
/// </summary>
public static class SigtranBenchmarkExecution
{
    /// <summary>Creates smoke benchmark evidence from retained report data.</summary>
    /// <param name="reportSha256">The retained benchmark report digest.</param>
    /// <param name="durationMilliseconds">The workload duration in milliseconds.</param>
    /// <param name="passedChecks">The number of passed workload checks.</param>
    /// <returns>The smoke benchmark execution evidence.</returns>
    public static SigtranBenchmarkExecutionEvidence CreateSmokeBenchmark(
        string reportSha256,
        long durationMilliseconds,
        int passedChecks)
    {
        return new(
            "artifacts/benchmarks/sigtran.net-smoke-benchmark.json",
            reportSha256,
            durationMilliseconds,
            passedChecks,
            smokeOnly: true);
    }
}
