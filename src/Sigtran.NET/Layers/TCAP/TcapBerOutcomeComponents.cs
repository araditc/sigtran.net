using System.Formats.Asn1;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>Convenience names for local application error codes.</summary>
public enum TcapReturnErrorCode : byte
{
    /// <summary>Unrecognized error.</summary>
    Unrecognized = 0,
    /// <summary>Unexpected data value.</summary>
    UnexpectedDataValue = 1,
    /// <summary>System failure.</summary>
    SystemFailure = 2
}

/// <summary>Identifies a TCAP Reject problem by family and meaning.</summary>
public enum TcapRejectProblemCode : byte
{
    /// <summary>General: unrecognized component.</summary>
    UnrecognizedComponent = 0,
    /// <summary>General: mistyped component.</summary>
    MistypedComponent = 1,
    /// <summary>Invoke: duplicate invocation ID.</summary>
    DuplicateInvokeId = 2,
    /// <summary>General: badly structured component.</summary>
    BadlyStructuredComponent = 3,
    /// <summary>Invoke: unrecognized operation.</summary>
    UnrecognizedOperation = 11,
    /// <summary>Invoke: mistyped argument.</summary>
    MistypedArgument = 12,
    /// <summary>Invoke: resource limitation.</summary>
    ResourceLimitation = 13,
    /// <summary>Invoke: release in progress.</summary>
    ReleaseInProgress = 14,
    /// <summary>Invoke: unrecognized linked ID.</summary>
    UnrecognizedLinkedId = 15,
    /// <summary>Invoke: unexpected linked response.</summary>
    UnexpectedLinkedResponse = 16,
    /// <summary>Invoke: unexpected linked operation.</summary>
    UnexpectedLinkedOperation = 17,
    /// <summary>Return result: unrecognized invocation.</summary>
    ResultUnrecognizedInvokeId = 20,
    /// <summary>Return result: unexpected response.</summary>
    UnexpectedResult = 21,
    /// <summary>Return result: mistyped result value.</summary>
    MistypedResult = 22,
    /// <summary>Return error: unrecognized invocation.</summary>
    ErrorUnrecognizedInvokeId = 30,
    /// <summary>Return error: unexpected error response.</summary>
    UnexpectedErrorResponse = 31,
    /// <summary>Return error: unrecognized error code.</summary>
    UnrecognizedErrorCode = 32,
    /// <summary>Return error: unexpected error code.</summary>
    UnexpectedErrorCode = 33,
    /// <summary>Return error: mistyped error parameter.</summary>
    MistypedErrorParameter = 34
}

/// <summary>Represents a TCAP ReturnResultLast component.</summary>
public sealed class TcapBerReturnResultComponent
{
    /// <summary>Creates a result component.</summary>
    /// <param name="invokeId">The invoke ID octet.</param>
    /// <param name="operationCode">The local operation code when a result value is present.</param>
    /// <param name="parameters">The complete encoded result value, or empty for an ID-only result.</param>
    public TcapBerReturnResultComponent(byte invokeId, TcapOperationCode? operationCode, ReadOnlyMemory<byte> parameters)
    {
        InvokeId = invokeId;
        OperationCode = operationCode;
        Parameters = parameters;
    }

    /// <summary>The invoke ID octet.</summary>
    public byte InvokeId { get; }

    /// <summary>The local operation code, if present.</summary>
    public TcapOperationCode? OperationCode { get; }

    /// <summary>The encoded result value, if present.</summary>
    public ReadOnlyMemory<byte> Parameters { get; }

    /// <summary>Encodes the component, with the optional opcode and result inside a SEQUENCE.</summary>
    /// <returns>The complete component TLV.</returns>
    public byte[] Encode()
    {
        AsnWriter writer = new(AsnEncodingRules.BER);
        using (writer.PushSequence(TcapOutcomeEncoding.Tag(TcapComponentType.ReturnResultLast)))
        {
            writer.WriteInteger(unchecked((sbyte)InvokeId));
            if (!Parameters.IsEmpty)
            {
                if (!OperationCode.HasValue)
                {
                    throw new InvalidOperationException("A TCAP result value requires the local operation code.");
                }

                using (writer.PushSequence())
                {
                    writer.WriteInteger((byte)OperationCode.Value);
                    writer.WriteEncodedValue(Parameters.Span);
                }
            }
        }

        return writer.Encode();
    }

