using System.Formats.Asn1;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// MAP SMS delivery status values used by the SDK profile.
/// </summary>
public enum MapSmsDeliveryStatus : byte
{
    /// <summary>Delivery succeeded.</summary>
    Delivered = 0,

    /// <summary>The subscriber is absent.</summary>
    AbsentSubscriber = 1,

    /// <summary>Memory capacity was exceeded.</summary>
    MemoryCapacityExceeded = 2,

    /// <summary>Equipment protocol error.</summary>
    EquipmentProtocolError = 3,

    /// <summary>Unknown delivery failure.</summary>
    UnknownFailure = 255
}

/// <summary>
/// Represents MAP ReportSM-DeliveryStatus parameters.
/// </summary>
public sealed class MapReportShortMessageDeliveryStatus
{
    /// <summary>MSISDN universal OCTET STRING tag.</summary>
    public const byte MsisdnTag = 4;

    /// <summary>Service centre address universal OCTET STRING tag.</summary>
    public const byte ServiceCentreAddressTag = 4;

    /// <summary>Delivery outcome universal ENUMERATED tag.</summary>
    public const byte DeliveryStatusTag = 10;

    /// <summary>Creates ReportSM-DeliveryStatus parameters.</summary>
    /// <param name="msisdn">The MSISDN.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="deliveryStatus">The delivery status.</param>
    public MapReportShortMessageDeliveryStatus(MapSmsAddress msisdn, MapSmsAddress serviceCentreAddress, MapSmsDeliveryStatus deliveryStatus)
    {
        Msisdn = msisdn ?? throw new ArgumentNullException(nameof(msisdn));
        ServiceCentreAddress = serviceCentreAddress ?? throw new ArgumentNullException(nameof(serviceCentreAddress));
        DeliveryStatus = deliveryStatus;
    }

    /// <summary>The MSISDN.</summary>
    public MapSmsAddress Msisdn { get; }

    /// <summary>The service centre address.</summary>
    public MapSmsAddress ServiceCentreAddress { get; }

    /// <summary>The delivery status.</summary>
    public MapSmsDeliveryStatus DeliveryStatus { get; }

    /// <summary>Encodes the operation parameters.</summary>
    /// <returns>The encoded parameters.</returns>
    public byte[] Encode()
    {
        return MapSmsAsn1.Encode(fields =>
        {
            MapSmsAsn1.WriteAddress(fields, Msisdn, MapSmsAddressKind.Msisdn);
            MapSmsAsn1.WriteAddress(fields, ServiceCentreAddress, MapSmsAddressKind.ServiceCentre);
            fields.WriteEnumeratedValue(DeliveryStatus switch
            {
                MapSmsDeliveryStatus.MemoryCapacityExceeded => DeliveryOutcome.MemoryCapacityExceeded,
                MapSmsDeliveryStatus.AbsentSubscriber => DeliveryOutcome.AbsentSubscriber,
                MapSmsDeliveryStatus.Delivered => DeliveryOutcome.SuccessfulTransfer,
                _ => throw new InvalidOperationException("The status has no MAP SM-DeliveryOutcome encoding.")
            });
        });
    }

    /// <summary>Attempts to decode ReportSM-DeliveryStatus parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapReportShortMessageDeliveryStatus? message, out string? error)
    {
        return MapSmsAsn1.TryDecode(data, static fields =>
        {
            MapSmsAddress msisdn = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Msisdn);
            MapSmsAddress serviceCentre = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.ServiceCentre);
            MapSmsDeliveryStatus status = fields.ReadEnumeratedValue<DeliveryOutcome>() switch
            {
                DeliveryOutcome.MemoryCapacityExceeded => MapSmsDeliveryStatus.MemoryCapacityExceeded,
                DeliveryOutcome.AbsentSubscriber => MapSmsDeliveryStatus.AbsentSubscriber,
                DeliveryOutcome.SuccessfulTransfer => MapSmsDeliveryStatus.Delivered,
                _ => throw new AsnContentException("Unrecognized MAP SM-DeliveryOutcome.")
            };
            return new MapReportShortMessageDeliveryStatus(msisdn, serviceCentre, status);
        }, out message, out error);
    }

    private enum DeliveryOutcome : byte
    {
        MemoryCapacityExceeded = 0,
        AbsentSubscriber = 1,
        SuccessfulTransfer = 2
    }
}
