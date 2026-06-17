using sigtran.net.Layers.M3UA;
using sigtran.net.Core.Interfaces;

Run("M3UA Payload Data uses network byte order and RFC-style TLV length", M3uaPayloadDataUsesNetworkOrder);
Run("M3UA decoder returns the complete Protocol Data value", M3uaDecoderReturnsProtocolDataValue);
Run("M3UA parses Payload Data optional fields", M3uaParsesPayloadDataOptionalFields);
Run("M3UA rejects Payload Data without Protocol Data", M3uaRejectsPayloadDataWithoutProtocolData);
Run("M3UA dispatches known typed messages", M3uaDispatchesKnownTypedMessages);
Run("M3UA dispatcher rejects unsupported message types", M3uaDispatcherRejectsUnsupportedMessageTypes);
Run("M3UA route table resolves the most specific DATA route", M3uaRouteTableResolvesMostSpecificDataRoute);
Run("M3UA route table rejects ambiguous DATA routes", M3uaRouteTableRejectsAmbiguousDataRoutes);
Run("M3UA route table rejects duplicate selectors", M3uaRouteTableRejectsDuplicateSelectors);
Run("M3UA inbound processor updates ASP state and routes DATA", M3uaInboundProcessorUpdatesAspStateAndRoutesData);
Run("M3UA inbound processor can require active ASP for DATA", M3uaInboundProcessorCanRequireActiveAspForData);
Run("M3UA inbound processor rejects unrouted DATA when routes exist", M3uaInboundProcessorRejectsUnroutedDataWhenRoutesExist);
Run("M3UA outbound processor applies defaults to DATA", M3uaOutboundProcessorAppliesDefaultsToData);
Run("M3UA outbound processor can require active ASP for DATA", M3uaOutboundProcessorCanRequireActiveAspForData);
Run("M3UA outbound processor builds ASP Active with default Routing Context", M3uaOutboundProcessorBuildsAspActiveWithDefaultRoutingContext);
Run("M3UA transport session sends outbound DATA", M3uaTransportSessionSendsOutboundData);
Run("M3UA transport session receives inbound DATA", M3uaTransportSessionReceivesInboundData);
Run("M3UA transport session disposes owned socket", M3uaTransportSessionDisposesOwnedSocket);
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
Run("M3UA parses SSNM Signalling Congestion messages", M3uaParsesSignallingCongestion);
Run("M3UA rejects SCON without Affected Point Code", M3uaRejectsSconWithoutAffectedPointCode);
Run("M3UA parses RKM Registration Request messages", M3uaParsesRegistrationRequest);
Run("M3UA parses RKM Registration Response messages", M3uaParsesRegistrationResponse);
Run("M3UA parses RKM Deregistration messages", M3uaParsesDeregistrationMessages);
Run("M3UA rejects Routing Key without Destination Point Code", M3uaRejectsRoutingKeyWithoutDestinationPointCode);

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

