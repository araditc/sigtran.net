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
Run("M3UA ASP state machine follows the active lifecycle", M3uaAspStateMachineFollowsActiveLifecycle);
Run("M3UA ASP state machine rejects invalid transitions", M3uaAspStateMachineRejectsInvalidTransitions);
Run("M3UA ASP session applies acknowledgement lifecycle messages", M3uaAspSessionAppliesAcknowledgementLifecycle);
Run("M3UA ASP session rejects acknowledgement messages in the wrong state", M3uaAspSessionRejectsWrongStateAcknowledgement);
Run("M3UA parses Management Error messages", M3uaParsesManagementError);
Run("M3UA parses Management Notify messages", M3uaParsesManagementNotify);
Run("M3UA rejects invalid Management Notify status information", M3uaRejectsInvalidManagementNotifyStatusInformation);
Run("M3UA parses SSNM Destination Unavailable messages", M3uaParsesSsnmDestinationUnavailable);
Run("M3UA rejects SSNM messages without Affected Point Code", M3uaRejectsSsnmWithoutAffectedPointCode);
Run("M3UA parses SSNM Destination User Part Unavailable messages", M3uaParsesDestinationUserPartUnavailable);
Run("M3UA rejects DUPU with non-zero affected point-code mask", M3uaRejectsDupuWithNonZeroMask);

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

static void M3uaAspStateMachineFollowsActiveLifecycle()
{
    M3uaAspStateMachine machine = new();
    AssertEqual(M3uaAspState.Down, machine.State, "initial ASP state");

    Assert(
        machine.TryApply(M3uaAspEvent.AspUpAcknowledged, out M3uaAspStateTransition up, out string? upError),
        upError ?? "ASP Up Ack transition failed");
    AssertEqual(M3uaAspState.Down, up.From, "ASP Up Ack from");
    AssertEqual(M3uaAspState.Inactive, up.To, "ASP Up Ack to");
    Assert(up.Changed, "ASP Up Ack should change state");

    Assert(
        machine.TryApply(M3uaAspEvent.AspActiveAcknowledged, out M3uaAspStateTransition active, out string? activeError),
        activeError ?? "ASP Active Ack transition failed");
    AssertEqual(M3uaAspState.Active, machine.State, "active ASP state");
    AssertEqual(M3uaAspState.Inactive, active.From, "ASP Active Ack from");
    AssertEqual(M3uaAspState.Active, active.To, "ASP Active Ack to");

    Assert(
        machine.TryApply(M3uaAspEvent.AspInactiveAcknowledged, out M3uaAspStateTransition inactive, out string? inactiveError),
        inactiveError ?? "ASP Inactive Ack transition failed");
    AssertEqual(M3uaAspState.Inactive, machine.State, "inactive ASP state");
    AssertEqual(M3uaAspState.Active, inactive.From, "ASP Inactive Ack from");
    AssertEqual(M3uaAspState.Inactive, inactive.To, "ASP Inactive Ack to");

    Assert(
        machine.TryApply(M3uaAspEvent.AspDownAcknowledged, out M3uaAspStateTransition down, out string? downError),
        downError ?? "ASP Down Ack transition failed");
    AssertEqual(M3uaAspState.Down, machine.State, "down ASP state");
    AssertEqual(M3uaAspState.Inactive, down.From, "ASP Down Ack from");
    AssertEqual(M3uaAspState.Down, down.To, "ASP Down Ack to");
}

static void M3uaAspStateMachineRejectsInvalidTransitions()
{
    M3uaAspStateMachine machine = new();
    Assert(
        !machine.TryApply(M3uaAspEvent.AspActiveAcknowledged, out _, out string? error),
        "ASP Active Ack from Down should be rejected");
    Assert(error?.Contains("Cannot apply", StringComparison.Ordinal) == true, error ?? "missing invalid transition error");
    AssertEqual(M3uaAspState.Down, machine.State, "state after rejected transition");

    Assert(
        machine.TryApply(M3uaAspEvent.TransportLost, out M3uaAspStateTransition lost, out string? lostError),
        lostError ?? "TransportLost transition failed");
    AssertEqual(M3uaAspState.Down, lost.To, "TransportLost target");
    Assert(!lost.Changed, "TransportLost from Down should not change state");
}

