namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// SCCP optional parameter names used by connectionless messages.
/// </summary>
public enum SccpOptionalParameterName : byte
{
    /// <summary>End of optional parameters.</summary>
    EndOfOptionalParameters = 0x00,

    /// <summary>Segmentation parameter.</summary>
    Segmentation = 0x10
}

/// <summary>
/// Represents the SCCP segmentation optional parameter.
/// </summary>
public readonly struct SccpSegmentationParameter
{
    /// <summary>The encoded segmentation parameter value length.</summary>
    public const int EncodedLength = 4;

    /// <summary>Creates an SCCP segmentation parameter.</summary>
    /// <param name="localReference">The 24-bit segmentation local reference.</param>
    /// <param name="remainingSegments">The number of remaining segments.</param>
    /// <param name="firstSegment">Whether this segment is the first segment.</param>
    public SccpSegmentationParameter(uint localReference, byte remainingSegments, bool firstSegment)
    {
        if (localReference > 0xFFFFFF)
        {
            throw new ArgumentOutOfRangeException(nameof(localReference), "Segmentation local reference must fit in 24 bits.");
        }

        if (remainingSegments > 0x0F)
        {
            throw new ArgumentOutOfRangeException(nameof(remainingSegments), "Remaining segments must fit in four bits.");
        }

        LocalReference = localReference;
        RemainingSegments = remainingSegments;
        FirstSegment = firstSegment;
    }

    /// <summary>The 24-bit segmentation local reference.</summary>
    public uint LocalReference { get; }

    /// <summary>The number of remaining segments.</summary>
    public byte RemainingSegments { get; }

    /// <summary>Whether this segment is the first segment.</summary>
    public bool FirstSegment { get; }

    /// <summary>Encodes the segmentation parameter value.</summary>
    /// <param name="destination">A buffer with at least four bytes.</param>
    public void Encode(Span<byte> destination)
    {
        if (destination.Length < EncodedLength)
        {
            throw new ArgumentException("SCCP segmentation parameter requires four bytes.", nameof(destination));
        }

        destination[0] = (byte)(RemainingSegments | (FirstSegment ? 0x80 : 0x00));
        destination[1] = (byte)(LocalReference >> 16);
        destination[2] = (byte)(LocalReference >> 8);
        destination[3] = (byte)LocalReference;
    }

    /// <summary>Decodes a segmentation parameter value.</summary>
    /// <param name="source">The encoded segmentation parameter value.</param>
    /// <returns>The decoded segmentation parameter.</returns>
    public static SccpSegmentationParameter Decode(ReadOnlySpan<byte> source)
    {
        if (source.Length != EncodedLength)
        {
            throw new ArgumentException("SCCP segmentation parameter value must be exactly four bytes.", nameof(source));
        }

        uint localReference = ((uint)source[1] << 16) | ((uint)source[2] << 8) | source[3];
        return new(localReference, (byte)(source[0] & 0x0F), (source[0] & 0x80) != 0);
    }

    /// <summary>Formats a compact segmentation summary.</summary>
    /// <returns>A compact segmentation summary.</returns>
    public string Describe()
    {
        return $"segref=0x{LocalReference:X6} remaining={RemainingSegments} first={FirstSegment}";
    }
}
