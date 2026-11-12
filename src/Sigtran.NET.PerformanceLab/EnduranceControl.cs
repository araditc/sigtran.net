using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>A sequenced private control-file command; it is not a public network authorization API.</summary>
internal sealed record EnduranceCommand(int Sequence, int Id, string Action, string Nonce)
{
    internal static EnduranceCommand Parse(byte[] bytes)
    {
        if (bytes.Length is < 1 or > 4096) throw new InvalidDataException("Control frame size is invalid.");
        using JsonDocument document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        string[] names = root.EnumerateObject().Select(p => p.Name).ToArray();
        if (names.Length != 4 || !names.Order().SequenceEqual(new[] { "Action", "Id", "Nonce", "Sequence" }.Order()))
            throw new InvalidDataException("Unknown or duplicate control fields.");
        var value = new EnduranceCommand(root.GetProperty("Sequence").GetInt32(), root.GetProperty("Id").GetInt32(),
            root.GetProperty("Action").GetString()!, root.GetProperty("Nonce").GetString()!);
        if (value.Sequence is < 1 or > 27 || value.Id is < 1 or > 9
            || value.Action is not ("begin" or "release" or "capture-stopped")
            || !Regex.IsMatch(value.Nonce ?? "", "\\A[0-9a-f]{32}\\z"))
            throw new InvalidDataException("Invalid control identity/action/nonce.");
        return value;
    }
}

/// <summary>Coordinates real injected faults with a durable SDK-side intent, drain, fresh probes and capture stop.</summary>
internal sealed class EnduranceControl(EnduranceOptions options, CapacitySession session, EnduranceJournal journal)
{
    private long _observedFaults;
    private int? _window;
    internal EnduranceClock? Clock { get; private set; }

