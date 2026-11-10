using System.Net.Sockets;
using Sigtran.NET.Layers.MTP2;

namespace Sigtran.NET.Layers.MTP3;

public sealed partial class Mtp3LinkNetwork
{
    private static readonly TimeSpan ControlTimeout = TimeSpan.FromSeconds(1);
    private readonly Dictionary<Mtp3LinkBinding, LinkControl> _controls;
    private readonly Task _linkMonitor;

    /// <summary>Reports whether changeover from this link has finished and the link is held off traffic.</summary>
    /// <param name="adjacentPointCode">The adjacent point code identifying the linkset.</param>
    /// <param name="linkCode">The link code within the linkset.</param>
    /// <returns>True after diversion, including emergency diversion without replay.</returns>
    public bool IsLinkDiverted(uint adjacentPointCode, byte linkCode)
        => FindControl(adjacentPointCode, linkCode).Traffic == LinkTraffic.Diverted;

    private LinkControl FindControl(uint adjacent, byte linkCode)
        => _controls.Values.FirstOrDefault(control =>
            control.Binding.AdjacentPointCode == adjacent && control.Binding.LinkCode == linkCode)
            ?? throw new ArgumentException("The adjacent point code and link code do not identify a configured link.");

    private async Task MonitorLinksAsync()
    {
        using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(50));
        CancellationToken ct = _lifetime.Token;
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await _egress.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    await ApplyLinkChangesAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    _egress.Release();
                }
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private async Task ApplyLinkChangesAsync(CancellationToken ct)
    {
        foreach (LinkControl control in _controls.Values)
        {
            bool usable = IsOperational(control.Binding);
            if (usable)
            {
                control.ObservedService = true;
            }

            PeerSignal? order = control.TakeOrder();
            if (control.Traffic == LinkTraffic.Diverted)
            {
                if (order is not null)
                {
                    // A late order must not replay the completed generation again.
                    using CancellationTokenSource deadline = NewControlDeadline(ct);
                    try
                    {
                        await SendManagementAsync(order.Carrier, control.Binding, new(Mtp3ManagementKind.EmergencyChangeoverAcknowledgement),
                            deadline.Token).ConfigureAwait(false);
                    }
                    catch (Exception error) when (IsControlFailure(error, ct))
                    {
                    }
                }

                continue;
            }

            Mtp2LinkState state = control.Binding.Link.State;
            if (order is not null || (control.ObservedService && !usable
                && state is not (Mtp2LinkState.Busy or Mtp2LinkState.ProcessorOutage)))
            {
                await ChangeoverAsync(control, order, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task ChangeoverAsync(LinkControl control, PeerSignal? order, CancellationToken ct)
    {
        Mtp3LinkBinding affected = control.Binding;
        control.Traffic = LinkTraffic.ChangingOver;
        try
        {
            IMtp2LinkRecovery? recovery = affected.Link as IMtp2LinkRecovery;
            uint? localSequence = null;
            if (recovery is not null)
            {
                using CancellationTokenSource freeze = NewControlDeadline(ct);
                try
                {
                    var (sequence, acceptedCount) = await recovery.FreezeForChangeoverAsync(freeze.Token).ConfigureAwait(false);
                    await control.WaitForIngressAsync(acceptedCount, freeze.Token).ConfigureAwait(false);
                    localSequence = sequence is <= 0xFF_FFFF ? sequence : null;
                }
                catch (Exception error) when (IsControlFailure(error, ct))
                {
                    // An unconfirmed boundary allows diversion, never retransmission.
                    recovery = null;
                }
            }

            Mtp3LinkBinding? carrier = order?.Carrier;
            if (carrier is null || !CanRoute(carrier))
            {
                carrier = _routing.SelectLink(affected.AdjacentPointCode, affected.LinkCode, CanRoute);
            }

            if (carrier is null)
            {
                return;
            }

            using CancellationTokenSource exchange = NewControlDeadline(ct);
            PeerSignal confirmed;
            if (order is null)
            {
                Task<PeerSignal> response = control.ExpectChangeover();
                await SendManagementAsync(carrier, affected, localSequence is { } sequence
                    ? new(Mtp3ManagementKind.ExtendedChangeoverOrder, sequence)
                    : new(Mtp3ManagementKind.EmergencyChangeoverOrder), exchange.Token).ConfigureAwait(false);
                confirmed = await response.WaitAsync(exchange.Token).ConfigureAwait(false);
            }
            else
            {
                confirmed = order;
            }

            if (IsChangeoverOrder(confirmed.Message.Kind))
            {
                // This also resolves simultaneous orders without taking the egress gate in a reader.
                await SendManagementAsync(carrier, affected, localSequence is { } sequence
                    ? new(Mtp3ManagementKind.ExtendedChangeoverAcknowledgement, sequence)
                    : new(Mtp3ManagementKind.EmergencyChangeoverAcknowledgement), exchange.Token).ConfigureAwait(false);
            }

            if (confirmed.Message.Kind is Mtp3ManagementKind.ExtendedChangeoverOrder or Mtp3ManagementKind.ExtendedChangeoverAcknowledgement
                && recovery is not null && recovery.TryRetrieveAfter(confirmed.Message.Value, out var messages))
            {
                foreach (ReadOnlyMemory<byte> msu in messages)
                {
                    if (Mtp3Msu.TryDecode(msu.Span, out Mtp3TransferMessage? transfer)
                        && transfer!.ServiceInformation.NetworkIndicator == _networkIndicator
                        && transfer.RoutingLabel.OriginatingPointCode == _localPointCode
                        && (byte)transfer.ServiceInformation.ServiceIndicator >= 2
                        && _routing.CanUseRoute(transfer.RoutingLabel.DestinationPointCode, affected.AdjacentPointCode))
                    {
                        await carrier.Link.SendAsync(msu, exchange.Token).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception error) when (IsControlFailure(error, ct))
        {
            // Leave uncertain records unreplayed on a timeout or alternate send failure.
        }
        finally
        {
            control.EndChangeover();
            control.Traffic = LinkTraffic.Diverted;
        }
    }

    private void ObserveManagement(Mtp3LinkBinding carrier, Mtp3TransferMessage transfer, Mtp3ManagementMessage message)
    {
        _routing.UpdateRoute(carrier.AdjacentPointCode, message);
        byte linkCode = transfer.RoutingLabel.SignallingLinkSelection;
        LinkControl? affected = _controls.Values.FirstOrDefault(control =>
            control.Binding.AdjacentPointCode == carrier.AdjacentPointCode && control.Binding.LinkCode == linkCode);
        if (affected is not null && affected.Binding != carrier)
        {
            if (IsChangeoverOrder(message.Kind))
            {
                affected.ObserveOrder(new(carrier, message));
            }
            else if (message.Kind is Mtp3ManagementKind.ExtendedChangeoverAcknowledgement or Mtp3ManagementKind.EmergencyChangeoverAcknowledgement)
            {
                affected.ObserveAcknowledgement(new(carrier, message));
            }
        }
    }

    private ValueTask SendManagementAsync(Mtp3LinkBinding carrier, Mtp3LinkBinding affected, Mtp3ManagementMessage body, CancellationToken ct)
        => carrier.Link.SendAsync(Mtp3Msu.Encode(new(
            new(Mtp3ServiceIndicator.SignallingNetworkManagement, _networkIndicator),
            new(affected.AdjacentPointCode, _localPointCode, affected.LinkCode), body.Encode())), ct);

    private static bool IsChangeoverOrder(Mtp3ManagementKind kind)
        => kind is Mtp3ManagementKind.ExtendedChangeoverOrder or Mtp3ManagementKind.EmergencyChangeoverOrder;

    private static CancellationTokenSource NewControlDeadline(CancellationToken ct)
    {
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(ct);
        source.CancelAfter(ControlTimeout);
        return source;
    }

    private static bool IsControlFailure(Exception error, CancellationToken lifetime)
        => error is IOException or SocketException or InvalidOperationException or ArgumentException or TimeoutException
            || error is OperationCanceledException && !lifetime.IsCancellationRequested;

    private enum LinkTraffic { Normal, ChangingOver, Diverted }

    private sealed record PeerSignal(Mtp3LinkBinding Carrier, Mtp3ManagementMessage Message);

    /// <summary>Coordinates bounded peer control and ingress progress independently of egress serialization.</summary>
    private sealed partial class LinkControl(Mtp3LinkBinding binding)
    {
        private readonly Lock _sync = new();
        private PeerSignal? _order;
        private TaskCompletionSource<PeerSignal>? _changeover;
        private TaskCompletionSource? _ingressChanged;
        private long _processed;

        internal Mtp3LinkBinding Binding { get; } = binding;
        internal bool ObservedService { get; set; }
        internal volatile LinkTraffic Traffic;

        internal PeerSignal? TakeOrder()
        {
            lock (_sync)
            {
                PeerSignal? order = _order;
                _order = null;
                return order;
            }
        }

        internal Task<PeerSignal> ExpectChangeover()
        {
            lock (_sync)
            {
                _changeover = new(TaskCreationOptions.RunContinuationsAsynchronously);
                if (_order is { } order)
                {
                    _changeover.SetResult(order);
                    _order = null;
                }

                return _changeover.Task;
            }
        }

        internal void ObserveOrder(PeerSignal order)
        {
            lock (_sync)
            {
                if (_changeover?.TrySetResult(order) != true)
                {
                    _order ??= order;
                }
            }
        }

        internal void ObserveAcknowledgement(PeerSignal acknowledgement)
        {
            lock (_sync)
            {
                _changeover?.TrySetResult(acknowledgement);
            }
        }

        internal void EndChangeover()
        {
            lock (_sync)
            {
                _changeover = null;
            }
        }

        internal void MarkIngressProcessed()
        {
            lock (_sync)
            {
                _processed++;
                _ingressChanged?.TrySetResult();
                _ingressChanged = null;
            }
        }

        internal async Task WaitForIngressAsync(long accepted, CancellationToken ct)
        {
            while (true)
            {
                Task changed;
                lock (_sync)
                {
                    if (_processed >= accepted)
                    {
                        return;
                    }

                    _ingressChanged ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
                    changed = _ingressChanged.Task;
                }

                await changed.WaitAsync(ct).ConfigureAwait(false);
            }
        }
    }
}
