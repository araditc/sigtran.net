using System.Security.Cryptography;
using System.Text.Json;

internal static partial class EnduranceTests
{
    internal static void Journal()
    {
        string root = Path.Combine(Path.GetTempPath(), "sigtran-endurance-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, "checkpoints.jsonl");
        try
        {
            using (EnduranceJournal journal = new(path))
            {
                journal.Append(new { Kind = "start", Counter = (long)uint.MaxValue + 5 });
                journal.Append(new { Kind = "checkpoint", Counter = (long)uint.MaxValue + 10 });
                try { using EnduranceJournal duplicate = new(path); throw new InvalidOperationException("Existing journal was overwritten"); }
                catch (IOException) { }
            }
            string previous = new('0', 64);
            int sequence = 0;
            foreach (string line in File.ReadLines(path))
            {
                using JsonDocument document = JsonDocument.Parse(line);
                var frame = document.RootElement;
                Check(frame.GetProperty("Sequence").GetInt32() == ++sequence && frame.GetProperty("PreviousSha256").GetString() == previous,
                    "Journal sequence or predecessor mismatch");
                byte[] bytes = Convert.FromBase64String(frame.GetProperty("PayloadBase64").GetString()!);
                byte[] digestInput = [..Convert.FromHexString(previous), ..bytes];
                previous = Convert.ToHexStringLower(SHA256.HashData(digestInput));
                Check(frame.GetProperty("Sha256").GetString() == previous, "Journal must hash original payload bytes");
            }
            using EnduranceJournal bounded = new(Path.Combine(root, "bounded.jsonl"), 256);
            try { bounded.Append(new { Data = new string('x', 1024) }); throw new InvalidOperationException("Journal exceeded its disk bound"); }
            catch (IOException) { }
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    internal static void Runner() => RunnerAsync().GetAwaiter().GetResult();

    private static async Task RunnerAsync()
    {
        var settings = new EnduranceRunSettings(TimeSpan.FromMilliseconds(100), 4, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(2));
        EnduranceLedger ledger = new(4);
        int samples = 0;
        var result = await EnduranceRunner.RunAsync(settings, ledger, async (_, ct) =>
        { await Task.Delay(250, ct); return CapacityOutcome.Success; }, () => true,
            sample => { Check(sample.Counts.Reconciled, "Every live ledger snapshot must reconcile"); samples++; });
        Check(result.Counts.Reconciled && result.Counts.Unresolved == 0 && result.Counts.Pending == 0
            && result.MeasuredByOperation.Sum() == 0 && result.LateSuccesses == 4 && !result.Aborted && samples > 0,
            "Deadline results must drain, remain late and never disappear");

        TaskCompletionSource admitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenSource cancel = new();
        var cancelled = EnduranceRunner.RunAsync(settings with { Duration = TimeSpan.FromSeconds(2) }, new(4), async (_, ct) =>
        { admitted.TrySetResult(); await Task.Delay(500, ct); return CapacityOutcome.Success; }, () => true, _ => { }, cancel.Token);
        await admitted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancel.Cancel();
        result = await cancelled;
        Check(result.Aborted && result.Counts.Reconciled && result.Counts.Outcomes[(int)CapacityOutcome.Cancelled] > 0
            && result.Counts.Unresolved == 0, "External cancellation must leave every admitted request terminal");
    }
}
