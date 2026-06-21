namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Selects the SCCP address routing mode.
/// </summary>
public enum SccpRoutingIndicator : byte
{
    /// <summary>Route using global title translation.</summary>
    RouteOnGlobalTitle = 0,

    /// <summary>Route using point code and subsystem number.</summary>
    RouteOnSubsystemNumber = 1
}

/// <summary>
/// Identifies the global title format present in an SCCP party address.
/// </summary>
public enum SccpGlobalTitleIndicator : byte
{
    /// <summary>No global title is present.</summary>
    None = 0,

    /// <summary>Global title contains nature of address only.</summary>
    NatureOfAddressOnly = 1,

    /// <summary>Global title contains translation type only.</summary>
    TranslationTypeOnly = 2,

    /// <summary>Global title contains translation type, numbering plan, and encoding scheme.</summary>
    TranslationTypeNumberingPlanEncoding = 3,

    /// <summary>Global title contains translation type, numbering plan, encoding scheme, and nature of address.</summary>
    TranslationTypeNumberingPlanEncodingNature = 4
}

/// <summary>
/// Represents an SCCP global title using TBCD digits.
/// </summary>
public sealed class SccpGlobalTitle
{
    /// <summary>Creates an SCCP global title.</summary>
    /// <param name="digits">The global title digits.</param>
    /// <param name="translationType">The translation type.</param>
    /// <param name="numberingPlan">The numbering plan value.</param>
    /// <param name="natureOfAddress">The nature of address value.</param>
    public SccpGlobalTitle(string digits, byte translationType = 0, byte numberingPlan = 1, byte natureOfAddress = 4)
    {
        if (string.IsNullOrWhiteSpace(digits))
        {
            throw new ArgumentException("Global title digits are required.", nameof(digits));
        }

        string normalized = digits.Trim().TrimStart('+');
        if (!normalized.All(char.IsDigit))
        {
            throw new ArgumentException("Global title digits must be numeric.", nameof(digits));
        }

        Digits = normalized;
        TranslationType = translationType;
        NumberingPlan = numberingPlan;
        NatureOfAddress = natureOfAddress;
    }

    /// <summary>The global title digits.</summary>
    public string Digits { get; }

    /// <summary>The translation type.</summary>
    public byte TranslationType { get; }

    /// <summary>The numbering plan value.</summary>
    public byte NumberingPlan { get; }

    /// <summary>The nature of address value.</summary>
    public byte NatureOfAddress { get; }
}

/// <summary>
/// Represents an SCCP called or calling party address.
/// </summary>
public sealed class SccpPartyAddress
{
    /// <summary>Creates an SCCP party address.</summary>
    /// <param name="routingIndicator">The routing indicator.</param>
    /// <param name="subsystemNumber">The optional subsystem number.</param>
    /// <param name="pointCode">The optional 14-bit point code.</param>
    /// <param name="globalTitle">The optional global title.</param>
    public SccpPartyAddress(
        SccpRoutingIndicator routingIndicator,
        SubsystemNumber? subsystemNumber = null,
        ushort? pointCode = null,
        SccpGlobalTitle? globalTitle = null)
    {
        if (pointCode > 0x3FFF)
        {
            throw new ArgumentOutOfRangeException(nameof(pointCode), "SCCP point code must fit in 14 bits.");
        }

        RoutingIndicator = routingIndicator;
        SubsystemNumber = subsystemNumber;
        PointCode = pointCode;
        GlobalTitle = globalTitle;
    }

    /// <summary>The routing indicator.</summary>
    public SccpRoutingIndicator RoutingIndicator { get; }

    /// <summary>The optional subsystem number.</summary>
    public SubsystemNumber? SubsystemNumber { get; }

    /// <summary>The optional 14-bit point code.</summary>
    public ushort? PointCode { get; }

    /// <summary>The optional global title.</summary>
    public SccpGlobalTitle? GlobalTitle { get; }

