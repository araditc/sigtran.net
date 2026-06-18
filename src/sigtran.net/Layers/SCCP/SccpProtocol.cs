namespace sigtran.net.Layers.SCCP;

/// <summary>
/// SCCP connectionless message type codes.
/// </summary>
public enum SccpMessageType : byte
{
    /// <summary>Unitdata message.</summary>
    Unitdata = 0x09,

    /// <summary>Unitdata service message.</summary>
    UnitdataService = 0x0A,

    /// <summary>Extended unitdata message.</summary>
    ExtendedUnitdata = 0x11,

    /// <summary>Extended unitdata service message.</summary>
    ExtendedUnitdataService = 0x12,

    /// <summary>Long unitdata message.</summary>
    LongUnitdata = 0x13,

    /// <summary>Long unitdata service message.</summary>
    LongUnitdataService = 0x14
}

/// <summary>
/// SCCP connectionless protocol class.
/// </summary>
public enum SccpConnectionlessClass : byte
{
    /// <summary>Basic connectionless class.</summary>
    Class0 = 0,

    /// <summary>Sequenced connectionless class.</summary>
    Class1 = 1
}

/// <summary>
/// Represents the SCCP protocol class octet used by UDT-style messages.
/// </summary>
public readonly struct SccpProtocolClass
{
    private const byte ReturnMessageOnErrorMask = 0x80;
    private const byte ClassMask = 0x0F;

    /// <summary>Creates an SCCP protocol class value.</summary>
    /// <param name="connectionlessClass">The connectionless protocol class.</param>
    /// <param name="returnMessageOnError">Whether the return-message-on-error option is set.</param>
    public SccpProtocolClass(SccpConnectionlessClass connectionlessClass, bool returnMessageOnError = false)
    {
        if (connectionlessClass is not SccpConnectionlessClass.Class0 and not SccpConnectionlessClass.Class1)
        {
            throw new ArgumentOutOfRangeException(nameof(connectionlessClass), "Only SCCP connectionless classes 0 and 1 are supported.");
        }

        ConnectionlessClass = connectionlessClass;
        ReturnMessageOnError = returnMessageOnError;
    }

    /// <summary>The connectionless protocol class.</summary>
    public SccpConnectionlessClass ConnectionlessClass { get; }

    /// <summary>Whether the return-message-on-error option is set.</summary>
    public bool ReturnMessageOnError { get; }

    /// <summary>Encodes the protocol class octet.</summary>
    /// <returns>The encoded protocol class octet.</returns>
    public byte Encode()
    {
        byte value = (byte)ConnectionlessClass;
        if (ReturnMessageOnError)
        {
            value |= ReturnMessageOnErrorMask;
        }

        return value;
    }

    /// <summary>Decodes a protocol class octet.</summary>
    /// <param name="value">The encoded protocol class octet.</param>
    /// <returns>The decoded protocol class value.</returns>
    public static SccpProtocolClass Decode(byte value)
    {
        return new(
            (SccpConnectionlessClass)(value & ClassMask),
            returnMessageOnError: (value & ReturnMessageOnErrorMask) != 0);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{ConnectionlessClass} returnOnError={ReturnMessageOnError}";
    }
}
