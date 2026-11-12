using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MAP;
using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;
using Sigtran.NET.Layers.SCTP;
using Sigtran.NET.Layers.TCAP;

/// <summary>Uses generation-fenced sends on the fixed ITU lab route; never queues work for a later association.</summary>
internal sealed class CapacityNetwork(IMtp3Network lower,
    Func<Mtp3TransferMessage, CancellationToken, ValueTask> send) : IMtp3Network
{
    private long _foreignTransfers;
    internal long ForeignTransfers => Interlocked.Read(ref _foreignTransfers);

    public ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
        => send(new(message.ServiceInformation, message.RoutingLabel, message.UserPayload,
            message.NetworkAppearance, 100, message.CorrelationId), ct);

    public async ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default)
    {
        while (true)
        {
            var message = await lower.ReceiveAsync(ct).ConfigureAwait(false);
            if (message.RoutingContext == 100 && message.ServiceInformation.NetworkIndicator == 2
                && message.ServiceInformation.ServiceIndicator == Mtp3ServiceIndicator.Sccp
                && message.RoutingLabel.OriginatingPointCode == 2 && message.RoutingLabel.DestinationPointCode == 1)
                return message;
            Interlocked.Increment(ref _foreignTransfers);
        }
    }
}

/// <summary>Owns one real native SCTP association and all upper-layer receive consumers.</summary>
internal sealed class CapacitySession : IAsyncDisposable
{
    private readonly M3uaRuntime _runtime;
    private readonly CapacityNetwork _network;
    private readonly SccpConnectionlessService _sccp;
    private readonly TcapDialogueManager _tcap;
    private readonly CancellationTokenSource _readers = new();
    private Task[] _observers = [];
    private long _unsolicited;
    private long _returns;
    private int _peakOutbound;
    private int _peakInbound;
    private readonly PerformanceMessages _messages = PerformanceMessages.CreateSynthetic();
    private readonly IMapSmsService _map;
    private readonly CapacitySocketFactory _sockets;
    internal CapacitySocketProfile SocketProfile => _sockets.Snapshot();
    private static readonly TimeSpan InvokeTimeout = TimeSpan.FromSeconds(5);

