namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Base class for all TCAP components.  A component represents a single
/// unit of dialogue logic in TCAP: an Invoke (request), ReturnResult
/// (response), ReturnError or Reject.  This base class provides common
/// properties and defines methods for encoding and decoding.
/// </summary>
public abstract class TcapComponent
{
    /// <summary>The Invoke ID, used to match a response to its request.</summary>
    public byte InvokeId { get; protected set; }
    /// <summary>The operation code (where applicable).</summary>
    public TcapOperationCode OperationCode { get; protected set; }
    /// <summary>Component parameters (ASN.1 payload or simplified bytes).</summary>
    public ReadOnlyMemory<byte> Parameters { get; protected set; }

    /// <summary>
    /// Encodes this component into a byte array according to a simplified
    /// format.  Subclasses must implement this to provide type‑specific
    /// markers and layout.
    /// </summary>
    /// <returns>The encoded bytes of the component.</returns>
    public abstract byte[] Encode();

    /// <summary>
    /// Attempts to decode the provided bytes as this component type.  The
    /// instance properties will be updated on success.
    /// </summary>
    /// <param name="data">The bytes to parse.</param>
    /// <returns>True if parsing succeeded; false otherwise.</returns>
    public abstract bool TryDecode(ReadOnlySpan<byte> data);
}