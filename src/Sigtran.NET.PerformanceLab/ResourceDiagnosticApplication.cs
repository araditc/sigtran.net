using System.Globalization;
using System.Text.Json;

/// <summary>Fences real-stack memory diagnostics to IPv4 loopback and at most one hour; never qualifies endurance.</summary>
internal sealed record ResourceDiagnosticOptions(CapacityOptions Session, int DurationSeconds)
{
    internal static ResourceDiagnosticOptions Parse(string[] args)
    {
        HashSet<string> allowed = ["remote-ip", "remote-port", "source-commit", "output", "duration-seconds",
            "concurrency", "queue-capacity", "socket-profile"];
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
            if (i + 1 >= args.Length || !args[i].StartsWith("--", StringComparison.Ordinal)
                || !allowed.Contains(args[i][2..]) || !values.TryAdd(args[i][2..], args[i + 1]))
                throw new ArgumentException("Unknown, repeated or incomplete resource-session option.");
        if (values.GetValueOrDefault("remote-ip") != "127.0.0.1")
            throw new ArgumentException("A resource session permits only the isolated IPv4 loopback peer.");
        if (!int.TryParse(values.GetValueOrDefault("duration-seconds", "3600"), NumberStyles.None,
            CultureInfo.InvariantCulture, out int duration) || duration is < 10 or > 3600)
            throw new ArgumentException("A resource session requires 10..3600 diagnostic seconds.");
        List<string> session = [];
        foreach (var pair in values.Where(x => x.Key != "duration-seconds")) session.AddRange(["--" + pair.Key, pair.Value]);
        session.AddRange(["--duration-seconds", "1", "--diagnostic", "true"]);
        CapacityOptions options = CapacityOptions.Parse(session.ToArray());
        if (options.Concurrency > 1024) throw new ArgumentException("At most 1024 fixed workers are permitted.");
        return new(options, duration);
    }
}

/// <summary>Reuses the actual native SCTP/M3UA/SCCP/TCAP/MAP session and continuous ledger without fault injection.</summary>
internal static class ResourceDiagnosticApplication
{
    internal const string Policy = "local-map-resource-diagnostic-v1";

