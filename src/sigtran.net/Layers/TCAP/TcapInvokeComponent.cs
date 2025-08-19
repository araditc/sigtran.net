namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Represents a TCAP Invoke component.  An Invoke requests the remote
/// peer to perform an operation identified by the OperationCode.  The
/// simplified encoding used here packs the invoke ID, operation code
/// and parameters into a fixed layout.
/// </summary>
public sealed class TcapInvokeComponent : TcapComponent
{
    /// <summary>Type marker used in the simplified encoding.</summary>
    private const byte TypeMarker = 0xA1;

    /// <summary>
    /// Constructs a new Invoke component with the specified values.
    /// </summary>
    public TcapInvokeComponent(byte invokeId, TcapOperationCode opCode, ReadOnlyMemory<byte> parameters)
    {
        InvokeId = invokeId;
        OperationCode = opCode;
        Parameters = parameters;
    }

    /// <inheritdoc />
    public override byte[] Encode()
    {
        int len = Parameters.Length;
        byte[] buffer = new byte[4 + len];
        buffer[0] = TypeMarker;
        buffer[1] = InvokeId;
        buffer[2] = (byte)OperationCode;
        buffer[3] = (byte)len;
        if (len > 0)
        {
            Parameters.Span.CopyTo(buffer.AsSpan(4));
        }
        return buffer;
    }

    /// <inheritdoc />
    public override bool TryDecode(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
        {
            return false;
        }

        if (data[0] != TypeMarker)
        {
            return false;
        }

        byte id = data[1];
        TcapOperationCode op = (TcapOperationCode)data[2];
        int paramLen = data[3];
        if (data.Length < 4 + paramLen)
        {
            return false;
        }

        InvokeId = id;
        OperationCode = op;
        Parameters = data.Slice(4, paramLen).ToArray();
        return true;
    }
}