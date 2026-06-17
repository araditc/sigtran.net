using sigtran.net.Layers.M3UA;

Run("M3UA Payload Data uses network byte order and RFC-style TLV length", M3uaPayloadDataUsesNetworkOrder);
Run("M3UA decoder returns the complete Protocol Data value", M3uaDecoderReturnsProtocolDataValue);
Run("M3UA parameter reader skips padding between TLVs", M3uaParameterReaderSkipsPadding);
Run("M3UA builds ASP Up with ASP Identifier and Info String", M3uaBuildsAspUp);
Run("M3UA builds Heartbeat Ack with unchanged heartbeat data", M3uaBuildsHeartbeatAck);
Run("M3UA builds ASP Active with Traffic Mode and Routing Context", M3uaBuildsAspActive);
Run("M3UA builds ASP Inactive Ack with Routing Context", M3uaBuildsAspInactiveAck);
Run("M3UA parses ASP Up into a typed ASPSM message", M3uaParsesAspUp);
Run("M3UA parses ASP Active into a typed ASPTM message", M3uaParsesAspActive);
Run("M3UA rejects malformed typed Routing Context", M3uaRejectsMalformedTypedRoutingContext);

static void M3uaPayloadDataUsesNetworkOrder()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] payload = [0xDE, 0xAD, 0xBE, 0xEF];

    bool ok = M3uaMessageBuilder.BuildPayloadData(
        buffer,
        payload,
        opc: 0x01020304,
        dpc: 0x05060708,
        si: 3,
        ni: 2,
        mp: 1,
        sls: 15,
        out int written,
        out string? error);

    Assert(ok, error ?? "builder failed");
    AssertEqual(28, written, "total length");
    AssertSequence([0x00, 0x00, 0x00, 0x1C], buffer.Slice(4, 4), "message length");
    AssertSequence([0x02, 0x10], buffer.Slice(8, 2), "Protocol Data tag");
    AssertSequence([0x00, 0x14], buffer.Slice(10, 2), "Protocol Data parameter length");
    AssertSequence([0x01, 0x02, 0x03, 0x04], buffer.Slice(12, 4), "OPC");
    AssertSequence([0x05, 0x06, 0x07, 0x08], buffer.Slice(16, 4), "DPC");
    AssertSequence([3, 2, 1, 15], buffer.Slice(20, 4), "SI/NI/MP/SLS");
    AssertSequence(payload, buffer.Slice(24, payload.Length), "user payload");
}

static void M3uaDecoderReturnsProtocolDataValue()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] payload = [0xAA, 0xBB, 0xCC];

    bool built = M3uaMessageBuilder.BuildPayloadData(
        buffer,
        payload,
        opc: 1,
        dpc: 2,
        si: 3,
        ni: 2,
        mp: 0,
        sls: 7,
        out int written,
        out string? buildError);

    Assert(built, buildError ?? "builder failed");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "decode failed");
    AssertEqual(M3uaMessageClass.Transfer, message.MessageClass, "message class");
    AssertEqual((byte)M3uaTransferMessageType.PayloadData, message.MessageType, "message type");
    Assert(message.TryGetProtocolData(out ReadOnlySpan<byte> protocolData, out string? protocolError), protocolError ?? "Protocol Data missing");

    AssertEqual(15, protocolData.Length, "Protocol Data value length");
    AssertSequence([0x00, 0x00, 0x00, 0x01], protocolData.Slice(0, 4), "decoded OPC");
    AssertSequence([0x00, 0x00, 0x00, 0x02], protocolData.Slice(4, 4), "decoded DPC");
    AssertSequence([3, 2, 0, 7], protocolData.Slice(8, 4), "decoded SI/NI/MP/SLS");
    AssertSequence(payload, protocolData.Slice(12), "decoded user payload");
}

static void M3uaParameterReaderSkipsPadding()
{
    Span<byte> buffer = stackalloc byte[32];
    byte[] info = [0x41, 0x42, 0x43];
    byte[] routingContext = [0x00, 0x00, 0x00, 0x05];

    Assert(
        M3uaParameterWriter.TryWrite(buffer, M3uaParameterTag.InfoString, info, out int firstWritten, out string? firstError),
        firstError ?? "first parameter write failed");
    AssertEqual(8, firstWritten, "first padded length");
    Assert(
        M3uaParameterWriter.TryWrite(buffer.Slice(firstWritten), M3uaParameterTag.RoutingContext, routingContext, out int secondWritten, out string? secondError),
        secondError ?? "second parameter write failed");

    ReadOnlySpan<byte> parameters = buffer.Slice(0, firstWritten + secondWritten);
    M3uaParameterReader reader = new(parameters);

    Assert(reader.TryRead(out M3uaParameter first, out string? firstReadError), firstReadError ?? "first parameter read failed");
    AssertEqual(M3uaParameterTag.InfoString, first.Tag, "first tag");
    AssertEqual(7, first.Length, "first length excluding padding");
    AssertEqual(8, first.PaddedLength, "first padded length from reader");
    AssertSequence(info, first.Value, "first value");

    Assert(reader.TryRead(out M3uaParameter second, out string? secondReadError), secondReadError ?? "second parameter read failed");
    AssertEqual(M3uaParameterTag.RoutingContext, second.Tag, "second tag");
    AssertSequence(routingContext, second.Value, "second value");

    Assert(!reader.TryRead(out _, out string? endError), endError ?? "reader should be exhausted");
    Assert(
        M3uaParameterReader.TryFind(parameters, M3uaParameterTag.RoutingContext, out ReadOnlySpan<byte> found, out string? findError),
        findError ?? "routing context not found");
    AssertSequence(routingContext, found, "found routing context");
}

