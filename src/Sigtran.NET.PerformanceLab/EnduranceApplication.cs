using System.Text.Json;

/// <summary>Runs native MAP endurance traffic without declaring lab topology or release readiness on its own.</summary>
internal static class EnduranceApplication
{
    internal static async Task<int> RunAsync(string[] args)
    {
        EnduranceOptions options = EnduranceOptions.Parse(args);
        Directory.CreateDirectory(options.Session.Output);
        using EnduranceJournal journal = new(Path.Combine(options.Session.Output, "checkpoints.jsonl"));
        using EnduranceObservationLog observations = new(Path.Combine(options.Session.Output, "resource-observations.bin"));
        using CancellationTokenSource stop = new();
        ConsoleCancelEventHandler handler = (_, value) => { value.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        EnduranceLedger ledger = new(options.Session.Concurrency);
        EnduranceMonitor monitor = new(options.Session.Concurrency, options.Session.QueueCapacity,
            options.Diagnostic ? options.DurationSeconds / 6.0 : 600);
        try
        {
            await using CapacitySession session = new(options.Session, allowReconnect: true);
            using CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            startup.CancelAfter(TimeSpan.FromSeconds(30));
            await session.StartAsync(startup.Token).ConfigureAwait(false);
            var socketProfile = session.SocketProfile;
            EnduranceControl control = new(options, session, journal);
            bool first = true;
            var settings = new EnduranceRunSettings(TimeSpan.FromSeconds(options.DurationSeconds), options.Session.Concurrency,
                TimeSpan.FromSeconds(EnduranceOptions.CheckpointSeconds), TimeSpan.FromSeconds(10));
            EnduranceRun result = await EnduranceRunner.RunAsync(settings, ledger, session.InvokeAsync, () => session.IsActive,
                sample =>
                {
                    var stack = session.Snapshot();
                    if (first)
                    {
                        ThreadPool.GetMinThreads(out int minimumWorkers, out int minimumIoThreads);
                        ThreadPool.GetMaxThreads(out int maximumWorkers, out int maximumIoThreads);
                        journal.Append(new { Kind = "start", SchemaVersion = 1, Policy = EnduranceOptions.Policy,
                            Workload = CapacityOptions.Workload, options.Session.SourceCommit, options.Diagnostic,
                            options.DurationSeconds, options.Session.Concurrency, options.Session.QueueCapacity,
                            sample.StartedAtUtc, ProcessId = Environment.ProcessId, Runtime = Environment.Version.ToString(),
                            LocalPort = session.LocalPort, HeartbeatIntervalSeconds = 2, HeartbeatTimeoutSeconds = 2,
                            ObservationSeconds = 1, CheckpointSeconds = EnduranceOptions.CheckpointSeconds,
                            ServerGc = System.Runtime.GCSettings.IsServerGC, SocketProfile = socketProfile, FaultPlan = options.FaultPlan(),
                            ThreadPoolLimits = new { MinimumWorkers = minimumWorkers, MaximumWorkers = maximumWorkers,
                                MinimumIoThreads = minimumIoThreads, MaximumIoThreads = maximumIoThreads }, Sample = sample, Stack = stack });
                        first = false;
                    }
                    else journal.Append(new { Kind = "checkpoint", Sample = sample, Stack = stack, ResourceSummary = monitor.Snapshot() });
                    EnduranceControl.WriteAtomic(Path.Combine(options.Session.Output, "live-status.json"), new {
                        State = "running", sample.StartedAtUtc, sample.ElapsedSeconds, sample.Counts, Stack = stack });
                    Console.WriteLine($"endurance elapsed={sample.ElapsedSeconds:F1} successes={sample.Counts.Successful} pending={sample.Counts.Pending}");
                }, stop.Token, control.RunAsync, sample =>
                {
                    var observedStack = session.Snapshot();
                    observations.Append(sample, observedStack);
                    monitor.Observe(sample, observedStack);
                }).ConfigureAwait(false);
            control.ObserveFaults(null);
            for (int i = 0; i < 100 && session.Snapshot().Active > 0; i++) await Task.Delay(10).ConfigureAwait(false);
            var final = session.Snapshot();
            if (monitor.Snapshot().WithinBounds)
            {
                using EnduranceResourceProbe finalProbe = new();
                EnduranceSample finalSample = new(result.StartedAtUtc, control.Clock!.ElapsedSeconds, result.Counts, finalProbe.Read());
                observations.Append(finalSample, final);
                monitor.Observe(finalSample, final);
            }
            var resources = monitor.Snapshot();
            bool passed = !result.Aborted && result.Counts.Reconciled && result.Counts.Pending == 0
                && result.Counts.UnexpectedFailures == 0 && result.Counts.RecoveredFaults == options.FaultPlan().Length
                && final.Pending == 0 && final.Active == 0 && final.Malformed == 0 && final.DroppedEvents == 0
                && final.ForeignTransfers == 0 && final.UnsolicitedInvokes == 0 && final.SccpReturns == 0
                && final.OutboundQueue == 0 && final.InboundQueue == 0 && final.ReadersHealthy
                && resources.WithinBounds && resources.BaselineWorkingSetBytes is not null;
            var report = new { SchemaVersion = 1, Policy = EnduranceOptions.Policy, options.Session.SourceCommit,
                options.Diagnostic, Run = result, Stack = final, ResourceSummary = resources, SocketProfile = session.SocketProfile,
                ExecutionPassed = passed, QualificationPassed = false, StablePromotion = false };
            journal.Append(new { Kind = "finish", Report = report });
            string output = Path.Combine(options.Session.Output, "sdk.json");
            await using (FileStream stream = new(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                await JsonSerializer.SerializeAsync(stream, report, new JsonSerializerOptions { WriteIndented = true });
            EnduranceControl.WriteAtomic(Path.Combine(options.Session.Output, "live-status.json"), new {
                State = passed ? "completed" : "failed", result.StartedAtUtc, result.ElapsedSeconds, result.Counts });
            return passed ? 0 : 1;
        }
        catch (Exception error)
        {
            journal.Append(new { Kind = "failed", Error = error.GetType().Name, Counts = ledger.Snapshot(), ResourceSummary = monitor.Snapshot() });
            Console.Error.WriteLine(error);
            return 1;
        }
        finally { Console.CancelKeyPress -= handler; }
    }
}
