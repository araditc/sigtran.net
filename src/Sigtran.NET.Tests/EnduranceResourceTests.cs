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
        string path = Path.Combine(Path.GetTempPath(), "sigtran-observations-" + Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            EnduranceResources resources = new(1, 2, 3, 4, 5, 6, 7.5, 8, 9, 10, 11, 12, 13);
            CapacityStackMetrics stack = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, true);
            using (EnduranceObservationLog log = new(path)) log.Append(new(DateTimeOffset.UtcNow, 1.25, new EnduranceLedger(1).Snapshot(), resources), stack);
            using BinaryReader reader = new(File.OpenRead(path));
            Check(System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8)) == "SGENOBS1"
                && reader.BaseStream.Length == 8 + EnduranceObservationLog.RecordBytes, "Resource record header or width differs");
            Check(reader.ReadDouble() == 1.25, "Elapsed time must be a real double, not a sample index");
            for (long i = 1; i <= 6; i++) Check(reader.ReadInt64() == i, "Resource counter order/width differs");
            Check(reader.ReadDouble() == 7.5, "CPU time must retain its fractional seconds");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