static void M3uaBuildsAspUp()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] info = [0x6E, 0x6F, 0x64, 0x65];

    Assert(
        M3uaMessageBuilder.BuildAspUp(buffer, aspIdentifier: 0x01020304, info, out int written, out string? error),
        error ?? "ASP Up build failed");

    AssertEqual(24, written, "ASP Up length");
    AssertSequence([0x01, 0x00, 0x03, 0x01], buffer.Slice(0, 4), "ASP Up header");
    AssertSequence([0x00, 0x00, 0x00, 0x18], buffer.Slice(4, 4), "ASP Up message length");
    AssertSequence([0x00, 0x11, 0x00, 0x08], buffer.Slice(8, 4), "ASP Identifier TLV");
    AssertSequence([0x01, 0x02, 0x03, 0x04], buffer.Slice(12, 4), "ASP Identifier value");
    AssertSequence([0x00, 0x04, 0x00, 0x08], buffer.Slice(16, 4), "Info String TLV");
    AssertSequence(info, buffer.Slice(20, 4), "Info String value");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Up decode failed");
    AssertEqual(M3uaMessageClass.Aspsm, message.MessageClass, "ASP Up class");
    AssertEqual((byte)M3uaAspsmMessageType.AspUp, message.MessageType, "ASP Up type");
}

static void M3uaBuildsHeartbeatAck()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] heartbeatData = [0x10, 0x20, 0x30, 0x40, 0x50];

    Assert(
        M3uaMessageBuilder.BuildHeartbeatAck(buffer, heartbeatData, out int written, out string? error),
        error ?? "Heartbeat Ack build failed");

    AssertEqual(20, written, "Heartbeat Ack length");
    AssertSequence([0x01, 0x00, 0x03, 0x06], buffer.Slice(0, 4), "Heartbeat Ack header");
    AssertSequence([0x00, 0x00, 0x00, 0x14], buffer.Slice(4, 4), "Heartbeat Ack message length");
    AssertSequence([0x00, 0x09, 0x00, 0x09], buffer.Slice(8, 4), "Heartbeat Data TLV");
    AssertSequence(heartbeatData, buffer.Slice(12, heartbeatData.Length), "Heartbeat Data value");
    AssertSequence([0x00, 0x00, 0x00], buffer.Slice(17, 3), "Heartbeat Data padding");

    Assert(
        M3uaParameterReader.TryFind(buffer.Slice(8, written - 8), M3uaParameterTag.HeartbeatData, out ReadOnlySpan<byte> found, out string? findError),
        findError ?? "Heartbeat Data not found");
    AssertSequence(heartbeatData, found, "found Heartbeat Data");
}

static void M3uaBuildsAspActive()
{
    Span<byte> buffer = stackalloc byte[64];
    uint[] routingContexts = [0x01020304, 0x05060708];
    byte[] info = [0x61, 0x63, 0x74];

    Assert(
        M3uaMessageBuilder.BuildAspActive(buffer, M3uaTrafficModeType.Loadshare, routingContexts, info, out int written, out string? error),
        error ?? "ASP Active build failed");

    AssertEqual(36, written, "ASP Active length");
    AssertSequence([0x01, 0x00, 0x04, 0x01], buffer.Slice(0, 4), "ASP Active header");
    AssertSequence([0x00, 0x00, 0x00, 0x24], buffer.Slice(4, 4), "ASP Active message length");
    AssertSequence([0x00, 0x0B, 0x00, 0x08], buffer.Slice(8, 4), "Traffic Mode Type TLV");
    AssertSequence([0x00, 0x00, 0x00, 0x02], buffer.Slice(12, 4), "Traffic Mode Type value");
    AssertSequence([0x00, 0x06, 0x00, 0x0C], buffer.Slice(16, 4), "Routing Context TLV");
    AssertSequence([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08], buffer.Slice(20, 8), "Routing Context values");
    AssertSequence([0x00, 0x04, 0x00, 0x07], buffer.Slice(28, 4), "Info String TLV");
    AssertSequence(info, buffer.Slice(32, 3), "Info String value");
    AssertSequence([0x00], buffer.Slice(35, 1), "Info String padding");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Active decode failed");
    AssertEqual(M3uaMessageClass.Asptm, message.MessageClass, "ASP Active class");
    AssertEqual((byte)M3uaAsptmMessageType.AspActive, message.MessageType, "ASP Active type");
    Assert(
        M3uaParameterReader.TryFind(message.Parameters.Span, M3uaParameterTag.RoutingContext, out ReadOnlySpan<byte> found, out string? findError),
        findError ?? "Routing Context not found");
    AssertSequence([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08], found, "found Routing Context");
}

