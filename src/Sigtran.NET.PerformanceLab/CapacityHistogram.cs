using System.Diagnostics;

/// <summary>Records measured successful latency in fixed 0.1 ms buckets with an explicit overflow bucket.</summary>
internal sealed class CapacityHistogram
{
    private readonly long[] _buckets = new long[100_001];
    private long _maximum;

    internal void Record(long ticks)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ticks);
        int bucket = (int)Math.Min(100_000, ticks * 10_000d / Stopwatch.Frequency);
        Interlocked.Increment(ref _buckets[bucket]);
        long observed = Volatile.Read(ref _maximum);
        while (ticks > observed)
        {
            long previous = Interlocked.CompareExchange(ref _maximum, ticks, observed);
            if (previous == observed) break;
            observed = previous;
        }
    }

    /// <summary>Returns upper-bound quantiles after workers stop; overflow quantiles use the observed maximum.</summary>
    internal CapacityLatency Snapshot()
    {
        long count = _buckets.Sum();
        double maximum = Volatile.Read(ref _maximum) * 1000d / Stopwatch.Frequency;
        double Quantile(double quantile)
        {
            if (count == 0) return 0;
            long rank = (long)Math.Ceiling(count * quantile);
            long cumulative = 0;
            for (int i = 0; i < _buckets.Length; i++)
            {
                cumulative += _buckets[i];
                if (cumulative >= rank) return i == 100_000 ? maximum : Math.Min(maximum, (i + 1) / 10d);
            }
            throw new InvalidOperationException("Latency histogram changed during snapshot.");
        }
        return new(count, Quantile(.5), Quantile(.95), Quantile(.99), maximum, .1);
    }
}

/// <summary>Quantiles have a declared resolution and never imply exact retained transaction samples.</summary>
internal sealed record CapacityLatency(long Count, double P50Milliseconds, double P95Milliseconds,
    double P99Milliseconds, double MaximumMilliseconds, double ResolutionMilliseconds);

/// <summary>Process-wide allocations and resources cover measurement plus bounded drain, not just successful work.</summary>
internal sealed record CapacityResources(long AllocatedBytes, int Gen0Collections, int Gen1Collections,
    int Gen2Collections, double AverageCpuPercent, double PeakCpuPercent, long PeakWorkingSetMegabytes,
    int LogicalProcessors, bool ServerGc);
