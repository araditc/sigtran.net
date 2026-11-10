using System.Formats.Asn1;

namespace Sigtran.NET.Layers.MAP;

/// <summary>The IMSI and network-node number in a MAP RoutingInfoForSM result.</summary>
public sealed class MapSmsRoutingInfo
{
    private static readonly Asn1Tag LocationTag = new(TagClass.ContextSpecific, 0, true);
    private static readonly Asn1Tag NetworkNodeTag = MapSmsAsn1.Context(1);

    /// <summary>Creates a routing result for the single-node SMS profile.</summary>
    /// <param name="imsi">The destination subscriber IMSI.</param>
    /// <param name="networkNodeNumber">The ISDN address of the serving node, represented as an MSISDN kind.</param>
    public MapSmsRoutingInfo(MapSmsAddress imsi, MapSmsAddress networkNodeNumber)
    {
        Imsi = imsi ?? throw new ArgumentNullException(nameof(imsi));
        NetworkNodeNumber = networkNodeNumber ?? throw new ArgumentNullException(nameof(networkNodeNumber));
    }

    /// <summary>The subscriber identity.</summary>
    public MapSmsAddress Imsi { get; }

    /// <summary>The serving-node ISDN address.</summary>
    public MapSmsAddress NetworkNodeNumber { get; }

    /// <summary>Encodes RoutingInfoForSM-Res with the mandatory location information.</summary>
    /// <returns>The complete result SEQUENCE.</returns>
    public byte[] Encode() => MapSmsAsn1.Encode(fields =>
    {
        MapSmsAsn1.WriteAddress(fields, Imsi, MapSmsAddressKind.Imsi);
        using (fields.PushSequence(LocationTag))
        {
            MapSmsAsn1.WriteAddress(fields, NetworkNodeNumber, MapSmsAddressKind.Msisdn, NetworkNodeTag);
        }
    });

    /// <summary>Attempts to decode one supported RoutingInfoForSM-Res value.</summary>
    /// <param name="data">The encoded result SEQUENCE.</param>
    /// <param name="result">The decoded result on success.</param>
    /// <param name="error">The decoding error, if any.</param>
    /// <returns>True when the result is valid for the single-node profile.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapSmsRoutingInfo? result, out string? error)
        => MapSmsAsn1.TryDecode(data, static fields =>
        {
            MapSmsAddress imsi = MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Imsi);
            AsnReader location = fields.ReadSequence(LocationTag);
            MapSmsAddress node = MapSmsAsn1.ReadAddress(location, MapSmsAddressKind.Msisdn, NetworkNodeTag);
            location.ThrowIfNotEmpty();
            return new MapSmsRoutingInfo(imsi, node);
        }, out result, out error);
}