    internal CapacitySession(CapacityOptions options)
    {
        _sockets = new(options.SocketProfile == CapacitySocketFactory.LanProfile);
        M3uaDelegateRuntimeSessionFactory factory = new(async ct =>
        {
            NativeSctpConnector connector = new(_sockets);
            var transport = await connector.ConnectAsync(new(new(options.RemoteIp, options.RemotePort),
                outboundStreams: 8, inboundStreams: 8, defaultPayloadProtocolIdentifier: 3),
                new(new(options.QueueCapacity, 64 * 1024 * 1024),
                    new(connectTimeout: TimeSpan.FromSeconds(5), sendTimeout: TimeSpan.FromSeconds(5),
                        receiveTimeout: TimeSpan.FromSeconds(30), shutdownTimeout: TimeSpan.FromSeconds(5)),
                    new(maxAttempts: 0), requireKernelMetadata: true, enableNoDelay: true), ct).ConfigureAwait(false);
            M3uaAspSession asp = new();
            return new("capacity-peer", new M3uaTransportSession((ISctpTransport)transport,
                new(asp, requireActiveAspForPayload: true),
                new(asp, routingContext: 100, requireActiveAspForPayload: true)));
        });
        _runtime = new(factory, new(new(1, M3uaTrafficModeType.Loadshare), new(maxAttempts: 0),
            options.QueueCapacity, options.QueueCapacity, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5)));
        _network = new(_runtime, _runtime.SendOnActiveAssociationAsync);
        _sccp = new(_network, new(2, 1, 1), 2, options: new(
            inboundQueueCapacity: options.QueueCapacity, returnQueueCapacity: options.QueueCapacity));
        _tcap = new(_sccp, new(eventQueueCapacity: options.QueueCapacity,
            componentQueueCapacity: options.QueueCapacity, maximumDialogues: options.QueueCapacity,
            maximumPendingInvokesPerDialogue: 1, invokeTimeout: TimeSpan.FromSeconds(5)));
        _map = new MapSmsService(_tcap, new(SccpRoutingIndicator.RouteOnSubsystemNumber, SubsystemNumber.MAP, 2),
            new(SccpRoutingIndicator.RouteOnSubsystemNumber, SubsystemNumber.MAP, 1));
    }

    internal async Task StartAsync(CancellationToken ct)
    {
        await _runtime.StartAsync(ct).ConfigureAwait(false);
        await _tcap.StartAsync(ct).ConfigureAwait(false);
        _observers = [ObserveAsync(0), ObserveAsync(1), ObserveAsync(2)];
    }

    internal async ValueTask<CapacityOutcome> InvokeAsync(int operation, CancellationToken ct)
    {
        var outcome = await (operation switch
        {
            0 => _map.InvokeRoutingInfoForShortMessageAsync(_messages.RoutingInfo, InvokeTimeout, ct),
            1 => _map.InvokeMoForwardShortMessageAsync(_messages.MobileOriginated, InvokeTimeout, ct),
            2 => _map.InvokeMtForwardShortMessageAsync(_messages.MobileTerminated, InvokeTimeout, ct),
            3 => _map.InvokeReportShortMessageDeliveryStatusAsync(_messages.DeliveryStatus, InvokeTimeout, ct),
            4 => _map.InvokeAlertServiceCentreAsync(_messages.Alert, InvokeTimeout, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        }).ConfigureAwait(false);
        return Classify(outcome.Outcome);
    }

    internal static CapacityOutcome Classify(MapSmsOperationOutcomeKind outcome) => outcome switch
    {
        MapSmsOperationOutcomeKind.Result => CapacityOutcome.Success,
        MapSmsOperationOutcomeKind.Error => CapacityOutcome.Error,
        MapSmsOperationOutcomeKind.Reject => CapacityOutcome.Reject,
        MapSmsOperationOutcomeKind.TimedOut => CapacityOutcome.Timeout,
        MapSmsOperationOutcomeKind.MalformedResult => CapacityOutcome.MalformedResult,
        _ => CapacityOutcome.DialogueClosed
    };

    /// <summary>Samples bounded queues; protocol totals are captured after drain, before shutdown.</summary>
    internal CapacityStackMetrics Snapshot()
    {
        var m3ua = _runtime.GetMetrics();
        var tcap = _tcap.GetMetrics();
        _peakOutbound = Math.Max(_peakOutbound, m3ua.OutboundQueueDepth);
        _peakInbound = Math.Max(_peakInbound, m3ua.InboundQueueDepth);
        return new(m3ua.SentTransfers, m3ua.ReceivedTransfers, m3ua.ReconnectAttempts, m3ua.Faults,
            tcap.OpenedDialogues, tcap.ClosedDialogues, tcap.PendingInvokes, tcap.ActiveDialogues,
            tcap.MalformedTransactions, tcap.DroppedDialogueEvents, _network.ForeignTransfers,
            Interlocked.Read(ref _unsolicited), Interlocked.Read(ref _returns),
            m3ua.OutboundQueueDepth, m3ua.InboundQueueDepth, _peakOutbound, _peakInbound,
            _observers.Length == 3 && _observers.All(task => !task.IsCompleted));
    }

    private async Task ObserveAsync(int kind)
    {
        try
        {
            while (!_readers.IsCancellationRequested)
            {
                if (kind == 0) _ = await _tcap.ReceiveAsync(_readers.Token).ConfigureAwait(false);
                else if (kind == 1)
                {
                    _ = await _tcap.ReceiveComponentAsync(_readers.Token).ConfigureAwait(false);
                    Interlocked.Increment(ref _unsolicited);
                }
                else
                {
                    _ = await _sccp.ReceiveReturnAsync(_readers.Token).ConfigureAwait(false);
                    Interlocked.Increment(ref _returns);
                }
            }
        }
        catch (OperationCanceledException) when (_readers.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        await _readers.CancelAsync().ConfigureAwait(false);
        try { await Task.WhenAll(_observers).ConfigureAwait(false); }
        finally
        {
            await _tcap.DisposeAsync().ConfigureAwait(false);
            await _sccp.DisposeAsync().ConfigureAwait(false);
            await _runtime.DisposeAsync().ConfigureAwait(false);
            _readers.Dispose();
        }
    }
}

/// <summary>Reconciles protocol completions and exposes unexpected work and queue observations.</summary>
internal sealed record CapacityStackMetrics(long Sent, long Received, long Reconnects, long Faults,
    long Opened, long Closed, int Pending, int Active, long Malformed, long DroppedEvents,
    long ForeignTransfers, long UnsolicitedInvokes, long SccpReturns, int OutboundQueue, int InboundQueue,
    int PeakOutboundQueue, int PeakInboundQueue, bool ReadersHealthy);
