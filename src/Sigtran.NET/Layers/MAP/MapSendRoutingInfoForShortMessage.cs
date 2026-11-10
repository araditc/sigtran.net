using System.Formats.Asn1;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents MAP SendRoutingInfoForSM parameters.
/// </summary>
public sealed class MapSendRoutingInfoForShortMessage
{
    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte MsisdnTag = 0;

    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte ServiceCentreAddressTag = 1;

    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte GprsSupportIndicatorTag = 2;

    private static readonly Asn1Tag MsisdnWireTag = MapSmsAsn1.Context(0);
    private static readonly Asn1Tag PriorityTag = MapSmsAsn1.Context(1);
    private static readonly Asn1Tag ServiceCentreWireTag = MapSmsAsn1.Context(2);
    private static readonly Asn1Tag GprsSupportWireTag = MapSmsAsn1.Context(7);

    /// <summary>Creates SendRoutingInfoForSM parameters.</summary>
    /// <param name="msisdn">The destination MSISDN.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="gprsSupportIndicator">Whether GPRS delivery information is requested.</param>
    public MapSendRoutingInfoForShortMessage(MapSmsAddress msisdn, MapSmsAddress serviceCentreAddress, bool gprsSupportIndicator = false)
        : this(msisdn, serviceCentreAddress, gprsSupportIndicator, smRpPriority: false)
    {
    }

    /// <summary>Creates SendRoutingInfoForSM parameters with explicit priority.</summary>
    /// <param name="msisdn">The destination MSISDN.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="gprsSupportIndicator">Whether two routing numbers can be handled.</param>
    /// <param name="smRpPriority">The mandatory SM-RP-PRI boolean value.</param>
    public MapSendRoutingInfoForShortMessage(MapSmsAddress msisdn, MapSmsAddress serviceCentreAddress,
        bool gprsSupportIndicator, bool smRpPriority)
    {
        Msisdn = msisdn ?? throw new ArgumentNullException(nameof(msisdn));
        ServiceCentreAddress = serviceCentreAddress ?? throw new ArgumentNullException(nameof(serviceCentreAddress));
        GprsSupportIndicator = gprsSupportIndicator;
        SmRpPriority = smRpPriority;
    }

    /// <summary>The destination MSISDN.</summary>
    public MapSmsAddress Msisdn { get; }

    /// <summary>The service centre address.</summary>
    public MapSmsAddress ServiceCentreAddress { get; }

    /// <summary>Whether GPRS delivery information is requested.</summary>
    public bool GprsSupportIndicator { get; }

    /// <summary>The SM-RP priority flag.</summary>
    public bool SmRpPriority { get; }

    /// <summary>Encodes the operation parameters.</summary>
    /// <returns>The encoded parameters.</returns>
    public byte[] Encode()
    {
        return MapSmsAsn1.Encode(fields =>
        {
            MapSmsAsn1.WriteAddress(fields, Msisdn, MapSmsAddressKind.Msisdn, MsisdnWireTag);
            fields.WriteBoolean(SmRpPriority, PriorityTag);
            MapSmsAsn1.WriteAddress(fields, ServiceCentreAddress, MapSmsAddressKind.ServiceCentre,
                ServiceCentreWireTag);
            if (GprsSupportIndicator)
            {
                fields.WriteNull(GprsSupportWireTag);
            }
        });
    }

    /// <summary>Attempts to decode SendRoutingInfoForSM parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapSendRoutingInfoForShortMessage? message, out string? error)
    {
        return MapSmsAsn1.TryDecode(data, static fields =>
        {
            MapSmsAddress msisdn = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Msisdn, MsisdnWireTag);
            bool priority = fields.ReadBoolean(PriorityTag);
            MapSmsAddress serviceCentre = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.ServiceCentre,
                ServiceCentreWireTag);
            bool gprs = MapSmsAsn1.HasTag(fields, GprsSupportWireTag);
            if (gprs) fields.ReadNull(GprsSupportWireTag);
            return new MapSendRoutingInfoForShortMessage(msisdn, serviceCentre, gprs, priority);
        }, out message, out error);
    }
}
