namespace sigtran.net.Layers.MAP;

/// <summary>
/// Represents MAP SendRoutingInfoForSM parameters.
/// </summary>
public sealed class MapSendRoutingInfoForShortMessage
{
    /// <summary>MSISDN parameter tag.</summary>
    public const byte MsisdnTag = 0;

    /// <summary>Service Centre address parameter tag.</summary>
    public const byte ServiceCentreAddressTag = 1;

    /// <summary>GPRS support indicator parameter tag.</summary>
    public const byte GprsSupportIndicatorTag = 2;

    /// <summary>Creates SendRoutingInfoForSM parameters.</summary>
    /// <param name="msisdn">The destination MSISDN.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="gprsSupportIndicator">Whether GPRS delivery information is requested.</param>
    public MapSendRoutingInfoForShortMessage(MapSmsAddress msisdn, MapSmsAddress serviceCentreAddress, bool gprsSupportIndicator = false)
    {
        Msisdn = msisdn ?? throw new ArgumentNullException(nameof(msisdn));
        ServiceCentreAddress = serviceCentreAddress ?? throw new ArgumentNullException(nameof(serviceCentreAddress));
        GprsSupportIndicator = gprsSupportIndicator;
    }

    /// <summary>The destination MSISDN.</summary>
    public MapSmsAddress Msisdn { get; }

    /// <summary>The service centre address.</summary>
    public MapSmsAddress ServiceCentreAddress { get; }

    /// <summary>Whether GPRS delivery information is requested.</summary>
    public bool GprsSupportIndicator { get; }

    /// <summary>Encodes the operation parameters.</summary>
    /// <returns>The encoded parameters.</returns>
    public byte[] Encode()
    {
        MapSmsParameterSet set = new();
        set.Add(MsisdnTag, Msisdn.Encode());
        set.Add(ServiceCentreAddressTag, ServiceCentreAddress.Encode());
        if (GprsSupportIndicator)
        {
            set.Add(GprsSupportIndicatorTag, [0x01]);
        }

        return set.Encode();
    }

    /// <summary>Attempts to decode SendRoutingInfoForSM parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapSendRoutingInfoForShortMessage? message, out string? error)
    {
        message = null;
        if (!MapSmsParameterSet.TryDecode(data, out MapSmsParameterSet? set, out error))
        {
            return false;
        }

        MapSmsAddress? msisdn = null;
        MapSmsAddress? serviceCentreAddress = null;
        bool gprs = false;
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
            else if (parameter.TagNumber == GprsSupportIndicatorTag)
            {
                gprs = !parameter.Value.IsEmpty && parameter.Value.Span[0] != 0;
            }
        }

        if (msisdn is null || serviceCentreAddress is null)
        {
            error = "SendRoutingInfoForSM requires MSISDN and Service Centre address.";
            return false;
        }

        message = new(msisdn, serviceCentreAddress, gprs);
        error = null;
        return true;
    }
}
