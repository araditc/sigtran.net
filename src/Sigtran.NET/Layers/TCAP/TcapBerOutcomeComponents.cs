namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// TCAP ReturnError problem codes used by the SDK foundation.
/// </summary>
public enum TcapReturnErrorCode : byte
{
    /// <summary>Unrecognized error.</summary>
    Unrecognized = 0,

    /// <summary>Unexpected data value.</summary>
    UnexpectedDataValue = 1,

    /// <summary>System failure.</summary>
    SystemFailure = 2
}

/// <summary>
/// TCAP Reject problem codes used by the SDK foundation.
/// </summary>
public enum TcapRejectProblemCode : byte
{
    /// <summary>General unrecognized component.</summary>
    UnrecognizedComponent = 0,

    /// <summary>Mistyped component.</summary>
    MistypedComponent = 1,

    /// <summary>Duplicate invoke identifier.</summary>
    DuplicateInvokeId = 2
}

/// <summary>
/// Represents a BER TCAP ReturnResult component.
/// </summary>
public sealed class TcapBerReturnResultComponent
{
    /// <summary>Creates a BER ReturnResult component.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="operationCode">The optional operation code.</param>
    /// <param name="parameters">The result parameters.</param>
    public TcapBerReturnResultComponent(byte invokeId, TcapOperationCode? operationCode, ReadOnlyMemory<byte> parameters)
    {
        InvokeId = invokeId;
        OperationCode = operationCode;
        Parameters = parameters;
    }

    /// <summary>The invoke id.</summary>
    public byte InvokeId { get; }

    /// <summary>The optional operation code.</summary>
    public TcapOperationCode? OperationCode { get; }

    /// <summary>The result parameters.</summary>
    public ReadOnlyMemory<byte> Parameters { get; }

    /// <summary>Encodes the component.</summary>
    /// <returns>The encoded component bytes.</returns>
    public byte[] Encode()
    {
        TcapComponentContentWriter writer = new();
        writer.WriteInteger(InvokeId);
        if (OperationCode.HasValue)
        {
            writer.WriteInteger((byte)OperationCode.Value);
        }

        writer.WriteElement(TcapComponentTags.Parameter, Parameters.Span);
        return writer.Wrap(TcapComponentTags.Component(TcapComponentType.ReturnResultLast));
    }

    /// <summary>Attempts to decode a ReturnResult component.</summary>
    /// <param name="data">The encoded bytes.</param>
    /// <param name="component">The decoded component on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerReturnResultComponent? component, out string? error)
    {
        component = null;
        if (!TcapComponentContentReader.TryUnwrap(data, TcapComponentType.ReturnResultLast, out TcapComponentContentReader reader, out error))
        {
            return false;
        }

        if (!reader.TryReadInteger(out byte invokeId, out error))
        {
            return false;
        }

        TcapOperationCode? operationCode = null;
        if (reader.PeekTag(TcapComponentTags.Integer))
        {
            if (!reader.TryReadInteger(out byte operationCodeValue, out error))
            {
                return false;
            }

            operationCode = (TcapOperationCode)operationCodeValue;
        }

        if (!reader.TryReadElement(TcapComponentTags.Parameter, out ReadOnlyMemory<byte> parameters, out error))
        {
            return false;
        }

        component = new(invokeId, operationCode, parameters);
        return true;
    }
}

/// <summary>
/// Represents a BER TCAP ReturnError component.
/// </summary>
public sealed class TcapBerReturnErrorComponent
{
    /// <summary>Creates a BER ReturnError component.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="parameters">The error parameters.</param>
    public TcapBerReturnErrorComponent(byte invokeId, TcapReturnErrorCode errorCode, ReadOnlyMemory<byte> parameters)
    {
        InvokeId = invokeId;
        ErrorCode = errorCode;
        Parameters = parameters;
    }

    /// <summary>The invoke id.</summary>
    public byte InvokeId { get; }

    /// <summary>The error code.</summary>
    public TcapReturnErrorCode ErrorCode { get; }

    /// <summary>The error parameters.</summary>
    public ReadOnlyMemory<byte> Parameters { get; }

    /// <summary>Encodes the component.</summary>
    /// <returns>The encoded component bytes.</returns>
    public byte[] Encode()
    {
        TcapComponentContentWriter writer = new();
        writer.WriteInteger(InvokeId);
        writer.WriteInteger((byte)ErrorCode);
        writer.WriteElement(TcapComponentTags.Parameter, Parameters.Span);
        return writer.Wrap(TcapComponentTags.Component(TcapComponentType.ReturnError));
    }

