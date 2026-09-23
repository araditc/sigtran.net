using System.Runtime.CompilerServices;
using System.Threading.Channels;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class RuntimeFanInTests
{
    [ModuleInitializer]
    internal static void RunAll()
    {
        Run("HA runtime fan-in rejects duplicate lane names", DuplicateLaneNamesFailClosedAsync);
        Run("HA runtime fan-in isolates startup failure", StartupFailureDoesNotBlockHealthyLaneAsync);
        Run("HA runtime fan-in does not wait for a slow peer lane", SlowStartupDoesNotGateHealthyLaneAsync);
        Run("HA runtime fan-in applies bounded aggregate backpressure", BoundedFanInPreservesAssociationIdentityAsync);
        Run("HA runtime fan-in isolates terminal lane fault", TerminalLaneFaultDoesNotStopHealthyLaneAsync);
    }

    private static async Task DuplicateLaneNamesFailClosedAsync()
    {
        FakeRuntimeLane first = new("dup");
        FakeRuntimeLane second = new("DUP");

        await ThrowsAsync<ArgumentException>(() =>
        {
            _ = new M3uaHaRuntimeSupervisor([first, second], inboundCapacity: 1);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private static async Task StartupFailureDoesNotBlockHealthyLaneAsync()
    {
        FakeRuntimeLane failed = new("failed", failStart: true);
        FakeRuntimeLane healthy = new("healthy");
        await using M3uaHaRuntimeSupervisor supervisor = new([failed, healthy], inboundCapacity: 2);

        await supervisor.StartAsync().ConfigureAwait(false);

        M3uaHaRuntimeSupervisorSnapshot started = supervisor.GetSnapshot();
        Equal(M3uaRuntimeState.Faulted, Lane(started, "failed").State, "A failed startup lane must remain faulted.");
        Equal(M3uaRuntimeState.Active, Lane(started, "healthy").State, "A healthy lane must activate independently.");

        healthy.Emit(CreateTransfer(sls: 3, routingContext: 100));
        M3uaHaInboundTransfer inbound = await supervisor.ReceiveAsync().ConfigureAwait(false);
        Equal("healthy", inbound.AssociationName, "Inbound fan-in must preserve the source association name.");
        Equal((byte)3, inbound.Message.RoutingLabel.SignallingLinkSelection, "Healthy-lane traffic must remain available after another lane fails startup.");
    }

    private static async Task SlowStartupDoesNotGateHealthyLaneAsync()
    {
        FakeRuntimeLane slow = new("slow", stallStart: true);
        FakeRuntimeLane healthy = new("healthy");
        await using M3uaHaRuntimeSupervisor supervisor = new([slow, healthy], inboundCapacity: 2);
        using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(500));

        await supervisor.StartAsync(timeout.Token).ConfigureAwait(false);

        M3uaHaRuntimeSupervisorSnapshot started = supervisor.GetSnapshot();
        Equal(M3uaRuntimeState.Starting, Lane(started, "slow").State, "A slow lane may remain in startup while another association becomes usable.");
        Equal(M3uaRuntimeState.Active, Lane(started, "healthy").State, "A healthy lane must not wait for an unrelated peer startup attempt.");

        healthy.Emit(CreateTransfer(sls: 4, routingContext: 100));
        M3uaHaInboundTransfer inbound = await supervisor.ReceiveAsync(timeout.Token).ConfigureAwait(false);
        Equal("healthy", inbound.AssociationName, "Healthy traffic must flow while another association is still starting.");
    }

    private static async Task BoundedFanInPreservesAssociationIdentityAsync()
    {
        FakeRuntimeLane a = new("a");
        FakeRuntimeLane b = new("b");
        await using M3uaHaRuntimeSupervisor supervisor = new([a, b], inboundCapacity: 1);

        await supervisor.StartAsync().ConfigureAwait(false);

        a.Emit(CreateTransfer(sls: 1, routingContext: 100));
        b.Emit(CreateTransfer(sls: 2, routingContext: 200));

        await WaitUntilAsync(
            () => supervisor.GetSnapshot().PendingInboundTransfers == 2,
            "Both accepted lane transfers should be visible while one producer is backpressured by capacity=1.")
            .ConfigureAwait(false);

        M3uaHaRuntimeSupervisorSnapshot pressured = supervisor.GetSnapshot();
        Equal(1, pressured.InboundCapacity, "The supervisor must retain the configured aggregate inbound capacity.");
        Equal(2, pressured.PendingInboundTransfers, "Pending accounting must include the queued transfer and the blocked producer.");

        M3uaHaInboundTransfer first = await supervisor.ReceiveAsync().ConfigureAwait(false);
        M3uaHaInboundTransfer second = await supervisor.ReceiveAsync().ConfigureAwait(false);
        HashSet<string> sources = new(StringComparer.OrdinalIgnoreCase)
        {
            first.AssociationName,
            second.AssociationName
        };

        Equal(2, sources.Count, "Both independently managed association lanes must reach the aggregate reader.");
        Equal(true, sources.Contains("a"), "Association a must be represented in fan-in output.");
        Equal(true, sources.Contains("b"), "Association b must be represented in fan-in output.");

        await WaitUntilAsync(
            () => supervisor.GetSnapshot().PendingInboundTransfers == 0,
            "Pending accounting must return to zero after both transfers are consumed.")
            .ConfigureAwait(false);

        M3uaHaRuntimeSupervisorSnapshot drained = supervisor.GetSnapshot();
        Equal(1L, Lane(drained, "a").ReceivedTransfers, "Association a receive accounting must remain isolated.");
        Equal(1L, Lane(drained, "b").ReceivedTransfers, "Association b receive accounting must remain isolated.");
    }

    private static async Task TerminalLaneFaultDoesNotStopHealthyLaneAsync()
    {
        FakeRuntimeLane a = new("a");
        FakeRuntimeLane b = new("b");
        await using M3uaHaRuntimeSupervisor supervisor = new([a, b], inboundCapacity: 2);

        await supervisor.StartAsync().ConfigureAwait(false);
        a.Fault("synthetic association loss");

        await WaitUntilAsync(
            () => Lane(supervisor.GetSnapshot(), "a").State == M3uaRuntimeState.Faulted,
            "The faulted lane must become terminal without changing the healthy lane.")
            .ConfigureAwait(false);

        M3uaHaRuntimeSupervisorSnapshot isolated = supervisor.GetSnapshot();
        Equal(M3uaRuntimeState.Faulted, Lane(isolated, "a").State, "The failed association lane must be faulted.");
        Equal(M3uaRuntimeState.Active, Lane(isolated, "b").State, "A peer lane must remain active after another lane faults.");
        Equal(true, Lane(isolated, "a").FaultEvents >= 1, "The faulting lane must retain a diagnostic fault count.");

        b.Emit(CreateTransfer(sls: 9, routingContext: 100));
        M3uaHaInboundTransfer inbound = await supervisor.ReceiveAsync().ConfigureAwait(false);
        Equal("b", inbound.AssociationName, "Healthy inbound traffic must continue after an independent lane fault.");
        Equal((byte)9, inbound.Message.RoutingLabel.SignallingLinkSelection, "Healthy-lane payload metadata must be preserved.");
    }

    private static M3uaHaRuntimeLaneSnapshot Lane(
        M3uaHaRuntimeSupervisorSnapshot snapshot,
        string associationName)
    {
        M3uaHaRuntimeLaneSnapshot[] matches = snapshot.Lanes
            .Where(lane => string.Equals(
                lane.AssociationName,
                associationName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Equal(1, matches.Length, $"Expected exactly one snapshot for association '{associationName}'.");
        return matches[0];
    }

    private static Mtp3TransferMessage CreateTransfer(byte sls, uint? routingContext) =>
        new(
            new Mtp3ServiceInformationOctet(Mtp3ServiceIndicator.Sccp, networkIndicator: 2),
            new Mtp3RoutingLabel(
                destinationPointCode: 1234,
                originatingPointCode: 4321,
                signallingLinkSelection: sls),
            new byte[] { 0x01, sls },
            routingContext: routingContext);

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        string failureMessage)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            try
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested)
            {
                throw new InvalidOperationException(failureMessage);
            }
        }
    }

    private static void Run(string name, Func<Task> test)
    {
        try
        {
            test().GetAwaiter().GetResult();
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
            Environment.ExitCode = 1;
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

    private sealed class FakeRuntimeLane : IM3uaAssociationRuntimeLane
    {
        private readonly Channel<Mtp3TransferMessage> _inbound =
            Channel.CreateUnbounded<Mtp3TransferMessage>();
        private readonly bool _failStart;
        private readonly bool _stallStart;
        private M3uaRuntimeState _state = M3uaRuntimeState.Stopped;
        private long _received;

        internal FakeRuntimeLane(
            string associationName,
            bool failStart = false,
            bool stallStart = false)
        {
            AssociationName = associationName;
            _failStart = failStart;
            _stallStart = stallStart;
        }

        public string AssociationName { get; }

        public M3uaRuntimeState State => _state;

        public event EventHandler<M3uaRuntimeEventArgs>? RuntimeEvent;

        public async ValueTask StartAsync(CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (_failStart)
            {
                _state = M3uaRuntimeState.Faulted;
                Raise(M3uaRuntimeEventKind.FaultObserved, "synthetic startup failure");
                throw new InvalidOperationException("Synthetic startup failure.");
            }

            if (_stallStart)
            {
                _state = M3uaRuntimeState.Starting;
                Raise(M3uaRuntimeEventKind.StateChanged, "synthetic slow startup");
                await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
                return;
            }

            _state = M3uaRuntimeState.Active;
            Raise(M3uaRuntimeEventKind.AspActivated, "synthetic active");
        }

        public async ValueTask<Mtp3TransferMessage> ReceiveAsync(
            CancellationToken ct = default)
        {
            Mtp3TransferMessage message =
                await _inbound.Reader.ReadAsync(ct).ConfigureAwait(false);
            Interlocked.Increment(ref _received);
            return message;
        }

        public ValueTask StopAsync(CancellationToken ct = default)
        {
            _state = M3uaRuntimeState.Stopped;
            _inbound.Writer.TryComplete();
            Raise(M3uaRuntimeEventKind.ShutdownCompleted, "synthetic stopped");
            return ValueTask.CompletedTask;
        }

        public M3uaRuntimeMetrics GetMetrics() => new(
            _state,
            outboundQueueDepth: 0,
            inboundQueueDepth: 0,
            sentTransfers: 0,
            receivedTransfers: Interlocked.Read(ref _received),
            heartbeatsSent: 0,
            heartbeatsAcknowledged: 0,
            heartbeatTimeouts: 0,
            reconnectAttempts: 0,
            faults: _state == M3uaRuntimeState.Faulted ? 1 : 0);

        internal void Emit(Mtp3TransferMessage message)
        {
            if (!_inbound.Writer.TryWrite(message))
            {
                throw new InvalidOperationException(
                    $"Synthetic lane '{AssociationName}' rejected an inbound transfer.");
            }
        }

        internal void Fault(string detail)
        {
            _state = M3uaRuntimeState.Faulted;
            Raise(M3uaRuntimeEventKind.FaultObserved, detail);
            _inbound.Writer.TryComplete(new InvalidOperationException(detail));
        }

        private void Raise(M3uaRuntimeEventKind kind, string detail)
        {
            RuntimeEvent?.Invoke(
                this,
                new M3uaRuntimeEventArgs(
                    kind,
                    _state,
                    DateTimeOffset.UtcNow,
                    AssociationName,
                    detail));
        }
    }
}
