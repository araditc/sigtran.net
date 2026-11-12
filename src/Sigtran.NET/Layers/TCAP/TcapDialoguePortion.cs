using System.Formats.Asn1;
using Sigtran.NET.Core.Utilities;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Represents an ASN.1 object identifier used by TCAP dialogue portions.
/// </summary>
public sealed class TcapObjectIdentifier
{
    private string? _text;
    /// <summary>Creates an object identifier from arcs.</summary>
    /// <param name="arcs">The object identifier arcs.</param>
    public TcapObjectIdentifier(params uint[] arcs)
    {
        if (arcs.Length < 2)
        {
            throw new ArgumentException("Object identifier requires at least two arcs.", nameof(arcs));
        }

        if (arcs[0] > 2 || (arcs[0] < 2 && arcs[1] > 39))
        {
            throw new ArgumentOutOfRangeException(nameof(arcs), "Object identifier first arcs are invalid.");
        }

        Arcs = Array.AsReadOnly(arcs.ToArray());
    }

    /// <summary>The object identifier arcs.</summary>
    public IReadOnlyList<uint> Arcs { get; }

    /// <summary>Encodes the object identifier value bytes.</summary>
    /// <returns>The encoded object identifier value bytes.</returns>
    public byte[] EncodeValue()
    {
        List<byte> result = [];
        WriteBase128(result, (ulong)Arcs[0] * 40 + Arcs[1]);
        for (int i = 2; i < Arcs.Count; i++)
        {
            WriteBase128(result, Arcs[i]);
        }

        return result.ToArray();
    }

    /// <summary>Decodes object identifier value bytes.</summary>
    /// <param name="value">The encoded value bytes.</param>
    /// <returns>The decoded object identifier.</returns>
    public static TcapObjectIdentifier DecodeValue(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            throw new ArgumentException("Object identifier value is empty.", nameof(value));
        }

        int offset = 0;
        // The first subidentifier combines two arcs and can be larger than UInt32.
        ulong first = ReadBase128(value, ref offset, (ulong)uint.MaxValue + 80);
        uint root = (uint)Math.Min(first / 40, 2);
        List<uint> arcs = [root, (uint)(first - root * 40)];
        while (offset < value.Length)
        {
            arcs.Add((uint)ReadBase128(value, ref offset, uint.MaxValue));
        }

        return new(arcs.ToArray());
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _text ??= string.Join(".", Arcs);
    }

    private static ulong ReadBase128(ReadOnlySpan<byte> value, ref int offset, ulong max)
    {
        if (value[offset] == 0x80)
        {
            throw new ArgumentException("Object identifier arc has a leading zero group.", nameof(value));
        }

        ulong arc = 0;
        while (offset < value.Length)
        {
            byte octet = value[offset++];
            arc = (arc << 7) | (uint)(octet & 0x7F);
            if (arc > max)
            {
                throw new ArgumentException("Object identifier arc is out of range.", nameof(value));
            }

            if ((octet & 0x80) == 0)
            {
                return arc;
            }
        }

        throw new ArgumentException("Object identifier base-128 value is truncated.", nameof(value));
    }

    private static void WriteBase128(List<byte> result, ulong value)
    {
        Span<byte> stack = stackalloc byte[5];
        int count = 0;
        stack[count++] = (byte)(value & 0x7F);
        value >>= 7;
        while (value > 0)
        {
            stack[count++] = (byte)(0x80 | (value & 0x7F));
            value >>= 7;
        }

        for (int i = count - 1; i >= 0; i--)
        {
            result.Add(stack[i]);
        }
    }
}

/// <summary>Identifies the source of an association response diagnostic.</summary>
public enum TcapDialogueDiagnosticSource : byte
{
    /// <summary>The dialogue service user.</summary>
    User = 1,
    /// <summary>The dialogue service provider.</summary>
    Provider = 2
}

/// <summary>
/// Represents a Q.773 structured dialogue request (AARQ) or response (AARE).
/// </summary>
public sealed class TcapDialoguePortion
{
    private const string StructuredDialogueOid = "0.0.17.773.1.1.1";
    private static readonly byte[] StructuredDialogueValue = new TcapObjectIdentifier(0, 0, 17, 773, 1, 1, 1).EncodeValue();
    private static readonly TcapBerTag External = new(TcapBerTagClass.Universal, true, 8);
    private static readonly Asn1Tag SingleAsn1 = Context(0);
    private static readonly Asn1Tag ApplicationContextTag = Context(1);
    private static readonly Asn1Tag ResultTag = Context(2);
    private static readonly Asn1Tag DiagnosticTag = Context(3);
    private static readonly Asn1Tag UserInformationTag = Context(30);
    private static readonly Asn1Tag ProtocolVersionTag = new(TagClass.ContextSpecific, 0);

