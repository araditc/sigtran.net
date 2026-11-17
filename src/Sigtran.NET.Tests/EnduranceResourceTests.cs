using Sigtran.NET.Layers.TCAP;

internal static partial class EnduranceTests
{
    internal static void ResourceBounds()
    {
        EnduranceLedger ledger = new(2);
        EnduranceResources resources = new(100 * 1024 * 1024, 100 * 1024 * 1024, 20, 20, 0, 100, 1, 0, 0, 0, 4, 0, 0);
        CapacityStackMetrics stack = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true);
        EnduranceMonitor monitor = new(2, 8192, 1);
        monitor.Observe(new(DateTimeOffset.UtcNow, 0, ledger.Snapshot(), resources), stack);
        ledger.TryAdmit(0, 0, out var request);
        ledger.Complete(request, CapacityOutcome.Success, 1);
        monitor.Observe(new(DateTimeOffset.UtcNow, 1, ledger.Snapshot(), resources), stack);
        Check(monitor.Snapshot().BaselineWorkingSetBytes == resources.WorkingSetBytes && monitor.Snapshot().WithinBounds,
            "Resource baseline must be explicit and not require forced collection");
        try { monitor.Observe(new(DateTimeOffset.UtcNow, 2, ledger.Snapshot(), resources with { PeakWorkingSetBytes = 513L * 1024 * 1024 }), stack); }
        catch (InvalidOperationException) { }
        Check(!monitor.Snapshot().WithinBounds && monitor.Snapshot().Violations.Contains("working-set-bound"),
            "Resource breach must remain visible after fail-fast");

        EnduranceMonitor queues = new(2, 16, 0);
        try { queues.Observe(new(DateTimeOffset.UtcNow, 0, ledger.Snapshot(), resources), stack with { InboundQueue = 17 }); }
        catch (InvalidOperationException) { }
        Check(queues.Snapshot().Violations.Contains("queue-context-bound"), "Oversized queues cannot qualify");

