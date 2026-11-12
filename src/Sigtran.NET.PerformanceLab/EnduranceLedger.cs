/// <summary>Identifies a worker-owned admission; sequence fencing rejects stale or duplicate completions.</summary>
internal readonly record struct EnduranceAdmission(int Worker, long Sequence, int Operation, long StartedTicks, bool RecoveryProbe);

/// <summary>Bounds live accounting by worker count, not billions of transactions or run duration.</summary>
internal sealed class EnduranceLedger
{
    private readonly object _sync = new();
    private readonly EnduranceAdmission?[] _slots;
    private readonly long[] _started = new long[5], _successes = new long[5], _outcomes = new long[8], _duringFault = new long[8];
    private readonly int _concurrency;
    private long _sequence;
    private int _pending, _peak, _violations, _recovered, _probeSuccesses;
    private long _faultStart, _faultFailures;
    private int? _faultId;
    private bool _paused, _fatal;

    internal EnduranceLedger(int concurrency, long initialSequence = 0)
    {
        if (concurrency is < 1 or > 1024 || initialSequence < 0) throw new ArgumentOutOfRangeException(nameof(concurrency));
        _concurrency = concurrency;
        _slots = new EnduranceAdmission?[concurrency + 1];
        _sequence = initialSequence;
    }

    internal bool Fatal { get { lock (_sync) return _fatal; } }
    internal int Pending { get { lock (_sync) return _pending; } }

    /// <summary>Reserves one fixed worker slot; the extra slot is only for serial, drained recovery probes.</summary>
    internal bool TryAdmit(int worker, long ticks, out EnduranceAdmission admission, bool recoveryProbe = false)
    {
        lock (_sync)
        {
            if (ticks < 0 || worker < 0 || worker > _concurrency || (worker == _concurrency) != recoveryProbe)
                throw new ArgumentOutOfRangeException(nameof(worker));
            admission = default;
            if (_fatal || (!recoveryProbe && _paused)) return false;
            if (recoveryProbe && (!_paused || _faultId is null || _pending != 0))
                throw new InvalidOperationException("Recovery probes require a paused, drained fault window.");
            if (_slots[worker] is not null) throw new InvalidOperationException("Worker already owns an admission.");
            long sequence = checked(_sequence + 1);
            int operation = (int)((sequence - 1) % 5);
            admission = new(worker, sequence, operation, ticks, recoveryProbe);
            _sequence = sequence;
            _slots[worker] = admission;
            _started[operation]++;
            _pending++;
            _peak = Math.Max(_peak, _pending);
            return true;
        }
    }

    /// <summary>Every admitted invoke enters this terminal path once, including fault timeouts and cancellation.</summary>
    internal void Complete(EnduranceAdmission admission, CapacityOutcome outcome, long ticks)
    {
        lock (_sync)
        {
            if ((uint)admission.Worker >= _slots.Length || (uint)outcome >= 8 || ticks < admission.StartedTicks
                || _slots[admission.Worker] is not { } owned || owned != admission)
            {
                _violations++;
                _fatal = _paused = true;
                throw new InvalidOperationException("Duplicate, stale or invalid terminal completion.");
            }
            _slots[admission.Worker] = null;
            _pending--;
            _outcomes[(int)outcome]++;
            if (outcome == CapacityOutcome.Success)
            {
                _successes[admission.Operation]++;
                if (admission.RecoveryProbe) _probeSuccesses++;
                return;
            }
            bool declaredFaultOutcome = !admission.RecoveryProbe && _faultId is not null && ticks >= _faultStart
                && outcome is CapacityOutcome.Timeout or CapacityOutcome.TransportFailure or CapacityOutcome.DialogueClosed;
            _paused = true; // The first fault outcome fences new admissions; at most one in-flight cohort can fail.
            if (declaredFaultOutcome)
            {
                _duringFault[(int)outcome]++;
                _faultFailures++;
            }
            else _fatal = true;
        }
    }

    /// <summary>Marks a known scheduled injection while traffic is in flight; this is classification, not proof of causation.</summary>
    internal void BeginFault(int id, long ticks)
    {
        lock (_sync)
        {
            if (_faultId is not null || id != _recovered + 1 || _pending == 0 || _fatal || ticks < 0)
                throw new InvalidOperationException("A fault requires ordered identity and active traffic.");
            _faultId = id;
            _faultStart = ticks;
            _faultFailures = 0;
            _probeSuccesses = 0;
        }
    }

    internal void Pause() { lock (_sync) _paused = true; }

    /// <summary>Requires five successful fresh probes after draining, without replaying any prior request.</summary>
    internal void EndFault(int id)
    {
        lock (_sync)
        {
            if (_faultId != id || !_paused || _pending != 0 || _probeSuccesses != 5 || _faultFailures > _concurrency || _fatal)
                throw new InvalidOperationException("Fault recovery is not reconciled and probed.");
            _faultId = null;
            _recovered++;
        }
    }

    /// <summary>The controller acknowledges capture stop before ordinary traffic may resume.</summary>
    internal void Resume()
    {
        lock (_sync)
        {
            if (_faultId is not null || _fatal) throw new InvalidOperationException("Cannot resume an unresolved fault.");
            _paused = false;
        }
    }

    /// <summary>A short lock makes each checkpoint consistent without retaining individual transaction objects.</summary>
    internal EnduranceCounts Snapshot()
    {
        lock (_sync) return new(_started.ToArray(), _successes.ToArray(), _outcomes.ToArray(), _duringFault.ToArray(),
            _pending, _peak, _violations, _recovered, _faultId, _fatal);
    }
}

/// <summary>Preserves every terminal outcome; fault-window outcomes are not successes or hidden losses.</summary>
internal sealed record EnduranceCounts(long[] StartedByOperation, long[] SuccessfulByOperation, long[] Outcomes,
    long[] FaultWindowOutcomes, int Pending, int PeakPending, int AccountingViolations, int RecoveredFaults,
    int? ActiveFaultId, bool Fatal)
{
    internal long Started => StartedByOperation.Sum();
    internal long Successful => SuccessfulByOperation.Sum();
    internal long Terminal => Outcomes.Sum();
    internal long FaultTerminals => FaultWindowOutcomes.Sum();
    internal long UnexpectedFailures => Outcomes.Skip(1).Sum() - FaultTerminals;
    internal long Unresolved => Started - Terminal;
    internal bool Reconciled => AccountingViolations == 0 && Unresolved == Pending
        && Outcomes[0] == Successful && FaultWindowOutcomes[0] == 0
        && FaultWindowOutcomes.Zip(Outcomes).All(pair => pair.First <= pair.Second);
}