static void M3uaParsesPayloadDataOptionalFields()
{
    Span<byte> buffer = stackalloc byte[80];
    byte[] payload = [0xCA, 0xFE, 0x01];

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            payload,
            opc: 0x00010203,
            dpc: 0x00040506,
            si: 3,
            ni: 2,
            mp: 1,
            sls: 8,
            networkAppearance: 0x00000007,
            routingContext: 0x00000064,
            correlationId: 0x0000002A,
            out int written,
            out string? buildError),
        buildError ?? "DATA build failed");

    AssertEqual(52, written, "DATA length with optional fields");
    AssertSequence([0x02, 0x00, 0x00, 0x08], buffer.Slice(8, 4), "Network Appearance TLV");
    AssertSequence([0x00, 0x06, 0x00, 0x08], buffer.Slice(16, 4), "Routing Context TLV");
    AssertSequence([0x02, 0x10, 0x00, 0x13], buffer.Slice(24, 4), "Protocol Data TLV");
    AssertSequence([0x00, 0x13, 0x00, 0x08], buffer.Slice(44, 4), "Correlation Id TLV");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(message, out M3uaPayloadDataMessage? typed, out string? parseError),
        parseError ?? "DATA typed parse failed");

    AssertEqual((uint?)0x00000007, typed!.NetworkAppearance, "typed DATA Network Appearance");
    AssertEqual((uint?)0x00000064, typed.RoutingContext, "typed DATA Routing Context");
    AssertEqual((uint)0x00010203, typed.OriginatingPointCode, "typed DATA OPC");
    AssertEqual((uint)0x00040506, typed.DestinationPointCode, "typed DATA DPC");
    AssertEqual((byte)3, typed.ServiceIndicator, "typed DATA SI");
    AssertEqual((byte)2, typed.NetworkIndicator, "typed DATA NI");
    AssertEqual((byte)1, typed.MessagePriority, "typed DATA MP");
    AssertEqual((byte)8, typed.SignallingLinkSelection, "typed DATA SLS");
    AssertSequence(payload, typed.UserPayload, "typed DATA user payload");
    AssertEqual((uint?)0x0000002A, typed.CorrelationId, "typed DATA Correlation Id");
}

static void M3uaRejectsPayloadDataWithoutProtocolData()
{
    Span<byte> buffer = stackalloc byte[16];
    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Transfer;
    buffer[3] = (byte)M3uaTransferMessageType.PayloadData;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 8;

    M3uaMessage message = DecodeMessage(buffer.Slice(0, 8));
    Assert(
        !M3uaTypedMessageParser.TryParsePayloadData(message, out _, out string? parseError),
        "DATA without Protocol Data should be rejected");
    Assert(parseError?.Contains("Missing Protocol Data", StringComparison.Ordinal) == true, parseError ?? "missing DATA parse error");
}

static void M3uaDispatchesKnownTypedMessages()
{
    Span<byte> buffer = stackalloc byte[128];

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01, 0x02],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: 100,
            correlationId: null,
            out int written,
            out string? dataBuildError),
        dataBuildError ?? "DATA build failed");

    M3uaMessage dataMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseKnown(dataMessage, out M3uaTypedMessage? dataTyped, out string? dataParseError),
        dataParseError ?? "DATA dispatch failed");
    AssertEqual(M3uaTypedMessageKind.PayloadData, dataTyped!.Kind, "DATA dispatch kind");
    M3uaPayloadDataMessage payloadData = dataTyped.As<M3uaPayloadDataMessage>();
    AssertEqual((uint?)100, payloadData.RoutingContext, "DATA dispatch Routing Context");
    AssertSequence([0x01, 0x02], payloadData.UserPayload, "DATA dispatch payload");

    Assert(
        M3uaMessageBuilder.BuildDestinationUserPartUnavailable(
            buffer,
            networkAppearance: null,
            ReadOnlySpan<uint>.Empty,
            new M3uaAffectedPointCode(mask: 0, pointCode: 0x00012345),
            M3uaUserPartUnavailableCause.Unknown,
            M3uaMtp3UserIdentity.Sccp,
            ReadOnlySpan<byte>.Empty,
            out written,
            out string? dupuBuildError),
        dupuBuildError ?? "DUPU build failed");

    M3uaMessage dupuMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseKnown(dupuMessage, out M3uaTypedMessage? dupuTyped, out string? dupuParseError),
        dupuParseError ?? "DUPU dispatch failed");
    AssertEqual(M3uaTypedMessageKind.DestinationUserPartUnavailable, dupuTyped!.Kind, "DUPU dispatch kind");
    AssertEqual(M3uaMtp3UserIdentity.Sccp, dupuTyped.As<M3uaDestinationUserPartUnavailableMessage>().UserIdentity, "DUPU dispatch user identity");

    M3uaRegistrationResult[] results = [new(1, M3uaRegistrationStatus.SuccessfullyRegistered, 100)];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, results, out written, out string? regBuildError),
        regBuildError ?? "REG RSP build failed");

    M3uaMessage registrationMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseKnown(registrationMessage, out M3uaTypedMessage? registrationTyped, out string? registrationParseError),
        registrationParseError ?? "REG RSP dispatch failed");
    AssertEqual(M3uaTypedMessageKind.RegistrationResponse, registrationTyped!.Kind, "REG RSP dispatch kind");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, registrationTyped.As<M3uaRegistrationResponseMessage>().Results[0].Status, "REG RSP dispatch status");
}

