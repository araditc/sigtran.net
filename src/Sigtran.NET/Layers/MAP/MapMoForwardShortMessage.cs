namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents MAP MO-ForwardSM parameters for the SMS profile.
/// </summary>
public sealed class MapMoForwardShortMessage
{
    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte SmRpDaTag = 0;

    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte SmRpOaTag = 1;

    /// <summary>Legacy standalone parameter identifier; not an ASN.1 wire tag.</summary>
    public const byte SmRpUiTag = 2;

    /// <summary>Creates MAP MO-ForwardSM parameters.</summary>
    /// <param name="smRpDa">The destination address.</param>
    /// <param name="smRpOa">The originating address.</param>
    /// <param name="smRpUi">The short message transfer protocol data unit.</param>
    public MapMoForwardShortMessage(MapSmsAddress smRpDa, MapSmsAddress smRpOa, ReadOnlyMemory<byte> smRpUi)
    {
        SmRpDa = smRpDa ?? throw new ArgumentNullException(nameof(smRpDa));
        SmRpOa = smRpOa ?? throw new ArgumentNullException(nameof(smRpOa));
        SmRpUi = smRpUi.Length is 0 or > MapSmsAsn1.MaxSignalInfoBytes
            ? throw new ArgumentOutOfRangeException(nameof(smRpUi), "MAP SignalInfo must be 1..200 octets.")
            : smRpUi;
    }

    /// <summary>The destination address.</summary>
    public MapSmsAddress SmRpDa { get; }

    /// <summary>The originating address.</summary>
    public MapSmsAddress SmRpOa { get; }

    /// <summary>The short message transfer protocol data unit.</summary>
    public ReadOnlyMemory<byte> SmRpUi { get; }

    /// <summary>Encodes the operation parameters.</summary>
    /// <returns>The encoded operation parameters.</returns>
    public byte[] Encode()
    {
        return MapSmsAsn1.Encode(fields =>
        {
            MapSmsAsn1.WriteAddress(fields, SmRpDa, MapSmsAddressKind.ServiceCentre, MapSmsAsn1.Context(4));
            MapSmsAsn1.WriteAddress(fields, SmRpOa, MapSmsAddressKind.Msisdn, MapSmsAsn1.Context(2));
            fields.WriteOctetString(SmRpUi.Span);
        });
    }

    /// <summary>Attempts to decode MO-ForwardSM parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapMoForwardShortMessage? message, out string? error)
    {
        return MapSmsAsn1.TryDecode(data, static fields => new MapMoForwardShortMessage(
            MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.ServiceCentre, MapSmsAsn1.Context(4)),
            MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Msisdn, MapSmsAsn1.Context(2)),
            MapSmsAsn1.ReadSignalInfo(fields)), out message, out error);
    }
}