    /// <summary>Creates a structured dialogue request.</summary>
    /// <param name="applicationContext">The requested application context.</param>
    /// <param name="userInformation">Optional concatenated, encoded EXTERNAL values.</param>
    public TcapDialoguePortion(TcapObjectIdentifier applicationContext, ReadOnlyMemory<byte> userInformation = default)
        : this(applicationContext, userInformation, null, TcapDialogueDiagnosticSource.User, 0)
    {
    }

    private TcapDialoguePortion(TcapObjectIdentifier applicationContext, ReadOnlyMemory<byte> userInformation,
        bool? accepted, TcapDialogueDiagnosticSource source, byte diagnostic)
    {
        ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        UserInformation = userInformation;
        Accepted = accepted;
        DiagnosticSource = source;
        Diagnostic = diagnostic;
    }

    /// <summary>The requested or negotiated application context.</summary>
    public TcapObjectIdentifier ApplicationContext { get; }

    /// <summary>The concatenated EXTERNAL values in user-information, if present.</summary>
    public ReadOnlyMemory<byte> UserInformation { get; }

    /// <summary>Null for AARQ, true for accepted AARE, or false for rejected AARE.</summary>
    public bool? Accepted { get; }

    /// <summary>The response diagnostic source.</summary>
    public TcapDialogueDiagnosticSource DiagnosticSource { get; }

    /// <summary>The source-specific response diagnostic code.</summary>
    public byte Diagnostic { get; }

    /// <summary>Creates a structured dialogue response.</summary>
    /// <param name="applicationContext">The application context in the response.</param>
    /// <param name="accepted">Whether the association is accepted.</param>
    /// <param name="source">The response diagnostic source.</param>
    /// <param name="diagnostic">The source-specific diagnostic (0..2, zero when accepted).</param>
    /// <param name="userInformation">Optional concatenated, encoded EXTERNAL values.</param>
    /// <returns>The dialogue response.</returns>
    public static TcapDialoguePortion CreateResponse(TcapObjectIdentifier applicationContext, bool accepted,
        TcapDialogueDiagnosticSource source = TcapDialogueDiagnosticSource.User, byte diagnostic = 0,
        ReadOnlyMemory<byte> userInformation = default)
    {
        if (source is not (TcapDialogueDiagnosticSource.User or TcapDialogueDiagnosticSource.Provider))
        {
            throw new ArgumentOutOfRangeException(nameof(source));
        }

        if (diagnostic > 2 || (accepted && diagnostic != 0))
        {
            throw new ArgumentOutOfRangeException(nameof(diagnostic));
        }

        return new(applicationContext, userInformation, accepted, source, diagnostic);
    }

    /// <summary>Encodes the EXTERNAL value inside the transaction dialogue portion.</summary>
    /// <returns>The complete EXTERNAL TLV.</returns>
    public byte[] Encode()
    {
        using BerWriterScope encoding = BerWriterCache.Rent();
        AsnWriter fields = encoding.Writer;
        fields.WriteObjectIdentifier(StructuredDialogueOid);
        using (fields.PushSequence(SingleAsn1))
        using (fields.PushSequence(new Asn1Tag(TagClass.Application, Accepted.HasValue ? 1 : 0, true)))
        {
            using (fields.PushSequence(ApplicationContextTag))
            {
                fields.WriteObjectIdentifier(ApplicationContext.ToString());
            }

            if (Accepted is { } accepted)
            {
                using (fields.PushSequence(ResultTag))
                {
                    fields.WriteInteger(accepted ? 0 : 1);
                }

                using (fields.PushSequence(DiagnosticTag))
                using (fields.PushSequence(Context((byte)DiagnosticSource)))
                {
                    fields.WriteInteger(Diagnostic);
                }
            }

            if (!UserInformation.IsEmpty)
            {
                using (fields.PushSequence(UserInformationTag))
                {
                    AsnReader values = new(UserInformation, AsnEncodingRules.BER);
                    while (values.HasData)
                    {
                        fields.WriteEncodedValue(ReadExternalValue(values));
                    }
                }
            }
        }

        return WrapExternal(fields.Encode());
    }

    /// <summary>Encodes a Q.773 ABRT issued by the dialogue service user.</summary>
    internal static byte[] EncodeUserAbort()
    {
        using BerWriterScope encoding = BerWriterCache.Rent();
        AsnWriter fields = encoding.Writer;
        fields.WriteObjectIdentifier(StructuredDialogueOid);
        using (fields.PushSequence(SingleAsn1))
        using (fields.PushSequence(new Asn1Tag(TagClass.Application, 4, true)))
        {
            fields.WriteInteger(0, new Asn1Tag(TagClass.ContextSpecific, 0));
        }

        return WrapExternal(fields.Encode());
    }

