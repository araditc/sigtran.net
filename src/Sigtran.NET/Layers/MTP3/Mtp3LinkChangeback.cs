namespace Sigtran.NET.Layers.MTP3;

public sealed partial class Mtp3LinkNetwork
{
    private byte _nextChangebackCode;

    /// <summary>Restores traffic to an aligned, diverted link after acknowledged same-linkset barriers.</summary>
    /// <param name="adjacentPointCode">The adjacent point code identifying the linkset.</param>
    /// <param name="linkCode">The restored link's signalling link code.</param>
    /// <param name="ct">Cancels the attempt without promoting the restored link.</param>
    /// <returns>True only when every surviving alternate acknowledged the changeback.</returns>
    public async ValueTask<bool> ChangebackAsync(uint adjacentPointCode, byte linkCode, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        LinkControl affected = FindControl(adjacentPointCode, linkCode);
        using CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetime.Token);
        await _egress.WaitAsync(operation.Token).ConfigureAwait(false);
        try
        {
            await ApplyLinkChangesAsync(_lifetime.Token).ConfigureAwait(false);
            operation.Token.ThrowIfCancellationRequested();
            if (affected.Traffic != LinkTraffic.Diverted || !IsOperational(affected.Binding))
            {
                return false;
            }

            Mtp3LinkBinding[] alternates = _routing.Links.Where(binding =>
                binding.AdjacentPointCode == adjacentPointCode && binding != affected.Binding && CanRoute(binding)).ToArray();
            if (alternates.Length == 0)
            {
                return false;
            }

            ChangebackBarrier[] barriers = alternates.Select(binding =>
                affected.ExpectChangeback(binding, unchecked(++_nextChangebackCode))).ToArray();
            try
            {
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    using CancellationTokenSource deadline = NewControlDeadline(operation.Token);
                    try
                    {
                        foreach (ChangebackBarrier barrier in barriers)
                        {
                            if (!barrier.Confirmed.IsCompletedSuccessfully)
                            {
                                await SendManagementAsync(barrier.Carrier, affected.Binding,
                                    new(Mtp3ManagementKind.ChangebackDeclaration, barrier.Code), deadline.Token).ConfigureAwait(false);
                            }
                        }

                        await Task.WhenAll(barriers.Select(barrier => barrier.Confirmed)).WaitAsync(deadline.Token).ConfigureAwait(false);
                        operation.Token.ThrowIfCancellationRequested();
                        return IsOperational(affected.Binding) && alternates.All(CanRoute) && affected.TryRestoreTraffic();
                    }
                    catch (Exception error) when (IsControlFailure(error, operation.Token))
                    {
                        // Retry the unconfirmed barriers once; never release on timeout alone.
                    }
                }

                return false;
            }
            finally
            {
                affected.EndChangeback();
            }
        }
        finally
        {
            _egress.Release();
        }
    }

    private async ValueTask ObserveManagementAsync(Mtp3LinkBinding carrier, Mtp3TransferMessage transfer,
        Mtp3ManagementMessage message, CancellationToken ct)
    {
        _routing.UpdateRoute(carrier.AdjacentPointCode, message);
        byte linkCode = transfer.RoutingLabel.SignallingLinkSelection;
        LinkControl? affected = _controls.Values.FirstOrDefault(control =>
            control.Binding.AdjacentPointCode == carrier.AdjacentPointCode && control.Binding.LinkCode == linkCode);
        if (affected is null)
        {
            return;
        }

        if (message.Kind == Mtp3ManagementKind.ChangebackAcknowledgement)
        {
            affected.ObserveChangeback((byte)message.Value);
        }
        else if (affected.Binding != carrier)
        {
            if (IsChangeoverOrder(message.Kind))
            {
                affected.ObserveOrder(new(carrier, message));
            }
            else if (message.Kind is Mtp3ManagementKind.ExtendedChangeoverAcknowledgement or Mtp3ManagementKind.EmergencyChangeoverAcknowledgement)
            {
                affected.ObserveAcknowledgement(new(carrier, message));
            }
            else if (message.Kind == Mtp3ManagementKind.ChangebackDeclaration && IsOperational(carrier))
            {
                // This reader has already queued the preceding MSUs on the same ordered stream.
                using CancellationTokenSource deadline = NewControlDeadline(ct);
                try
                {
                    await SendManagementAsync(carrier, affected.Binding, new(Mtp3ManagementKind.ChangebackAcknowledgement, message.Value),
                        deadline.Token).ConfigureAwait(false);
                }
                catch (Exception error) when (IsControlFailure(error, ct))
                {
                    // The sender can repeat the declaration if this acknowledgement did not leave.
                }
            }
        }
    }

    private sealed record ChangebackBarrier(Mtp3LinkBinding Carrier, byte Code, Task Confirmed);

    private sealed partial class LinkControl
    {
        private readonly Dictionary<byte, TaskCompletionSource> _changeback = new();

        internal ChangebackBarrier ExpectChangeback(Mtp3LinkBinding carrier, byte code)
        {
            lock (_sync)
            {
                TaskCompletionSource acknowledgement = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _changeback.Add(code, acknowledgement);
                return new(carrier, code, acknowledgement.Task);
            }
        }

        internal void ObserveChangeback(byte code)
        {
            lock (_sync)
            {
                if (_changeback.TryGetValue(code, out TaskCompletionSource? acknowledgement))
                {
                    acknowledgement.TrySetResult();
                }
            }
        }

        internal void EndChangeback()
        {
            lock (_sync)
            {
                _changeback.Clear();
            }
        }

        internal bool TryRestoreTraffic()
        {
            lock (_sync)
            {
                if (_order is not null)
                {
                    return false;
                }

                Traffic = LinkTraffic.Normal;
                return true;
            }
        }
    }
}
