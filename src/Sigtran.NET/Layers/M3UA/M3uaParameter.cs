using System.Buffers.Binary;

namespace Sigtran.NET.Layers.M3UA;

/// <summary>
/// A decoded M3UA TLV parameter. The Length includes tag, length, and value,
/// while PaddedLength also includes trailing alignment octets.
/// </summary>
public readonly ref struct M3uaParameter
{
    /// <summary>Creates a decoded M3UA parameter view.</summary>
    /// <param name="tag">The parameter tag.</param>
    /// <param name="length">The encoded parameter length, excluding padding.</param>
    /// <param name="value">The decoded parameter value bytes.</param>
    /// <param name="paddedLength">The encoded parameter length including padding.</param>
    public M3uaParameter(M3uaParameterTag tag, int length, ReadOnlySpan<byte> value, int paddedLength)
    {
        Tag = tag;
        Length = length;
        Value = value;
        PaddedLength = paddedLength;
    }

    /// <summary>The parameter tag.</summary>
    public M3uaParameterTag Tag { get; }

    /// <summary>The parameter length, including the TLV header and excluding padding.</summary>
    public int Length { get; }

    /// <summary>The parameter value bytes.</summary>
    public ReadOnlySpan<byte> Value { get; }

    /// <summary>The parameter length rounded up to a 32-bit boundary.</summary>
    public int PaddedLength { get; }
}

/// <summary>
/// Iterates over an M3UA parameter block without allocating.
/// </summary>
public ref struct M3uaParameterReader
{
    private ReadOnlySpan<byte> _remaining;

    /// <summary>Creates a reader over an encoded M3UA parameter block.</summary>
    /// <param name="parameters">The encoded parameter block.</param>
    public M3uaParameterReader(ReadOnlySpan<byte> parameters)
    {
        _remaining = parameters;
    }

    /// <summary>Reads the next parameter, returning false at end of input or on error.</summary>
    /// <param name="parameter">The decoded parameter on success.</param>
    /// <param name="error">An error message if decoding fails.</param>
    /// <returns>True if a parameter was decoded; otherwise false.</returns>
    public bool TryRead(out M3uaParameter parameter, out string? error)
    {
        parameter = default;
        error = null;

        if (_remaining.IsEmpty)
        {
            return false;
        }

        if (_remaining.Length < M3uaProtocol.ParameterHeaderLength)
        {
            error = "Incomplete M3UA parameter header";
            return false;
        }

        M3uaParameterTag tag = (M3uaParameterTag)BinaryPrimitives.ReadUInt16BigEndian(_remaining);
        ushort length = BinaryPrimitives.ReadUInt16BigEndian(_remaining.Slice(2, 2));

        if (length < M3uaProtocol.ParameterHeaderLength)
        {
            error = $"Invalid M3UA parameter length {length}";
            return false;
        }

        if (length > _remaining.Length)
        {
            error = $"M3UA parameter length {length} exceeds remaining buffer {_remaining.Length}";
            return false;
        }

        int paddedLength = M3uaProtocol.AlignToUInt32(length);
        if (paddedLength > _remaining.Length)
        {
            error = $"M3UA padded parameter length {paddedLength} exceeds remaining buffer {_remaining.Length}";
            return false;
        }

        parameter = new(
            tag,
            length,
            _remaining.Slice(M3uaProtocol.ParameterHeaderLength, length - M3uaProtocol.ParameterHeaderLength),
            paddedLength);
        _remaining = _remaining.Slice(paddedLength);
        return true;
    }

    /// <summary>Finds the first parameter with the specified tag.</summary>
    /// <param name="parameters">The encoded parameter block.</param>
    /// <param name="tag">The tag to search for.</param>
    /// <param name="value">The matching parameter value on success.</param>
    /// <param name="error">An error message if parsing fails or the parameter is not found.</param>
    /// <returns>True if the parameter was found; otherwise false.</returns>
    public static bool TryFind(
        ReadOnlySpan<byte> parameters,
        M3uaParameterTag tag,
        out ReadOnlySpan<byte> value,
        out string? error)
    {
        value = default;
        M3uaParameterReader reader = new(parameters);

        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            if (parameter.Tag == tag)
            {
                value = parameter.Value;
                return true;
            }
        }

        if (error is not null)
        {
            return false;
        }

        error = $"M3UA parameter {tag} not found";
        return false;
    }

    /// <summary>
    /// Counts the TLV parameters in an encoded M3UA parameter block.
    /// </summary>
    /// <param name="parameters">The encoded parameter block.</param>
    /// <param name="count">The decoded parameter count on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if all parameters were counted; otherwise false.</returns>
    public static bool TryCount(ReadOnlySpan<byte> parameters, out int count, out string? error)
    {
        count = 0;
        M3uaParameterReader reader = new(parameters);
        while (reader.TryRead(out _, out error))
        {
            count++;
        }

        return error is null;
    }
}