    /// <summary>Attempts to decode a ReturnError component.</summary>
    /// <param name="data">The encoded bytes.</param>
    /// <param name="component">The decoded component on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerReturnErrorComponent? component, out string? error)
    {
        component = null;
        if (!TcapComponentContentReader.TryUnwrap(data, TcapComponentType.ReturnError, out TcapComponentContentReader reader, out error)
            || !reader.TryReadInteger(out byte invokeId, out error)
            || !reader.TryReadInteger(out byte errorCode, out error)
            || !reader.TryReadElement(TcapComponentTags.Parameter, out ReadOnlyMemory<byte> parameters, out error))
        {
            return false;
        }

        component = new(invokeId, (TcapReturnErrorCode)errorCode, parameters);
        return true;
    }
}

/// <summary>
/// Represents a BER TCAP Reject component.
/// </summary>
public sealed class TcapBerRejectComponent
{
    /// <summary>Creates a BER Reject component.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="problemCode">The reject problem code.</param>
    public TcapBerRejectComponent(byte invokeId, TcapRejectProblemCode problemCode)
    {
        InvokeId = invokeId;
        ProblemCode = problemCode;
    }

    /// <summary>The invoke id.</summary>
    public byte InvokeId { get; }

    /// <summary>The reject problem code.</summary>
    public TcapRejectProblemCode ProblemCode { get; }

    /// <summary>Encodes the component.</summary>
    /// <returns>The encoded component bytes.</returns>
    public byte[] Encode()
    {
        TcapComponentContentWriter writer = new();
        writer.WriteInteger(InvokeId);
        writer.WriteInteger((byte)ProblemCode);
        return writer.Wrap(TcapComponentTags.Component(TcapComponentType.Reject));
    }

    /// <summary>Attempts to decode a Reject component.</summary>
    /// <param name="data">The encoded bytes.</param>
    /// <param name="component">The decoded component on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerRejectComponent? component, out string? error)
    {
        component = null;
        if (!TcapComponentContentReader.TryUnwrap(data, TcapComponentType.Reject, out TcapComponentContentReader reader, out error)
            || !reader.TryReadInteger(out byte invokeId, out error)
            || !reader.TryReadInteger(out byte problemCode, out error))
        {
            return false;
        }

        component = new(invokeId, (TcapRejectProblemCode)problemCode);
        return true;
    }
}

internal sealed class TcapComponentContentWriter
{
    private readonly byte[] _content;
    private int _offset;

    public TcapComponentContentWriter()
    {
        _content = new byte[512];
        _offset = 0;
    }

    public void WriteInteger(byte value)
    {
        Span<byte> encoded = stackalloc byte[1];
        encoded[0] = value;
        WriteElement(TcapComponentTags.Integer, encoded);
    }

    public void WriteElement(TcapBerTag tag, ReadOnlySpan<byte> value)
    {
        if (!TcapBer.TryWriteElement(_content.AsSpan(_offset), tag, value, out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        _offset += written;
    }

    public byte[] Wrap(TcapBerTag tag)
    {
        byte[] result = new byte[_offset + 4];
        if (!TcapBer.TryWriteElement(result, tag, _content.AsSpan(0, _offset), out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        Array.Resize(ref result, written);
        return result;
    }
}

internal sealed class TcapComponentContentReader
{
    private readonly ReadOnlyMemory<byte> _content;
    private int _offset;

    private TcapComponentContentReader(ReadOnlyMemory<byte> content)
    {
        _content = content;
        _offset = 0;
    }

    public static bool TryUnwrap(
        ReadOnlySpan<byte> data,
        TcapComponentType componentType,
        out TcapComponentContentReader reader,
        out string? error)
    {
        reader = null!;
        if (!TcapBer.TryReadElement(data, out TcapBerElement wrapper, out error))
        {
            return false;
        }

        if (wrapper.Tag.TagClass != TcapBerTagClass.ContextSpecific || wrapper.Tag.Number != (byte)componentType)
        {
            error = $"BER component is not {componentType}";
            return false;
        }

        reader = new(wrapper.Value);
        return true;
    }

    public bool PeekTag(TcapBerTag tag)
    {
        return _offset < _content.Length && _content.Span[_offset] == tag.Encode();
    }

    public bool TryReadInteger(out byte value, out string? error)
    {
        value = 0;
        if (!TcapBer.TryReadElement(_content.Span[_offset..], out TcapBerElement element, out error))
        {
            return false;
        }

        if (element.Tag.Encode() != TcapComponentTags.Integer.Encode() || element.Value.Length != 1)
        {
            error = "TCAP integer field is malformed";
            return false;
        }

        value = element.Value.Span[0];
        _offset += element.TotalLength;
        return true;
    }

    public bool TryReadElement(TcapBerTag tag, out ReadOnlyMemory<byte> value, out string? error)
    {
        value = default;
        if (!TcapBer.TryReadElement(_content.Span[_offset..], out TcapBerElement element, out error))
        {
            return false;
        }

        if (element.Tag.Encode() != tag.Encode())
        {
            error = "TCAP component field tag did not match";
            return false;
        }

        value = element.Value;
        _offset += element.TotalLength;
        return true;
    }
}
