namespace Sigtran.NET.Layers.MTP3;

/// <summary>ITU heading codes with H0 in the low nibble and H1 in the high nibble.</summary>
internal enum Mtp3ManagementKind : byte
{
    TransferProhibited = 0x14,
    TransferRestricted = 0x34,
    TransferAllowed = 0x54,
    ExtendedChangeoverOrder = 0x31,
    ExtendedChangeoverAcknowledgement = 0x41,
    EmergencyChangeoverOrder = 0x12,
    EmergencyChangeoverAcknowledgement = 0x22,
    ChangebackDeclaration = 0x51,
    ChangebackAcknowledgement = 0x61
}

/// <summary>Encodes and validates the SNMM body after the MTP3 routing label.</summary>
internal readonly record struct Mtp3ManagementMessage(Mtp3ManagementKind Kind, uint Value = 0)
{
    /// <summary>Encodes the supported Q.704 and Q.2210 bodies in signalling octet order.</summary>
    internal byte[] Encode() => Kind switch
    {
        Mtp3ManagementKind.TransferProhibited or Mtp3ManagementKind.TransferRestricted
            or Mtp3ManagementKind.TransferAllowed when Value <= 0x3FFF => [(byte)Kind, (byte)Value, (byte)(Value >> 8)],
        Mtp3ManagementKind.ExtendedChangeoverOrder or Mtp3ManagementKind.ExtendedChangeoverAcknowledgement
            when Value <= 0xFF_FFFF => [(byte)Kind, (byte)Value, (byte)(Value >> 8), (byte)(Value >> 16)],
        Mtp3ManagementKind.EmergencyChangeoverOrder or Mtp3ManagementKind.EmergencyChangeoverAcknowledgement
            when Value == 0 => [(byte)Kind],
        Mtp3ManagementKind.ChangebackDeclaration or Mtp3ManagementKind.ChangebackAcknowledgement
            when Value <= byte.MaxValue => [(byte)Kind, (byte)Value],
        _ => throw new ArgumentOutOfRangeException(nameof(Value), "Unsupported SNMM heading or value.")
    };

    /// <summary>Rejects unsupported headings, incorrect body lengths and nonzero point-code spare bits.</summary>
    internal static bool TryDecode(ReadOnlySpan<byte> payload, out Mtp3ManagementMessage message)
    {
        message = default;
        if (payload.IsEmpty)
        {
            return false;
        }

        Mtp3ManagementKind kind = (Mtp3ManagementKind)payload[0];
        switch (kind)
        {
            case Mtp3ManagementKind.TransferProhibited or Mtp3ManagementKind.TransferRestricted
                or Mtp3ManagementKind.TransferAllowed when payload.Length == 3 && (payload[2] & 0xC0) == 0:
                message = new(kind, (uint)(payload[1] | (payload[2] << 8)));
                return true;
            case Mtp3ManagementKind.ExtendedChangeoverOrder or Mtp3ManagementKind.ExtendedChangeoverAcknowledgement
                when payload.Length == 4:
                message = new(kind, (uint)(payload[1] | (payload[2] << 8) | (payload[3] << 16)));
                return true;
            case Mtp3ManagementKind.EmergencyChangeoverOrder or Mtp3ManagementKind.EmergencyChangeoverAcknowledgement
                when payload.Length == 1:
                message = new(kind);
                return true;
            case Mtp3ManagementKind.ChangebackDeclaration or Mtp3ManagementKind.ChangebackAcknowledgement
                when payload.Length == 2:
                message = new(kind, payload[1]);
                return true;
            default:
                return false;
        }
    }
}