static void M3uaBuildsAspInactiveAck()
{
    Span<byte> buffer = stackalloc byte[32];
    uint[] routingContexts = [0x00000009, 0x0000000A];

    Assert(
        M3uaMessageBuilder.BuildAspInactiveAck(buffer, routingContexts, ReadOnlySpan<byte>.Empty, out int written, out string? error),
        error ?? "ASP Inactive Ack build failed");

    AssertEqual(20, written, "ASP Inactive Ack length");
    AssertSequence([0x01, 0x00, 0x04, 0x04], buffer.Slice(0, 4), "ASP Inactive Ack header");
    AssertSequence([0x00, 0x00, 0x00, 0x14], buffer.Slice(4, 4), "ASP Inactive Ack message length");
    AssertSequence([0x00, 0x06, 0x00, 0x0C], buffer.Slice(8, 4), "Routing Context TLV");
    AssertSequence([0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x0A], buffer.Slice(12, 8), "Routing Context values");
}

static void M3uaParsesAspUp()
{
    Span<byte> buffer = stackalloc byte[64];
    byte[] info = [0x61, 0x73, 0x70];

    Assert(
        M3uaMessageBuilder.BuildAspUp(buffer, 0x0000002A, info, out int written, out string? buildError),
        buildError ?? "ASP Up build failed");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Up decode failed");
    Assert(
        M3uaTypedMessageParser.TryParseAspsm(message, out M3uaAspStateMaintenanceMessage? typed, out string? parseError),
        parseError ?? "ASP Up typed parse failed");

    AssertEqual(M3uaAspsmMessageType.AspUp, typed!.MessageType, "typed ASPSM type");
    AssertEqual((uint?)0x0000002A, typed.AspIdentifier, "typed ASP Identifier");
    AssertSequence(info, typed.InfoString.Span, "typed Info String");
    AssertEqual(0, typed.HeartbeatData.Length, "typed Heartbeat Data length");
}

static void M3uaParsesAspActive()
{
    Span<byte> buffer = stackalloc byte[64];
    uint[] routingContexts = [7, 8];

    Assert(
        M3uaMessageBuilder.BuildAspActive(buffer, M3uaTrafficModeType.Override, routingContexts, ReadOnlySpan<byte>.Empty, out int written, out string? buildError),
        buildError ?? "ASP Active build failed");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, written), out string? decodeError), decodeError ?? "ASP Active decode failed");
    Assert(
        M3uaTypedMessageParser.TryParseAsptm(message, out M3uaAspTrafficMaintenanceMessage? typed, out string? parseError),
        parseError ?? "ASP Active typed parse failed");

    AssertEqual(M3uaAsptmMessageType.AspActive, typed!.MessageType, "typed ASPTM type");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Override, typed.TrafficModeType, "typed Traffic Mode");
    AssertSequence([0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x08], UInt32SpanToBytes(typed.RoutingContexts), "typed Routing Contexts");
}

static void M3uaRejectsMalformedTypedRoutingContext()
{
    Span<byte> buffer = stackalloc byte[32];
    byte[] malformedRoutingContext = [0x00, 0x00, 0x00];

    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Asptm;
    buffer[3] = (byte)M3uaAsptmMessageType.AspActive;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 16;
    Assert(
        M3uaParameterWriter.TryWrite(buffer.Slice(8), M3uaParameterTag.RoutingContext, malformedRoutingContext, out int parameterWritten, out string? writeError),
        writeError ?? "malformed parameter write failed");
    AssertEqual(8, parameterWritten, "malformed parameter padded length");

    M3uaMessage message = new();
    Assert(message.TryDecode(buffer.Slice(0, 16), out string? decodeError), decodeError ?? "malformed message decode failed");
    Assert(
        !M3uaTypedMessageParser.TryParseAsptm(message, out _, out string? parseError),
        "malformed Routing Context should be rejected");
    Assert(parseError?.Contains("non-empty multiple of 4 bytes", StringComparison.Ordinal) == true, parseError ?? "missing parse error");
}

static void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.ExitCode = 1;
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual, string label)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
    }
}

static void AssertSequence(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual, string label)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException($"{label}: expected {Convert.ToHexString(expected)}, got {Convert.ToHexString(actual)}");
    }
}

static byte[] UInt32SpanToBytes(ReadOnlySpan<uint> values)
{
    byte[] bytes = new byte[values.Length * sizeof(uint)];
    for (int i = 0; i < values.Length; i++)
    {
        bytes[i * sizeof(uint)] = (byte)((values[i] >> 24) & 0xFF);
        bytes[(i * sizeof(uint)) + 1] = (byte)((values[i] >> 16) & 0xFF);
        bytes[(i * sizeof(uint)) + 2] = (byte)((values[i] >> 8) & 0xFF);
        bytes[(i * sizeof(uint)) + 3] = (byte)(values[i] & 0xFF);
    }

    return bytes;
}