    /// <summary>Attempts to decode one complete ReturnResultLast component.</summary>
    /// <param name="data">The encoded component bytes.</param>
    /// <param name="component">The result on success.</param>
    /// <param name="error">The decoding error, if any.</param>
    /// <returns>True when decoding succeeds.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerReturnResultComponent? component, out string? error)
    {
        component = null;
        try
        {
            AsnReader fields = TcapOutcomeEncoding.ReadFields(data, TcapComponentType.ReturnResultLast);
            byte invokeId = TcapBerInvokeComponent.ReadInvokeId(fields);
            TcapOperationCode? opcode = null;
            ReadOnlyMemory<byte> result = default;
            if (fields.HasData)
            {
                AsnReader sequence = fields.ReadSequence();
                opcode = (TcapOperationCode)TcapOutcomeEncoding.ReadLocalCode(sequence);
                result = sequence.ReadEncodedValue();
                sequence.ThrowIfNotEmpty();
            }

            fields.ThrowIfNotEmpty();
            component = new(invokeId, opcode, result);
            error = null;
            return true;
        }
        catch (AsnContentException exception)
        {
            error = exception.Message;
            return false;
        }
    }
}

/// <summary>Represents a TCAP ReturnError component with a local error code.</summary>
public sealed class TcapBerReturnErrorComponent
{
    /// <summary>Creates an error component.</summary>
    /// <param name="invokeId">The invoke ID octet.</param>
    /// <param name="errorCode">The application-defined local error code.</param>
    /// <param name="parameters">The complete encoded error parameter, or empty if absent.</param>
    public TcapBerReturnErrorComponent(byte invokeId, TcapReturnErrorCode errorCode, ReadOnlyMemory<byte> parameters)
    {
        InvokeId = invokeId;
        ErrorCode = errorCode;
        Parameters = parameters;
    }

    /// <summary>The invoke ID octet.</summary>
    public byte InvokeId { get; }

    /// <summary>The application-defined local error code.</summary>
    public TcapReturnErrorCode ErrorCode { get; }

    /// <summary>The encoded error parameter, if present.</summary>
    public ReadOnlyMemory<byte> Parameters { get; }

    /// <summary>Encodes the component with its optional open-type parameter.</summary>
    /// <returns>The complete component TLV.</returns>
    public byte[] Encode()
    {
        AsnWriter writer = new(AsnEncodingRules.BER);
        using (writer.PushSequence(TcapOutcomeEncoding.Tag(TcapComponentType.ReturnError)))
        {
            writer.WriteInteger(unchecked((sbyte)InvokeId));
            writer.WriteInteger((byte)ErrorCode);
            if (!Parameters.IsEmpty)
            {
                writer.WriteEncodedValue(Parameters.Span);
            }
        }

        return writer.Encode();
    }

    /// <summary>Attempts to decode one complete ReturnError component.</summary>
    /// <param name="data">The encoded component bytes.</param>
    /// <param name="component">The decoded error on success.</param>
    /// <param name="error">The decoding error, if any.</param>
    /// <returns>True when decoding succeeds.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerReturnErrorComponent? component, out string? error)
    {
        component = null;
        try
        {
            AsnReader fields = TcapOutcomeEncoding.ReadFields(data, TcapComponentType.ReturnError);
            byte invokeId = TcapBerInvokeComponent.ReadInvokeId(fields);
            var code = (TcapReturnErrorCode)TcapOutcomeEncoding.ReadLocalCode(fields);
            ReadOnlyMemory<byte> parameter = fields.HasData ? fields.ReadEncodedValue() : default;
            fields.ThrowIfNotEmpty();
            component = new(invokeId, code, parameter);
            error = null;
            return true;
        }
        catch (AsnContentException exception)
        {
            error = exception.Message;
            return false;
        }
    }
}

/// <summary>Represents a TCAP Reject component with a categorized problem.</summary>
public sealed class TcapBerRejectComponent
{
    /// <summary>Creates a Reject correlated to a known invoke ID.</summary>
    /// <param name="invokeId">The invoke ID octet.</param>
    /// <param name="problemCode">The reject problem.</param>
    public TcapBerRejectComponent(byte invokeId, TcapRejectProblemCode problemCode)
        : this((byte?)invokeId, problemCode)
    {
    }

    /// <summary>Creates a Reject, optionally without an invoke ID.</summary>
    /// <param name="invokeId">The invoke ID octet, or null if unavailable.</param>
    /// <param name="problemCode">The reject problem.</param>
    public TcapBerRejectComponent(byte? invokeId, TcapRejectProblemCode problemCode)
    {
        CorrelatedInvokeId = invokeId;
        ProblemCode = problemCode;
    }

    /// <summary>The invoke ID octet, or zero if no ID is present. Use <see cref="CorrelatedInvokeId"/> to distinguish absence.</summary>
    public byte InvokeId => CorrelatedInvokeId.GetValueOrDefault();

