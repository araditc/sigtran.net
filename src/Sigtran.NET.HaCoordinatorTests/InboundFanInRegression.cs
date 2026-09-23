using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class InboundFanInRegression
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        RunAsync().GetAwaiter().GetResult();
    }

    private static async Task RunAsync()
    {
        await RunCaseAsync(
            "Inbound fan-in rejects duplicate association names",
            RejectsDuplicateAssociationNames).ConfigureAwait(false);
        await RunCaseAsync(
            "Inbound fan-in preserves association identity",
            PreservesAssociationIdentity).ConfigureAwait(false);
        await RunCaseAsync(
            "Inbound fan-in keeps healthy lanes alive after peer fault",
            KeepsHealthyLaneAliveAfterPeerFault).ConfigureAwait(false);
        await RunCaseAsync(
            "Inbound fan-in applies bounded backpressure",
            AppliesBoundedBackpressure).ConfigureAwait(false);
        await RunCaseAsync(
            "Inbound fan-in disposal cancels blocked receives",
            DisposalCancelsBlockedReceives).ConfigureAwait(false);
    }

    private static Task RejectsDuplicateAssociationNames()
    {
        FakeInboundNetwork first = new();
        FakeInboundNetwork second = new();
        Throws<ArgumentException>(() =>
            _ = new M3uaHaInboundFanIn(
                [
                    new M3uaInboundSource("assoc-a", first),
                    new M3uaInboundSource("ASSOC-A", second)
                ],
                capacity: 4));
        Throws<ArgumentOutOfRangeException>(() =>
            _ = new M3uaHaInboundFanIn(
                [new M3uaInboundSource("assoc-a", first)],
                capacity: 0));
        return Task.CompletedTask;
    }

    private static async Task PreservesAssociationIdentity()
    {
        FakeInboundNetwork first = new();
        FakeInboundNetwork second = new();
        await using M3uaHaInboundFanIn fanIn = new(
            [
                new M3uaInboundSource("assoc-a", first),
                new M3uaInboundSource("assoc-b", second)
            ],
            capacity: 4);

        second.Enqueue(CreateTransfer(7, 0xB2));
        M3uaInboundTransfer inbound = await fanIn.ReceiveAsync()
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5))
            .ConfigureAwait(false);

        Equal("assoc-b", inbound.AssociationName, "Fan-in must retain the originating association identity.");
        Equal((byte)7, inbound.Message.RoutingLabel.SignallingLinkSelection, "SLS changed during inbound fan-in.");
        Equal((byte)0xB2, inbound.Message.UserPayload.Span[0], "Payload changed during inbound fan-in.");

        M3uaInboundAssociationSnapshot snapshot = fanIn.GetSnapshot()
            .Single(item => item.AssociationName == "assoc-b");
        Equal(1L, snapshot.ReceivedTransfers, "Per-association receive accounting mismatch.");
        Equal(M3uaInboundAssociationState.Running, snapshot.State, "Healthy association should remain running.");
    }

    private static async Task KeepsHealthyLaneAliveAfterPeerFault()
    {
        FakeInboundNetwork failed = new();
        FakeInboundNetwork healthy = new();
        await using M3uaHaInboundFanIn fanIn = new(
            [
                new M3uaInboundSource("failed", failed),
                new M3uaInboundSource("healthy", healthy)
            ],
            capacity: 4);

        failed.Fail(new IOException("synthetic peer reset"));
        await WaitUntilAsync(
            () => fanIn.GetSnapshot().Single(item => item.AssociationName == "failed").State
                == M3uaInboundAssociationState.Faulted,
            "Faulted inbound lane was not isolated.").ConfigureAwait(false);

        healthy.Enqueue(CreateTransfer(2, 0x21));
        M3uaInboundTransfer inbound = await fanIn.ReceiveAsync()
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(5))
            .ConfigureAwait(false);

        Equal("healthy", inbound.AssociationName, "A peer-lane fault must not terminate healthy inbound lanes.");
        M3uaInboundAssociationSnapshot failedSnapshot = fanIn.GetSnapshot()
            .Single(item => item.AssociationName == "failed");
        Equal(M3uaInboundAssociationState.Faulted, failedSnapshot.State, "Faulted association state mismatch.");
        Equal("synthetic peer reset", failedSnapshot.LastFault, "Fault detail must remain association-scoped.");
    }

    private static async Task AppliesBoundedBackpressure()
    {
        FakeInboundNetwork source = new();
        await using M3uaHaInboundFanIn fanIn = new(
            [new M3uaInboundSource("assoc-a", source)],
            capacity: 1);

        source.Enqueue(CreateTransfer(1, 0x11));
        source.Enqueue(CreateTransfer(1, 0x12));

        await WaitUntilAsync(
            () => fanIn.GetSnapshot().Single().ReceivedTransfers == 1,
            "First inbound transfer was not published.").ConfigureAwait(false);
        await Task.Delay(100).ConfigureAwait(false);
        Equal(
            1L,
            fanIn.GetSnapshot().Single().ReceivedTransfers,
            "A full aggregate channel must backpressure the source pump instead of growing unbounded.");

        M3uaInboundTransfer first = await fanIn.ReceiveAsync().ConfigureAwait(false);
        Equal((byte)0x11, first.Message.UserPayload.Span[0], "First bounded transfer mismatch.");

        await WaitUntilAsync(
            () => fanIn.GetSnapshot().Single().ReceivedTransfers == 2,
            "Second inbound transfer did not advance after pressure cleared.").ConfigureAwait(false);
        M3uaInboundTransfer second = await fanIn.ReceiveAsync().ConfigureAwait(false);
        Equal((byte)0x12, second.Message.UserPayload.Span[0], "Second bounded transfer mismatch.");
    }

    private static async Task DisposalCancelsBlockedReceives()
    {
        FakeInboundNetwork first = new();
        FakeInboundNetwork second = new();
        M3uaHaInboundFanIn fanIn = new(
            [
                new M3uaInboundSource("assoc-a", first),
                new M3uaInboundSource("assoc-b", second)
            ],
            capacity: 2);

        await fanIn.DisposeAsync().ConfigureAwait(false);
        M3uaInboundAssociationSnapshot[] snapshots = fanIn.GetSnapshot().ToArray();
        Equal(2, snapshots.Length, "Disposal snapshot association count mismatch.");
        Equal(
            2,
            snapshots.Count(item => item.State == M3uaInboundAssociationState.Stopped),
            "Orderly disposal must stop every blocked inbound pump.");
        await ThrowsAsync<ObjectDisposedException>(
            () => fanIn.ReceiveAsync().AsTask()).ConfigureAwait(false);
    }

    private static Mtp3TransferMessage CreateTransfer(byte sls, byte payload) =>
        new(
            new Mtp3ServiceInformationOctet(
                Mtp3ServiceIndicator.Sccp,
                networkIndicator: 2),
            new Mtp3RoutingLabel(
                destinationPointCode: 1234,
                originatingPointCode: 4321,
                signallingLinkSelection: sls),
            new byte[] { payload },
            routingContext: 100);

    private static async Task WaitUntilAsync(
        Func<bool> predicate,
        string failureMessage)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (!predicate())
        {
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TimeoutException(failureMessage);
            }

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private static async Task RunCaseAsync(string name, Func<Task> test)
    {
        try
        {
            await test().ConfigureAwait(false);
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
            throw;
        }
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }

    private static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private static async Task ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Expected exception {typeof(TException).Name}.");
    }

    private sealed class FakeInboundNetwork : IMtp3Network
    {
        private readonly Channel<Mtp3TransferMessage> _inbound =
            Channel.CreateUnbounded<Mtp3TransferMessage>();

        public ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            ct.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }

        public ValueTask<Mtp3TransferMessage> ReceiveAsync(
            CancellationToken ct = default)
        {
            return _inbound.Reader.ReadAsync(ct);
        }

        internal void Enqueue(Mtp3TransferMessage message)
        {
            if (!_inbound.Writer.TryWrite(message))
            {
                throw new InvalidOperationException("Synthetic inbound lane rejected a transfer.");
            }
        }

        internal void Fail(Exception error)
        {
            if (!_inbound.Writer.TryComplete(error))
            {
                throw new InvalidOperationException("Synthetic inbound lane was already completed.");
            }
        }
    }
}
