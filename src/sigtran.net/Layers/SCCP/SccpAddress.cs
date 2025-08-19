namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Represents a minimal SCCP address used in a UDT.  Addresses may
/// include a subsystem number and/or a global title.  For early testing
/// only E.164 global titles are supported and are encoded into TBCD
/// format.  Real implementations must handle more general formats.
/// </summary>
public sealed class SccpAddress
{
    /// <summary>The E.164 digits for the global title, e.g. "17201234567".</summary>
    public string? GlobalTitleE164 { get; init; }
    /// <summary>The subsystem number.  If null or Unknown then no SSN is encoded.</summary>
    public SubsystemNumber? Subsystem { get; init; }

    /// <summary>
    /// Encodes this address into a Called/Calling Party Address byte array
    /// according to a simplified interpretation of Q.713.  The returned
    /// array contains the Address Indicator followed by optional SSN and
    /// TBCD‑encoded GT digits.
    /// </summary>
    /// <param name="isCalled">True if encoding a called party, false if calling.</param>
    /// <returns>The encoded address bytes.</returns>
    public byte[] EncodePartyAddress(bool isCalled)
    {
        bool hasGt = !string.IsNullOrWhiteSpace(GlobalTitleE164);
        bool hasSsn = Subsystem.HasValue && Subsystem.Value != SubsystemNumber.Unknown;
        byte ai = 0x00;
        // Routing indicator: 0 = route on GT; 1 = route on SSN
        ai |= (byte)(hasGt ? 0x00 : 0x01);
        // GT included flag
        if (hasGt)
        {
            ai |= 0x02;
        }

        // SSN included flag
        if (hasSsn)
        {
            ai |= 0x04;
        }

        // Numbering plan (E.164) – we use bits 4‑7 to indicate plan
        if (hasGt)
        {
            ai |= 0x10;
        }

        int length = 1 + (hasSsn ? 1 : 0);
        byte[]? gtBytes = null;
        if (hasGt)
        {
            string digits = GlobalTitleE164!.Trim().TrimStart('+');
            gtBytes = TbcdEncode(digits);
            length += gtBytes.Length;
        }
        byte[] result = new byte[length];
        int offset = 0;
        result[offset++] = ai;
        if (hasSsn)
        {
            result[offset++] = (byte)Subsystem!.Value;
        }

        if (hasGt && gtBytes != null)
        {
            Buffer.BlockCopy(gtBytes, 0, result, offset, gtBytes.Length);
        }
        return result;
    }

    /// <summary>
    /// Encodes numeric digits into TBCD format (swapped nibbles with 0xF
    /// padding).  E.g. "123" becomes { 0x21, 0xF3 }.
    /// </summary>
    private static byte[] TbcdEncode(string digits)
    {
        if (string.IsNullOrEmpty(digits))
        {
            return Array.Empty<byte>();
        }

        int len = (digits.Length + 1) / 2;
        byte[] buf = new byte[len];
        int bi = 0;
        for (int i = 0; i < digits.Length; i += 2)
        {
            byte low = (byte)(digits[i] - '0');
            byte high = 0x0F;
            if (i + 1 < digits.Length)
            {
                high = (byte)(digits[i + 1] - '0');
            }
            buf[bi++] = (byte)((low & 0x0F) | ((high & 0x0F) << 4));
        }
        return buf;
    }
}