using System.Formats.Asn1;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents MAP SendRoutingInfoForSM parameters.
/// </summary>
public sealed class MapSendRoutingInfoForShortMessage
{
    /// <summary>MSISDN context-specific tag.</summary>
    public const byte MsisdnTag = 0;

    /// <summary>Service centre address context-specific tag.</summary>
    public const byte ServiceCentreAddressTag = 2;

    /// <summary>GPRS support indicator context-specific tag.</summary>
    public const byte GprsSupportIndicatorTag = 7;

    private static readonly Asn1Tag PriorityTag = MapSmsAsn1.Context(1);

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
            MapSmsAsn1.WriteAddress(fields, Msisdn, MapSmsAddressKind.Msisdn, MapSmsAsn1.Context(MsisdnTag));
            fields.WriteBoolean(SmRpPriority, PriorityTag);
            MapSmsAsn1.WriteAddress(fields, ServiceCentreAddress, MapSmsAddressKind.ServiceCentre,
                MapSmsAsn1.Context(ServiceCentreAddressTag));
            if (GprsSupportIndicator)
            {
                fields.WriteNull(MapSmsAsn1.Context(GprsSupportIndicatorTag));
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
            MapSmsAddress msisdn = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Msisdn, MapSmsAsn1.Context(MsisdnTag));
            bool priority = fields.ReadBoolean(PriorityTag);
            MapSmsAddress serviceCentre = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.ServiceCentre,
                MapSmsAsn1.Context(ServiceCentreAddressTag));
            Asn1Tag supportTag = MapSmsAsn1.Context(GprsSupportIndicatorTag);
            bool gprs = MapSmsAsn1.HasTag(fields, supportTag);
            if (gprs) fields.ReadNull(supportTag);
            return new MapSendRoutingInfoForShortMessage(msisdn, serviceCentre, gprs, priority);
        }, out message, out error);
    }
}
