namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents MAP AlertServiceCentre parameters.
/// </summary>
public sealed class MapAlertServiceCentre
{
    /// <summary>MSISDN parameter tag.</summary>
    public const byte MsisdnTag = 0;

    /// <summary>Service Centre address parameter tag.</summary>
    public const byte ServiceCentreAddressTag = 1;

    /// <summary>Creates AlertServiceCentre parameters.</summary>
    /// <param name="msisdn">The subscriber MSISDN.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    public MapAlertServiceCentre(MapSmsAddress msisdn, MapSmsAddress serviceCentreAddress)
    {
        Msisdn = msisdn ?? throw new ArgumentNullException(nameof(msisdn));
        ServiceCentreAddress = serviceCentreAddress ?? throw new ArgumentNullException(nameof(serviceCentreAddress));
    }

    /// <summary>The subscriber MSISDN.</summary>
    public MapSmsAddress Msisdn { get; }

    /// <summary>The service centre address.</summary>
    public MapSmsAddress ServiceCentreAddress { get; }

    /// <summary>Encodes the operation parameters.</summary>
    /// <returns>The encoded parameters.</returns>
    public byte[] Encode()
    {
        MapSmsParameterSet set = new();
        set.Add(MsisdnTag, Msisdn.Encode());
        set.Add(ServiceCentreAddressTag, ServiceCentreAddress.Encode());
        return set.Encode();
    }

    /// <summary>Attempts to decode AlertServiceCentre parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapAlertServiceCentre? message, out string? error)
    {
        message = null;
        if (!MapSmsParameterSet.TryDecode(data, out MapSmsParameterSet? set, out error))
        {
            return false;
        }

        MapSmsAddress? msisdn = null;
        MapSmsAddress? serviceCentreAddress = null;
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
        }

        if (msisdn is null || serviceCentreAddress is null)
        {
            error = "AlertServiceCentre requires MSISDN and Service Centre address.";
            return false;
        }

        message = new(msisdn, serviceCentreAddress);
        error = null;
        return true;
    }
}
