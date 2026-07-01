using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// Exposes an M3UA transport session as an MTP3 network contract for upper protocol layers.
/// </summary>
public sealed class M3uaMtp3Network : IMtp3Network
{
    /// <summary>Creates an M3UA-backed MTP3 network adapter.</summary>
    /// <param name="session">The M3UA transport session.</param>
    public M3uaMtp3Network(M3uaTransportSession session)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <summary>The M3UA transport session used by this network adapter.</summary>
    public M3uaTransportSession Session { get; }

    /// <inheritdoc />
    public async ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await Session.SendPayloadDataAsync(
            message.UserPayload,
            message.RoutingLabel.OriginatingPointCode,
            message.RoutingLabel.DestinationPointCode,
            (byte)message.ServiceInformation.ServiceIndicator,
            message.ServiceInformation.NetworkIndicator,
            message.ServiceInformation.MessagePriority,
            message.RoutingLabel.SignallingLinkSelection,
            message.NetworkAppearance,
            message.RoutingContext,
            message.CorrelationId,
            ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default)
    {
        M3uaInboundProcessingResult? result = await Session.ReceiveAsync(ct).ConfigureAwait(false);
        if (result is null)
        {
            throw new EndOfStreamException("The M3UA transport closed before an MTP3 transfer was received.");
        }

        if (result.TypedMessage.Kind != M3uaTypedMessageKind.PayloadData)
        {
            throw new InvalidOperationException($"Expected M3UA Payload Data, received {result.TypedMessage.Kind}.");
        }

        M3uaPayloadDataMessage payload = result.TypedMessage.As<M3uaPayloadDataMessage>();
        Mtp3ServiceInformationOctet serviceInformation = new(
            (Mtp3ServiceIndicator)payload.ServiceIndicator,
            payload.NetworkIndicator,
            payload.MessagePriority);
        Mtp3RoutingLabel routingLabel = new(
            payload.DestinationPointCode,
            payload.OriginatingPointCode,
            payload.SignallingLinkSelection);

        return new(
            serviceInformation,
            routingLabel,
            payload.UserPayload.ToArray(),
            payload.NetworkAppearance,
            payload.RoutingContext,
            payload.CorrelationId);
    }
}
