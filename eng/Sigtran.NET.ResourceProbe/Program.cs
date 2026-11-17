using System.Diagnostics;
using System.Globalization;
using System.Runtime;
using System.Text.Json;

// This tool deliberately references no SDK, sockets, service controller or Kafka package.
Dictionary<string, string> values = new(StringComparer.Ordinal);
HashSet<string> allowed = ["--seconds", "--output", "--allocation-delay-ms", "--retention-slots", "--allocation-mode"];
for (int i = 0; i < args.Length; i += 2)
    if (i + 1 >= args.Length || !allowed.Contains(args[i]) || !values.TryAdd(args[i], args[i + 1]))
        throw new ArgumentException("Unknown, repeated or incomplete resource-probe option.");
int seconds = Number("--seconds", 60, 1, 1800);
int allocationDelay = Number("--allocation-delay-ms", 20, 0, 100);
int retentionSlots = Number("--retention-slots", 2048, 64, 4096);
string allocationMode = values.GetValueOrDefault("--allocation-mode", "threads");
if (allocationMode is not ("threads" or "threadpool")) throw new ArgumentException("Invalid --allocation-mode.");
if (!values.TryGetValue("--output", out string? outputPath)) throw new ArgumentException("A new --output directory is required.");
string output = Path.GetFullPath(outputPath);
if (Directory.Exists(output)) throw new IOException("A diagnostic output directory cannot be reused.");
Directory.CreateDirectory(output);
using StreamWriter anomalies = new(new FileStream(Path.Combine(output, "anomalies.jsonl"), FileMode.CreateNew));
using CancellationTokenSource stop = new();
using Process process = Process.GetCurrentProcess();
int workers = Math.Min(3, Math.Max(1, Environment.ProcessorCount - 1));
Thread[] allocators = allocationMode == "threads" ? new Thread[workers] : [];
Task[] asyncAllocators = [];
for (int i = 0; i < allocators.Length; i++)
{
    allocators[i] = new Thread(Allocate) { IsBackground = true, Name = "bounded-allocation-probe" };
    allocators[i].Start();
}
if (allocationMode == "threadpool")
{
    ThreadPool.GetMinThreads(out _, out int minIo); ThreadPool.GetMaxThreads(out _, out int maxIo);
    if (!ThreadPool.SetMinThreads(4, minIo) || !ThreadPool.SetMaxThreads(4, maxIo))
        throw new InvalidOperationException("Cannot configure isolated diagnostic worker limits.");
    asyncAllocators = Enumerable.Range(0, workers).Select(_ => Task.Run(AllocateAsync)).ToArray();
}
DateTimeOffset started = DateTimeOffset.UtcNow;
Stopwatch clock = Stopwatch.StartNew();
long samples = 0, rawNegative = 0, retried = 0, exhausted = 0, invalidRawReads = 0, savedAnomalies = 0;
long maximumAccepted = 0, minimumRaw = long.MaxValue, maximumHeap = 0, peakRss = 0;
long allocatedBefore = GC.GetTotalAllocatedBytes(false);
int[] collectionsBefore = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
try
{
    while (clock.Elapsed.TotalSeconds < seconds)
    {
        ManagedMemoryReading reading = ManagedMemorySampler.Read();
        GCMemoryInfo info = GC.GetGCMemoryInfo();
        samples++;
        minimumRaw = Math.Min(minimumRaw, reading.First);
        if (reading.First < 0) rawNegative++;
        if (reading.Attempts > 1) retried++;
        invalidRawReads += (reading.First < 0 ? 1 : 0) + (reading.Attempts > 1 && reading.Second < 0 ? 1 : 0)
            + (reading.Attempts > 2 && reading.Third < 0 ? 1 : 0);
        if (reading.Value < 0 || !reading.Consistent) exhausted++;
        else maximumAccepted = Math.Max(maximumAccepted, reading.Value);
        maximumHeap = Math.Max(maximumHeap, info.HeapSizeBytes);
        if (reading.Attempts > 1 && savedAnomalies < 4096)
        {
            anomalies.WriteLine(JsonSerializer.Serialize(new { ElapsedSeconds = clock.Elapsed.TotalSeconds,
                Reading = reading, info.Index, info.Generation, info.HeapSizeBytes, info.FragmentedBytes }));
            savedAnomalies++;
        }
        if ((samples & 1023) == 0)
        {
            process.Refresh();
            peakRss = Math.Max(peakRss, process.PeakWorkingSet64);
            if (peakRss > 512L * 1024 * 1024) throw new InvalidOperationException("Diagnostic RSS budget exceeded.");
            anomalies.Flush();
        }
        // Accelerated sampling increases exposure to the anomaly; this is not a duration qualification.
        Thread.SpinWait(64);
    }
}
finally
{
    stop.Cancel();
    foreach (var allocator in allocators)
        if (!allocator.Join(TimeSpan.FromSeconds(5))) throw new TimeoutException("Allocation worker did not stop.");
    if (!Task.WhenAll(asyncAllocators).Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Async allocation worker did not stop.");
}
anomalies.Flush();
process.Refresh();
peakRss = Math.Max(peakRss, process.PeakWorkingSet64);
var report = new
{
    SchemaVersion = 1, EvidenceKind = "standalone-managed-memory-diagnostic", Diagnostic = true,
    QualificationPassed = false, StablePromotion = false, StartedAtUtc = started, FinishedAtUtc = DateTimeOffset.UtcNow,
    RequestedSeconds = seconds, ElapsedSeconds = clock.Elapsed.TotalSeconds, Runtime = Environment.Version.ToString(),
    RuntimeDescription = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
    ServerGc = GCSettings.IsServerGC, Processors = Environment.ProcessorCount, AllocationWorkers = workers,
    AllocationBatch = 256, AllocationDelayMilliseconds = allocationDelay, RetentionSlots = retentionSlots,
    AllocationMode = allocationMode,
    Sampler = ManagedMemorySampler.Method, Samples = samples, FirstReadNegativeSamples = rawNegative,
    RetriedSamples = retried, InvalidRawReads = invalidRawReads, ExhaustedSamples = exhausted,
    SavedAnomalies = savedAnomalies, AnomalyLogTruncated = retried > savedAnomalies, MinimumFirstRawBytes = minimumRaw,
    MaximumAcceptedManagedBytes = maximumAccepted, MaximumLastGcHeapBytes = maximumHeap,
    PeakWorkingSetBytes = peakRss, AllocatedBytes = GC.GetTotalAllocatedBytes(false) - allocatedBefore,
    Gen0Collections = GC.CollectionCount(0) - collectionsBefore[0], Gen1Collections = GC.CollectionCount(1) - collectionsBefore[1],
    Gen2Collections = GC.CollectionCount(2) - collectionsBefore[2], ProcessCpuSeconds = process.TotalProcessorTime.TotalSeconds,
    ForcedCollections = false, NetworkTraffic = false, KafkaChanged = false
};
using (FileStream file = new(Path.Combine(output, "summary.json"), FileMode.CreateNew))
    JsonSerializer.Serialize(file, report, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(JsonSerializer.Serialize(report));
return exhausted == 0 ? 0 : 1;

void Allocate()
{
    // A bounded live ring supplies ordinary allocation-driven collections, not explicit GC.Collect calls.
    byte[][] ring = new byte[retentionSlots][];
    int cursor = 0;
    while (!stop.IsCancellationRequested)
    {
        for (int i = 0; i < 256; i++)
        {
            byte[] value = new byte[4096 + (cursor & 7) * 128];
            value[0] = (byte)cursor;
            ring[(uint)cursor++ % (uint)retentionSlots] = value;
        }
        if (allocationDelay > 0) Thread.Sleep(allocationDelay);
        else Thread.SpinWait(128);
    }
    GC.KeepAlive(ring);
}

async Task AllocateAsync()
{
    byte[][] ring = new byte[retentionSlots][];
    int cursor = 0;
    try
    {
        while (!stop.IsCancellationRequested)
        {
            for (int i = 0; i < 256; i++)
            {
                byte[] value = new byte[4096 + (cursor & 7) * 128];
                value[0] = (byte)cursor;
                ring[(uint)cursor++ % (uint)retentionSlots] = value;
            }
            if (allocationDelay > 0) await Task.Delay(allocationDelay, stop.Token).ConfigureAwait(false);
            else await Task.Yield();
        }
    }
    catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
    GC.KeepAlive(ring);
}

int Number(string name, int fallback, int minimum, int maximum)
{
    if (!int.TryParse(values.GetValueOrDefault(name, fallback.ToString(CultureInfo.InvariantCulture)),
        NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < minimum || value > maximum)
        throw new ArgumentException("Invalid " + name);
    return value;
}