static void M3uaDispatcherRejectsUnsupportedMessageTypes()
{
    Span<byte> buffer = stackalloc byte[8];
    buffer[0] = 1;
    buffer[1] = 0;
    buffer[2] = (byte)M3uaMessageClass.Management;
    buffer[3] = 0x7F;
    buffer[4] = 0;
    buffer[5] = 0;
    buffer[6] = 0;
    buffer[7] = 8;

    M3uaMessage message = DecodeMessage(buffer);
    Assert(
        !M3uaTypedMessageParser.TryParseKnown(message, out _, out string? parseError),
        "unsupported Management type should be rejected");
    Assert(parseError?.Contains("Unsupported Management message type", StringComparison.Ordinal) == true, parseError ?? "missing dispatcher parse error");
}

static void M3uaRouteTableResolvesMostSpecificDataRoute()
{
    M3uaPayloadRouteTable table = new();
    Assert(table.TryAdd(new M3uaPayloadRoute("sccp-default", null, routingContext: 100, null, serviceIndicator: 3), out string? addDefaultError), addDefaultError ?? "default route add failed");
    Assert(table.TryAdd(new M3uaPayloadRoute("map-home", networkAppearance: 7, routingContext: 100, destinationPointCode: 0x00040506, serviceIndicator: 3), out string? addSpecificError), addSpecificError ?? "specific route add failed");

    M3uaPayloadDataMessage message = new(
        networkAppearance: 7,
        routingContext: 100,
        originatingPointCode: 0x00010203,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3,
        networkIndicator: 2,
        messagePriority: 0,
        signallingLinkSelection: 7,
        userPayload: [0x01, 0x02],
        correlationId: null);

    Assert(table.TryResolve(message, out M3uaPayloadRoute? route, out string? resolveError), resolveError ?? "route resolve failed");
    AssertEqual("map-home", route!.Name, "resolved route name");
}

static void M3uaRouteTableRejectsAmbiguousDataRoutes()
{
    M3uaPayloadRouteTable table = new();
    Assert(table.TryAdd(new M3uaPayloadRoute("by-rc", null, routingContext: 100, null, null), out string? addRcError), addRcError ?? "RC route add failed");
    Assert(table.TryAdd(new M3uaPayloadRoute("by-dpc", null, routingContext: null, destinationPointCode: 0x00040506, null), out string? addDpcError), addDpcError ?? "DPC route add failed");

    M3uaPayloadDataMessage message = new(
        networkAppearance: null,
        routingContext: 100,
        originatingPointCode: 1,
        destinationPointCode: 0x00040506,
        serviceIndicator: 3,
        networkIndicator: 2,
        messagePriority: 0,
        signallingLinkSelection: 7,
        userPayload: ReadOnlySpan<byte>.Empty,
        correlationId: null);

    Assert(!table.TryResolve(message, out _, out string? resolveError), "ambiguous routes should be rejected");
    Assert(resolveError?.Contains("same specificity", StringComparison.Ordinal) == true, resolveError ?? "missing ambiguity error");
}

static void M3uaRouteTableRejectsDuplicateSelectors()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);

    Assert(table.TryAdd(first, out string? firstError), firstError ?? "first route add failed");
    Assert(!table.TryAdd(second, out string? secondError), "duplicate selectors should be rejected");
    Assert(secondError?.Contains("same selectors", StringComparison.Ordinal) == true, secondError ?? "missing duplicate route error");
}

