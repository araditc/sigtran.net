using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>Shares one real monotonic origin between admission, fault control and checkpoints.</summary>
internal sealed class EnduranceClock(TimeSpan duration)
{
    private readonly long _origin = Stopwatch.GetTimestamp();
    internal DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;
    internal long DeadlineTicks { get; } = checked((long)(duration.TotalSeconds * Stopwatch.Frequency));
    internal long ElapsedTicks => Stopwatch.GetTimestamp() - _origin;
    internal double ElapsedSeconds => ElapsedTicks / (double)Stopwatch.Frequency;
}

/// <summary>Internal short durations exist for tests; the external CLI separately enforces the 72-hour qualification fence.</summary>
internal readonly record struct EnduranceRunSettings(TimeSpan Duration, int Concurrency,
    TimeSpan CheckpointInterval, TimeSpan DrainTimeout);

/// <summary>Samples only the owned process; peak RSS is the OS high-water mark, while managed memory remains sampled.</summary>
internal sealed class EnduranceResourceProbe : IDisposable
{
    private readonly Process _process = Process.GetCurrentProcess();
    internal EnduranceResources Read()
    {
        _process.Refresh();
        GCMemoryInfo info = GC.GetGCMemoryInfo();
        return new(_process.WorkingSet64, _process.PeakWorkingSet64, GC.GetTotalMemory(false), info.HeapSizeBytes,
            info.FragmentedBytes, GC.GetTotalAllocatedBytes(false), _process.TotalProcessorTime.TotalSeconds,
            GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2),
            ThreadPool.ThreadCount, ThreadPool.PendingWorkItemCount, ThreadPool.CompletedWorkItemCount);
    }
    public void Dispose() => _process.Dispose();
}

/// <summary>Resource counters are observations, not a claim that every transient managed-heap peak was sampled.</summary>
internal sealed record EnduranceResources(long WorkingSetBytes, long PeakWorkingSetBytes, long ManagedBytes,
    long HeapSizeBytes, long FragmentedBytes, long AllocatedBytes, double ProcessCpuSeconds,
    int Gen0, int Gen1, int Gen2, int ThreadPoolThreads, long ThreadPoolPending, long ThreadPoolCompleted);

/// <summary>One consistent ledger snapshot with an actual monotonic sample timestamp.</summary>
internal sealed record EnduranceSample(DateTimeOffset StartedAtUtc, double ElapsedSeconds, EnduranceCounts Counts, EnduranceResources Resources);

/// <summary>Separates completions before the deadline from late successes without losing either.</summary>
internal sealed record EnduranceRun(DateTimeOffset StartedAtUtc, DateTimeOffset FinishedAtUtc,
    double MeasuredSeconds, double ElapsedSeconds, long[] MeasuredByOperation, long LateSuccesses,
    EnduranceCounts Counts, CapacityLatency Latency, bool Aborted, string[] Errors);

