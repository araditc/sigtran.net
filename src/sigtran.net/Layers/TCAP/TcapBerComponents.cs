namespace sigtran.net.Layers.TCAP;

/// <summary>
/// TCAP component type tag numbers.
/// </summary>
public enum TcapComponentType : byte
{
    /// <summary>Invoke component.</summary>
    Invoke = 1,

    /// <summary>Return Result Last component.</summary>
    ReturnResultLast = 2,

    /// <summary>Return Error component.</summary>
    ReturnError = 3,

    /// <summary>Reject component.</summary>
    Reject = 4,

    /// <summary>Return Result Not Last component.</summary>
    ReturnResultNotLast = 7
}

/// <summary>
/// Provides TCAP component tags and field tags.
/// </summary>
public static class TcapComponentTags
{
    /// <summary>Invoke ID universal integer field.</summary>
    public static readonly TcapBerTag Integer = new(TcapBerTagClass.Universal, constructed: false, number: 2);

    /// <summary>Operation code universal integer field.</summary>
    public static readonly TcapBerTag OperationCode = new(TcapBerTagClass.Universal, constructed: false, number: 2);

    /// <summary>Parameter set universal octet string field.</summary>
    public static readonly TcapBerTag Parameter = new(TcapBerTagClass.Universal, constructed: false, number: 4);

    /// <summary>Builds a context-specific component tag.</summary>
    /// <param name="componentType">The component type.</param>
    /// <returns>The BER component tag.</returns>
    public static TcapBerTag Component(TcapComponentType componentType)
    {
        return new(TcapBerTagClass.ContextSpecific, constructed: true, (byte)componentType);
    }
}

/// <summary>
/// Represents a BER-encoded TCAP Invoke component.
/// </summary>
public sealed class TcapBerInvokeComponent
{
    /// <summary>Creates a BER TCAP Invoke component.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="parameters">The operation parameters.</param>
    /// <param name="linkedInvokeId">The optional linked invoke id.</param>
    public TcapBerInvokeComponent(byte invokeId, TcapOperationCode operationCode, ReadOnlyMemory<byte> parameters, byte? linkedInvokeId = null)
    {
        InvokeId = invokeId;
        OperationCode = operationCode;
        Parameters = parameters;
        LinkedInvokeId = linkedInvokeId;
    }

    /// <summary>The invoke id.</summary>
    public byte InvokeId { get; }

    /// <summary>The optional linked invoke id.</summary>
    public byte? LinkedInvokeId { get; }

    /// <summary>The operation code.</summary>
    public TcapOperationCode OperationCode { get; }

    /// <summary>The operation parameters.</summary>
    public ReadOnlyMemory<byte> Parameters { get; }

    /// <summary>Encodes this component as BER bytes.</summary>
    /// <returns>The encoded component.</returns>
    public byte[] Encode()
    {
        Span<byte> content = stackalloc byte[512];
        int offset = 0;
        WriteRequiredInteger(content, ref offset, InvokeId);
        if (LinkedInvokeId.HasValue)
        {
            WriteRequiredInteger(content, ref offset, LinkedInvokeId.Value);
        }

        WriteRequiredInteger(content, ref offset, (byte)OperationCode);
        WriteRequiredElement(content, ref offset, TcapComponentTags.Parameter, Parameters.Span);

        byte[] result = new byte[offset + 4];
        if (!TcapBer.TryWriteElement(result, TcapComponentTags.Component(TcapComponentType.Invoke), content.Slice(0, offset), out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        Array.Resize(ref result, written);
        return result;
    }

    /// <summary>Attempts to decode a BER TCAP Invoke component.</summary>
    /// <param name="data">The encoded component bytes.</param>
    /// <param name="component">The decoded component on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerInvokeComponent? component, out string? error)
    {
        component = null;
        error = null;
        if (!TcapBer.TryReadElement(data, out TcapBerElement wrapper, out error))
        {
            return false;
        }

        if (wrapper.Tag.TagClass != TcapBerTagClass.ContextSpecific || wrapper.Tag.Number != (byte)TcapComponentType.Invoke)
        {
            error = "BER component is not a TCAP Invoke";
            return false;
        }

        ReadOnlySpan<byte> content = wrapper.Value.Span;
        if (!TryReadInteger(content, out byte invokeId, out int consumed, out error))
        {
            return false;
        }

        content = content[consumed..];
        if (!TryReadInteger(content, out byte secondInteger, out consumed, out error))
        {
            return false;
        }

        content = content[consumed..];
        byte? linkedInvokeId = null;
        byte operationCodeValue = secondInteger;
        if (content.Length > 0 && content[0] == TcapComponentTags.Integer.Encode())
        {
            linkedInvokeId = secondInteger;
            if (!TryReadInteger(content, out operationCodeValue, out consumed, out error))
            {
                return false;
            }

            content = content[consumed..];
        }

        if (!TcapBer.TryReadElement(content, out TcapBerElement parameters, out error))
        {
            return false;
        }

        if (parameters.Tag.Encode() != TcapComponentTags.Parameter.Encode())
        {
            error = "TCAP Invoke parameter field is missing";
            return false;
        }

        component = new(invokeId, (TcapOperationCode)operationCodeValue, parameters.Value, linkedInvokeId);
        return true;
    }

    private static void WriteRequiredInteger(Span<byte> destination, ref int offset, byte value)
    {
        WriteRequiredElement(destination, ref offset, TcapComponentTags.Integer, [value]);
    }

    private static void WriteRequiredElement(Span<byte> destination, ref int offset, TcapBerTag tag, ReadOnlySpan<byte> value)
    {
        if (!TcapBer.TryWriteElement(destination[offset..], tag, value, out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        offset += written;
    }

    private static bool TryReadInteger(ReadOnlySpan<byte> data, out byte value, out int consumed, out string? error)
    {
        value = 0;
        consumed = 0;
        if (!TcapBer.TryReadElement(data, out TcapBerElement element, out error))
        {
            return false;
        }

        if (element.Tag.Encode() != TcapComponentTags.Integer.Encode() || element.Value.Length != 1)
        {
            error = "TCAP integer field is malformed";
            return false;
        }

        value = element.Value.Span[0];
        consumed = element.TotalLength;
        return true;
    }
}
