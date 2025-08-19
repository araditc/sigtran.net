namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Represents a TCAP ReturnResult component.  It conveys the outcome
/// of a previously requested Invoke.  The simplified encoding mirrors
/// the Invoke layout but uses a different type marker.
/// </summary>
public sealed class TcapReturnResultComponent : TcapComponent
{
    private const byte TypeMarker = 0xA2;

    /// <summary>
    /// Constructs a ReturnResult with the given values.
    /// </summary>
    public TcapReturnResultComponent(byte invokeId, TcapOperationCode opCode, ReadOnlyMemory<byte> parameters)
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