static void M3uaInboundProcessorUpdatesAspStateAndRoutesData()
{
    Span<byte> buffer = stackalloc byte[128];
    M3uaPayloadRouteTable routes = new();
    Assert(
        routes.TryAdd(new M3uaPayloadRoute("map-home", networkAppearance: 7, routingContext: 100, destinationPointCode: 0x00040506, serviceIndicator: 3), out string? addError),
        addError ?? "route add failed");
    M3uaInboundProcessor processor = new(payloadRoutes: routes, requireActiveAspForPayload: true);

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 0x0000002A, ReadOnlySpan<byte>.Empty, out int written, out string? upBuildError),
        upBuildError ?? "ASP Up Ack build failed");
    Assert(
        processor.TryProcess(buffer.Slice(0, written), out M3uaInboundProcessingResult? upResult, out string? upError),
        upError ?? "ASP Up Ack process failed");
    AssertEqual(M3uaAspState.Inactive, processor.AspSession.State, "processor ASP state after ASP Up Ack");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, upResult!.StateTransition!.Value.Event, "processor ASP Up event");

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, [100], ReadOnlySpan<byte>.Empty, out written, out string? activeBuildError),
        activeBuildError ?? "ASP Active Ack build failed");
    Assert(
        processor.TryProcess(buffer.Slice(0, written), out M3uaInboundProcessingResult? activeResult, out string? activeError),
        activeError ?? "ASP Active Ack process failed");
    AssertEqual(M3uaAspState.Active, processor.AspSession.State, "processor ASP state after ASP Active Ack");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, activeResult!.StateTransition!.Value.Event, "processor ASP Active event");

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01, 0x02],
            opc: 0x00010203,
            dpc: 0x00040506,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: 7,
            routingContext: 100,
            correlationId: null,
            out written,
            out string? dataBuildError),
        dataBuildError ?? "DATA build failed");
    Assert(
        processor.TryProcess(buffer.Slice(0, written), out M3uaInboundProcessingResult? dataResult, out string? dataError),
        dataError ?? "DATA process failed");
    AssertEqual(M3uaTypedMessageKind.PayloadData, dataResult!.TypedMessage.Kind, "processor DATA kind");
    AssertEqual("map-home", dataResult.PayloadRoute!.Name, "processor DATA route");
}

static void M3uaInboundProcessorCanRequireActiveAspForData()
{
    Span<byte> buffer = stackalloc byte[80];
    M3uaInboundProcessor processor = new(requireActiveAspForPayload: true);
    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: null,
            correlationId: null,
            out int written,
            out string? buildError),
        buildError ?? "DATA build failed");

    Assert(
        !processor.TryProcess(buffer.Slice(0, written), out _, out string? processError),
        "processor should reject DATA while ASP is not active");
    Assert(processError?.Contains("ASP is Down", StringComparison.Ordinal) == true, processError ?? "missing ASP state error");
}

static void M3uaInboundProcessorRejectsUnroutedDataWhenRoutesExist()
{
    Span<byte> buffer = stackalloc byte[80];
    M3uaPayloadRouteTable routes = new();
    Assert(routes.TryAdd(new M3uaPayloadRoute("isup", null, routingContext: 200, null, serviceIndicator: 5), out string? addError), addError ?? "route add failed");
    M3uaInboundProcessor processor = new(new M3uaAspSession(M3uaAspState.Active), routes);

    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: 100,
            correlationId: null,
            out int written,
            out string? buildError),
        buildError ?? "DATA build failed");

    Assert(
        !processor.TryProcess(buffer.Slice(0, written), out _, out string? processError),
        "processor should reject unrouted DATA when routes exist");
    Assert(processError?.Contains("No Payload Data route", StringComparison.Ordinal) == true, processError ?? "missing route error");
}

