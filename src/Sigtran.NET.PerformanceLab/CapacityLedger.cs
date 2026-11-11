/// <summary>Separates valid in-window MAP results from every other terminal outcome.</summary>
internal enum CapacityOutcome { Success, Error, Reject, Timeout, Cancelled, TransportFailure, MalformedResult, DialogueClosed }

/// <summary>Bounds accounting memory by duration and operation kinds, never transaction volume.</summary>
internal sealed class CapacityLedger(int seconds, long frequency, long? endTicks = null)
{
    private readonly long[] _windows = new long[seconds > 0 ? seconds : throw new ArgumentOutOfRangeException(nameof(seconds))];
    private readonly long[] _started = new long[5];
    private readonly long[] _successes = new long[5];
    private readonly long[] _late = new long[5];
    private readonly long[] _outcomes = new long[8];
    private readonly long _frequency = frequency > 0 ? frequency : throw new ArgumentOutOfRangeException(nameof(frequency));
    private readonly long _endTicks = endTicks ?? checked(seconds * frequency);

    internal void Admit(int operation)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(operation);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(operation, 5);
        Interlocked.Increment(ref _started[operation]);
    }

    /// <summary>The caller owns one admission; completion is recorded exactly once in its worker's finally path.</summary>
    internal void Complete(int operation, CapacityOutcome outcome, long admittedTicks, long completedTicks)
    {
        if ((uint)operation >= 5 || (uint)outcome >= 8 || admittedTicks < 0 || completedTicks < admittedTicks)
            throw new ArgumentOutOfRangeException(nameof(operation));
        Interlocked.Increment(ref _outcomes[(int)outcome]);
        if (outcome != CapacityOutcome.Success) return;
        long second = completedTicks / _frequency;
        if (completedTicks < _endTicks && second < _windows.Length)
        {
            Interlocked.Increment(ref _successes[operation]);
            Interlocked.Increment(ref _windows[(int)second]);
        }
        else Interlocked.Increment(ref _late[operation]);
    }

    /// <summary>Called after all workers drain; intermediate snapshots are not qualification evidence.</summary>
    internal CapacityCounts Snapshot() => new(_started.ToArray(), _successes.ToArray(), _late.ToArray(),
        _outcomes.ToArray(), _windows.ToArray());
}

/// <summary>Retains both measured throughput and complete admission-to-terminal reconciliation.</summary>
internal sealed record CapacityCounts(long[] StartedByOperation, long[] MeasuredByOperation,
    long[] LateByOperation, long[] Outcomes, long[] CompletedPerSecond)
{
    internal long Started => StartedByOperation.Sum();
    internal long Measured => MeasuredByOperation.Sum();
    internal long Late => LateByOperation.Sum();
    internal long Failed => Outcomes.Skip(1).Sum();
    internal long Unresolved => Started - Outcomes.Sum();
    internal bool Reconciled => Unresolved == 0 && Outcomes[0] == Measured + Late
        && CompletedPerSecond.Sum() == Measured;
}