    internal async Task RunAsync(EnduranceExecution execution, CancellationToken ct)
    {
        Clock = execution.Clock;
        int sequence = 0;
        string? lastDigest = null, nonce = null;
        HashSet<string> nonces = [];
        double began = 0;
        var plan = options.FaultPlan();
        string commandPath = Path.Combine(options.Session.Output, "control-command.json");
        while (!ct.IsCancellationRequested)
        {
            ObserveFaults(execution.Clock);
            if (File.Exists(commandPath))
            {
                if (new FileInfo(commandPath).Length > 4096 || (File.GetAttributes(commandPath) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Invalid control file.");
                byte[] bytes = await File.ReadAllBytesAsync(commandPath, ct).ConfigureAwait(false);
                string digest = Convert.ToHexStringLower(SHA256.HashData(bytes));
                EnduranceCommand command = EnduranceCommand.Parse(bytes);
                if (command.Sequence == sequence && digest == lastDigest)
                { await Task.Delay(50, ct).ConfigureAwait(false); continue; }
                string expectedAction = ((command.Sequence - 1) % 3) switch { 0 => "begin", 1 => "release", _ => "capture-stopped" };
                int index = (command.Sequence - 1) / 3;
                if (command.Sequence != sequence + 1 || index >= plan.Length || command.Id != plan[index].Id || command.Action != expectedAction)
                    throw new InvalidDataException("Control sequence skipped, repeated or changed the declared plan.");
                var incident = plan[index];
                if (command.Action == "begin")
                {
                    if (!nonces.Add(command.Nonce)) throw new InvalidDataException("Fault nonce was reused.");
                    while (execution.Clock.ElapsedSeconds < incident.OffsetSeconds)
                        await Task.Delay(10, ct).ConfigureAwait(false);
                    ObserveFaults(execution.Clock);
                    while (!execution.Ledger.TryBeginFault(command.Id, execution.Clock.ElapsedTicks))
                    { ObserveFaults(execution.Clock); await Task.Delay(10, ct).ConfigureAwait(false); }
                    _window = command.Id;
                    nonce = command.Nonce;
                    began = execution.Clock.ElapsedSeconds;
                    journal.Append(new { Kind = "fault-begin", ElapsedSeconds = began, Incident = incident,
                        command.Nonce, Counts = execution.Ledger.Snapshot(), Stack = session.Snapshot() });
                }
                else
                {
                    if (nonce != command.Nonce || _window != command.Id)
                        throw new InvalidDataException("Fault identity changed during recovery.");
                    if (command.Action == "release")
                    {
                        execution.Ledger.Pause();
                        using CancellationTokenSource recovery = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        double remaining = EnduranceOptions.RecoverySeconds - (execution.Clock.ElapsedSeconds - began);
                        if (remaining <= 0) throw new TimeoutException("Fault recovery budget expired.");
                        recovery.CancelAfter(TimeSpan.FromSeconds(remaining));
                        while (execution.Ledger.Pending != 0 || !session.IsActive)
                            await Task.Delay(10, recovery.Token).ConfigureAwait(false);
                        journal.Append(new { Kind = "recovery-probes-start", ElapsedSeconds = execution.Clock.ElapsedSeconds,
                            ObservedAtUtc = DateTimeOffset.UtcNow, Incident = incident, command.Nonce,
                            LocalPort = session.LocalPort, Counts = execution.Ledger.Snapshot() });
                        for (int i = 0; i < 5; i++) await execution.ProbeAsync(recovery.Token).ConfigureAwait(false);
                        while (session.Snapshot().Active != 0 || session.Snapshot().Pending != 0)
                            await Task.Delay(10, recovery.Token).ConfigureAwait(false);
                        ObserveFaults(execution.Clock);
                        execution.Ledger.EndFault(command.Id);
                        journal.Append(new { Kind = "fault-recovered", ElapsedSeconds = execution.Clock.ElapsedSeconds,
                            ObservedAtUtc = DateTimeOffset.UtcNow,
                            Incident = incident, command.Nonce, ProbeSuccesses = 5, Counts = execution.Ledger.Snapshot(), Stack = session.Snapshot() });
                    }
                    else
                    {
                        if (execution.Clock.ElapsedSeconds - began > EnduranceOptions.RecoverySeconds)
                            throw new TimeoutException("Capture acknowledgement exceeded recovery budget.");
                        ObserveFaults(execution.Clock);
                        journal.Append(new { Kind = "fault-resumed", ElapsedSeconds = execution.Clock.ElapsedSeconds,
                            Incident = incident, command.Nonce, Counts = execution.Ledger.Snapshot() });
                        _window = null;
                        execution.Ledger.Resume();
                    }
                }
                sequence = command.Sequence;
                lastDigest = digest;
                WriteAtomic(Path.Combine(options.Session.Output, "control-ack.json"), new { command.Sequence,
                    command.Id, command.Action, command.Nonce, ElapsedSeconds = execution.Clock.ElapsedSeconds });
            }
            await Task.Delay(50, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Polling lifecycle counters avoids allocating a runtime event object for every DATA transfer.</summary>
    internal void ObserveFaults(EnduranceClock? clock)
    {
        long current = session.Snapshot().Faults;
        if (current < _observedFaults) throw new InvalidDataException("Runtime fault counter decreased.");
        if (current == _observedFaults) return;
        journal.Append(new { Kind = "transport-fault-observed", ElapsedSeconds = clock?.ElapsedSeconds,
            FaultId = _window, Previous = _observedFaults, Current = current });
        _observedFaults = current;
        if (_window is null) throw new InvalidOperationException("Transport fault outside a declared injection window.");
    }

    internal static void WriteAtomic(string path, object value)
    {
        string pending = path + ".pending-" + Guid.NewGuid().ToString("N");
        try
        {
            using (FileStream stream = new(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { JsonSerializer.Serialize(stream, value); stream.Flush(flushToDisk: true); }
            File.Move(pending, path, overwrite: true);
        }
        finally { if (File.Exists(pending)) File.Delete(pending); }
    }
}
