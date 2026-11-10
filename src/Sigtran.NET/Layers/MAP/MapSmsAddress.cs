namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Identifies the kind of MAP SMS address.
/// </summary>
public enum MapSmsAddressKind : byte
{
    /// <summary>MSISDN address.</summary>
    Msisdn = 1,

    /// <summary>IMSI identity.</summary>
    Imsi = 2,

    /// <summary>Service Centre address.</summary>
    ServiceCentre = 3
}

/// <summary>
/// Represents a MAP SMS address or identity encoded with TBCD digits.
/// </summary>
public sealed class MapSmsAddress
{
    /// <summary>Creates a MAP SMS address.</summary>
    /// <param name="kind">The address kind.</param>
    /// <param name="digits">The numeric digits.</param>
    /// <param name="natureOfAddress">The MAP nature of address (1 for international).</param>
    /// <param name="numberingPlan">The numbering plan value.</param>
    public MapSmsAddress(MapSmsAddressKind kind, string digits, byte natureOfAddress = 1, byte numberingPlan = 1)
    {
        if (string.IsNullOrWhiteSpace(digits))
        {
            throw new ArgumentException("MAP SMS address digits are required.", nameof(digits));
        }

        string normalized = digits.Trim().TrimStart('+');
        if (normalized.Length == 0 || !normalized.All(c => c is >= '0' and <= '9'))
        {
            throw new ArgumentException("MAP SMS address digits must be numeric.", nameof(digits));
        }

        Kind = kind;
        Digits = normalized;
        NatureOfAddress = natureOfAddress;
        NumberingPlan = numberingPlan;
    }

    /// <summary>The address kind.</summary>
    public MapSmsAddressKind Kind { get; }

    /// <summary>The numeric digits.</summary>
    public string Digits { get; }

    /// <summary>The nature of address value.</summary>
    public byte NatureOfAddress { get; }

    /// <summary>The numbering plan value.</summary>
    public byte NumberingPlan { get; }

    /// <summary>Encodes the standalone SDK address representation, not a MAP wire AddressString.</summary>
    /// <returns>The encoded address bytes.</returns>
    public byte[] Encode()
    {
        byte[] tbcd = EncodeTbcd(Digits);
        byte[] result = new byte[3 + tbcd.Length];
        result[0] = (byte)Kind;
        result[1] = NatureOfAddress;
        result[2] = NumberingPlan;
        Buffer.BlockCopy(tbcd, 0, result, 3, tbcd.Length);
        return result;
    }

    /// <summary>Decodes an address.</summary>
    /// <param name="data">The encoded address bytes.</param>
    /// <param name="oddDigitCount">Whether the TBCD value contains an odd number of digits.</param>
    /// <returns>The decoded address.</returns>
    public static MapSmsAddress Decode(ReadOnlySpan<byte> data, bool oddDigitCount)
    {
        if (data.Length < 3)
        {
            throw new ArgumentException("MAP SMS address is too short.", nameof(data));
        }

        return new(
            (MapSmsAddressKind)data[0],
            DecodeTbcd(data[3..], oddDigitCount),
            data[1],
            data[2]);
    }

    /// <summary>Encodes digits as TBCD bytes.</summary>
    /// <param name="digits">The numeric digits.</param>
    /// <returns>The TBCD bytes.</returns>
    public static byte[] EncodeTbcd(string digits)
    {
        string normalized = digits.Trim().TrimStart('+');
        if (normalized.Length == 0 || !normalized.All(c => c is >= '0' and <= '9'))
        {
            throw new ArgumentException("TBCD digits must be numeric.", nameof(digits));
        }

        byte[] result = new byte[(normalized.Length + 1) / 2];
        for (int i = 0; i < normalized.Length; i += 2)
        {
            byte low = (byte)(normalized[i] - '0');
            byte high = i + 1 < normalized.Length ? (byte)(normalized[i + 1] - '0') : (byte)0x0F;
            result[i / 2] = (byte)(low | (high << 4));
        }

        return result;
    }

    /// <summary>Decodes TBCD bytes as digits.</summary>
    /// <param name="data">The TBCD bytes.</param>
    /// <param name="oddDigitCount">Whether the value contains an odd number of digits.</param>
    /// <returns>The decoded digits.</returns>
    public static string DecodeTbcd(ReadOnlySpan<byte> data, bool oddDigitCount)
    {
        if (data.IsEmpty)
        {
            throw new ArgumentException("TBCD digits are missing.", nameof(data));
        }

        char[] chars = new char[checked(data.Length * 2 - (oddDigitCount ? 1 : 0))];
        for (int i = 0; i < data.Length; i++)
        {
            int low = data[i] & 0x0F;
            int high = data[i] >> 4;
            bool filler = oddDigitCount && i == data.Length - 1;
            if (low > 9 || (filler ? high != 0x0F : high > 9))
            {
                throw new ArgumentException("Invalid TBCD digit or filler.", nameof(data));
            }

            chars[2 * i] = (char)('0' + low);
            if (!filler)
            {
                chars[2 * i + 1] = (char)('0' + high);
            }
        }

        return new string(chars);
    }
}