static void M3uaAspSessionAppliesAcknowledgementLifecycle()
{
    M3uaAspSession session = new();
    Span<byte> buffer = stackalloc byte[64];

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, 0x0000002A, ReadOnlySpan<byte>.Empty, out int written, out string? buildUpError),
        buildUpError ?? "ASP Up Ack build failed");
    M3uaMessage upAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(upAck, out M3uaAspStateTransition up, out string? upError),
        upError ?? "ASP Up Ack session apply failed");
    AssertEqual(M3uaAspState.Inactive, session.State, "session state after ASP Up Ack");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, up.Event, "ASP Up Ack event");
    AssertEqual((uint?)0x0000002A, session.AspIdentifier, "session ASP Identifier");

    uint[] routingContexts = [100, 200];
    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, routingContexts, ReadOnlySpan<byte>.Empty, out written, out string? buildActiveError),
        buildActiveError ?? "ASP Active Ack build failed");
    M3uaMessage activeAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(activeAck, out M3uaAspStateTransition active, out string? activeError),
        activeError ?? "ASP Active Ack session apply failed");
    AssertEqual(M3uaAspState.Active, session.State, "session state after ASP Active Ack");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, active.Event, "ASP Active Ack event");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Loadshare, session.TrafficModeType, "session Traffic Mode");
    AssertSequence([0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0xC8], UInt32SpanToBytes(session.RoutingContexts), "session Routing Contexts");

    Assert(
        M3uaMessageBuilder.BuildHeartbeatAck(buffer, [0x01, 0x02], out written, out string? buildBeatError),
        buildBeatError ?? "Heartbeat Ack build failed");
    M3uaMessage heartbeatAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(heartbeatAck, out M3uaAspStateTransition heartbeat, out string? heartbeatError),
        heartbeatError ?? "Heartbeat Ack session apply failed");
    AssertEqual(M3uaAspState.Active, session.State, "session state after Heartbeat Ack");
    AssertEqual(M3uaAspEvent.HeartbeatAcknowledged, heartbeat.Event, "Heartbeat Ack event");
    Assert(!heartbeat.Changed, "Heartbeat Ack should not change state");

    Assert(
        M3uaMessageBuilder.BuildAspInactiveAck(buffer, [100], ReadOnlySpan<byte>.Empty, out written, out string? buildInactiveError),
        buildInactiveError ?? "ASP Inactive Ack build failed");
    M3uaMessage inactiveAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(inactiveAck, out M3uaAspStateTransition inactive, out string? inactiveError),
        inactiveError ?? "ASP Inactive Ack session apply failed");
    AssertEqual(M3uaAspState.Inactive, session.State, "session state after ASP Inactive Ack");
    AssertEqual(M3uaAspEvent.AspInactiveAcknowledged, inactive.Event, "ASP Inactive Ack event");
    AssertSequence([0x00, 0x00, 0x00, 0x64], UInt32SpanToBytes(session.RoutingContexts), "session inactive Routing Contexts");

    Assert(
        M3uaMessageBuilder.BuildAspDownAck(buffer, ReadOnlySpan<byte>.Empty, out written, out string? buildDownError),
        buildDownError ?? "ASP Down Ack build failed");
    M3uaMessage downAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        session.TryApplyAcknowledgement(downAck, out M3uaAspStateTransition down, out string? downError),
        downError ?? "ASP Down Ack session apply failed");
    AssertEqual(M3uaAspState.Down, session.State, "session state after ASP Down Ack");
    AssertEqual(M3uaAspEvent.AspDownAcknowledged, down.Event, "ASP Down Ack event");
    AssertEqual(null, session.TrafficModeType, "session Traffic Mode after ASP Down Ack");
    AssertEqual(0, session.RoutingContexts.Length, "session Routing Context count after ASP Down Ack");
}

static void M3uaAspSessionRejectsWrongStateAcknowledgement()
{
    M3uaAspSession session = new();
    Span<byte> buffer = stackalloc byte[32];
    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Override, [1], ReadOnlySpan<byte>.Empty, out int written, out string? buildError),
        buildError ?? "ASP Active Ack build failed");

    M3uaMessage activeAck = DecodeMessage(buffer.Slice(0, written));
    Assert(
        !session.TryApplyAcknowledgement(activeAck, out _, out string? error),
        "ASP Active Ack from Down should be rejected by session");
    Assert(error?.Contains("Cannot apply", StringComparison.Ordinal) == true, error ?? "missing session transition error");
    AssertEqual(M3uaAspState.Down, session.State, "session state after rejected acknowledgement");
}

