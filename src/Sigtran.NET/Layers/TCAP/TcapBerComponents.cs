using System.Formats.Asn1;

namespace Sigtran.NET.Layers.TCAP;

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
    private static readonly Asn1Tag InvokeTag = new(TagClass.ContextSpecific, 1, isConstructed: true);
    private static readonly Asn1Tag LinkedIdTag = new(TagClass.ContextSpecific, 0);

    /// <summary>Creates a BER TCAP Invoke component.</summary>
    /// <param name="invokeId">The invoke id.</param>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="parameters">One complete encoded ASN.1 argument, or empty if absent.</param>
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

    /// <summary>The encoded ASN.1 argument including its tag and length, if present.</summary>
    public ReadOnlyMemory<byte> Parameters { get; }

    /// <summary>Encodes this component as BER bytes.</summary>
    /// <returns>The encoded component.</returns>
    public byte[] Encode()
    {
        AsnWriter writer = new(AsnEncodingRules.BER);
        using (writer.PushSequence(InvokeTag))
        {
            writer.WriteInteger(unchecked((sbyte)InvokeId));
            if (LinkedInvokeId is { } linkedId)
            {
                writer.WriteInteger(unchecked((sbyte)linkedId), LinkedIdTag);
            }

            writer.WriteInteger((byte)OperationCode);
            if (!Parameters.IsEmpty)
            {
                writer.WriteEncodedValue(Parameters.Span);
            }
        }

        return writer.Encode();
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
        try
        {
            AsnReader wrapper = new(data.ToArray(), AsnEncodingRules.BER);
            AsnReader fields = wrapper.ReadSequence(InvokeTag);
            wrapper.ThrowIfNotEmpty();
            byte invokeId = ReadInvokeId(fields);
            byte? linkedId = fields.HasData && fields.PeekTag().HasSameClassAndValue(LinkedIdTag)
                ? ReadInvokeId(fields, LinkedIdTag)
                : null;
            if (!fields.TryReadInt32(out int operation) || operation is < 0 or > byte.MaxValue)
            {
                throw new AsnContentException("TCAP requires a supported local operation code.");
            }

            ReadOnlyMemory<byte> argument = fields.HasData ? fields.ReadEncodedValue() : default;
            fields.ThrowIfNotEmpty();
            component = new(invokeId, (TcapOperationCode)operation, argument, linkedId);
            error = null;
            return true;
        }
        catch (AsnContentException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    internal static byte ReadInvokeId(AsnReader fields, Asn1Tag? tag = null)
    {
        // Public IDs preserve the octet; Q.773 interprets that octet as a signed INTEGER.
        if (!fields.TryReadInt32(out int value, tag) || value is < sbyte.MinValue or > sbyte.MaxValue)
        {
            throw new AsnContentException("TCAP invoke ID must be in -128..127.");
        }

        return unchecked((byte)value);
    }
}
