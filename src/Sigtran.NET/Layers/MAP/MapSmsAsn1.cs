using System.Formats.Asn1;

namespace Sigtran.NET.Layers.MAP;

/// <summary>Shared TS 29.002 ASN.1 fields for the supported MAP SMS profile.</summary>
internal static class MapSmsAsn1
{
    private const int MaxParameterBytes = 1024;
    internal const int MaxSignalInfoBytes = 200;

    internal static Asn1Tag Context(int number) => new(TagClass.ContextSpecific, number);

    internal static byte[] Encode(Action<AsnWriter> fields)
    {
        AsnWriter writer = new(AsnEncodingRules.BER);
        using (writer.PushSequence()) fields(writer);
        return writer.Encode();
    }

    internal static bool TryDecode<T>(ReadOnlySpan<byte> data, Func<AsnReader, T> readFields,
        out T? result, out string? error) where T : class
    {
        result = null;
        if (data.Length is 0 or > MaxParameterBytes)
        {
            error = "MAP SMS parameter size is outside the supported profile.";
            return false;
        }

        try
        {
            AsnReader reader = new(data.ToArray(), AsnEncodingRules.BER);
            AsnReader sequence = reader.ReadSequence();
            reader.ThrowIfNotEmpty();
            T decoded = readFields(sequence);
            sequence.ThrowIfNotEmpty();
            result = decoded;
            error = null;
            return true;
        }
        catch (Exception exception) when (exception is AsnContentException or ArgumentException)
        {
            error = exception.Message;
            return false;
        }
    }

    internal static void WriteAddress(AsnWriter writer, MapSmsAddress address, MapSmsAddressKind kind, Asn1Tag? tag = null)
    {
        if (address.Kind != kind || !ValidDigitCount(kind, address.Digits.Length))
        {
            throw new ArgumentException($"Invalid MAP {kind} address.", nameof(address));
        }

        byte[] digits = MapSmsAddress.EncodeTbcd(address.Digits);
        if (kind == MapSmsAddressKind.Imsi)
        {
            writer.WriteOctetString(digits, tag);
            return;
        }

        if (!ValidNature(address.NatureOfAddress) || address.NumberingPlan > 0x0F)
        {
            throw new ArgumentException("Invalid MAP address metadata.", nameof(address));
        }

        byte type = (byte)(0x80 | (address.NatureOfAddress << 4) | address.NumberingPlan);
        writer.WriteOctetString([type, .. digits], tag);
    }

    internal static MapSmsAddress ReadAddress(AsnReader reader, MapSmsAddressKind kind, Asn1Tag? tag = null)
    {
        ReadOnlySpan<byte> encoded = reader.ReadOctetString(tag);
        byte nature = 1;
        byte plan = 1;
        if (kind != MapSmsAddressKind.Imsi)
        {
            if (encoded.Length < 2 || (encoded[0] & 0x80) == 0)
            {
                throw new AsnContentException("MAP AddressString requires a non-extended type octet and digits.");
            }

            nature = (byte)((encoded[0] >> 4) & 7);
            plan = (byte)(encoded[0] & 0x0F);
            if (!ValidNature(nature))
            {
                throw new AsnContentException("Unsupported MAP address nature.");
            }

            encoded = encoded[1..];
        }

        if (encoded.IsEmpty || encoded.Length > 19)
        {
            throw new AsnContentException("Invalid MAP address digit length.");
        }

        string digits = MapSmsAddress.DecodeTbcd(encoded, (encoded[^1] >> 4) == 0x0F);
        if (!ValidDigitCount(kind, digits.Length))
        {
            throw new AsnContentException($"Invalid MAP {kind} digit count.");
        }

        return new(kind, digits, nature, plan);
    }

    internal static byte[] ReadSignalInfo(AsnReader reader)
    {
        byte[] value = reader.ReadOctetString();
        if (value.Length is 0 or > MaxSignalInfoBytes)
        {
            throw new AsnContentException("MAP SignalInfo length must be 1..200 octets.");
        }

        return value;
    }

    internal static bool HasTag(AsnReader reader, Asn1Tag tag)
        => reader.HasData && reader.PeekTag().HasSameClassAndValue(tag);

    private static bool ValidNature(byte nature) => nature is <= 4 or 6;

    private static bool ValidDigitCount(MapSmsAddressKind kind, int length) => kind switch
    {
        MapSmsAddressKind.Imsi => length is >= 5 and <= 15,
        MapSmsAddressKind.Msisdn => length is >= 1 and <= 15,
        MapSmsAddressKind.ServiceCentre => length is >= 1 and <= 38,
        _ => false
    };
}
