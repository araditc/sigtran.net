using System.Text.Json;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

if (args is ["--self-test"])
{
    AllocationSummary summary = new();
    summary.Record("Bytes", 100, ["Encode", "Dispatch", "Dispatch"]);
    summary.Record("Task", 50, ["Dispatch"]);
    if (summary.Samples != 2 || summary.EstimatedBytes != 150 || summary.Methods["Dispatch"] != 150
        || summary.Types["Bytes"] != 100) throw new InvalidOperationException("Allocation attribution regression.");
    Console.WriteLine("PASS allocation sample aggregation and recursive-frame deduplication");
    return 0;
}
if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: Sigtran.NET.TraceAnalysis FILE.nettrace | --self-test");
    return 2;
}

string converted = TraceLog.CreateFromEventPipeDataFile(Path.GetFullPath(args[0]));
using TraceLog log = new(converted);
AllocationSummary allocations = new();
foreach (TraceEvent entry in log.Events)
{
    if (entry is not GCAllocationTickTraceData allocation) continue;
    List<string> frames = [];
    for (TraceCallStack? frame = allocation.CallStack(); frame is not null && frames.Count < 128; frame = frame.Caller)
        frames.Add(frame.CodeAddress.FullMethodName);
    allocations.Record(allocation.TypeName, Math.Max(0, allocation.AllocationAmount64), frames);
}
Console.WriteLine(JsonSerializer.Serialize(new
{
    SchemaVersion = 1, Kind = "weighted-allocation-tick-samples", allocations.Samples,
    allocations.EstimatedBytes, EventsLost = log.EventsLost,
    Attribution = "Allocation ticks are sampled and weighted; type bytes are estimates, method bytes are inclusive and overlap.",
    Types = allocations.Types.OrderByDescending(item => item.Value).Take(40).Select(item => new { Type = item.Key, EstimatedBytes = item.Value }),
    Sites = allocations.Sites.OrderByDescending(item => item.Value).Take(60).Select(item => new { Site = item.Key, EstimatedBytes = item.Value }),
    Methods = allocations.Methods.OrderByDescending(item => item.Value).Take(60).Select(item => new { Method = item.Key, InclusiveEstimatedBytes = item.Value })
}, new JsonSerializerOptions { WriteIndented = true }));
return allocations.Samples > 0 && log.EventsLost == 0 ? 0 : 1;

/// <summary>Aggregates bounded trace samples without presenting weighted attribution as exact allocation counts.</summary>
internal sealed class AllocationSummary
{
    internal Dictionary<string, long> Types { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, long> Methods { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, long> Sites { get; } = new(StringComparer.Ordinal);
    internal long Samples { get; private set; }
    internal long EstimatedBytes { get; private set; }

    internal void Record(string type, long bytes, IEnumerable<string> stack)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytes);
        Samples++;
        EstimatedBytes += bytes;
        Types[type] = Types.GetValueOrDefault(type) + bytes;
        string[] frames = stack.Distinct(StringComparer.Ordinal).ToArray();
        string site = type + " @ " + (frames.FirstOrDefault(frame => frame.StartsWith("Sigtran.NET.", StringComparison.Ordinal))
            ?? frames.FirstOrDefault() ?? "unresolved");
        Sites[site] = Sites.GetValueOrDefault(site) + bytes;
        foreach (string method in frames)
            Methods[method] = Methods.GetValueOrDefault(method) + bytes;
    }
}
