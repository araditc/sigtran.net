namespace Sigtran.NET.Layers.MTP3;

/// <summary>
/// Represents one MTP3 transfer primitive exchanged with an upper user part such as SCCP or ISUP.
/// </summary>
public sealed class Mtp3TransferMessage
{
    /// <summary>Creates an MTP3 transfer message.</summary>
    /// <param name="serviceInformation">The MTP3 service information octet.</param>
    /// <param name="routingLabel">The MTP3 routing label.</param>
    /// <param name="userPayload">The user-part payload.</param>
    /// <param name="networkAppearance">The optional network appearance when carried by an adaptation layer.</param>
    /// <param name="routingContext">The optional routing context when carried by an adaptation layer.</param>
    /// <param name="correlationId">The optional correlation id when carried by an adaptation layer.</param>
    public Mtp3TransferMessage(
        Mtp3ServiceInformationOctet serviceInformation,
        Mtp3RoutingLabel routingLabel,
        ReadOnlyMemory<byte> userPayload,
        uint? networkAppearance = null,
        uint? routingContext = null,
        uint? correlationId = null)
    {
        if (userPayload.IsEmpty)
        {
            throw new ArgumentException("MTP3 transfer user payload must not be empty.", nameof(userPayload));
        }

        ServiceInformation = serviceInformation;
        RoutingLabel = routingLabel;
        UserPayload = userPayload;
        NetworkAppearance = networkAppearance;
        RoutingContext = routingContext;
        CorrelationId = correlationId;
    }

    /// <summary>The MTP3 service information octet.</summary>
    public Mtp3ServiceInformationOctet ServiceInformation { get; }

    /// <summary>The MTP3 routing label.</summary>
    public Mtp3RoutingLabel RoutingLabel { get; }

    /// <summary>The user-part payload carried by this transfer.</summary>
    public ReadOnlyMemory<byte> UserPayload { get; }

    /// <summary>The optional network appearance when carried by an adaptation layer.</summary>
    public uint? NetworkAppearance { get; }

    /// <summary>The optional routing context when carried by an adaptation layer.</summary>
    public uint? RoutingContext { get; }

    /// <summary>The optional correlation id when carried by an adaptation layer.</summary>
    public uint? CorrelationId { get; }
}

/// <summary>
/// Provides the MTP3 network contract consumed by SCCP and other MTP3 users.
/// </summary>
public interface IMtp3Network
{
    /// <summary>
    /// Sends one MTP3 transfer primitive to the signalling network.
    /// </summary>
    /// <param name="message">The MTP3 transfer primitive.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the transfer has been queued or sent.</returns>
    ValueTask SendAsync(Mtp3TransferMessage message, CancellationToken ct = default);

    /// <summary>
    /// Receives one MTP3 transfer primitive from the signalling network.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The received MTP3 transfer primitive.</returns>
    ValueTask<Mtp3TransferMessage> ReceiveAsync(CancellationToken ct = default);
}