static void M3uaParsesManagementError()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000011, 0x00000022];
    byte[] diagnostic = [0xDE, 0xAD, 0xBE, 0xEF, 0x01];

    Assert(
        M3uaMessageBuilder.BuildError(buffer, M3uaErrorCode.InvalidRoutingContext, routingContexts, 0x00000005, diagnostic, out int written, out string? buildError),
        buildError ?? "Error build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Management, message.MessageClass, "Error message class");
    AssertEqual((byte)M3uaManagementMessageType.Error, message.MessageType, "Error message type");
    Assert(
        M3uaTypedMessageParser.TryParseError(message, out M3uaErrorMessage? typed, out string? parseError),
        parseError ?? "Error typed parse failed");

    AssertEqual(M3uaErrorCode.InvalidRoutingContext, typed!.ErrorCode, "typed Error Code");
    AssertEqual((uint?)0x00000005, typed.NetworkAppearance, "typed Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x22], UInt32SpanToBytes(typed.RoutingContexts), "typed Error Routing Contexts");
    AssertSequence(diagnostic, typed.DiagnosticInformation.Span, "typed Diagnostic Information");
}

static void M3uaParsesManagementNotify()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000033];
    byte[] info = [0x61, 0x73, 0x2D, 0x61, 0x63, 0x74];

    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            (ushort)M3uaApplicationServerState.Active,
            aspIdentifier: 0x0000002A,
            routingContexts,
            info,
            out int written,
            out string? buildError),
        buildError ?? "Notify build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Management, message.MessageClass, "Notify message class");
    AssertEqual((byte)M3uaManagementMessageType.Notify, message.MessageType, "Notify message type");
    Assert(
        M3uaTypedMessageParser.TryParseNotify(message, out M3uaNotifyMessage? typed, out string? parseError),
        parseError ?? "Notify typed parse failed");

    AssertEqual(M3uaNotifyStatusType.ApplicationServerStateChange, typed!.StatusType, "typed Notify Status Type");
    AssertEqual((ushort)M3uaApplicationServerState.Active, typed.StatusInformation, "typed Notify Status Information");
    AssertEqual((uint?)0x0000002A, typed.AspIdentifier, "typed Notify ASP Identifier");
    AssertSequence([0x00, 0x00, 0x00, 0x33], UInt32SpanToBytes(typed.RoutingContexts), "typed Notify Routing Contexts");
    AssertSequence(info, typed.InfoString.Span, "typed Notify Info String");
}

static void M3uaRejectsInvalidManagementNotifyStatusInformation()
{
    Span<byte> buffer = stackalloc byte[32];
    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            statusInformation: 99,
            aspIdentifier: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? buildError),
        buildError ?? "Notify build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        !M3uaTypedMessageParser.TryParseNotify(message, out _, out string? parseError),
        "invalid Notify status information should be rejected");
    Assert(parseError?.Contains("Unknown Notify Status Information", StringComparison.Ordinal) == true, parseError ?? "missing Notify parse error");
}

