namespace sigtran.net.Core.Interfaces;

/// <summary>
/// Represents a unit of signalling that can be encoded into bytes or decoded
/// from bytes.  Implementations should aim for minimal allocations and
/// prefer spans over arrays for performance.  This interface is used to
/// abstract over the different protocol data units in the SIGTRAN stack.
/// </summary>
public interface ISigtranMessage
{
    /// <summary>
    /// Encodes the current message into a binary representation.  The
    /// returned span is only valid until the message is mutated or
    /// disposed.  Encoders may return a slice of an internal buffer.
    /// </summary>
    /// <returns>A read‑only span over the encoded bytes.</returns>
    ReadOnlySpan<byte> Encode();

    /// <summary>
    /// Decodes the message from the supplied buffer.  This will replace
    /// the contents of the message instance with the decoded values.  If
    /// decoding fails the message should be left in an unspecified state.
    /// </summary>
    /// <param name="buffer">The raw bytes to decode.</param>
    /// <param name="error">A descriptive error message on failure.</param>
    /// <returns>True if decoding succeeds; otherwise false.</returns>
    bool TryDecode(ReadOnlySpan<byte> buffer, out string? error);
}