/// <summary>Keeps constant-size resource/progress aggregates; no per-second or per-request list grows with duration.</summary>
internal sealed class EnduranceMonitor(int concurrency, int queueCapacity, double baselineAfterSeconds)
{
    private readonly object _sync = new();
    private readonly HashSet<string> _violations = [];
    private int _observations, _peakPending, _peakActive, _peakOutbound, _peakInbound;
    private double _lastElapsed = -1, _maximumGap, _lastProgress, _maximumNoProgress;
    private long _lastSuccesses, _peakRss, _peakManaged, _peakHeap, _growth;
    private long? _baseline;

    /// <summary>Resource violations are recorded before failing execution, so the failed bound remains inspectable.</summary>
    internal void Observe(EnduranceSample sample, CapacityStackMetrics stack)
    {
        lock (_sync)
        {
            if (!double.IsFinite(sample.ElapsedSeconds) || sample.ElapsedSeconds < 0 || sample.ElapsedSeconds <= _lastElapsed)
                throw new InvalidDataException("Non-monotonic endurance observations.");
            if (_lastElapsed >= 0) _maximumGap = Math.Max(_maximumGap, sample.ElapsedSeconds - _lastElapsed);
            _lastElapsed = sample.ElapsedSeconds;
            _observations++;
            var resource = sample.Resources;
            // Preserve invalid samples in the observation log, but never hide them behind peak aggregates.
            if (resource.WorkingSetBytes < 0 || resource.PeakWorkingSetBytes < resource.WorkingSetBytes
                || resource.ManagedBytes < 0 || resource.HeapSizeBytes < 0 || resource.FragmentedBytes < 0
                || resource.AllocatedBytes < 0 || !double.IsFinite(resource.ProcessCpuSeconds) || resource.ProcessCpuSeconds < 0
                || resource.Gen0 < 0 || resource.Gen1 < 0 || resource.Gen2 < 0 || resource.ThreadPoolThreads < 0
                || resource.ThreadPoolPending < 0 || resource.ThreadPoolCompleted < 0)
                _violations.Add("invalid-resource-observation");
            _peakRss = Math.Max(_peakRss, sample.Resources.PeakWorkingSetBytes);
            _peakManaged = Math.Max(_peakManaged, sample.Resources.ManagedBytes);
            _peakHeap = Math.Max(_peakHeap, sample.Resources.HeapSizeBytes);
            _peakPending = Math.Max(_peakPending, sample.Counts.PeakPending);
            _peakActive = Math.Max(_peakActive, stack.Active);
            _peakOutbound = Math.Max(_peakOutbound, Math.Max(stack.OutboundQueue, stack.PeakOutboundQueue));
            _peakInbound = Math.Max(_peakInbound, Math.Max(stack.InboundQueue, stack.PeakInboundQueue));
            if (_baseline is null && sample.ElapsedSeconds >= baselineAfterSeconds) _baseline = sample.Resources.WorkingSetBytes;
            if (_baseline is { } baseline) _growth = Math.Max(_growth, sample.Resources.WorkingSetBytes - baseline);
            if (sample.Counts.Successful > _lastSuccesses)
            { _lastProgress = sample.ElapsedSeconds; _lastSuccesses = sample.Counts.Successful; }
            _maximumNoProgress = Math.Max(_maximumNoProgress, sample.ElapsedSeconds - _lastProgress);
            const long mib = 1024 * 1024;
            if (_peakRss > EnduranceOptions.MaximumWorkingSetMiB * mib) _violations.Add("working-set-bound");
            if (Math.Max(_peakManaged, _peakHeap) > EnduranceOptions.MaximumManagedHeapMiB * mib) _violations.Add("managed-heap-bound");
            if (_growth > EnduranceOptions.MaximumResidentGrowthMiB * mib) _violations.Add("resident-growth-bound");
            if (!sample.Counts.Reconciled || sample.Counts.Pending < 0 || _peakPending > concurrency) _violations.Add("terminal-accounting-bound");
            if (stack.Active < 0 || stack.Pending < 0 || stack.OutboundQueue < 0 || stack.InboundQueue < 0
                || _peakActive > queueCapacity || stack.Pending > queueCapacity || _peakOutbound > queueCapacity || _peakInbound > queueCapacity)
                _violations.Add("queue-context-bound");
            if (stack.Malformed != 0 || stack.DroppedEvents != 0 || stack.ForeignTransfers != 0
                || stack.UnsolicitedInvokes != 0 || stack.SccpReturns != 0 || !stack.ReadersHealthy)
                _violations.Add("unexpected-protocol-work");
            if (_maximumNoProgress > 120) _violations.Add("successful-progress-stall");
            if (_violations.Count != 0) throw new InvalidOperationException("Endurance resource/progress bound violated.");
        }
    }

    internal EnduranceResourceSummary Snapshot()
    {
        lock (_sync) return new(_observations, _lastElapsed, _maximumGap, _maximumNoProgress,
            _peakRss, _peakManaged, _peakHeap, _baseline, baselineAfterSeconds, _growth,
            _peakPending, _peakActive, _peakOutbound, _peakInbound, _violations.Count == 0, _violations.Order().ToArray());
    }
}

/// <summary>OS peak RSS and sampled managed-memory/queue observations have explicitly different confidence boundaries.</summary>
internal sealed record EnduranceResourceSummary(int Observations, double LastElapsedSeconds,
    double MaximumObservationGapSeconds, double MaximumNoProgressSeconds, long PeakWorkingSetBytes,
    long MaximumObservedManagedBytes, long MaximumObservedHeapBytes, long? BaselineWorkingSetBytes,
    double BaselineAfterSeconds, long MaximumResidentGrowthBytes, int PeakPending, int PeakActive,
    int PeakOutboundQueue, int PeakInboundQueue, bool WithinBounds, string[] Violations);