    /// <summary>Encodes this party address into SCCP address bytes.</summary>
    /// <returns>The encoded party address bytes.</returns>
    public byte[] Encode()
    {
        bool hasPointCode = PointCode.HasValue;
        bool hasSubsystemNumber = SubsystemNumber.HasValue && SubsystemNumber.Value != global::Sigtran.NET.Layers.SCCP.SubsystemNumber.Unknown;
        SccpGlobalTitleIndicator globalTitleIndicator = GlobalTitle is null
            ? SccpGlobalTitleIndicator.None
            : SccpGlobalTitleIndicator.TranslationTypeNumberingPlanEncodingNature;

        byte indicator = 0;
        if (hasPointCode)
        {
            indicator |= 0x01;
        }

        if (hasSubsystemNumber)
        {
            indicator |= 0x02;
        }

        indicator |= (byte)(((byte)globalTitleIndicator & 0x0F) << 2);
        if (RoutingIndicator == SccpRoutingIndicator.RouteOnSubsystemNumber)
        {
            indicator |= 0x40;
        }

        byte[] digits = GlobalTitle is null ? Array.Empty<byte>() : TbcdEncode(GlobalTitle.Digits);
        int length = 1 + (hasPointCode ? 2 : 0) + (hasSubsystemNumber ? 1 : 0) + (GlobalTitle is null ? 0 : 3 + digits.Length);
        byte[] result = new byte[length];
        int offset = 0;
        result[offset++] = indicator;

        if (hasPointCode)
        {
            ushort pointCode = PointCode!.Value;
            result[offset++] = (byte)pointCode;
            result[offset++] = (byte)(pointCode >> 8);
        }

        if (hasSubsystemNumber)
        {
            result[offset++] = (byte)SubsystemNumber!.Value;
        }

        if (GlobalTitle is not null)
        {
            result[offset++] = GlobalTitle.TranslationType;
            byte encodingScheme = (byte)(GlobalTitle.Digits.Length % 2 == 0 ? 0x02 : 0x01);
            result[offset++] = (byte)(((GlobalTitle.NumberingPlan & 0x0F) << 4) | encodingScheme);
            result[offset++] = GlobalTitle.NatureOfAddress;
            Buffer.BlockCopy(digits, 0, result, offset, digits.Length);
        }

        return result;
    }

    /// <summary>Attempts to decode an SCCP party address.</summary>
    /// <param name="data">The address bytes.</param>
    /// <param name="address">The decoded address on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out SccpPartyAddress? address, out string? error)
    {
        address = null;
        error = null;
        if (data.IsEmpty)
        {
            error = "SCCP party address is empty";
            return false;
        }

        byte indicator = data[0];
        bool hasPointCode = (indicator & 0x01) != 0;
        bool hasSubsystemNumber = (indicator & 0x02) != 0;
        SccpGlobalTitleIndicator globalTitleIndicator = (SccpGlobalTitleIndicator)((indicator >> 2) & 0x0F);
        SccpRoutingIndicator routingIndicator = (indicator & 0x40) != 0
            ? SccpRoutingIndicator.RouteOnSubsystemNumber
            : SccpRoutingIndicator.RouteOnGlobalTitle;

        int offset = 1;
        ushort? pointCode = null;
        if (hasPointCode)
        {
            if (data.Length < offset + 2)
            {
                error = "SCCP party address is missing point code bytes";
                return false;
            }

            pointCode = (ushort)(data[offset] | (data[offset + 1] << 8));
            offset += 2;
        }

        SubsystemNumber? subsystemNumber = null;
        if (hasSubsystemNumber)
        {
            if (data.Length < offset + 1)
            {
                error = "SCCP party address is missing subsystem number";
                return false;
            }

            subsystemNumber = (SubsystemNumber)data[offset++];
        }

        SccpGlobalTitle? globalTitle = null;
        if (globalTitleIndicator == SccpGlobalTitleIndicator.TranslationTypeNumberingPlanEncodingNature)
        {
            if (data.Length < offset + 3)
            {
                error = "SCCP party address is missing global title header";
                return false;
            }

            byte translationType = data[offset++];
            byte numberingPlanAndEncoding = data[offset++];
            byte natureOfAddress = data[offset++];
            string digits = TbcdDecode(data[offset..], oddDigitCount: (numberingPlanAndEncoding & 0x0F) == 0x01);
            globalTitle = new(digits, translationType, (byte)(numberingPlanAndEncoding >> 4), natureOfAddress);
            offset = data.Length;
        }
        else if (globalTitleIndicator != SccpGlobalTitleIndicator.None)
        {
            error = $"Unsupported SCCP global title indicator {globalTitleIndicator}";
            return false;
        }

        if (offset != data.Length)
        {
            error = "SCCP party address contains trailing bytes";
            return false;
        }

        address = new(routingIndicator, subsystemNumber, pointCode, globalTitle);
        return true;
    }

    private static byte[] TbcdEncode(string digits)
    {
        byte[] result = new byte[(digits.Length + 1) / 2];
        for (int i = 0; i < digits.Length; i += 2)
        {
            byte low = (byte)(digits[i] - '0');
            byte high = i + 1 < digits.Length ? (byte)(digits[i + 1] - '0') : (byte)0x0F;
            result[i / 2] = (byte)(low | (high << 4));
        }

        return result;
    }

    private static string TbcdDecode(ReadOnlySpan<byte> data, bool oddDigitCount)
    {
        char[] chars = new char[data.Length * 2];
        int length = 0;
        for (int i = 0; i < data.Length; i++)
        {
            byte low = (byte)(data[i] & 0x0F);
            byte high = (byte)((data[i] >> 4) & 0x0F);
            chars[length++] = (char)('0' + low);
            if (high != 0x0F)
            {
                chars[length++] = (char)('0' + high);
            }
        }

        if (!oddDigitCount && length > 0 && length % 2 != 0)
        {
            length--;
        }

        return new string(chars, 0, length);
    }
}