static void M3uaOutboundProcessorAppliesDefaultsToData()
{
    Span<byte> buffer = stackalloc byte[96];
    M3uaOutboundProcessor processor = new(networkAppearance: 7, routingContext: 100);

    Assert(
        processor.TryBuildPayloadData(
            buffer,
            userPayload: [0xAA],
            originatingPointCode: 1,
            destinationPointCode: 2,
            serviceIndicator: 3,
            networkIndicator: 2,
            messagePriority: 0,
            signallingLinkSelection: 7,
            networkAppearance: null,
            routingContext: null,
            correlationId: 42,
            out int written,
            out string? buildError),
        buildError ?? "outbound DATA build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(message, out M3uaPayloadDataMessage? typed, out string? parseError),
        parseError ?? "outbound DATA parse failed");
    AssertEqual((uint?)7, typed!.NetworkAppearance, "outbound DATA default Network Appearance");
    AssertEqual((uint?)100, typed.RoutingContext, "outbound DATA default Routing Context");
    AssertEqual((uint?)42, typed.CorrelationId, "outbound DATA Correlation Id");
    AssertSequence([0xAA], typed.UserPayload, "outbound DATA payload");
}

static void M3uaOutboundProcessorCanRequireActiveAspForData()
{
    Span<byte> buffer = stackalloc byte[64];
    M3uaOutboundProcessor processor = new(requireActiveAspForPayload: true);

    Assert(
        !processor.TryBuildPayloadData(
            buffer,
            userPayload: [0xAA],
            originatingPointCode: 1,
            destinationPointCode: 2,
            serviceIndicator: 3,
            networkIndicator: 2,
            messagePriority: 0,
            signallingLinkSelection: 7,
            networkAppearance: null,
            routingContext: null,
            correlationId: null,
            out _,
            out string? buildError),
        "outbound DATA should be rejected while ASP is not active");
    Assert(buildError?.Contains("ASP is Down", StringComparison.Ordinal) == true, buildError ?? "missing outbound ASP state error");
}

static void M3uaOutboundProcessorBuildsAspActiveWithDefaultRoutingContext()
{
    Span<byte> buffer = stackalloc byte[64];
    M3uaOutboundProcessor processor = new(routingContext: 100);

    Assert(
        processor.TryBuildAspActive(
            buffer,
            M3uaTrafficModeType.Loadshare,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? buildError),
        buildError ?? "outbound ASP Active build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParseAsptm(message, out M3uaAspTrafficMaintenanceMessage? typed, out string? parseError),
        parseError ?? "outbound ASP Active parse failed");
    AssertEqual(M3uaAsptmMessageType.AspActive, typed!.MessageType, "outbound ASP Active type");
    AssertSequence([0x00, 0x00, 0x00, 0x64], UInt32SpanToBytes(typed.RoutingContexts), "outbound ASP Active Routing Context");
}

static void M3uaTransportSessionSendsOutboundData()
{
    FakeSctpSocket socket = new();
    M3uaOutboundProcessor outbound = new(networkAppearance: 7, routingContext: 100);
    using M3uaTransportSession session = new(socket, outboundProcessor: outbound, leaveOpen: true);

    session.SendPayloadDataAsync(
        userPayload: new byte[] { 0xCA, 0xFE },
        originatingPointCode: 1,
        destinationPointCode: 2,
        serviceIndicator: 3,
        networkIndicator: 2,
        messagePriority: 0,
        signallingLinkSelection: 7,
        correlationId: 42).GetAwaiter().GetResult();

    AssertEqual(1, socket.SentPackets.Count, "sent packet count");
    M3uaMessage message = DecodeMessage(socket.SentPackets[0].Span);
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(message, out M3uaPayloadDataMessage? typed, out string? parseError),
        parseError ?? "sent DATA parse failed");
    AssertEqual((uint?)7, typed!.NetworkAppearance, "sent DATA Network Appearance");
    AssertEqual((uint?)100, typed.RoutingContext, "sent DATA Routing Context");
    AssertEqual((uint?)42, typed.CorrelationId, "sent DATA Correlation Id");
    AssertSequence([0xCA, 0xFE], typed.UserPayload, "sent DATA payload");
}

