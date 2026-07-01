using Sigtran.NET.Layers.MTP3;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Provides SCCP connectionless Unitdata service primitives over an MTP3 network contract.
/// </summary>
public sealed class SccpConnectionlessService : ISccpService
{
    /// <summary>Creates an SCCP connectionless service.</summary>
    /// <param name="network">The lower MTP3 network contract.</param>
    /// <param name="routingLabel">The routing label used for outbound SCCP transfer messages.</param>
    /// <param name="networkIndicator">The MTP3 network indicator used for outbound SCCP transfer messages.</param>
    /// <param name="messagePriority">The MTP3 message priority used for outbound SCCP transfer messages.</param>
    public SccpConnectionlessService(
        IMtp3Network network,
        Mtp3RoutingLabel routingLabel,
        byte networkIndicator = 0,
        byte messagePriority = 0)
    {
        Network = network ?? throw new ArgumentNullException(nameof(network));
        RoutingLabel = routingLabel;
        ServiceInformation = new(Mtp3ServiceIndicator.Sccp, networkIndicator, messagePriority);
    }

    /// <inheritdoc />
    public IMtp3Network Network { get; }

    /// <summary>The MTP3 routing label used for outbound SCCP transfer messages.</summary>
    public Mtp3RoutingLabel RoutingLabel { get; }

    /// <summary>The MTP3 service information octet used for outbound SCCP transfer messages.</summary>
    public Mtp3ServiceInformationOctet ServiceInformation { get; }

    /// <inheritdoc />
    public async ValueTask SendUnitdataAsync(SccpUnitdataMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        Mtp3TransferMessage transfer = new(ServiceInformation, RoutingLabel, message.Encode());
        await Network.SendAsync(transfer, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<SccpUnitdataMessage> ReceiveUnitdataAsync(CancellationToken ct = default)
    {
        Mtp3TransferMessage transfer = await Network.ReceiveAsync(ct).ConfigureAwait(false);
        if (transfer.ServiceInformation.ServiceIndicator != Mtp3ServiceIndicator.Sccp)
        {
            throw new InvalidOperationException($"Expected SCCP service indicator, received {transfer.ServiceInformation.ServiceIndicator}.");
        }

        if (!SccpUnitdataMessage.TryDecode(transfer.UserPayload.Span, out SccpUnitdataMessage? message, out string? error))
        {
            throw new InvalidDataException(error);
        }

        return message!;
    }
}
