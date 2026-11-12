using System.Diagnostics;
using System.Globalization;

/// <summary>Samples cumulative runtime and host counters without tracing individual transactions.</summary>
internal sealed class CapacityDiagnostics(int maximumSamples, TimeSpan? interval = null) : IAsyncDisposable
{
    private readonly List<CapacityObservation> _observations = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly TimeSpan _interval = interval ?? TimeSpan.FromSeconds(1);
    private Task? _task;
    private long _origin;
    private TimeSpan _initialPause;
    private TimeSpan _initialCpu;
    private long _initialWorkItems;
    private int[] _initialCollections = [];

    internal void Start(long origin)
    {
        if (maximumSamples < 2 || _interval <= TimeSpan.Zero || _task is not null)
            throw new InvalidOperationException("Invalid or already-started capacity diagnostics.");
        _origin = origin;
        _initialPause = GC.GetTotalPauseDuration();
        using Process process = Process.GetCurrentProcess();
        _initialCpu = process.TotalProcessorTime;
        _initialWorkItems = ThreadPool.CompletedWorkItemCount;
        _initialCollections = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];
        Capture();
        _task = SampleAsync();
    }

    internal async Task<CapacityObservation[]> StopAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        if (_task is not null) await _task.ConfigureAwait(false);
        if (_observations.Count < maximumSamples && _task is not null) Capture();
        return _observations.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        if (_task is not null) await _task.ConfigureAwait(false);
        _stop.Dispose();
    }

    private async Task SampleAsync()
    {
        using PeriodicTimer timer = new(_interval);
        try
        {
            while (_observations.Count < maximumSamples - 1
                && await timer.WaitForNextTickAsync(_stop.Token).ConfigureAwait(false)) Capture();
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }

    private void Capture()
    {
        using Process process = Process.GetCurrentProcess();
        // Actual monotonic offsets expose delayed timer callbacks; sample indices are never seconds.
        _observations.Add(new((Stopwatch.GetTimestamp() - _origin) / (double)Stopwatch.Frequency,
            (GC.GetTotalPauseDuration() - _initialPause).TotalMilliseconds,
            GC.CollectionCount(0) - _initialCollections[0], GC.CollectionCount(1) - _initialCollections[1],
            GC.CollectionCount(2) - _initialCollections[2], ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount, ThreadPool.CompletedWorkItemCount - _initialWorkItems,
            (process.TotalProcessorTime - _initialCpu).TotalSeconds,
            ReadLinux("/proc/net/sctp/snmp", ParseSctp), ReadLinux("/proc/stat", ParseHostCpu)));
    }

    private static T? ReadLinux<T>(string path, Func<string, T?> parse) where T : class
    {
        if (!OperatingSystem.IsLinux()) return null;
        try { return parse(File.ReadAllText(path)); }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    /// <summary>These are network-namespace counters, not a claim of association-only attribution.</summary>
    internal static Dictionary<string, long> ParseSctp(string text)
    {
        string[] names = ["SctpT3Retransmits", "SctpFastRetransmits", "SctpInPktDiscards",
            "SctpInDataChunkDiscards", "SctpOutSCTPPacks", "SctpInSCTPPacks", "SctpCurrEstab"];
        Dictionary<string, long> counters = new(StringComparer.Ordinal);
        foreach (string line in text.Split('\n'))
        {
            string[] fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 2 && names.Contains(fields[0], StringComparer.Ordinal)
                && long.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out long value))
                counters[fields[0]] = value;
        }
        return counters;
    }

    internal static CapacityHostCpu? ParseHostCpu(string text)
    {
        string[] fields = text.Split('\n')[0].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length < 9 || fields[0] != "cpu") return null;
        long[] ticks = new long[8];
        for (int i = 0; i < ticks.Length; i++)
            if (!long.TryParse(fields[i + 1], NumberStyles.None, CultureInfo.InvariantCulture, out ticks[i])) return null;
        // Guest fields overlap user/nice; adding them would double-count host time.
        return new(ticks[0] + ticks[1] + ticks[2] + ticks[5] + ticks[6], ticks[3], ticks[4], ticks[7]);
    }
}

/// <summary>Cumulative values retain timing and permit interval deltas without profiler attachment.</summary>
internal sealed record CapacityObservation(double ElapsedSeconds, double GcPauseMilliseconds,
    int Gen0, int Gen1, int Gen2, int ThreadPoolThreads, long ThreadPoolPending, long ThreadPoolCompleted,
    double ProcessCpuSeconds, Dictionary<string, long>? HostSctp, CapacityHostCpu? HostCpu);

/// <summary>Linux aggregate CPU ticks; zero steal does not prove a dedicated hypervisor.</summary>
internal sealed record CapacityHostCpu(long BusyTicks, long IdleTicks, long IoWaitTicks, long StealTicks);
