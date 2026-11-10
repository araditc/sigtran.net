namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents MAP AlertServiceCentre parameters.
/// </summary>
public sealed class MapAlertServiceCentre
{
    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte MsisdnTag = 0;

    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
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
        return MapSmsAsn1.Encode(fields =>
        {
            MapSmsAsn1.WriteAddress(fields, Msisdn, MapSmsAddressKind.Msisdn);
            MapSmsAsn1.WriteAddress(fields, ServiceCentreAddress, MapSmsAddressKind.ServiceCentre);
        });
    }

    /// <summary>Attempts to decode AlertServiceCentre parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapAlertServiceCentre? message, out string? error)
    {
        return MapSmsAsn1.TryDecode(data, static fields => new MapAlertServiceCentre(
            MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Msisdn),
            MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.ServiceCentre)), out message, out error);
    }
}