static void M3uaTransportSessionReceivesInboundData()
{
    Span<byte> buffer = stackalloc byte[96];
    Assert(
        M3uaMessageBuilder.BuildPayloadData(
            buffer,
            userPayload: [0x01, 0x02],
            opc: 1,
            dpc: 2,
            si: 3,
            ni: 2,
            mp: 0,
            sls: 7,
            networkAppearance: null,
            routingContext: 100,
            correlationId: null,
            out int written,
            out string? buildError),
        buildError ?? "inbound DATA build failed");

    FakeSctpSocket socket = new();
    socket.QueueReceive(buffer.Slice(0, written).ToArray());
    M3uaPayloadRouteTable routes = new();
    Assert(routes.TryAdd(new M3uaPayloadRoute("sccp", null, routingContext: 100, null, serviceIndicator: 3), out string? addError), addError ?? "route add failed");
    M3uaInboundProcessor inbound = new(new M3uaAspSession(M3uaAspState.Active), routes, requireActiveAspForPayload: true);
    using M3uaTransportSession session = new(socket, inboundProcessor: inbound, leaveOpen: true);

    M3uaInboundProcessingResult? result = session.ReceiveAsync().GetAwaiter().GetResult();
    Assert(result is not null, "received result should not be null");
    AssertEqual(M3uaTypedMessageKind.PayloadData, result!.TypedMessage.Kind, "received typed kind");
    AssertEqual("sccp", result.PayloadRoute!.Name, "received route");
}

