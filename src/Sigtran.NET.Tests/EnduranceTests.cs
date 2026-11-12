using System.Text.Json;

/// <summary>Offline endurance contracts never count synthetic test time as live qualification evidence.</summary>
internal static partial class EnduranceTests
{
    internal static void Options()
    {
        string[] basic = ["--remote-ip", "192.0.2.2", "--source-commit", new('a', 40),
            "--output", Path.GetFullPath("artifacts/endurance-test"), "--socket-profile", "lan-bounded-v1"];
        var options = EnduranceOptions.Parse(basic);
        Check(options.DurationSeconds == 259200 && !options.Diagnostic, "A full run must be 72 contiguous hours");
        Check(options.Session.SocketProfile == "lan-bounded-v1", "Socket profile must remain explicit");
        var faults = options.FaultPlan();
        Check(faults.Length == 9 && faults.Select(f => f.Kind).Distinct().Count() == 3
            && faults.All(f => f.HoldSeconds == 10) && faults.Select(f => f.OffsetSeconds).SequenceEqual(faults.Select(f => f.OffsetSeconds).Order()),
            "Nine scheduled incidents must cover all three fault families");
        Reject([.. basic, "--duration-seconds", "360"]);
        Reject([.. basic, "--diagnostic", "maybe"]);
        Reject([.. basic, "--diagnostic", "true", "--duration-seconds", "299"]);
        Reject([.. basic, "--diagnostic", "true", "--duration-seconds", "3601"]);
        Reject([.. basic, "--source-commit", new('b', 40)]);
        Reject([.. basic, "--concurrency", "1025"]);
        var diagnostic = EnduranceOptions.Parse([.. basic, "--diagnostic", "true", "--duration-seconds", "360"]);
        Check(diagnostic.Diagnostic && diagnostic.FaultPlan().Length == 3, "A smoke test cannot masquerade as a full run");
        using JsonDocument policy = JsonDocument.Parse(File.ReadAllText("eng/qualification/endurance-policy.json"));
        Check(policy.RootElement.GetProperty("id").GetString() == EnduranceOptions.Policy
            && policy.RootElement.GetProperty("minimumDurationSeconds").GetInt32() == EnduranceOptions.RequiredSeconds
            && policy.RootElement.GetProperty("maximumWorkingSetMiB").GetInt32() == EnduranceOptions.MaximumWorkingSetMiB,
            "The versioned policy and executable contract must agree");
        void Reject(string[] args)
        {
            try { EnduranceOptions.Parse(args); } catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid endurance options accepted");
        }
    }

    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    internal static void Ledger()
    {
        EnduranceLedger ledger = new(2, (long)uint.MaxValue + 10);
        Check(ledger.TryAdmit(0, 1, out var first) && first.Sequence > uint.MaxValue, "Generation must be 64-bit");
        Check(ledger.TryAdmit(1, 2, out var second), "Two slots must admit independently");
        ledger.BeginFault(1, 3);
        ledger.Complete(first, CapacityOutcome.Timeout, 4);
        Check(!ledger.TryAdmit(0, 5, out _), "The first fault outcome must fence a second failure cohort");
        ledger.Complete(second, CapacityOutcome.Success, 5);
        Check(ledger.Snapshot().FaultTerminals == 1 && ledger.Snapshot().UnexpectedFailures == 0, "Fault outcome must remain visible");
        for (int i = 0; i < 5; i++)
        {
            Check(ledger.TryAdmit(2, 6 + i, out var probe, recoveryProbe: true), "Drained probes must be admitted");
            ledger.Complete(probe, CapacityOutcome.Success, 7 + i);
        }
        ledger.EndFault(1);
        ledger.Resume();
        Check(ledger.Snapshot().Reconciled && ledger.Snapshot().RecoveredFaults == 1 && ledger.Snapshot().Pending == 0,
            "Recovery must preserve admission/terminal identity");
        try { ledger.Complete(first, CapacityOutcome.Success, 20); }
        catch (InvalidOperationException) { }
        Check(ledger.Fatal && ledger.Snapshot().AccountingViolations == 1, "Duplicate completion must invalidate the run");

        foreach (CapacityOutcome outcome in new[] { CapacityOutcome.Error, CapacityOutcome.Reject, CapacityOutcome.MalformedResult, CapacityOutcome.Cancelled })
        {
            EnduranceLedger invalid = new(1);
            invalid.TryAdmit(0, 0, out var admission);
            invalid.BeginFault(1, 1);
            invalid.Complete(admission, outcome, 2);
            Check(invalid.Fatal && invalid.Snapshot().UnexpectedFailures == 1, "Unexpected outcomes cannot be hidden by a fault window");
        }
    }

    internal static void ConcurrentLedger()
    {
        EnduranceLedger ledger = new(16);
        Parallel.For(0, 16, worker =>
        {
            for (int i = 0; i < 1000; i++)
            {
                Check(ledger.TryAdmit(worker, i, out var admission), "Each worker owns exactly one slot");
                ledger.Complete(admission, CapacityOutcome.Success, i + 1);
            }
        });
        var counts = ledger.Snapshot();
        Check(counts.Started == 16000 && counts.Successful == 16000 && counts.Reconciled
            && counts.Unresolved == 0 && counts.PeakPending <= 16, "Parallel accounting must reconcile without volume-dependent storage");
    }
}