    /// <summary>The optional correlated invoke ID.</summary>
    public byte? CorrelatedInvokeId { get; }

    /// <summary>The reject problem.</summary>
    public TcapRejectProblemCode ProblemCode { get; }

    /// <summary>Encodes the component.</summary>
    /// <returns>The complete component TLV.</returns>
    public byte[] Encode()
    {
        (int family, int code) = ProblemCode switch
        {
            TcapRejectProblemCode.UnrecognizedComponent => (0, 0),
            TcapRejectProblemCode.MistypedComponent => (0, 1),
            TcapRejectProblemCode.BadlyStructuredComponent => (0, 2),
            TcapRejectProblemCode.DuplicateInvokeId => (1, 0),
            >= TcapRejectProblemCode.UnrecognizedOperation and <= TcapRejectProblemCode.UnexpectedLinkedOperation => (1, (byte)ProblemCode - 10),
            >= TcapRejectProblemCode.ResultUnrecognizedInvokeId and <= TcapRejectProblemCode.MistypedResult => (2, (byte)ProblemCode - 20),
            >= TcapRejectProblemCode.ErrorUnrecognizedInvokeId and <= TcapRejectProblemCode.MistypedErrorParameter => (3, (byte)ProblemCode - 30),
            _ => throw new InvalidOperationException("Invalid TCAP Reject problem.")
        };
        AsnWriter writer = new(AsnEncodingRules.BER);
        using (writer.PushSequence(TcapOutcomeEncoding.Tag(TcapComponentType.Reject)))
        {
            if (CorrelatedInvokeId is { } invokeId)
            {
                writer.WriteInteger(unchecked((sbyte)invokeId));
            }
            else
            {
                writer.WriteNull();
            }

            writer.WriteInteger(code, new Asn1Tag(TagClass.ContextSpecific, family));
        }

        return writer.Encode();
    }

    /// <summary>Attempts to decode one complete Reject component.</summary>
    /// <param name="data">The encoded component bytes.</param>
    /// <param name="component">The decoded Reject on success.</param>
    /// <param name="error">The decoding error, if any.</param>
    /// <returns>True when decoding succeeds.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapBerRejectComponent? component, out string? error)
    {
        component = null;
        try
        {
            AsnReader fields = TcapOutcomeEncoding.ReadFields(data, TcapComponentType.Reject);
            byte? invokeId = null;
            if (fields.PeekTag().HasSameClassAndValue(Asn1Tag.Null))
            {
                fields.ReadNull();
            }
            else
            {
                invokeId = TcapBerInvokeComponent.ReadInvokeId(fields);
            }

            Asn1Tag tag = fields.PeekTag();
            if (tag.TagClass != TagClass.ContextSpecific || !fields.TryReadInt32(out int code, tag))
            {
                throw new AsnContentException("Expected a categorized TCAP Reject problem.");
            }

            var problem = (tag.TagValue, code) switch
            {
                (0, 0) => TcapRejectProblemCode.UnrecognizedComponent,
                (0, 1) => TcapRejectProblemCode.MistypedComponent,
                (0, 2) => TcapRejectProblemCode.BadlyStructuredComponent,
                (1, 0) => TcapRejectProblemCode.DuplicateInvokeId,
                (1, >= 1 and <= 7) => (TcapRejectProblemCode)(10 + code),
                (2, >= 0 and <= 2) => (TcapRejectProblemCode)(20 + code),
                (3, >= 0 and <= 4) => (TcapRejectProblemCode)(30 + code),
                _ => throw new AsnContentException("Invalid TCAP Reject problem.")
            };

            fields.ThrowIfNotEmpty();
            component = new(invokeId, problem);
            error = null;
            return true;
        }
        catch (AsnContentException exception)
        {
            error = exception.Message;
            return false;
        }
    }
}

/// <summary>Reads the common envelope and local code range of TCAP outcome components.</summary>
internal static class TcapOutcomeEncoding
{
    internal static Asn1Tag Tag(TcapComponentType type) => new(TagClass.ContextSpecific, (byte)type, isConstructed: true);

    internal static AsnReader ReadFields(ReadOnlySpan<byte> data, TcapComponentType type)
    {
        AsnReader wrapper = new(data.ToArray(), AsnEncodingRules.BER);
        AsnReader fields = wrapper.ReadSequence(Tag(type));
        wrapper.ThrowIfNotEmpty();
        return fields;
    }

    internal static byte ReadLocalCode(AsnReader fields)
    {
        if (!fields.TryReadInt32(out int value) || value is < 0 or > byte.MaxValue)
        {
            throw new AsnContentException("TCAP requires a supported local code.");
        }

        return (byte)value;
    }
}