/// <summary>Uses the same terminal and measurement path for ordinary invokes and fresh recovery probes.</summary>
internal sealed class EnduranceExecution(EnduranceClock clock, EnduranceLedger ledger, int concurrency,
    Func<int, CancellationToken, ValueTask<CapacityOutcome>> invoke, long[] measured, CapacityHistogram histogram)
{
    internal EnduranceClock Clock => clock;
    internal EnduranceLedger Ledger => ledger;

    internal async ValueTask<CapacityOutcome> InvokeAsync(EnduranceAdmission admission, CancellationToken ct)
    {
        CapacityOutcome outcome = CapacityOutcome.TransportFailure;
        try { outcome = await invoke(admission.Operation, ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { outcome = CapacityOutcome.Cancelled; }
        catch (Exception) { outcome = CapacityOutcome.TransportFailure; }
        finally
        {
            long completed = clock.ElapsedTicks;
            ledger.Complete(admission, outcome, completed);
            if (outcome == CapacityOutcome.Success && completed < clock.DeadlineTicks)
            {
                Interlocked.Increment(ref measured[admission.Operation]);
                histogram.Record(completed - admission.StartedTicks);
            }
        }
        return outcome;
    }

    internal async Task ProbeAsync(CancellationToken ct)
    {
        if (!ledger.TryAdmit(concurrency, clock.ElapsedTicks, out var admission, recoveryProbe: true)
            || await InvokeAsync(admission, ct).ConfigureAwait(false) != CapacityOutcome.Success)
            throw new InvalidOperationException("A fresh recovery probe failed.");
    }
}

/// <summary>Runs one fixed worker set for the entire measurement; it never retains a Task or record per transaction.</summary>
internal static class EnduranceRunner
{
    internal static async Task<EnduranceRun> RunAsync(EnduranceRunSettings settings, EnduranceLedger ledger,
        Func<int, CancellationToken, ValueTask<CapacityOutcome>> invoke, Func<bool> connected,
        Action<EnduranceSample> checkpoint, CancellationToken ct = default,
        Func<EnduranceExecution, CancellationToken, Task>? control = null)
    {
        if (settings.Duration <= TimeSpan.Zero || settings.Duration > TimeSpan.FromSeconds(EnduranceOptions.RequiredSeconds)
            || settings.Concurrency is < 1 or > 1024 || settings.CheckpointInterval <= TimeSpan.Zero
            || settings.CheckpointInterval > TimeSpan.FromMinutes(1) || settings.DrainTimeout <= TimeSpan.Zero
            || settings.DrainTimeout > TimeSpan.FromSeconds(30)) throw new ArgumentOutOfRangeException(nameof(settings));
        using CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using CancellationTokenSource observing = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ConcurrentQueue<string> errors = new();
        long[] measured = new long[5];
        CapacityHistogram histogram = new();
        int readyWorkers = 0;
        EnduranceClock? clock = null;
        EnduranceExecution? execution = null;
        Task observer = Task.CompletedTask, supervisor = Task.CompletedTask;
        Task[] workers = Enumerable.Range(0, settings.Concurrency).Select(worker => Task.Run(() => WorkerAsync(worker))).ToArray();
        await ready.Task.ConfigureAwait(false);
        clock = new(settings.Duration);
        execution = new(clock, ledger, settings.Concurrency, invoke, measured, histogram);
        bool aborted;
        try
        {
            using EnduranceResourceProbe initial = new();
            checkpoint(new(clock.StartedAtUtc, clock.ElapsedSeconds, ledger.Snapshot(), initial.Read()));
            lifetime.CancelAfter(settings.Duration + settings.DrainTimeout);
            start.SetResult();
            observer = ObserveAsync();
            supervisor = SuperviseAsync();
            await Task.WhenAll(workers).ConfigureAwait(false);
            aborted = lifetime.IsCancellationRequested || ledger.Fatal || clock.ElapsedTicks < clock.DeadlineTicks;
        }
        catch
        {
            await lifetime.CancelAsync();
            start.TrySetCanceled(lifetime.Token);
            await Task.WhenAll(workers).ConfigureAwait(false);
            throw;
        }
        finally
        {
            await observing.CancelAsync();
            await Task.WhenAll(observer, supervisor).ConfigureAwait(false);
        }
        EnduranceCounts counts = ledger.Snapshot();
        return new(clock.StartedAtUtc, DateTimeOffset.UtcNow, Math.Min(settings.Duration.TotalSeconds, clock.ElapsedSeconds),
            clock.ElapsedSeconds, measured, counts.Successful - measured.Sum(), counts, histogram.Snapshot(),
            aborted || !errors.IsEmpty || !counts.Reconciled || counts.Pending != 0 || counts.ActiveFaultId is not null,
            errors.ToArray());

        async Task WorkerAsync(int worker)
        {
            if (Interlocked.Increment(ref readyWorkers) == settings.Concurrency) ready.SetResult();
            try
            {
                await start.Task.ConfigureAwait(false);
                while (!lifetime.IsCancellationRequested && clock!.ElapsedTicks < clock.DeadlineTicks && !ledger.Fatal)
                {
                    if (!connected() || !ledger.TryAdmit(worker, clock.ElapsedTicks, out var admission))
                    { await Task.Delay(10, lifetime.Token).ConfigureAwait(false); continue; }
                    await execution!.InvokeAsync(admission, lifetime.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }

        async Task ObserveAsync()
        {
            try
            {
                using EnduranceResourceProbe probe = new();
                using PeriodicTimer timer = new(settings.CheckpointInterval);
                while (await timer.WaitForNextTickAsync(observing.Token).ConfigureAwait(false))
                    checkpoint(new(clock!.StartedAtUtc, clock.ElapsedSeconds, ledger.Snapshot(), probe.Read()));
            }
            catch (OperationCanceledException) when (observing.IsCancellationRequested) { }
            catch (Exception error) { errors.Enqueue("checkpoint:" + error.GetType().Name); await lifetime.CancelAsync(); }
        }

        async Task SuperviseAsync()
        {
            try { if (control is not null) await control(execution!, observing.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (observing.IsCancellationRequested) { }
            catch (Exception error) { errors.Enqueue("control:" + error.GetType().Name); await lifetime.CancelAsync(); }
        }
    }
}
