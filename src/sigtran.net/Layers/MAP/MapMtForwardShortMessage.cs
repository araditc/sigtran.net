namespace sigtran.net.Layers.MAP;

/// <summary>
/// Represents MAP MT-ForwardSM parameters for the SMS profile.
/// </summary>
public sealed class MapMtForwardShortMessage
{
    /// <summary>SM-RP-DA parameter tag.</summary>
    public const byte SmRpDaTag = 0;

    /// <summary>SM-RP-OA parameter tag.</summary>
    public const byte SmRpOaTag = 1;

    /// <summary>SM-RP-UI parameter tag.</summary>
    public const byte SmRpUiTag = 2;

    /// <summary>Creates MAP MT-ForwardSM parameters.</summary>
    /// <param name="smRpDa">The destination address.</param>
    /// <param name="smRpOa">The originating address.</param>
    /// <param name="smRpUi">The short message transfer protocol data unit.</param>
    public MapMtForwardShortMessage(MapSmsAddress smRpDa, MapSmsAddress smRpOa, ReadOnlyMemory<byte> smRpUi)
    {
        SmRpDa = smRpDa ?? throw new ArgumentNullException(nameof(smRpDa));
        SmRpOa = smRpOa ?? throw new ArgumentNullException(nameof(smRpOa));
        SmRpUi = smRpUi.IsEmpty ? throw new ArgumentException("MT-ForwardSM user information is required.", nameof(smRpUi)) : smRpUi;
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
        MapSmsParameterSet set = new();
        set.Add(SmRpDaTag, SmRpDa.Encode());
        set.Add(SmRpOaTag, SmRpOa.Encode());
        set.Add(SmRpUiTag, SmRpUi.Span);
        return set.Encode();
    }

    /// <summary>Attempts to decode MT-ForwardSM parameters.</summary>
    /// <param name="data">The encoded parameters.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapMtForwardShortMessage? message, out string? error)
    {
        message = null;
        if (!MapSmsParameterSet.TryDecode(data, out MapSmsParameterSet? set, out error))
        {
            return false;
        }

        MapSmsAddress? smRpDa = null;
        MapSmsAddress? smRpOa = null;
        ReadOnlyMemory<byte> smRpUi = default;
        foreach (MapSmsParameter parameter in set!.Snapshot())
        {
            if (parameter.TagNumber == SmRpDaTag)
            {
                smRpDa = MapSmsAddress.Decode(parameter.Value.Span, oddDigitCount: true);
            }
            else if (parameter.TagNumber == SmRpOaTag)
            {
                smRpOa = MapSmsAddress.Decode(parameter.Value.Span, oddDigitCount: true);
            }
            else if (parameter.TagNumber == SmRpUiTag)
            {
                smRpUi = parameter.Value;
            }
        }

        if (smRpDa is null || smRpOa is null || smRpUi.IsEmpty)
        {
            error = "MT-ForwardSM requires SM-RP-DA, SM-RP-OA, and SM-RP-UI.";
            return false;
        }

        message = new(smRpDa, smRpOa, smRpUi);
        error = null;
        return true;
    }
}
