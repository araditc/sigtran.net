namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Represents a simplified SCCP Unitdata message (UDT).  The full SCCP
/// specification includes many optional parameters and a parameter pointer
/// table.  For early testing this class uses a flat layout:
///   MessageType (1 byte)
///   ProtocolClass (1 byte)
///   CalledParty (variable)
///   CallingParty (variable)
///   UserData (remainder)
/// A real implementation would follow the layout defined in ITU‑T Q.713.
/// </summary>
public sealed class SccpMessage
{
    /// <summary>UDT message type code.</summary>
    public const byte MessageTypeUDT = 0x09;

    /// <summary>The protocol class.  Class 0 means connectionless.</summary>
    public byte ProtocolClass { get; init; } = 0;

    /// <summary>The called party address.</summary>
    public SccpAddress CalledParty { get; init; } = new();

    /// <summary>The calling party address.</summary>
    public SccpAddress CallingParty { get; init; } = new();

    /// <summary>The user data payload, typically a TCAP byte array.</summary>
    public ReadOnlyMemory<byte> UserData { get; init; }

    /// <summary>
    /// Encodes this UDT into a flat byte array.  This non‑standard layout is
    /// used solely for internal testing.  The encoded message can be fed
    /// directly into the M3UA layer.
    /// </summary>
    /// <returns>A byte array containing the encoded message.</returns>
    public byte[] EncodeUdt()
    {
        byte[] called = CalledParty.EncodePartyAddress(isCalled: true);
        byte[] calling = CallingParty.EncodePartyAddress(isCalled: false);
        int length = 1 + 1 + called.Length + calling.Length + UserData.Length;
        byte[] buffer = new byte[length];
        int o = 0;
        buffer[o++] = MessageTypeUDT;
        buffer[o++] = ProtocolClass;
        Buffer.BlockCopy(called, 0, buffer, o, called.Length);
        o += called.Length;
        Buffer.BlockCopy(calling, 0, buffer, o, calling.Length);
        o += calling.Length;
        if (!UserData.IsEmpty)
        {
            ReadOnlySpan<byte> span = UserData.Span;
            span.CopyTo(buffer.AsSpan(o));
        }
        return buffer;
    }

    /// <summary>
    /// Attempts to parse a UDT using the same simplified layout.  Real SCCP
    /// messages cannot be parsed without reading parameter pointers; this
    /// implementation is not suitable for interop with other stacks.
    /// </summary>
    /// <param name="data">The raw bytes to parse.</param>
    /// <param name="msg">The parsed message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if parsing succeeded; otherwise false.</returns>
    public static bool TryParseUdt(ReadOnlySpan<byte> data, out SccpMessage? msg, out string? error)
    {
        msg = null;
        error = null;
        if (data.Length < 2)
        {
            error = "SCCP buffer too short";
            return false;
        }
        if (data[0] != MessageTypeUDT)
        {
            error = $"Unexpected SCCP message type 0x{data[0]:X2}";
            return false;
        }
        byte protocolClass = data[1];
        // For the simplified form we assume the remainder is user data.  A
        // production implementation must parse Called/Calling addresses.
        byte[] payload = data.Slice(2).ToArray();
        msg = new()
              {
                  ProtocolClass = protocolClass,
                  // We cannot extract real addresses from this layout; set defaults
                  CalledParty = new() { Subsystem = SubsystemNumber.MAP },
                  CallingParty = new() { Subsystem = SubsystemNumber.MAP },
                  UserData = payload
              };
        return true;
    }
}