    private static byte[] WrapExternal(byte[] encodedFields)
    {
        byte[] external = new byte[encodedFields.Length + 4];
        if (!TcapBer.TryWriteElement(external, External, encodedFields, out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        Array.Resize(ref external, written);
        return external;
    }

    /// <summary>Attempts to decode one structured AARQ or AARE EXTERNAL value.</summary>
    /// <param name="data">The encoded dialogue portion value.</param>
    /// <param name="dialoguePortion">The decoded portion on success.</param>
    /// <param name="error">The decoding error, if any.</param>
    /// <returns>True when the portion is supported and well-formed.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapDialoguePortion? dialoguePortion, out string? error)
    {
        dialoguePortion = null;
        if (!TcapBer.TryReadElement(data, out TcapBerElement external, out error)
            || external.Tag.Encode() != External.Encode() || external.TotalLength != data.Length)
        {
            error ??= "Expected one TCAP EXTERNAL value.";
            return false;
        }

        try
        {
            AsnReader fields = new(external.Value, AsnEncodingRules.BER);
            if (!ReadObjectIdentifierValue(fields).Span.SequenceEqual(StructuredDialogueValue))
            {
                throw new AsnContentException("Unsupported TCAP dialogue abstract syntax.");
            }

            AsnReader single = fields.ReadSequence(SingleAsn1);
            fields.ThrowIfNotEmpty();
            Asn1Tag tag = single.PeekTag();
            if (tag.TagClass != TagClass.Application || tag.TagValue is not (0 or 1))
            {
                throw new AsnContentException("Expected TCAP AARQ or AARE.");
            }

            AsnReader apdu = single.ReadSequence(tag);
            single.ThrowIfNotEmpty();
            if (apdu.HasData && apdu.PeekTag().HasSameClassAndValue(ProtocolVersionTag))
            {
                byte[] version = apdu.ReadBitString(out _, ProtocolVersionTag);
                if (version.Length == 0 || (version[0] & 0x80) == 0)
                {
                    throw new AsnContentException("TCAP dialogue protocol version 1 is required.");
                }
            }

            AsnReader contextField = apdu.ReadSequence(ApplicationContextTag);
            var oid = TcapObjectIdentifier.DecodeValue(ReadObjectIdentifierValue(contextField).Span);
            contextField.ThrowIfNotEmpty();
            bool? accepted = null;
            byte diagnostic = 0;
            TcapDialogueDiagnosticSource source = TcapDialogueDiagnosticSource.User;
            if (tag.TagValue == 1)
            {
                AsnReader result = apdu.ReadSequence(ResultTag);
                if (!result.TryReadInt32(out int resultValue) || resultValue is not (0 or 1))
                {
                    throw new AsnContentException("Invalid TCAP association result.");
                }

                result.ThrowIfNotEmpty();
                accepted = resultValue == 0;
                AsnReader diagnostics = apdu.ReadSequence(DiagnosticTag);
                Asn1Tag sourceTag = diagnostics.PeekTag();
                if (sourceTag.TagClass != TagClass.ContextSpecific || sourceTag.TagValue is not (1 or 2))
                {
                    throw new AsnContentException("Invalid TCAP association diagnostic source.");
                }

                AsnReader code = diagnostics.ReadSequence(sourceTag);
                if (!code.TryReadInt32(out int value) || value is < 0 or > 2 || (accepted == true && value != 0))
                {
                    throw new AsnContentException("Invalid TCAP association diagnostic.");
                }

                code.ThrowIfNotEmpty();
                diagnostics.ThrowIfNotEmpty();
                source = (TcapDialogueDiagnosticSource)sourceTag.TagValue;
                diagnostic = (byte)value;
            }

            ReadOnlyMemory<byte> userInformation = default;
            if (apdu.HasData)
            {
                AsnReader values = apdu.ReadSequence(UserInformationTag);
                using MemoryStream content = new();
                while (values.HasData)
                {
                    content.Write(ReadExternalValue(values));
                }

                userInformation = content.ToArray();
            }

            apdu.ThrowIfNotEmpty();
            dialoguePortion = new(oid, userInformation, accepted, source, diagnostic);
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is AsnContentException or OverflowException or ArgumentException)
        {
            error = exception.Message;
            return false;
        }
    }

    private static ReadOnlyMemory<byte> ReadObjectIdentifierValue(AsnReader reader)
    {
        ReadOnlyMemory<byte> encoded = reader.ReadEncodedValue();
        Asn1Tag tag = AsnDecoder.ReadEncodedValue(encoded.Span, AsnEncodingRules.BER,
            out int offset, out int length, out int consumed);
        if (tag != Asn1Tag.ObjectIdentifier || consumed != encoded.Length)
            throw new AsnContentException("Expected a primitive object identifier.");
        // The existing bounded base-128 codec validates the application OID arcs, not a string round trip.
        return encoded.Slice(offset, length);
    }

    private static ReadOnlySpan<byte> ReadExternalValue(AsnReader reader)
    {
        if (reader.PeekTag() != new Asn1Tag(TagClass.Universal, 8, true))
        {
            throw new AsnContentException("TCAP user-information must contain only EXTERNAL values.");
        }

        return reader.ReadEncodedValue().Span;
    }

    private static Asn1Tag Context(int value) => new(TagClass.ContextSpecific, value, true);
}