/// <summary>
/// Writes M3UA TLV parameters into caller-provided buffers.
/// </summary>
public static class M3uaParameterWriter
{
    /// <summary>Returns the padded encoded length for a value of the supplied length.</summary>
    /// <param name="valueLength">The parameter value length.</param>
    /// <returns>The encoded TLV length rounded up to a 32-bit boundary.</returns>
    public static int GetPaddedLength(int valueLength)
    {
        return M3uaProtocol.AlignToUInt32(M3uaProtocol.ParameterHeaderLength + valueLength);
    }

    /// <summary>Writes a complete M3UA TLV parameter.</summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="tag">The parameter tag.</param>
    /// <param name="value">The parameter value bytes.</param>
    /// <param name="written">The number of bytes written, including padding.</param>
    /// <param name="error">Set if the parameter cannot be written.</param>
    /// <returns>True if the parameter was written; otherwise false.</returns>
    public static bool TryWrite(
        Span<byte> buffer,
        M3uaParameterTag tag,
        ReadOnlySpan<byte> value,
        out int written,
        out string? error)
    {
        written = 0;
        if (!TryWriteHeader(buffer, tag, value.Length, out int paddedLength, out error))
        {
            return false;
        }

        value.CopyTo(buffer.Slice(M3uaProtocol.ParameterHeaderLength));
        written = paddedLength;
        return true;
    }

    /// <summary>Writes an M3UA TLV header and clears the trailing padding bytes.</summary>
    /// <param name="buffer">The destination buffer.</param>
    /// <param name="tag">The parameter tag.</param>
    /// <param name="valueLength">The value length that will follow the TLV header.</param>
    /// <param name="paddedLength">The encoded parameter length including padding.</param>
    /// <param name="error">Set if the header cannot be written.</param>
    /// <returns>True if the header was written; otherwise false.</returns>
    public static bool TryWriteHeader(
        Span<byte> buffer,
        M3uaParameterTag tag,
        int valueLength,
        out int paddedLength,
        out string? error)
    {
        paddedLength = 0;
        error = null;

        if (valueLength < 0)
        {
            error = $"Invalid M3UA parameter value length {valueLength}";
            return false;
        }

        int parameterLength = M3uaProtocol.ParameterHeaderLength + valueLength;
        if (parameterLength > ushort.MaxValue)
        {
            error = $"M3UA parameter length {parameterLength} exceeds 16-bit limit";
            return false;
        }

        paddedLength = M3uaProtocol.AlignToUInt32(parameterLength);
        if (buffer.Length < paddedLength)
        {
            error = $"Insufficient buffer size: need {paddedLength}, have {buffer.Length}";
            return false;
        }

        BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)tag);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2, 2), (ushort)parameterLength);
        buffer.Slice(parameterLength, paddedLength - parameterLength).Clear();
        return true;
    }
}
