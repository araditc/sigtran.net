using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Operations;

namespace Sigtran.NET.OperationsHost;

/// <summary>Restricts the MAP host to its one configured ITU M3UA route.</summary>
internal sealed class ReferenceMtp3Network(
    IMtp3Network network,
    Func<Mtp3TransferMessage, CancellationToken, ValueTask> sendOnActiveAssociation,
    SigtranNodeConfiguration node) : IMtp3Network
{
    /// <inheritdoc />
    public ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        if (!Matches(message, node.LocalPointCode, node.RemotePointCode)
            || (message.RoutingContext is uint context && context != node.RoutingContext))
        {
            throw new InvalidOperationException("MTP3 transfer is outside the configured reference route.");
        }

        return sendOnActiveAssociation(new(message.ServiceInformation, message.RoutingLabel, message.UserPayload,
            message.NetworkAppearance, node.RoutingContext, message.CorrelationId), ct);
    }

    /// <inheritdoc />
    public async ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            Mtp3TransferMessage message = await network.ReceiveAsync(ct).ConfigureAwait(false);
            if (Matches(message, node.RemotePointCode, node.LocalPointCode) && message.RoutingContext == node.RoutingContext)
            {
                return message;
            }
        }
    }

    private bool Matches(Mtp3TransferMessage message, uint origin, uint destination)
        => message.ServiceInformation.NetworkIndicator == node.NetworkIndicator
        && message.ServiceInformation.ServiceIndicator == Mtp3ServiceIndicator.Sccp
        && message.RoutingLabel.OriginatingPointCode == origin && message.RoutingLabel.DestinationPointCode == destination;
}
