namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// TCAP transaction package types.
/// </summary>
public enum TcapPackageType : byte
{
    /// <summary>Unidirectional package.</summary>
    Unidirectional = 1,

    /// <summary>Begin package.</summary>
    Begin = 2,

    /// <summary>End package.</summary>
    End = 4,

    /// <summary>Continue package.</summary>
    Continue = 5,

    /// <summary>Abort package.</summary>
    Abort = 7
}

/// <summary>
/// Represents a TCAP transaction identifier.
/// </summary>
public readonly struct TcapTransactionId
{
    /// <summary>The maximum transaction identifier length supported by the SDK.</summary>
    public const int MaxLength = 4;

    private readonly byte[] _bytes;

    /// <summary>Creates a TCAP transaction identifier.</summary>
    /// <param name="bytes">The transaction identifier bytes.</param>
    public TcapTransactionId(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            throw new ArgumentException("TCAP transaction id is required.", nameof(bytes));
        }

        if (bytes.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes), "TCAP transaction id must be at most four bytes.");
        }

        _bytes = bytes.ToArray();
    }

    /// <summary>The transaction identifier length.</summary>
    public int Length => _bytes.Length;

    /// <summary>Returns the transaction identifier bytes.</summary>
    /// <returns>A copy of the transaction identifier bytes.</returns>
    public byte[] ToArray()
    {
        return _bytes.ToArray();
    }

    /// <summary>Creates a transaction identifier from an unsigned integer.</summary>
    /// <param name="value">The integer value.</param>
    /// <returns>The transaction identifier.</returns>
    public static TcapTransactionId FromUInt32(uint value)
    {
        if (value <= byte.MaxValue)
        {
            return new([(byte)value]);
        }

        if (value <= ushort.MaxValue)
        {
            return new([(byte)(value >> 8), (byte)value]);
        }

        if (value <= 0xFFFFFF)
        {
            return new([(byte)(value >> 16), (byte)(value >> 8), (byte)value]);
        }

        return new([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
    }

    /// <summary>Formats the transaction identifier as hexadecimal text.</summary>
    /// <returns>A hexadecimal transaction identifier string.</returns>
    public override string ToString()
    {
        return Convert.ToHexString(_bytes);
    }
}

/// <summary>
/// Provides TCAP transaction portion tags.
/// </summary>
public static class TcapTransactionTags
{
    /// <summary>Originating transaction ID context tag number.</summary>
    public const byte OriginatingTransactionId = 8;

    /// <summary>Destination transaction ID context tag number.</summary>
    public const byte DestinationTransactionId = 9;

    /// <summary>Component portion context tag number.</summary>
    public const byte ComponentPortion = 12;

    /// <summary>Dialogue portion context tag number.</summary>
    public const byte DialoguePortion = 11;

    /// <summary>Builds an application-class tag for a package type.</summary>
    /// <param name="packageType">The package type.</param>
    /// <returns>The BER tag.</returns>
    public static TcapBerTag Package(TcapPackageType packageType)
    {
        return new(TcapBerTagClass.Application, constructed: true, (byte)packageType);
    }

    /// <summary>Builds a context-specific transaction id tag.</summary>
    /// <param name="originating">True for originating transaction id; false for destination transaction id.</param>
    /// <returns>The BER tag.</returns>
    public static TcapBerTag TransactionId(bool originating)
    {
        return new(TcapBerTagClass.ContextSpecific, constructed: false, originating ? OriginatingTransactionId : DestinationTransactionId);
    }
}
