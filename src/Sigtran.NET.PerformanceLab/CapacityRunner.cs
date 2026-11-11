using System.Diagnostics;

/// <summary>Runs bounded closed-loop workers without creating a Task for every transaction.</summary>
internal static class CapacityRunner
{
    /// <summary>Stops admission at the deadline, then drains without erasing pending operations.</summary>
    internal static async Task<CapacityRun> RunAsync(TimeSpan duration, int concurrency,
        Func<int, CancellationToken, ValueTask<CapacityOutcome>> invoke, CancellationToken ct,
        TimeSpan? drainTimeout = null)
    {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromSeconds(1800)
            || concurrency is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(duration));
        ArgumentNullException.ThrowIfNull(invoke);
        using CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        TimeSpan drain = drainTimeout ?? TimeSpan.FromSeconds(10);
        if (drain <= TimeSpan.Zero || drain > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(drainTimeout));
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readyWorkers = 0;
        long origin = 0;
        long sequence = -1;
        long deadline = checked((long)(duration.TotalSeconds * Stopwatch.Frequency));
        CapacityLedger ledger = new((int)Math.Ceiling(duration.TotalSeconds), Stopwatch.Frequency, deadline);
        CapacityHistogram histogram = new();
        using ProcessResourceSampler sampler = new();
        Task[] workers = Enumerable.Range(0, concurrency).Select(_ => Task.Run(WorkerAsync)).ToArray();
        await ready.Task.ConfigureAwait(false);
        long allocated = GC.GetTotalAllocatedBytes(false);
        int[] collections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        sampler.Start();
        origin = Stopwatch.GetTimestamp();
        lifetime.CancelAfter(duration + drain);
        start.SetResult();
        await Task.WhenAll(workers).ConfigureAwait(false);
        double elapsed = (Stopwatch.GetTimestamp() - origin) / (double)Stopwatch.Frequency;
        ProcessResourceSnapshot resources = sampler.Stop();
        return new(ledger.Snapshot(), Math.Min(duration.TotalSeconds, elapsed), elapsed,
            ct.IsCancellationRequested || lifetime.IsCancellationRequested, histogram.Snapshot(),
            new(Math.Max(0, GC.GetTotalAllocatedBytes(false) - allocated),
                GC.CollectionCount(0) - collections[0], GC.CollectionCount(1) - collections[1],
                GC.CollectionCount(2) - collections[2], resources.AverageCpuPercent,
                resources.PeakCpuPercent, resources.PeakWorkingSetMegabytes, Environment.ProcessorCount,
                System.Runtime.GCSettings.IsServerGC));

        async Task WorkerAsync()
        {
            if (Interlocked.Increment(ref readyWorkers) == concurrency) ready.SetResult();
            await start.Task.ConfigureAwait(false);
            while (!lifetime.IsCancellationRequested)
            {
                long admitted = Stopwatch.GetTimestamp() - origin;
                if (admitted >= deadline) break;
                int operation = (int)(Interlocked.Increment(ref sequence) % 5);
                ledger.Admit(operation);
                CapacityOutcome outcome = CapacityOutcome.TransportFailure;
                try { outcome = await invoke(operation, lifetime.Token).ConfigureAwait(false); }
                catch (OperationCanceledException) { outcome = CapacityOutcome.Cancelled; }
                catch (Exception) { outcome = CapacityOutcome.TransportFailure; }
                finally
                {
                    long completed = Stopwatch.GetTimestamp() - origin;
                    ledger.Complete(operation, outcome, admitted, completed);
                    if (outcome == CapacityOutcome.Success && completed < deadline)
                        histogram.Record(completed - admitted);
                }
            }
        }
    }
}

/// <summary>A measured stage excludes warmup and drain from its throughput denominator.</summary>
internal sealed record CapacityRun(CapacityCounts Counts, double MeasuredSeconds, double ElapsedSeconds,
    bool Aborted, CapacityLatency Latency, CapacityResources Resources);
