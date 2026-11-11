/// <summary>Tests capacity semantics without requiring Linux or accepting synthetic evidence.</summary>
internal static partial class CapacityTests
{
    internal static void Options()
    {
        string[] basic = ["--remote-ip", "192.0.2.2", "--source-commit", new('a', 40),
            "--output", Path.GetFullPath("artifacts/capacity-test")];
        var options = CapacityOptions.Parse(basic);
        Require(options.DurationSeconds == 1800 && !options.Diagnostic && CapacityOptions.TargetTps == 20000,
            "The qualification target must be fixed");
        Reject([.. basic, "--duration-seconds", "5"]);
        Reject([.. basic, "--concurrency", "0"]);
        Reject([.. basic, "--concurrency", "9000"]);
        Reject([.. basic, "--concurrency", "512", "--queue-capacity", "32"]);
        Reject([.. basic, "--source-commit", new('b', 40)]);
        Reject([.. basic, "--typo", "true"]);
        Require(CapacityOptions.Parse([.. basic, "--duration-seconds", "2", "--diagnostic", "true"]).Diagnostic,
            "A short run must explicitly be diagnostic");
        void Reject(string[] args)
        {
            try { CapacityOptions.Parse(args); } catch (ArgumentException) { return; }
            throw new InvalidOperationException("Invalid capacity option accepted");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    internal static void Ledger()
    {
        CapacityLedger ledger = new(2, 1000);
        ledger.Admit(0); ledger.Complete(0, CapacityOutcome.Success, 0, 999);
        ledger.Admit(1); ledger.Complete(1, CapacityOutcome.Success, 0, 1000);
        ledger.Admit(2); ledger.Complete(2, CapacityOutcome.Success, 1999, 2000);
        ledger.Admit(3); ledger.Complete(3, CapacityOutcome.Cancelled, 1999, 2001);
        ledger.Admit(4); ledger.Complete(4, CapacityOutcome.Timeout, 1999, 2002);
        var counts = ledger.Snapshot();
        Require(counts.Reconciled && counts.Measured == 2 && counts.Late == 1 && counts.Failed == 2,
            "Exact endpoint is drain, not measured TPS; cancellation and timeout must remain failures");
        Require(counts.CompletedPerSecond.SequenceEqual([1L, 1L]), "Seconds must use completion time");
        ledger.Admit(0);
        Require(!ledger.Snapshot().Reconciled && ledger.Snapshot().Unresolved == 1,
            "Admitted but uncompleted work must invalidate reconciliation");
    }

    internal static void Workers() => WorkersAsync().GetAwaiter().GetResult();

    private static async Task WorkersAsync()
    {
        var result = await CapacityRunner.RunAsync(TimeSpan.FromMilliseconds(200), 4, async (_, ct) =>
        {
            await Task.Delay(300, ct);
            return CapacityOutcome.Success;
        }, CancellationToken.None);
        Require(result.Counts.Reconciled && result.Counts.Failed == 0 && result.Counts.Measured == 0
            && result.Counts.Late > 0 && !result.Aborted, "Deadline must drain rather than cancel active work");
        using CancellationTokenSource cancel = new();
        var cancelled = CapacityRunner.RunAsync(TimeSpan.FromSeconds(3), 2, async (_, ct) =>
        {
            cancel.Cancel();
            await Task.Delay(1000, ct);
            return CapacityOutcome.Success;
        }, cancel.Token);
        var stopped = await cancelled;
        Require(stopped.Aborted && stopped.Counts.Reconciled && stopped.Counts.Failed > 0,
            "External cancellation must preserve terminal accounting");
        var failed = await CapacityRunner.RunAsync(TimeSpan.FromMilliseconds(10), 1,
            (_, _) => throw new IOException("Injected"), CancellationToken.None);
        Require(failed.Counts.Reconciled && failed.Counts.Failed > 0 && failed.Counts.Measured == 0,
            "Transport exceptions must not be successful completions");
    }
}