static void M3uaTransportSessionDisposesOwnedSocket()
{
    FakeSctpSocket socket = new();
    M3uaTransportSession session = new(socket);
    session.Dispose();

    Assert(socket.Disposed, "owned socket should be disposed");
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

static void M3uaParsesSignallingCongestion()
{
    Span<byte> buffer = stackalloc byte[96];
    uint[] routingContexts = [0x00000055];
    M3uaAffectedPointCode[] affectedPointCodes = [new(mask: 0, pointCode: 0x00112233)];
    M3uaAffectedPointCode concernedDestination = new(mask: 0, pointCode: 0x0000AAAA);
    byte[] info = [0x73, 0x63, 0x6F, 0x6E];

    Assert(
        M3uaMessageBuilder.BuildSignallingCongestion(
            buffer,
            networkAppearance: 0x00000007,
            routingContexts,
            affectedPointCodes,
            concernedDestination,
            congestionLevel: 2,
            info,
            out int written,
            out string? buildError),
        buildError ?? "SCON build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, "SCON message class");
    AssertEqual((byte)M3uaSsnmMessageType.SignallingCongestion, message.MessageType, "SCON message type");
    Assert(
        M3uaTypedMessageParser.TryParseSignallingCongestion(message, out M3uaSignallingCongestionMessage? typed, out string? parseError),
        parseError ?? "SCON typed parse failed");

    AssertEqual((uint?)0x00000007, typed!.NetworkAppearance, "typed SCON Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x55], UInt32SpanToBytes(typed.RoutingContexts), "typed SCON Routing Contexts");
    AssertEqual(1, typed.AffectedPointCodes.Length, "typed SCON affected point-code count");
    AssertEqual((byte)0, typed.AffectedPointCodes[0].Mask, "typed SCON affected point-code mask");
    AssertEqual((uint)0x00112233, typed.AffectedPointCodes[0].PointCode, "typed SCON affected point-code value");
    AssertEqual((byte)0, typed.ConcernedDestination!.Value.Mask, "typed SCON concerned destination mask");
    AssertEqual((uint)0x0000AAAA, typed.ConcernedDestination.Value.PointCode, "typed SCON concerned destination value");
    AssertEqual((uint?)2, typed.CongestionLevel, "typed SCON congestion level");
    AssertSequence(info, typed.InfoString.Span, "typed SCON Info String");
}

static void M3uaRejectsSconWithoutAffectedPointCode()
{
    Span<byte> buffer = stackalloc byte[64];
    Assert(
        !M3uaMessageBuilder.BuildSignallingCongestion(
            buffer,
            networkAppearance: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<M3uaAffectedPointCode>.Empty,
            concernedDestination: null,
            congestionLevel: null,
            ReadOnlySpan<byte>.Empty,
            out _,
            out string? buildError),
        "SCON without Affected Point Code should be rejected");
    Assert(buildError?.Contains("Affected Point Code", StringComparison.Ordinal) == true, buildError ?? "missing SCON affected point-code error");
}

static void M3uaParsesRegistrationRequest()
{
    Span<byte> buffer = stackalloc byte[128];
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 0x0000002A,
            routingContext: 0x00000064,
            trafficModeType: M3uaTrafficModeType.Loadshare,
            destinationPointCodes: [new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)],
            networkAppearance: 0x00000007,
            serviceIndicators: [3, 5],
            originatingPointCodes: [new M3uaAffectedPointCode(mask: 0xFF, pointCode: 0x0000ABCD)])
    ];

    Assert(
        M3uaMessageBuilder.BuildRegistrationRequest(buffer, routingKeys, out int written, out string? buildError),
        buildError ?? "REG REQ build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.RoutingKeyManagement, message.MessageClass, "REG REQ message class");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.RegistrationRequest, message.MessageType, "REG REQ message type");
    Assert(
        M3uaTypedMessageParser.TryParseRegistrationRequest(message, out M3uaRegistrationRequestMessage? typed, out string? parseError),
        parseError ?? "REG REQ typed parse failed");

    AssertEqual(1, typed!.RoutingKeys.Length, "typed REG REQ Routing Key count");
    M3uaRoutingKey routingKey = typed.RoutingKeys[0];
    AssertEqual((uint)0x0000002A, routingKey.LocalRoutingKeyIdentifier, "typed Local-RK-Identifier");
    AssertEqual((uint?)0x00000064, routingKey.RoutingContext, "typed Routing Context");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Loadshare, routingKey.TrafficModeType, "typed Traffic Mode");
    AssertEqual((uint?)0x00000007, routingKey.NetworkAppearance, "typed Network Appearance");
    AssertEqual(1, routingKey.DestinationPointCodes.Length, "typed DPC count");
    AssertEqual((uint)0x00112233, routingKey.DestinationPointCodes[0].PointCode, "typed DPC value");
    AssertSequence([3, 5], routingKey.ServiceIndicators, "typed Service Indicators");
    AssertEqual(1, routingKey.OriginatingPointCodes.Length, "typed OPC count");
    AssertEqual((byte)0xFF, routingKey.OriginatingPointCodes[0].Mask, "typed OPC mask");
    AssertEqual((uint)0x0000ABCD, routingKey.OriginatingPointCodes[0].PointCode, "typed OPC value");
}

static void M3uaParsesRegistrationResponse()
{
    Span<byte> buffer = stackalloc byte[96];
    M3uaRegistrationResult[] results =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064),
        new(0x0000002B, M3uaRegistrationStatus.ErrorRoutingKeyAlreadyRegistered, 0)
    ];

    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, results, out int written, out string? buildError),
        buildError ?? "REG RSP build failed");

    M3uaMessage message = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.RoutingKeyManagement, message.MessageClass, "REG RSP message class");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.RegistrationResponse, message.MessageType, "REG RSP message type");
    Assert(
        M3uaTypedMessageParser.TryParseRegistrationResponse(message, out M3uaRegistrationResponseMessage? typed, out string? parseError),
        parseError ?? "REG RSP typed parse failed");

    AssertEqual(2, typed!.Results.Length, "typed REG RSP result count");
    AssertEqual((uint)0x0000002A, typed.Results[0].LocalRoutingKeyIdentifier, "first REG RSP Local-RK-Identifier");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, typed.Results[0].Status, "first REG RSP status");
    AssertEqual((uint)0x00000064, typed.Results[0].RoutingContext, "first REG RSP Routing Context");
    AssertEqual(M3uaRegistrationStatus.ErrorRoutingKeyAlreadyRegistered, typed.Results[1].Status, "second REG RSP status");
}

