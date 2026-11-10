namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Represents MAP MT-ForwardSM parameters for the SMS profile.
/// </summary>
public sealed class MapMtForwardShortMessage
{
    /// <summary>SM-RP-DA IMSI choice tag.</summary>
    public const byte SmRpDaTag = 0;

    /// <summary>SM-RP-OA service-centre choice tag.</summary>
    public const byte SmRpOaTag = 4;

    /// <summary>SM-RP-UI universal OCTET STRING tag.</summary>
    public const byte SmRpUiTag = 4;

    /// <summary>Creates MAP MT-ForwardSM parameters.</summary>
    /// <param name="smRpDa">The destination address.</param>
    /// <param name="smRpOa">The originating address.</param>
    /// <param name="smRpUi">The short message transfer protocol data unit.</param>
    public MapMtForwardShortMessage(MapSmsAddress smRpDa, MapSmsAddress smRpOa, ReadOnlyMemory<byte> smRpUi)
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
            MapSmsAsn1.WriteAddress(fields, SmRpDa, MapSmsAddressKind.Imsi, MapSmsAsn1.Context(SmRpDaTag));
            MapSmsAsn1.WriteAddress(fields, SmRpOa, MapSmsAddressKind.ServiceCentre, MapSmsAsn1.Context(SmRpOaTag));
            fields.WriteOctetString(SmRpUi.Span);
        });
    }

    /// <summary>Attempts to decode MT-ForwardSM parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapMtForwardShortMessage? message, out string? error)
    {
        return MapSmsAsn1.TryDecode(data, static fields => new MapMtForwardShortMessage(
            MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.Imsi, MapSmsAsn1.Context(SmRpDaTag)),
            MapSmsAsn1.ReadAddress(fields, MapSmsAddressKind.ServiceCentre, MapSmsAsn1.Context(SmRpOaTag)),
            MapSmsAsn1.ReadSignalInfo(fields)), out message, out error);
    }
}
