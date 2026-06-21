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
    /// <summary>MSISDN parameter tag.</summary>
    public const byte MsisdnTag = 0;

    /// <summary>Service Centre address parameter tag.</summary>
    public const byte ServiceCentreAddressTag = 1;

    /// <summary>Delivery status parameter tag.</summary>
    public const byte DeliveryStatusTag = 2;

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
        MapSmsParameterSet set = new();
        set.Add(MsisdnTag, Msisdn.Encode());
        set.Add(ServiceCentreAddressTag, ServiceCentreAddress.Encode());
        set.Add(DeliveryStatusTag, [(byte)DeliveryStatus]);
        return set.Encode();
    }

    /// <summary>Attempts to decode ReportSM-DeliveryStatus parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapReportShortMessageDeliveryStatus? message, out string? error)
    {
        message = null;
        if (!MapSmsParameterSet.TryDecode(data, out MapSmsParameterSet? set, out error))
        {
            return false;
        }

        MapSmsAddress? msisdn = null;
        MapSmsAddress? serviceCentreAddress = null;
        MapSmsDeliveryStatus? status = null;
        foreach (MapSmsParameter parameter in set!.Snapshot())
        {
            if (parameter.TagNumber == MsisdnTag)
            {
                msisdn = MapSmsAddress.Decode(parameter.Value.Span, oddDigitCount: true);
            }
            else if (parameter.TagNumber == ServiceCentreAddressTag)
            {
                serviceCentreAddress = MapSmsAddress.Decode(parameter.Value.Span, oddDigitCount: true);
            }
            else if (parameter.TagNumber == DeliveryStatusTag && !parameter.Value.IsEmpty)
            {
                status = (MapSmsDeliveryStatus)parameter.Value.Span[0];
            }
        }

        if (msisdn is null || serviceCentreAddress is null || status is null)
        {
            error = "ReportSM-DeliveryStatus requires MSISDN, Service Centre address, and delivery status.";
            return false;
        }

        message = new(msisdn, serviceCentreAddress, status.Value);
        error = null;
        return true;
    }
}
