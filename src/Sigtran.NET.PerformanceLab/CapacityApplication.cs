using System.Diagnostics;
using System.Text.Json;

/// <summary>Executes the real MAP capacity workload without promoting topology or release qualification.</summary>
internal static class CapacityApplication
{
    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            CapacityOptions options = CapacityOptions.Parse(args);
            Directory.CreateDirectory(options.Output);
            string path = Path.Combine(options.Output, "sdk.json");
            if (File.Exists(path)) throw new IOException("Capacity output already exists.");
            using CancellationTokenSource stop = new();
            ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; stop.Cancel(); };
            Console.CancelKeyPress += handler;
            try
            {
                await using CapacitySession session = new(options);
                using CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                startup.CancelAfter(TimeSpan.FromSeconds(15));
                await session.StartAsync(startup.Token).ConfigureAwait(false);
                Console.WriteLine("capacity stage=warmup workload=" + CapacityOptions.Workload);
                var warmup = await CapacityRunner.RunAsync(TimeSpan.FromSeconds(options.WarmupSeconds),
                    options.Concurrency, session.InvokeAsync, stop.Token).ConfigureAwait(false);
                DateTimeOffset started = DateTimeOffset.UtcNow;
                Console.WriteLine("capacity stage=measured started=" + started.ToString("O"));
                using CancellationTokenSource observe = new();
                Task observer = ObserveAsync(session, observe.Token);
                CapacityRun measured;
                try
                {
                    measured = await CapacityRunner.RunAsync(TimeSpan.FromSeconds(options.DurationSeconds),
                        options.Concurrency, session.InvokeAsync, stop.Token).ConfigureAwait(false);
                }
                finally { await observe.CancelAsync(); await observer.ConfigureAwait(false); }
                // A result continuation can run just before TCAP removes its ended dialogue.
                for (int i = 0; i < 100 && session.Snapshot().Active > 0; i++)
                    await Task.Delay(10).ConfigureAwait(false);
                var metrics = session.Snapshot();
                long total = warmup.Counts.Started + measured.Counts.Started;
                bool executionPassed = !warmup.Aborted && !measured.Aborted && warmup.Counts.Reconciled
                    && measured.Counts.Reconciled && warmup.Counts.Failed == 0 && measured.Counts.Failed == 0
                    && metrics.Opened == total && metrics.Closed == total && metrics.Sent == total
                    && metrics.Received == total && metrics.Pending == 0 && metrics.Active == 0
                    && metrics.Faults == 0 && metrics.Reconnects == 0 && metrics.Malformed == 0
                    && metrics.DroppedEvents == 0 && metrics.ForeignTransfers == 0 && metrics.ReadersHealthy
                    && metrics.UnsolicitedInvokes == 0 && metrics.SccpReturns == 0;
                bool numericTargetMet = executionPassed && options.DurationSeconds == 1800
                    && measured.Counts.CompletedPerSecond.All(count => count >= CapacityOptions.TargetTps);
                ThreadPool.GetMinThreads(out int minimumWorkers, out int minimumIoThreads);
                ThreadPool.GetMaxThreads(out int maximumWorkers, out int maximumIoThreads);
                var report = new { SchemaVersion = 1, options.SourceCommit, Workload = CapacityOptions.Workload,
                    options.Diagnostic, CapacityPolicy = CapacityOptions.AcceptanceRevision,
                    TargetTps = CapacityOptions.TargetTps, options.DurationSeconds,
                    options.WarmupSeconds, options.Concurrency, options.QueueCapacity,
                    ProcessId = Environment.ProcessId, Runtime = Environment.Version.ToString(),
                    ThreadPoolLimits = new { MinimumWorkers = minimumWorkers, MaximumWorkers = maximumWorkers,
                        MinimumIoThreads = minimumIoThreads, MaximumIoThreads = maximumIoThreads },
                    RuntimeTuning = new { WorkerSpinLimitEnvironment =
                        Environment.GetEnvironmentVariable("DOTNET_ThreadPool_UnfairSemaphoreSpinLimit") },
                    StartedAtUtc = started, FinishedAtUtc = DateTimeOffset.UtcNow, Warmup = warmup,
                    Measured = measured, Stack = metrics, ExecutionPassed = executionPassed,
                    NumericTargetMet = numericTargetMet, QualificationPassed = false };
                string pending = Path.Combine(options.Output, "sdk.pending.json");
                await using (FileStream stream = new(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await JsonSerializer.SerializeAsync(stream, report, new JsonSerializerOptions { WriteIndented = true });
                File.Move(pending, path);
                Console.WriteLine($"capacity completed={measured.Counts.Measured} tps={measured.Counts.Measured / measured.MeasuredSeconds:F1} execution={executionPassed} numericTarget={numericTargetMet}");
                return executionPassed ? 0 : 1;
            }
            finally { Console.CancelKeyPress -= handler; }
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 2; }
    }

    private static async Task ObserveAsync(CapacitySession session, CancellationToken ct)
    {
        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
            int sample = 0;
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                var snapshot = session.Snapshot();
                if (++sample % 10 == 0)
                    Console.WriteLine($"capacity seconds={sample} received={snapshot.Received} pending={snapshot.Pending} queues={snapshot.OutboundQueue}/{snapshot.InboundQueue}");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
    }
}
