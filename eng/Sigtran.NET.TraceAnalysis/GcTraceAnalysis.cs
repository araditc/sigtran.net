using System.Text.Json;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

/// <summary>Decodes GC identities from a retained trace copy; does not infer causation or qualify endurance.</summary>
internal static class GcTraceAnalysis
{
    internal static int Run(string path)
    {
        if (new FileInfo(path).Length > 256L * 1048576) throw new IOException("GC trace exceeds diagnostic budget.");
        string converted = TraceLog.CreateFromEventPipeDataFile(Path.GetFullPath(path));
        using TraceLog log = new(converted);
        GcTraceSummary summary = new();
        foreach (TraceEvent entry in log.Events)
        {
            if (entry is GCStartTraceData start)
                summary.Start(start.ProcessID, start.Count, start.Depth, start.Reason.ToString(), start.TimeStampRelativeMSec);
            else if (entry is GCEndTraceData stop)
                summary.Stop(stop.ProcessID, stop.Count, stop.TimeStampRelativeMSec);
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            SchemaVersion = 1, EvidenceKind = "gc-eventpipe-diagnostic", Diagnostic = true,
            QualificationPassed = false, StablePromotion = false, RootCauseProven = false,
            SessionStartUtc = log.SessionStartTime.ToUniversalTime(), EventsLost = log.EventsLost,
            summary.Starts, summary.Stops, summary.MinimumIndex, summary.MaximumIndex,
            summary.FirstRelativeMilliseconds, summary.LastRelativeMilliseconds,
            summary.ProcessIds, summary.Generations, summary.Reasons
        }, new JsonSerializerOptions { WriteIndented = true }));
        return summary.Starts > 0 && summary.Stops > 0 && log.EventsLost == 0 ? 0 : 1;
    }
}

/// <summary>Uses bounded GC generation/reason/process aggregates instead of retaining an event per collection.</summary>
internal sealed class GcTraceSummary
{
    internal long Starts { get; private set; }
    internal long Stops { get; private set; }
    internal int? MinimumIndex { get; private set; }
    internal int? MaximumIndex { get; private set; }
    internal double? FirstRelativeMilliseconds { get; private set; }
    internal double LastRelativeMilliseconds { get; private set; }
    internal HashSet<int> ProcessIds { get; } = [];
    internal Dictionary<int, long> Generations { get; } = [];
    internal Dictionary<string, long> Reasons { get; } = new(StringComparer.Ordinal);

    internal void Start(int pid, int index, int generation, string reason, double milliseconds)
    {
        Observe(pid, index, milliseconds);
        if (generation is < 0 or > 2 || (Reasons.Count >= 64 && !Reasons.ContainsKey(reason)))
            throw new InvalidDataException("Unbounded GC event schema.");
        Starts++;
        Generations[generation] = Generations.GetValueOrDefault(generation) + 1;
        Reasons[reason] = Reasons.GetValueOrDefault(reason) + 1;
    }

    internal void Stop(int pid, int index, double milliseconds)
    { Observe(pid, index, milliseconds); Stops++; }

    private void Observe(int pid, int index, double milliseconds)
    {
        if (index < 0 || !double.IsFinite(milliseconds) || milliseconds < 0
            || (ProcessIds.Count >= 4 && !ProcessIds.Contains(pid)))
            throw new InvalidDataException("Invalid GC event identity/time.");
        ProcessIds.Add(pid);
        MinimumIndex = MinimumIndex is { } minimum ? Math.Min(minimum, index) : index;
        MaximumIndex = MaximumIndex is { } maximum ? Math.Max(maximum, index) : index;
        FirstRelativeMilliseconds ??= milliseconds;
        LastRelativeMilliseconds = Math.Max(LastRelativeMilliseconds, milliseconds);
    }
}