static void M3uaParsesDeregistrationMessages()
{
    Span<byte> buffer = stackalloc byte[96];
    uint[] routingContexts = [0x00000064, 0x00000065];

    Assert(
        M3uaMessageBuilder.BuildDeregistrationRequest(buffer, routingContexts, out int written, out string? requestBuildError),
        requestBuildError ?? "DEREG REQ build failed");

    M3uaMessage request = DecodeMessage(buffer.Slice(0, written));
    AssertEqual(M3uaMessageClass.RoutingKeyManagement, request.MessageClass, "DEREG REQ message class");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.DeregistrationRequest, request.MessageType, "DEREG REQ message type");
    Assert(
        M3uaTypedMessageParser.TryParseDeregistrationRequest(request, out M3uaDeregistrationRequestMessage? typedRequest, out string? requestParseError),
        requestParseError ?? "DEREG REQ typed parse failed");
    AssertSequence([0x00, 0x00, 0x00, 0x64, 0x00, 0x00, 0x00, 0x65], UInt32SpanToBytes(typedRequest!.RoutingContexts), "typed DEREG REQ Routing Contexts");

    M3uaDeregistrationResult[] results =
    [
        new(0x00000064, M3uaDeregistrationStatus.SuccessfullyDeregistered),
        new(0x00000065, M3uaDeregistrationStatus.ErrorNotRegistered)
    ];
    Assert(
        M3uaMessageBuilder.BuildDeregistrationResponse(buffer, results, out written, out string? responseBuildError),
        responseBuildError ?? "DEREG RSP build failed");

    M3uaMessage response = DecodeMessage(buffer.Slice(0, written));
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.DeregistrationResponse, response.MessageType, "DEREG RSP message type");
    Assert(
        M3uaTypedMessageParser.TryParseDeregistrationResponse(response, out M3uaDeregistrationResponseMessage? typedResponse, out string? responseParseError),
        responseParseError ?? "DEREG RSP typed parse failed");
    AssertEqual(2, typedResponse!.Results.Length, "typed DEREG RSP result count");
    AssertEqual(M3uaDeregistrationStatus.SuccessfullyDeregistered, typedResponse.Results[0].Status, "first DEREG RSP status");
    AssertEqual(M3uaDeregistrationStatus.ErrorNotRegistered, typedResponse.Results[1].Status, "second DEREG RSP status");
}

static void M3uaRejectsRoutingKeyWithoutDestinationPointCode()
{
    Span<byte> buffer = stackalloc byte[64];
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 1,
            routingContext: null,
            trafficModeType: null,
            destinationPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty,
            networkAppearance: null,
            serviceIndicators: ReadOnlySpan<byte>.Empty,
            originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
    ];

    Assert(
        !M3uaMessageBuilder.BuildRegistrationRequest(buffer, routingKeys, out _, out string? buildError),
        "Routing Key without Destination Point Code should be rejected");
    Assert(buildError?.Contains("Destination Point Code", StringComparison.Ordinal) == true, buildError ?? "missing Routing Key DPC error");
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

internal sealed class FakeSctpSocket : ISctpSocket
{
    private readonly Queue<byte[]> _receivePackets = new();

    public List<ReadOnlyMemory<byte>> SentPackets { get; } = new();

    public bool Disposed { get; private set; }

    public Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        SentPackets.Add(data.ToArray());
        return Task.CompletedTask;
    }

    public Task<int> ReceiveAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        if (_receivePackets.Count == 0)
        {
            return Task.FromResult(0);
        }

        byte[] packet = _receivePackets.Dequeue();
        packet.CopyTo(buffer);
        return Task.FromResult(packet.Length);
    }

    public void QueueReceive(byte[] packet)
    {
        _receivePackets.Enqueue(packet);
    }

    public void Dispose()
    {
        Disposed = true;
    }
}