    internal static async Task<int> RunAsync(string[] args)
    {
        ResourceDiagnosticOptions options = ResourceDiagnosticOptions.Parse(args);
        if (Directory.Exists(options.Session.Output)) throw new IOException("Resource-session output cannot be reused.");
        Directory.CreateDirectory(options.Session.Output);
        using EnduranceJournal journal = new(Path.Combine(options.Session.Output, "checkpoints.jsonl"));
        using EnduranceObservationLog observations = new(Path.Combine(options.Session.Output, "resource-observations.bin"));
        using FileStream detailsFile = new(Path.Combine(options.Session.Output, "gc-observations.jsonl"), FileMode.CreateNew);
        using StreamWriter details = new(detailsFile) { AutoFlush = true };
        using CancellationTokenSource stop = new();
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        EnduranceLedger ledger = new(options.Session.Concurrency);
        EnduranceMonitor monitor = new(options.Session.Concurrency, options.Session.QueueCapacity,
            Math.Min(600, options.DurationSeconds / 6.0));
        EnduranceClock? clock = null;
        try
        {
            await using CapacitySession session = new(options.Session);
            using CancellationTokenSource startup = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            startup.CancelAfter(TimeSpan.FromSeconds(30));
            await session.StartAsync(startup.Token).ConfigureAwait(false);
            bool first = true;
            EnduranceRun result = await EnduranceRunner.RunAsync(new(TimeSpan.FromSeconds(options.DurationSeconds),
                options.Session.Concurrency, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10)), ledger,
                session.InvokeAsync, () => session.IsActive, sample =>
                {
                    if (first)
                    {
                        ThreadPool.GetMaxThreads(out int maximumWorkers, out _);
                        journal.Append(new { Kind = "start", SchemaVersion = 1, Policy,
                            EvidenceKind = "local-map-resource-diagnostic", Diagnostic = true,
                            QualificationPassed = false, StablePromotion = false, options.Session.SourceCommit,
                            options.DurationSeconds, options.Session.Concurrency, options.Session.QueueCapacity,
                            Workload = CapacityOptions.Workload, sample.StartedAtUtc, ProcessId = Environment.ProcessId,
                            Runtime = Environment.Version.ToString(), MaximumWorkers = maximumWorkers,
                            ServerGc = System.Runtime.GCSettings.IsServerGC, ObservationFormat = EnduranceObservationLog.Format,
                            ManagedMemorySampling = new { Method = ManagedMemorySampler.Method, MaximumAttempts = 3, RetryDelayMilliseconds = 1 },
                            SocketProfile = session.SocketProfile, Sample = sample });
                        first = false;
                    }
                    else journal.Append(new { Kind = "checkpoint", Sample = sample, ResourceSummary = monitor.Snapshot() });
                    EnduranceControl.WriteAtomic(Path.Combine(options.Session.Output, "live-status.json"),
                        new { State = "running", sample.ElapsedSeconds, sample.Counts, Resources = monitor.Snapshot() });
                    Console.WriteLine($"resource-session elapsed={sample.ElapsedSeconds:F1} successes={sample.Counts.Successful} pending={sample.Counts.Pending}");
                }, stop.Token, (execution, token) =>
                {
                    clock = execution.Clock;
                    return Task.Delay(Timeout.Infinite, token);
                }, sample => Observe(sample, session.Snapshot())).ConfigureAwait(false);
            for (int i = 0; i < 100 && session.Snapshot().Active > 0; i++) await Task.Delay(10).ConfigureAwait(false);
            var stack = session.Snapshot();
            if (monitor.Snapshot().WithinBounds && clock is not null)
            {
                using EnduranceResourceProbe probe = new();
                Observe(new(result.StartedAtUtc, clock.ElapsedSeconds, result.Counts, probe.Read()), stack);
            }
            bool passed = !result.Aborted && result.MeasuredSeconds == options.DurationSeconds
                && result.Counts.Reconciled && result.Counts.Pending == 0 && result.Counts.UnexpectedFailures == 0
                && result.Counts.Successful > 0 && stack.Opened == stack.Closed && stack.Closed == result.Counts.Started
                && stack.Sent == result.Counts.Started && stack.Received == result.Counts.Successful
                && stack.Pending == 0 && stack.Active == 0 && stack.Faults == 0 && stack.Reconnects == 0
                && stack.Malformed == 0 && stack.DroppedEvents == 0 && stack.ForeignTransfers == 0
                && stack.UnsolicitedInvokes == 0 && stack.SccpReturns == 0 && stack.ReadersHealthy
                && monitor.Snapshot().WithinBounds;
            var report = new { SchemaVersion = 1, EvidenceKind = "local-map-resource-diagnostic", Policy,
                Diagnostic = true, LocalOnly = true, options.Session.SourceCommit, Run = result,
                Stack = stack, ResourceSummary = monitor.Snapshot(), SocketProfile = session.SocketProfile,
                ExecutionPassed = passed, QualificationPassed = false, StablePromotion = false, RootCauseProven = false };
            journal.Append(new { Kind = "finish", Report = report });
            using (FileStream file = new(Path.Combine(options.Session.Output, "sdk.json"), FileMode.CreateNew))
                JsonSerializer.Serialize(file, report, new JsonSerializerOptions { WriteIndented = true });
            EnduranceControl.WriteAtomic(Path.Combine(options.Session.Output, "live-status.json"),
                new { State = passed ? "completed" : "failed", result.ElapsedSeconds, result.Counts, Resources = monitor.Snapshot() });
            return passed ? 0 : 1;
        }
        catch (Exception error)
        {
            journal.Append(new { Kind = "failed", Error = error.GetType().Name, Counts = ledger.Snapshot(), Resources = monitor.Snapshot() });
            Console.Error.WriteLine(error);
            return 1;
        }
        finally { Console.CancelKeyPress -= handler; }

        void Observe(EnduranceSample sample, CapacityStackMetrics stack)
        {
            observations.Append(sample, stack);
            details.WriteLine(JsonSerializer.Serialize(new { Sample = sample, Stack = stack }));
            if (detailsFile.Position > 16 * 1048576) throw new IOException("GC context observation budget exceeded.");
            monitor.Observe(sample, stack);
        }
    }
}