        EnduranceResources[] invalidResources =
        [
            resources with { WorkingSetBytes = -1 }, resources with { PeakWorkingSetBytes = -1 },
            resources with { ManagedBytes = -710056 }, resources with { HeapSizeBytes = -1 },
            resources with { FragmentedBytes = -1 }, resources with { AllocatedBytes = -1 },
            resources with { ProcessCpuSeconds = -1 }, resources with { ProcessCpuSeconds = double.NaN },
            resources with { ProcessCpuSeconds = double.PositiveInfinity },
            resources with { Gen0 = -1 }, resources with { Gen1 = -1 }, resources with { Gen2 = -1 },
            resources with { ThreadPoolThreads = -1 }, resources with { ThreadPoolPending = -1 },
            resources with { ThreadPoolCompleted = -1 }
        ];
        foreach (var invalid in invalidResources)
        {
            EnduranceMonitor counters = new(2, 8192, 0);
            bool rejected = false;
            try { counters.Observe(new(DateTimeOffset.UtcNow, 0, ledger.Snapshot(), invalid), stack); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && !counters.Snapshot().WithinBounds
                && counters.Snapshot().Violations.Contains("invalid-resource-observation"),
                "Invalid resource readings must fail immediately, not disappear into nonnegative peak aggregates");
        }
    }

    internal static void IdentifierRollover()
    {
        TcapTransactionIdAllocator defaults = new(firstValue: 0x00fffffe);
        Check(defaults.Allocate().ToString() == "FFFFFE" && defaults.Allocate().ToString() == "FFFFFF"
            && defaults.Allocate().ToString() == "01", "The default 24-bit range must wrap without allocating reserved zero");
        TcapTransactionIdAllocator full = new(firstValue: uint.MaxValue - 1, maxValue: uint.MaxValue);
        Check(full.Allocate().ToString() == "FFFFFFFE" && full.Allocate().ToString() == "FFFFFFFF"
            && full.Allocate().ToString() == "01", "The full 32-bit range must wrap before unsigned overflow");
    }

    internal static void ObservationFormat()
    {
        Check(8L + (EnduranceOptions.RequiredSeconds + 60L) * EnduranceObservationLog.RecordBytes <= 64L * 1024 * 1024,
            "Audited retry metadata must fit the unchanged full-duration observation budget");
        string path = Path.Combine(Path.GetTempPath(), "sigtran-observations-" + Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            EnduranceResources resources = new(1, 2, -710056, 4, 5, 6, 7.5, 8, 9, 10, 11, 12, 13);
            CapacityStackMetrics stack = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true);
            using (EnduranceObservationLog log = new(path))
            {
                log.Append(new(DateTimeOffset.UtcNow, 1.25, new EnduranceLedger(1).Snapshot(), resources), stack);
                log.Append(new(DateTimeOffset.UtcNow, 2.25, new EnduranceLedger(1).Snapshot(),
                    resources with { ManagedBytes = 1234, ManagedMemory = new(-710056, 1234, 0, 2) }), stack);
            }
            using BinaryReader reader = new(File.OpenRead(path));
            Check(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8)) == "SGENOBS2"
                && reader.BaseStream.Length == 8 + 2 * EnduranceObservationLog.RecordBytes, "Resource record header or width differs");
            Check(reader.ReadDouble() == 1.25, "Elapsed time must be a real double, not a sample index");
            for (long i = 1; i <= 6; i++)
                Check(reader.ReadInt64() == (i == 3 ? -710056 : i), "Raw signed resource readings must never be clamped or reordered");
            Check(reader.ReadDouble() == 7.5, "CPU time must retain its fractional seconds");
            reader.BaseStream.Position = 8 + 208;
            Check(reader.ReadInt64() == -710056 && reader.ReadInt64() == 0 && reader.ReadInt64() == 0
                && reader.ReadInt64() == 1, "Every signed managed-memory attempt must remain in the binary record");
            reader.BaseStream.Position = 8 + EnduranceObservationLog.RecordBytes + 24;
            Check(reader.ReadInt64() == 1234, "The accepted value must be an actual fresh nonnegative reading");
            reader.BaseStream.Position = 8 + 2 * EnduranceObservationLog.RecordBytes - 32;
            Check(reader.ReadInt64() == -710056 && reader.ReadInt64() == 1234 && reader.ReadInt64() == 0
                && reader.ReadInt64() == 2, "A recovered retry must retain both the invalid and accepted raw values");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    internal static void ManagedMemorySampling()
    {
        foreach (long[] sequence in new long[][] { [20], [-1, 20], [-1, -2, 20], [-1, -2, -3], [300L * 1024 * 1024] })
        {
            int calls = 0, waits = 0;
            var reading = ManagedMemorySampler.Read(() => sequence[calls++], () => waits++);
            Check(reading.Consistent && calls == sequence.Length && waits == sequence.Length - 1
                && reading.Value == sequence[^1], "Only invalid negative reads may trigger bounded retries");
            Check(reading.First == sequence[0] && (calls < 2 || reading.Second == sequence[1])
                && (calls < 3 || reading.Third == sequence[2]), "Retries must preserve all raw attempted values");
        }
        EnduranceResources resources = new(100, 100, 20, 20, 0, 100, 1, 0, 0, 0, 4, 0, 0,
            new(-710056, 20, 0, 2));
        CapacityStackMetrics stack = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true);
        EnduranceMonitor monitor = new(2, 8192, 0);
        monitor.Observe(new(DateTimeOffset.UtcNow, 0, new EnduranceLedger(2).Snapshot(), resources), stack);
        Check(monitor.Snapshot().WithinBounds && monitor.Snapshot().RetriedManagedObservations == 1
            && monitor.Snapshot().NegativeManagedReadings == 1, "Recovered sampling anomalies must remain visible in aggregates");
        EnduranceMonitor invalid = new(2, 8192, 0);
        bool rejected = false;
        try { invalid.Observe(new(DateTimeOffset.UtcNow, 0, new EnduranceLedger(2).Snapshot(),
            resources with { ManagedMemory = new(10, 20, 0, 2) }), stack); }
        catch (InvalidOperationException) { rejected = true; }
        Check(rejected && !invalid.Snapshot().WithinBounds, "A valid earlier reading cannot be discarded to select another value");
    }
}