static void M3uaParsesSsnmDestinationUnavailable()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000012];
    M3uaAffectedPointCode[] affectedPointCodes =
    [
        new(mask: 0x00, pointCode: 0x00123456),
        new(mask: 0xFF, pointCode: 0x0000ABCD)
    ];
    byte[] info = [0x64, 0x75, 0x6E, 0x61];

    Assert(
        M3uaMessageBuilder.BuildDestinationUnavailable(
            buffer,
            networkAppearance: 0x00000005,
            routingContexts,
            affectedPointCodes,
            info,
            out int written,
            out string? buildError),
        buildError ?? "DUNA build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, "DUNA message class");
    AssertEqual((byte)M3uaSsnmMessageType.DestinationUnavailable, message.MessageType, "DUNA message type");
    Assert(
        M3uaTypedMessageParser.TryParseSsnm(message, out M3uaSsnmMessage? typed, out string? parseError),
        parseError ?? "DUNA typed parse failed");

    AssertEqual(M3uaSsnmMessageType.DestinationUnavailable, typed!.MessageType, "typed DUNA type");
    AssertEqual((uint?)0x00000005, typed.NetworkAppearance, "typed DUNA Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x12], UInt32SpanToBytes(typed.RoutingContexts), "typed DUNA Routing Contexts");
    AssertEqual(2, typed.AffectedPointCodes.Length, "typed affected point-code count");
    AssertEqual((byte)0x00, typed.AffectedPointCodes[0].Mask, "first affected point-code mask");
    AssertEqual((uint)0x00123456, typed.AffectedPointCodes[0].PointCode, "first affected point-code value");
    AssertEqual((byte)0xFF, typed.AffectedPointCodes[1].Mask, "second affected point-code mask");
    AssertEqual((uint)0x0000ABCD, typed.AffectedPointCodes[1].PointCode, "second affected point-code value");
    AssertSequence(info, typed.InfoString.Span, "typed DUNA Info String");
}

static void M3uaRejectsSsnmWithoutAffectedPointCode()
{
    Span<byte> buffer = stackalloc byte[32];
    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Ssnm;
    buffer[3] = (byte)M3uaSsnmMessageType.DestinationAvailable;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 8;

    M3uaMessage message = DecodeMessage(buffer.Slice(0, 8));
    Assert(
        !M3uaTypedMessageParser.TryParseSsnm(message, out _, out string? parseError),
        "SSNM without Affected Point Code should be rejected");
    Assert(parseError?.Contains("Missing Affected Point Code", StringComparison.Ordinal) == true, parseError ?? "missing SSNM parse error");
}

static void M3uaParsesDestinationUserPartUnavailable()
{
    Span<byte> buffer = stackalloc byte[80];
    uint[] routingContexts = [0x00000044];
    M3uaAffectedPointCode affectedPointCode = new(mask: 0, pointCode: 0x00012345);
    byte[] info = [0x64, 0x75, 0x70, 0x75];

    Assert(
        M3uaMessageBuilder.BuildDestinationUserPartUnavailable(
            buffer,
            networkAppearance: 0x00000007,
            routingContexts,
            affectedPointCode,
            M3uaUserPartUnavailableCause.InaccessibleRemoteUser,
            M3uaMtp3UserIdentity.Sccp,
            info,
            out int written,
            out string? buildError),
        buildError ?? "DUPU build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, "DUPU message class");
    AssertEqual((byte)M3uaSsnmMessageType.DestinationUserPartUnavailable, message.MessageType, "DUPU message type");
    Assert(
        M3uaTypedMessageParser.TryParseDestinationUserPartUnavailable(message, out M3uaDestinationUserPartUnavailableMessage? typed, out string? parseError),
        parseError ?? "DUPU typed parse failed");

    AssertEqual((uint?)0x00000007, typed!.NetworkAppearance, "typed DUPU Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x44], UInt32SpanToBytes(typed.RoutingContexts), "typed DUPU Routing Contexts");
    AssertEqual((byte)0, typed.AffectedPointCode.Mask, "typed DUPU mask");
    AssertEqual((uint)0x00012345, typed.AffectedPointCode.PointCode, "typed DUPU point code");
    AssertEqual(M3uaUserPartUnavailableCause.InaccessibleRemoteUser, typed.Cause, "typed DUPU cause");
    AssertEqual(M3uaMtp3UserIdentity.Sccp, typed.UserIdentity, "typed DUPU user identity");
    AssertSequence(info, typed.InfoString.Span, "typed DUPU Info String");
}

static void M3uaRejectsDupuWithNonZeroMask()
{
    Span<byte> buffer = stackalloc byte[64];
    Assert(
        !M3uaMessageBuilder.BuildDestinationUserPartUnavailable(
            buffer,
            networkAppearance: null,
            ReadOnlySpan<uint>.Empty,
            new M3uaAffectedPointCode(mask: 1, pointCode: 0x00012345),
            M3uaUserPartUnavailableCause.Unknown,
            M3uaMtp3UserIdentity.Sccp,
            ReadOnlySpan<byte>.Empty,
            out _,
            out string? buildError),
        "DUPU with non-zero mask should be rejected");
    Assert(buildError?.Contains("mask must be 0", StringComparison.Ordinal) == true, buildError ?? "missing DUPU mask error");
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

static M3uaMessage DecodeMessage(ReadOnlySpan<byte> encoded)
{
    M3uaMessage message = new();
    Assert(message.TryDecode(encoded, out string? error), error ?? "message decode failed");
    return message;
}
