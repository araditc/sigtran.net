using System.Net;
using System.Net.Sockets;

using sigtran.net.Layers.MAP;
using sigtran.net.Layers.M3UA;
using sigtran.net.Layers.MTP3;
using sigtran.net.Layers.SCCP;
using sigtran.net.Layers.SCTP;
using sigtran.net.Layers.TCAP;
using sigtran.net.Core.Interfaces;
using sigtran.net.Core.Utilities;

Run("SIGTRAN trace formatter emits summaries and hex dumps", SigtranTraceFormatterEmitsSummariesAndHexDumps);
Run("SIGTRAN conformance registry stores vectors deterministically", SigtranConformanceRegistryStoresVectorsDeterministically);
Run("SIGTRAN built-in vectors include M3UA and MAP payloads", SigtranBuiltInVectorsIncludeM3uaAndMapPayloads);
Run("SIGTRAN simulator script emits trace summaries", SigtranSimulatorScriptEmitsTraceSummaries);
Run("MAP SMS simulator flow builds TCAP backed script", MapSmsSimulatorFlowBuildsTcapBackedScript);
Run("SIGTRAN local TCP sample describes M3UA transport", SigtranLocalTcpSampleDescribesM3uaTransport);
Run("SIGTRAN sample catalog exposes supported scenarios", SigtranSampleCatalogExposesSupportedScenarios);
Run("TCAP BER element encodes short and long lengths", TcapBerElementEncodesShortAndLongLengths);
Run("TCAP transaction identifiers use BER context tags", TcapTransactionIdentifiersUseBerContextTags);
Run("TCAP BER Invoke component round-trips", TcapBerInvokeComponentRoundTrips);
Run("TCAP BER outcome components round-trip", TcapBerOutcomeComponentsRoundTrip);
Run("TCAP transaction message wraps component portion", TcapTransactionMessageWrapsComponentPortion);
Run("TCAP dialogue portion carries application context", TcapDialoguePortionCarriesApplicationContext);
Run("TCAP dialogue controller tracks state and invoke timeouts", TcapDialogueControllerTracksStateAndInvokeTimeouts);
Run("TCAP allocators issue transaction and invoke identifiers", TcapAllocatorsIssueTransactionAndInvokeIdentifiers);
Run("TCAP session builder creates Begin and End messages", TcapSessionBuilderCreatesBeginAndEndMessages);
Run("TCAP phase 4 readiness reports foundation status", TcapPhase4ReadinessReportsFoundationStatus);
Run("MAP SMS operation catalog and parameter set encode BER", MapSmsOperationCatalogAndParameterSetEncodeBer);
Run("MAP SMS address primitives encode TBCD digits", MapSmsAddressPrimitivesEncodeTbcdDigits);
Run("MAP MO-ForwardSM model encodes required parameters", MapMoForwardSmModelEncodesRequiredParameters);
Run("MAP MT-ForwardSM model encodes required parameters", MapMtForwardSmModelEncodesRequiredParameters);
Run("MAP SendRoutingInfoForSM model encodes routing parameters", MapSendRoutingInfoForSmModelEncodesRoutingParameters);
Run("MAP ReportSM-DeliveryStatus model encodes delivery status", MapReportSmDeliveryStatusModelEncodesDeliveryStatus);
Run("MAP AlertServiceCentre model encodes alert parameters", MapAlertServiceCentreModelEncodesAlertParameters);
Run("MAP SMS error mapper and extension container encode values", MapSmsErrorMapperAndExtensionContainerEncodeValues);
Run("MAP SMS TCAP client builds Begin Invoke transactions", MapSmsTcapClientBuildsBeginInvokeTransactions);
Run("MAP SMS phase 5 readiness reports foundation status", MapSmsPhase5ReadinessReportsFoundationStatus);
Run("MTP3 routing label and SIO round-trip", Mtp3RoutingLabelAndSioRoundTrip);
Run("SCCP protocol constants expose connectionless classes", SccpProtocolConstantsExposeConnectionlessClasses);
Run("SCCP party address encodes SSN and global title", SccpPartyAddressEncodesSsnAndGlobalTitle);
Run("SCCP UDT codec uses variable parameter pointers", SccpUdtCodecUsesVariableParameterPointers);
Run("SCCP XUDT codec preserves hop counter", SccpXudtCodecPreservesHopCounter);
Run("SCCP segmentation parameter round-trips", SccpSegmentationParameterRoundTrips);
Run("SCCP XUDT carries segmentation optional parameter", SccpXudtCarriesSegmentationOptionalParameter);
Run("SCCP LUDT codec carries long user data", SccpLudtCodecCarriesLongUserData);
Run("SCCP UDTS codec carries return cause", SccpUdtsCodecCarriesReturnCause);
Run("SCCP route table resolves SSN and global title routes", SccpRouteTableResolvesSsnAndGlobalTitleRoutes);
Run("SCCP phase 3 readiness reports foundation status", SccpPhase3ReadinessReportsFoundationStatus);
Run("SCTP payload metadata stores stream and PPID values", SctpPayloadMetadataStoresStreamAndPpidValues);
Run("SCTP association events describe lifecycle state", SctpAssociationEventsDescribeLifecycleState);
Run("SCTP connection options validate endpoints and stream counts", SctpConnectionOptionsValidateEndpointsAndStreamCounts);
Run("SCTP PPID helpers recognize SIGTRAN payload identifiers", SctpPpidHelpersRecognizeSigtranPayloadIdentifiers);
Run("SCTP stream selection policies choose outbound streams", SctpStreamSelectionPoliciesChooseOutboundStreams);
Run("SCTP reconnect policies compute bounded delays", SctpReconnectPoliciesComputeBoundedDelays);
Run("SCTP transport health snapshots expose association details", SctpTransportHealthSnapshotsExposeAssociationDetails);
Run("TCP SCTP adapter exposes development metadata and health", TcpSctpAdapterExposesDevelopmentMetadataAndHealth);
Run("SCTP transport readiness reports foundation status", SctpTransportReadinessReportsFoundationStatus);
Run("M3UA Payload Data uses network byte order and RFC-style TLV length", M3uaPayloadDataUsesNetworkOrder);
Run("M3UA protocol exposes public metadata", M3uaProtocolExposesPublicMetadata);
Run("M3UA alpha readiness report describes release gate", M3uaAlphaReadinessReportDescribesReleaseGate);
Run("M3UA decoder returns the complete Protocol Data value", M3uaDecoderReturnsProtocolDataValue);
Run("M3UA parses Payload Data optional fields", M3uaParsesPayloadDataOptionalFields);
Run("M3UA rejects Payload Data without Protocol Data", M3uaRejectsPayloadDataWithoutProtocolData);
Run("M3UA reports supported typed message kinds", M3uaReportsSupportedTypedMessageKinds);
Run("M3UA dispatches known typed messages", M3uaDispatchesKnownTypedMessages);
Run("M3UA dispatcher rejects unsupported message types", M3uaDispatcherRejectsUnsupportedMessageTypes);
Run("M3UA route table resolves the most specific DATA route", M3uaRouteTableResolvesMostSpecificDataRoute);
Run("M3UA route table rejects ambiguous DATA routes", M3uaRouteTableRejectsAmbiguousDataRoutes);
Run("M3UA route table rejects duplicate selectors", M3uaRouteTableRejectsDuplicateSelectors);
Run("M3UA route table removes and clears routes", M3uaRouteTableRemovesAndClearsRoutes);
Run("M3UA route table snapshots and finds routes by name", M3uaRouteTableSnapshotsAndFindsRoutesByName);
Run("M3UA route table replaces routes by selector", M3uaRouteTableReplacesRoutesBySelector);
Run("M3UA route table adds or replaces routes by selector", M3uaRouteTableAddsOrReplacesRoutesBySelector);
Run("M3UA inbound processor updates ASP state and routes DATA", M3uaInboundProcessorUpdatesAspStateAndRoutesData);
Run("M3UA inbound processor can require active ASP for DATA", M3uaInboundProcessorCanRequireActiveAspForData);
Run("M3UA inbound processor rejects unrouted DATA when routes exist", M3uaInboundProcessorRejectsUnroutedDataWhenRoutesExist);
Run("M3UA outbound processor applies defaults to DATA", M3uaOutboundProcessorAppliesDefaultsToData);
Run("M3UA outbound processor can require active ASP for DATA", M3uaOutboundProcessorCanRequireActiveAspForData);
Run("M3UA outbound processor builds ASP Active with default Routing Context", M3uaOutboundProcessorBuildsAspActiveWithDefaultRoutingContext);
Run("M3UA transport session sends outbound DATA", M3uaTransportSessionSendsOutboundData);
Run("M3UA transport session receives inbound DATA", M3uaTransportSessionReceivesInboundData);
Run("M3UA transport session waits for typed messages", M3uaTransportSessionWaitsForTypedMessages);
Run("M3UA transport session waits for ASP transitions", M3uaTransportSessionWaitsForAspTransitions);
Run("M3UA transport session disposes owned socket", M3uaTransportSessionDisposesOwnedSocket);
Run("M3UA transport session tracks counters", M3uaTransportSessionTracksCounters);
Run("M3UA transport session resets counters", M3uaTransportSessionResetsCounters);
Run("M3UA transport session notifies ASP transport loss", M3uaTransportSessionNotifiesAspTransportLoss);
Run("M3UA diagnostics format hex and summaries", M3uaDiagnosticsFormatHexAndSummaries);
Run("M3UA ASP client completes startup handshake", M3uaAspClientCompletesStartupHandshake);
Run("M3UA ASP startup options validate and describe settings", M3uaAspStartupOptionsValidateAndDescribeSettings);
Run("M3UA ASP client resets before startup handshake", M3uaAspClientResetsBeforeStartupHandshake);
Run("M3UA ASP client fails when acknowledgement is missing", M3uaAspClientFailsWhenAcknowledgementIsMissing);
Run("M3UA transport session sends Heartbeat", M3uaTransportSessionSendsHeartbeat);
Run("M3UA transport session acknowledges inbound Heartbeat", M3uaTransportSessionAcknowledgesInboundHeartbeat);
Run("M3UA ASP client sends Heartbeat and waits for Ack", M3uaAspClientSendsHeartbeatAndWaitsForAck);
Run("M3UA ASP client deactivates and stops", M3uaAspClientDeactivatesAndStops);
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
Run("M3UA ASP session resets negotiated state", M3uaAspSessionResetsNegotiatedState);
Run("M3UA ASP session rejects acknowledgement messages in the wrong state", M3uaAspSessionRejectsWrongStateAcknowledgement);
Run("M3UA parses Management Error messages", M3uaParsesManagementError);
Run("M3UA parses Management Notify messages", M3uaParsesManagementNotify);
Run("M3UA transport session sends Management messages", M3uaTransportSessionSendsManagementMessages);
Run("M3UA rejects invalid Management Notify status information", M3uaRejectsInvalidManagementNotifyStatusInformation);
Run("M3UA parses SSNM Destination Unavailable messages", M3uaParsesSsnmDestinationUnavailable);
Run("M3UA rejects SSNM messages without Affected Point Code", M3uaRejectsSsnmWithoutAffectedPointCode);
Run("M3UA parses SSNM Destination User Part Unavailable messages", M3uaParsesDestinationUserPartUnavailable);
Run("M3UA rejects DUPU with non-zero affected point-code mask", M3uaRejectsDupuWithNonZeroMask);
Run("M3UA parses SSNM Signalling Congestion messages", M3uaParsesSignallingCongestion);
Run("M3UA transport session sends SSNM messages", M3uaTransportSessionSendsSsnmMessages);
Run("M3UA rejects SCON without Affected Point Code", M3uaRejectsSconWithoutAffectedPointCode);
Run("M3UA parses RKM Registration Request messages", M3uaParsesRegistrationRequest);
Run("M3UA parses RKM Registration Response messages", M3uaParsesRegistrationResponse);
Run("M3UA parses RKM Deregistration messages", M3uaParsesDeregistrationMessages);
Run("M3UA exposes RKM response convenience helpers", M3uaExposesRkmResponseConvenienceHelpers);
Run("M3UA RKM client registers and deregisters routing keys", M3uaRkmClientRegistersAndDeregistersRoutingKeys);
Run("M3UA RKM client can require successful responses", M3uaRkmClientRequiresSuccessfulResponses);
Run("M3UA rejects Routing Key without Destination Point Code", M3uaRejectsRoutingKeyWithoutDestinationPointCode);

static void SigtranTraceFormatterEmitsSummariesAndHexDumps()
{
    SigtranTraceFrame frame = new(
        new DateTimeOffset(2026, 6, 19, 10, 15, 0, TimeSpan.Zero),
        "M3UA",
        SigtranTraceDirection.Outbound,
        "asp:2905",
        "sg:2905",
        new byte[] { 0x01, 0x00, 0x01, 0x01, 0x00, 0x00, 0x00, 0x08 });

    string summary = SigtranTraceFormatter.FormatSummary(frame);
    Assert(summary.Contains("M3UA asp:2905 -> sg:2905 bytes=8", StringComparison.Ordinal), summary);

    string dump = SigtranTraceFormatter.FormatHexDump(frame);
    Assert(dump.Contains("0000: 01 00 01 01 00 00 00 08", StringComparison.Ordinal), dump);
}

static void SigtranConformanceRegistryStoresVectorsDeterministically()
{
    SigtranConformanceRegistry registry = new();
    registry.Add(new SigtranConformanceVector("m3ua/aspup", "M3UA", "ASP Up", new byte[] { 0x01 }, "internal"));
    registry.Add(new SigtranConformanceVector("map/mo", "MAP", "MO ForwardSM", new byte[] { 0x02 }, "internal"));

    Assert(registry.TryGet("m3ua/aspup", out SigtranConformanceVector? vector), "conformance vector should be found");
    AssertEqual("M3UA", vector!.Protocol, "conformance vector protocol");
    Assert(vector.Describe().Contains("bytes=1", StringComparison.Ordinal), vector.Describe());
    AssertEqual("m3ua/aspup", registry.Snapshot()[0].Id, "conformance registry deterministic order");
    AssertThrows<InvalidOperationException>(() => registry.Add(vector));
}

static void SigtranBuiltInVectorsIncludeM3uaAndMapPayloads()
{
    SigtranConformanceRegistry registry = SigtranBuiltInVectors.CreateRegistry();
    AssertEqual(2, registry.Snapshot().Count, "built-in vector count");
    Assert(registry.TryGet("m3ua/aspsm/asp-up-basic", out SigtranConformanceVector? aspUp), "M3UA ASP Up vector should exist");
    AssertEqual("M3UA", aspUp!.Protocol, "M3UA vector protocol");
    Assert(aspUp.Payload.Length >= 8, "M3UA vector should contain a full message header");
    Assert(registry.TryGet("map/sms/mo-forward-sm-basic", out SigtranConformanceVector? mo), "MAP MO vector should exist");
    AssertEqual("MAP", mo!.Protocol, "MAP vector protocol");
}

static void SigtranSimulatorScriptEmitsTraceSummaries()
{
    SigtranSimulatorEndpoint asp = new("asp", SigtranSimulatorRole.Asp);
    SigtranSimulatorEndpoint sg = new("sg", SigtranSimulatorRole.SignallingGateway);
    SigtranSimulatorScript script = new();

    script.Add(new SigtranSimulatorStep(asp, sg, "M3UA", new byte[] { 0x01, 0x00, 0x03, 0x01 }, "ASP Up"));

    IReadOnlyList<string> summaries = script.FormatTraceSummaries(new DateTimeOffset(2026, 6, 19, 10, 0, 0, TimeSpan.Zero));

    AssertEqual(1, script.Snapshot().Count, "simulator step count");
    Assert(summaries[0].Contains("M3UA asp -> sg bytes=4", StringComparison.Ordinal), summaries[0]);
    AssertEqual(SigtranSimulatorRole.Asp, asp.Role, "simulator ASP role");
}

static void MapSmsSimulatorFlowBuildsTcapBackedScript()
{
    MapSmsAddress subscriber = new(MapSmsAddressKind.Msisdn, "989121234567");
    MapSmsAddress imsi = new(MapSmsAddressKind.Imsi, "432101234567890");
    MapSmsAddress serviceCentre = new(MapSmsAddressKind.ServiceCentre, "989120000000");

    SigtranSimulatorScript script = new MapSmsSimulatorFlowBuilder()
        .AddSendRoutingInfoForShortMessage(subscriber, serviceCentre, gprsSupportIndicator: true)
        .AddMobileTerminatedForwardShortMessage(imsi, serviceCentre, [0x11, 0x22])
        .AddReportShortMessageDeliveryStatus(subscriber, serviceCentre, MapSmsDeliveryStatus.Delivered)
        .Build();

    IReadOnlyList<SigtranSimulatorStep> steps = script.Snapshot();
    IReadOnlyList<string> summaries = script.FormatTraceSummaries(DateTimeOffset.UnixEpoch);

    AssertEqual(3, steps.Count, "MAP SMS simulator step count");
    AssertEqual("TCAP/MAP", steps[0].Protocol, "MAP SMS simulator protocol");
    Assert(summaries[0].Contains("smsc -> hlr", StringComparison.Ordinal), summaries[0]);
    Assert(summaries[1].Contains("smsc -> msc", StringComparison.Ordinal), summaries[1]);
    Assert(summaries[2].Contains("hlr -> smsc", StringComparison.Ordinal), summaries[2]);
    Assert(steps[0].Payload.Length > 20, "MAP SMS simulator payload should contain encoded TCAP");
}

static void SigtranLocalTcpSampleDescribesM3uaTransport()
{
    SigtranLocalTcpScenario scenario = SigtranTransportSamples.CreateLocalM3uaAspToSg(2905);
    SctpConnectionOptions options = scenario.ToClientConnectionOptions();

    AssertEqual("local-m3ua-asp-to-sg", scenario.Name, "local TCP scenario name");
    AssertEqual("sg", scenario.Server.Name, "local TCP server name");
    AssertEqual((uint)SctpPayloadProtocolIdentifiers.M3ua, scenario.Metadata.PayloadProtocolIdentifier, "local TCP scenario PPID");
    AssertEqual("127.0.0.1", options.RemoteEndpoint.Host, "local TCP remote host");
    AssertEqual(2905, options.RemoteEndpoint.Port, "local TCP remote port");
    Assert(scenario.Describe().Contains("asp@127.0.0.1:2906 -> sg@127.0.0.1:2905", StringComparison.Ordinal), scenario.Describe());
    AssertThrows<ArgumentOutOfRangeException>(() => SigtranTransportSamples.CreateLocalM3uaAspToSg(65535));
}

static void SigtranSampleCatalogExposesSupportedScenarios()
{
    IReadOnlyList<SigtranSampleDescriptor> samples = SigtranSampleCatalog.GetSamples();

    AssertEqual(4, samples.Count, "sample catalog count");
    AssertEqual("m3ua-asp-to-sg", samples[0].Id, "sample catalog deterministic first id");
    Assert(SigtranSampleCatalog.TryGet("SCCP-MAP-SMS", out SigtranSampleDescriptor? mapSample), "SCCP/MAP sample should be discoverable");
    AssertEqual(SigtranSampleKind.SccpMapSms, mapSample!.Kind, "SCCP/MAP sample kind");
    Assert(mapSample.Describe().Contains("SCCP, TCAP, MAP", StringComparison.Ordinal), mapSample.Describe());
    Assert(!SigtranSampleCatalog.TryGet("missing", out _), "missing sample should not be found");
}

static void TcapBerElementEncodesShortAndLongLengths()
{
    Span<byte> shortBuffer = stackalloc byte[8];
    TcapBerTag invokeTag = new(TcapBerTagClass.ContextSpecific, constructed: true, number: 1);
    Assert(TcapBer.TryWriteElement(shortBuffer, invokeTag, [0x01, 0x02], out int shortWritten, out string? error), error ?? "BER short write failed");
    AssertEqual(4, shortWritten, "BER short written length");
    AssertSequence([0xA1, 0x02, 0x01, 0x02], shortBuffer.Slice(0, shortWritten), "BER short element bytes");
    Assert(TcapBer.TryReadElement(shortBuffer.Slice(0, shortWritten), out TcapBerElement shortElement, out error), error ?? "BER short read failed");
    AssertEqual(TcapBerTagClass.ContextSpecific, shortElement.Tag.TagClass, "BER short tag class");
    Assert(shortElement.Tag.Constructed, "BER short tag constructed");
    AssertEqual((byte)1, shortElement.Tag.Number, "BER short tag number");
    AssertSequence([0x01, 0x02], shortElement.Value.Span, "BER short value");

    byte[] longValue = Enumerable.Range(0, 130).Select(i => (byte)i).ToArray();
    byte[] longBuffer = new byte[140];
    Assert(TcapBer.TryWriteElement(longBuffer, new TcapBerTag(TcapBerTagClass.Universal, constructed: false, number: 4), longValue, out int longWritten, out error), error ?? "BER long write failed");
    AssertEqual((byte)0x04, longBuffer[0], "BER octet string tag");
    AssertEqual((byte)0x81, longBuffer[1], "BER long length marker");
    AssertEqual((byte)130, longBuffer[2], "BER long length value");
    Assert(TcapBer.TryReadElement(longBuffer.AsSpan(0, longWritten), out TcapBerElement longElement, out error), error ?? "BER long read failed");
    AssertEqual(130, longElement.Value.Length, "BER long value length");
}

static void TcapTransactionIdentifiersUseBerContextTags()
{
    TcapTransactionId transactionId = TcapTransactionId.FromUInt32(0x010203);
    AssertEqual(3, transactionId.Length, "TCAP transaction id length");
    AssertEqual("010203", transactionId.ToString(), "TCAP transaction id hex");

    Span<byte> buffer = stackalloc byte[8];
    Assert(
        TcapBer.TryWriteElement(buffer, TcapTransactionTags.TransactionId(originating: true), transactionId.ToArray(), out int written, out string? error),
        error ?? "TCAP transaction id write failed");
    AssertSequence([0x88, 0x03, 0x01, 0x02, 0x03], buffer.Slice(0, written), "TCAP originating transaction id TLV");
    Assert(TcapBer.TryReadElement(buffer.Slice(0, written), out TcapBerElement element, out error), error ?? "TCAP transaction id read failed");
    AssertEqual(TcapBerTagClass.ContextSpecific, element.Tag.TagClass, "TCAP transaction id tag class");
    AssertEqual((byte)TcapTransactionTags.OriginatingTransactionId, element.Tag.Number, "TCAP transaction id tag number");

    TcapBerTag beginTag = TcapTransactionTags.Package(TcapPackageType.Begin);
    AssertEqual((byte)0x62, beginTag.Encode(), "TCAP Begin package tag");
    AssertThrows<ArgumentException>(() => new TcapTransactionId(ReadOnlySpan<byte>.Empty));
}

static void TcapBerInvokeComponentRoundTrips()
{
    TcapBerInvokeComponent invoke = new(
        invokeId: 7,
        TcapOperationCode.MoForwardShortMessage,
        new byte[] { 0xAA, 0xBB },
        linkedInvokeId: 3);

    byte[] encoded = invoke.Encode();
    AssertSequence([0xA1, 0x0D, 0x02, 0x01, 0x07, 0x02, 0x01, 0x03, 0x02, 0x01, 0x01, 0x04, 0x02, 0xAA, 0xBB], encoded, "TCAP BER Invoke bytes");
    Assert(TcapBerInvokeComponent.TryDecode(encoded, out TcapBerInvokeComponent? decoded, out string? error), error ?? "TCAP BER Invoke decode failed");
    AssertEqual((byte)7, decoded!.InvokeId, "TCAP decoded Invoke ID");
    AssertEqual((byte)3, decoded.LinkedInvokeId, "TCAP decoded linked Invoke ID");
    AssertEqual(TcapOperationCode.MoForwardShortMessage, decoded.OperationCode, "TCAP decoded operation code");
    AssertSequence([0xAA, 0xBB], decoded.Parameters.Span, "TCAP decoded Invoke parameters");
}

static void TcapBerOutcomeComponentsRoundTrip()
{
    TcapBerReturnResultComponent result = new(7, TcapOperationCode.MoForwardShortMessage, new byte[] { 0x10, 0x20 });
    byte[] resultBytes = result.Encode();
    AssertSequence([0xA2, 0x0A, 0x02, 0x01, 0x07, 0x02, 0x01, 0x01, 0x04, 0x02, 0x10, 0x20], resultBytes, "TCAP ReturnResult bytes");
    Assert(TcapBerReturnResultComponent.TryDecode(resultBytes, out TcapBerReturnResultComponent? decodedResult, out string? error), error ?? "TCAP ReturnResult decode failed");
    AssertEqual((byte)7, decodedResult!.InvokeId, "TCAP ReturnResult invoke id");
    AssertEqual(TcapOperationCode.MoForwardShortMessage, decodedResult.OperationCode, "TCAP ReturnResult operation");
    AssertSequence([0x10, 0x20], decodedResult.Parameters.Span, "TCAP ReturnResult parameters");

    TcapBerReturnErrorComponent returnError = new(7, TcapReturnErrorCode.SystemFailure, new byte[] { 0x01 });
    byte[] errorBytes = returnError.Encode();
    Assert(TcapBerReturnErrorComponent.TryDecode(errorBytes, out TcapBerReturnErrorComponent? decodedError, out error), error ?? "TCAP ReturnError decode failed");
    AssertEqual(TcapReturnErrorCode.SystemFailure, decodedError!.ErrorCode, "TCAP ReturnError code");
    AssertSequence([0x01], decodedError.Parameters.Span, "TCAP ReturnError parameters");

    TcapBerRejectComponent reject = new(7, TcapRejectProblemCode.DuplicateInvokeId);
    byte[] rejectBytes = reject.Encode();
    AssertSequence([0xA4, 0x06, 0x02, 0x01, 0x07, 0x02, 0x01, 0x02], rejectBytes, "TCAP Reject bytes");
    Assert(TcapBerRejectComponent.TryDecode(rejectBytes, out TcapBerRejectComponent? decodedReject, out error), error ?? "TCAP Reject decode failed");
    AssertEqual(TcapRejectProblemCode.DuplicateInvokeId, decodedReject!.ProblemCode, "TCAP Reject problem code");
}

static void TcapTransactionMessageWrapsComponentPortion()
{
    TcapBerInvokeComponent invoke = new(1, TcapOperationCode.MtForwardShortMessage, new byte[] { 0xAA });
    TcapTransactionMessage begin = new(
        TcapPackageType.Begin,
        originatingTransactionId: TcapTransactionId.FromUInt32(0x0102),
        componentPortion: invoke.Encode());

    byte[] encoded = begin.Encode();
    AssertEqual((byte)0x62, encoded[0], "TCAP Begin package tag");
    Assert(TcapTransactionMessage.TryDecode(encoded, out TcapTransactionMessage? decoded, out string? error), error ?? "TCAP transaction decode failed");
    AssertEqual(TcapPackageType.Begin, decoded!.PackageType, "TCAP decoded package type");
    AssertEqual("0102", decoded.OriginatingTransactionId?.ToString(), "TCAP decoded originating id");
    Assert(!decoded.ComponentPortion.IsEmpty, "TCAP decoded component portion should be present");
    Assert(TcapBerInvokeComponent.TryDecode(decoded.ComponentPortion.Span, out TcapBerInvokeComponent? decodedInvoke, out error), error ?? "TCAP decoded component portion failed");
    AssertEqual(TcapOperationCode.MtForwardShortMessage, decodedInvoke!.OperationCode, "TCAP decoded nested invoke operation");
}

static void TcapDialoguePortionCarriesApplicationContext()
{
    TcapObjectIdentifier oid = new(0, 0, 17, 773, 1, 1, 1);
    TcapDialoguePortion dialogue = new(oid, new byte[] { 0x55, 0x66 });
    byte[] encoded = dialogue.Encode();
    AssertEqual((byte)0x06, encoded[0], "TCAP dialogue OID tag");
    Assert(TcapDialoguePortion.TryDecode(encoded, out TcapDialoguePortion? decoded, out string? error), error ?? "TCAP dialogue decode failed");
    AssertEqual("0.0.17.773.1.1.1", decoded!.ApplicationContext.ToString(), "TCAP decoded application context");
    AssertSequence([0x55, 0x66], decoded.UserInformation.Span, "TCAP decoded dialogue user information");

    TcapTransactionMessage begin = new(
        TcapPackageType.Begin,
        originatingTransactionId: TcapTransactionId.FromUInt32(1),
        dialoguePortion: encoded);
    Assert(TcapTransactionMessage.TryDecode(begin.Encode(), out TcapTransactionMessage? decodedBegin, out error), error ?? "TCAP transaction with dialogue decode failed");
    Assert(TcapDialoguePortion.TryDecode(decodedBegin!.DialoguePortion.Span, out TcapDialoguePortion? nested, out error), error ?? "TCAP nested dialogue decode failed");
    AssertEqual(oid.ToString(), nested!.ApplicationContext.ToString(), "TCAP nested dialogue OID");
}

static void TcapDialogueControllerTracksStateAndInvokeTimeouts()
{
    DateTimeOffset sentAt = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
    TcapDialogueController dialogue = new(
        dialogueId: 100,
        new TcapInvokeTimeoutPolicy(TimeSpan.FromSeconds(5), maxPendingInvokes: 1));

    AssertEqual(TcapDialoguePhase.Idle, dialogue.Phase, "TCAP initial dialogue phase");
    dialogue.Begin();
    AssertEqual(TcapDialoguePhase.Open, dialogue.Phase, "TCAP begin dialogue phase");
    dialogue.RegisterInvoke(1, sentAt);
    AssertEqual(1, dialogue.PendingInvokeCount, "TCAP pending invoke count");
    Assert(!dialogue.IsInvokeTimedOut(1, sentAt.AddSeconds(4)), "TCAP invoke should not be timed out yet");
    Assert(dialogue.IsInvokeTimedOut(1, sentAt.AddSeconds(5)), "TCAP invoke should be timed out");
    AssertThrows<InvalidOperationException>(() => dialogue.RegisterInvoke(1, sentAt));
    Assert(dialogue.CompleteInvoke(1), "TCAP pending invoke should complete");
    dialogue.Continue();
    AssertEqual(TcapDialoguePhase.Continuing, dialogue.Phase, "TCAP continue dialogue phase");
    dialogue.End();
    AssertEqual(TcapDialoguePhase.Ended, dialogue.Phase, "TCAP ended dialogue phase");
}

static void TcapAllocatorsIssueTransactionAndInvokeIdentifiers()
{
    TcapTransactionIdAllocator transactionIds = new(firstValue: 0xFE, maxValue: 0xFF);
    AssertEqual("FE", transactionIds.Allocate().ToString(), "TCAP transaction allocator first id");
    AssertEqual("FF", transactionIds.Allocate().ToString(), "TCAP transaction allocator second id");
    AssertEqual("01", transactionIds.Allocate().ToString(), "TCAP transaction allocator wraps");

    TcapInvokeRegistry invokes = new();
    byte first = invokes.Allocate();
    AssertEqual((byte)1, first, "TCAP invoke allocator first id");
    invokes.Register(9);
    AssertEqual(2, invokes.Count, "TCAP invoke registry count");
    AssertThrows<InvalidOperationException>(() => invokes.Register(9));
    Assert(invokes.Complete(first), "TCAP invoke registry completes allocated id");
    AssertEqual(1, invokes.Count, "TCAP invoke registry count after complete");
}

static void TcapSessionBuilderCreatesBeginAndEndMessages()
{
    TcapSessionBuilder builder = new(new TcapTransactionIdAllocator(firstValue: 3), new TcapInvokeRegistry());
    TcapBuiltInvoke built = builder.BeginInvoke(new TcapObjectIdentifier(0, 0, 17, 773, 1, 1, 1), TcapOperationCode.MoForwardShortMessage, new byte[] { 0xCA });
    AssertEqual("03", built.OriginatingTransactionId.ToString(), "TCAP built transaction id");
    AssertEqual((byte)1, built.InvokeId, "TCAP built invoke id");
    Assert(TcapTransactionMessage.TryDecode(built.EncodedMessage, out TcapTransactionMessage? decodedBegin, out string? error), error ?? "TCAP built Begin decode failed");
    AssertEqual(TcapPackageType.Begin, decodedBegin!.PackageType, "TCAP built Begin package");

    byte[] end = builder.EndResult(built.OriginatingTransactionId, built.InvokeId, TcapOperationCode.MoForwardShortMessage, new byte[] { 0x01 });
    Assert(TcapTransactionMessage.TryDecode(end, out TcapTransactionMessage? decodedEnd, out error), error ?? "TCAP built End decode failed");
    AssertEqual(TcapPackageType.End, decodedEnd!.PackageType, "TCAP built End package");
    AssertEqual("03", decodedEnd.DestinationTransactionId?.ToString(), "TCAP built End destination id");
}

static void TcapPhase4ReadinessReportsFoundationStatus()
{
    AssertEqual("TCAP BER foundation", TcapPhase4Readiness.ReleaseLabel, "TCAP readiness label");
    AssertEqual(7, TcapPhase4Readiness.RequiredFoundationCapabilityCount, "TCAP readiness capability count");
    AssertEqual(7, TcapPhase4Readiness.GetFoundationCapabilities().Count, "TCAP readiness capability name count");
    Assert(
        TcapPhase4Readiness.ProductionGateDescription.Contains("interoperability", StringComparison.Ordinal),
        TcapPhase4Readiness.ProductionGateDescription);

    TcapPhase4ReadinessReport report = TcapPhase4Readiness.GetReport();
    Assert(report.FoundationReady, "TCAP foundation should be ready");
    Assert(!report.IsProductionReady, "TCAP should not claim production readiness without interop vectors");
    AssertEqual(7, report.FoundationCapabilityCount, "TCAP completed foundation capabilities");
    Assert(report.Describe().Contains("foundationCapabilities=7/7", StringComparison.Ordinal), report.Describe());
}

static void MapSmsOperationCatalogAndParameterSetEncodeBer()
{
    Assert(MapSmsOperationCatalog.TryGet(MapSmsOperationCode.MoForwardShortMessage, out MapSmsOperationMetadata metadata), "MAP MO operation metadata should exist");
    AssertEqual("mo-ForwardSM", metadata.Name, "MAP MO operation name");
    AssertEqual(5, MapSmsOperationCatalog.GetSupportedOperations().Count, "MAP supported SMS operation count");

    MapSmsParameterSet parameters = new();
    parameters.Add(0, [0x01, 0x02]);
    parameters.Add(1, [0xAA]);
    byte[] encoded = parameters.Encode();
    AssertSequence([0x80, 0x02, 0x01, 0x02, 0x81, 0x01, 0xAA], encoded, "MAP SMS parameter set BER");

    Assert(MapSmsParameterSet.TryDecode(encoded, out MapSmsParameterSet? decoded, out string? error), error ?? "MAP parameter decode failed");
    AssertEqual(2, decoded!.Snapshot().Count, "MAP decoded parameter count");
    AssertSequence([0xAA], decoded.Snapshot()[1].Value.Span, "MAP decoded second parameter");
}

static void MapSmsAddressPrimitivesEncodeTbcdDigits()
{
    byte[] tbcd = MapSmsAddress.EncodeTbcd("44123456789");
    AssertSequence([0x44, 0x21, 0x43, 0x65, 0x87, 0xF9], tbcd, "MAP TBCD bytes");
    AssertEqual("44123456789", MapSmsAddress.DecodeTbcd(tbcd, oddDigitCount: true), "MAP decoded TBCD digits");

    MapSmsAddress msisdn = new(MapSmsAddressKind.Msisdn, "+44123456789");
    byte[] encoded = msisdn.Encode();
    AssertSequence([0x01, 0x04, 0x01, 0x44, 0x21, 0x43, 0x65, 0x87, 0xF9], encoded, "MAP MSISDN address bytes");
    MapSmsAddress decoded = MapSmsAddress.Decode(encoded, oddDigitCount: true);
    AssertEqual(MapSmsAddressKind.Msisdn, decoded.Kind, "MAP decoded address kind");
    AssertEqual("44123456789", decoded.Digits, "MAP decoded address digits");
    AssertThrows<ArgumentException>(() => new MapSmsAddress(MapSmsAddressKind.Imsi, "12A"));
}

static void MapMoForwardSmModelEncodesRequiredParameters()
{
    MapMoForwardShortMessage mo = new(
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new byte[] { 0x11, 0x22 });

    byte[] encoded = mo.Encode();
    Assert(encoded[0] == 0x80 && encoded.Contains((byte)0x82), "MAP MO encoded parameter tags should be present");
    Assert(MapMoForwardShortMessage.TryDecode(encoded, out MapMoForwardShortMessage? decoded, out string? error), error ?? "MAP MO decode failed");
    AssertEqual(MapSmsAddressKind.ServiceCentre, decoded!.SmRpDa.Kind, "MAP MO decoded DA kind");
    AssertEqual("989121234567", decoded.SmRpOa.Digits, "MAP MO decoded OA digits");
    AssertSequence([0x11, 0x22], decoded.SmRpUi.Span, "MAP MO decoded user information");

    byte[] helper = MapSmsOperations.CreateMoForwardSm(decoded.SmRpDa, decoded.SmRpOa, decoded.SmRpUi.Span);
    AssertSequence(encoded, helper, "MAP MO helper encoding");
}

static void MapMtForwardSmModelEncodesRequiredParameters()
{
    MapMtForwardShortMessage mt = new(
        new MapSmsAddress(MapSmsAddressKind.Imsi, "432109876543210"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        new byte[] { 0x21, 0x43 });

    byte[] encoded = mt.Encode();
    Assert(MapMtForwardShortMessage.TryDecode(encoded, out MapMtForwardShortMessage? decoded, out string? error), error ?? "MAP MT decode failed");
    AssertEqual(MapSmsAddressKind.Imsi, decoded!.SmRpDa.Kind, "MAP MT decoded DA kind");
    AssertEqual(MapSmsAddressKind.ServiceCentre, decoded.SmRpOa.Kind, "MAP MT decoded OA kind");
    AssertSequence([0x21, 0x43], decoded.SmRpUi.Span, "MAP MT decoded user information");

    byte[] helper = MapSmsOperations.CreateMtForwardSm(decoded.SmRpDa, decoded.SmRpOa, decoded.SmRpUi.Span);
    AssertSequence(encoded, helper, "MAP MT helper encoding");
}

static void MapSendRoutingInfoForSmModelEncodesRoutingParameters()
{
    MapSendRoutingInfoForShortMessage sri = new(
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        gprsSupportIndicator: true);

    byte[] encoded = sri.Encode();
    Assert(MapSendRoutingInfoForShortMessage.TryDecode(encoded, out MapSendRoutingInfoForShortMessage? decoded, out string? error), error ?? "MAP SRI-SM decode failed");
    AssertEqual("989121234567", decoded!.Msisdn.Digits, "MAP SRI decoded MSISDN");
    AssertEqual(MapSmsAddressKind.ServiceCentre, decoded.ServiceCentreAddress.Kind, "MAP SRI decoded SC address kind");
    Assert(decoded.GprsSupportIndicator, "MAP SRI decoded GPRS indicator");
}

static void MapReportSmDeliveryStatusModelEncodesDeliveryStatus()
{
    MapReportShortMessageDeliveryStatus report = new(
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        MapSmsDeliveryStatus.MemoryCapacityExceeded);

    byte[] encoded = report.Encode();
    Assert(MapReportShortMessageDeliveryStatus.TryDecode(encoded, out MapReportShortMessageDeliveryStatus? decoded, out string? error), error ?? "MAP ReportSM decode failed");
    AssertEqual("989121234567", decoded!.Msisdn.Digits, "MAP ReportSM decoded MSISDN");
    AssertEqual(MapSmsDeliveryStatus.MemoryCapacityExceeded, decoded.DeliveryStatus, "MAP ReportSM decoded status");
}

static void MapAlertServiceCentreModelEncodesAlertParameters()
{
    MapAlertServiceCentre alert = new(
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"));

    byte[] encoded = alert.Encode();
    Assert(MapAlertServiceCentre.TryDecode(encoded, out MapAlertServiceCentre? decoded, out string? error), error ?? "MAP AlertSC decode failed");
    AssertEqual("989121234567", decoded!.Msisdn.Digits, "MAP AlertSC decoded MSISDN");
    AssertEqual("441234", decoded.ServiceCentreAddress.Digits, "MAP AlertSC decoded SC address");
}

static void MapSmsErrorMapperAndExtensionContainerEncodeValues()
{
    AssertEqual(
        MapSmsDeliveryStatus.AbsentSubscriber,
        MapSmsErrorMapper.ToDeliveryStatus(MapSmsErrorCode.AbsentSubscriberForShortMessage),
        "MAP absent subscriber error mapping");

    MapSmsExtensionContainer extensions = new();
    extensions.Add(5, [0xCA, 0xFE]);
    byte[] encoded = extensions.Encode();
    AssertSequence([0x85, 0x02, 0xCA, 0xFE], encoded, "MAP extension container bytes");
    Assert(MapSmsExtensionContainer.TryDecode(encoded, out MapSmsExtensionContainer? decoded, out string? error), error ?? "MAP extension decode failed");
    AssertEqual(1, decoded!.Snapshot().Count, "MAP extension count");
}

static void MapSmsTcapClientBuildsBeginInvokeTransactions()
{
    MapSmsTcapClient client = new(new TcapSessionBuilder(new TcapTransactionIdAllocator(firstValue: 7), new TcapInvokeRegistry()));
    MapMoForwardShortMessage mo = new(
        new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234"),
        new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567"),
        new byte[] { 0x11 });

    TcapBuiltInvoke built = client.BeginMoForwardShortMessage(mo);
    AssertEqual("07", built.OriginatingTransactionId.ToString(), "MAP TCAP built transaction id");
    Assert(TcapTransactionMessage.TryDecode(built.EncodedMessage, out TcapTransactionMessage? transaction, out string? error), error ?? "MAP TCAP transaction decode failed");
    Assert(TcapBerInvokeComponent.TryDecode(transaction!.ComponentPortion.Span, out TcapBerInvokeComponent? invoke, out error), error ?? "MAP TCAP invoke decode failed");
    AssertEqual((TcapOperationCode)MapSmsOperationCode.MoForwardShortMessage, invoke!.OperationCode, "MAP TCAP operation code");
    Assert(MapMoForwardShortMessage.TryDecode(invoke.Parameters.Span, out MapMoForwardShortMessage? decodedMo, out error), error ?? "MAP TCAP MO params decode failed");
    AssertEqual("989121234567", decodedMo!.SmRpOa.Digits, "MAP TCAP decoded MO originator");
}

static void MapSmsPhase5ReadinessReportsFoundationStatus()
{
    AssertEqual("MAP SMS profile foundation", MapSmsPhase5Readiness.ReleaseLabel, "MAP readiness label");
    AssertEqual(8, MapSmsPhase5Readiness.RequiredFoundationCapabilityCount, "MAP readiness capability count");
    Assert(
        MapSmsPhase5Readiness.ProductionGateDescription.Contains("interoperability", StringComparison.Ordinal),
        MapSmsPhase5Readiness.ProductionGateDescription);

    MapSmsPhase5ReadinessReport report = MapSmsPhase5Readiness.GetReport();
    Assert(report.FoundationReady, "MAP SMS foundation should be ready");
    Assert(!report.IsProductionReady, "MAP SMS should not claim production readiness without interop vectors");
    AssertEqual(8, report.FoundationCapabilityCount, "MAP completed foundation capabilities");
    Assert(report.Describe().Contains("foundationCapabilities=8/8", StringComparison.Ordinal), report.Describe());
}

static void Mtp3RoutingLabelAndSioRoundTrip()
{
    Mtp3ServiceInformationOctet sio = new(Mtp3ServiceIndicator.Sccp, networkIndicator: 2, messagePriority: 1);
    byte encodedSio = sio.Encode();
    AssertEqual((byte)0x93, encodedSio, "MTP3 SIO encoding");
    Mtp3ServiceInformationOctet decodedSio = Mtp3ServiceInformationOctet.Decode(encodedSio);
    AssertEqual(Mtp3ServiceIndicator.Sccp, decodedSio.ServiceIndicator, "MTP3 decoded SI");
    AssertEqual((byte)2, decodedSio.NetworkIndicator, "MTP3 decoded NI");
    AssertEqual((byte)1, decodedSio.MessagePriority, "MTP3 decoded MP");

    Mtp3RoutingLabel label = new(destinationPointCode: 0x1234, originatingPointCode: 0x2345, signallingLinkSelection: 0x0A);
    Span<byte> bytes = stackalloc byte[4];
    label.Encode(bytes);
    AssertSequence([0x34, 0x52, 0xD1, 0xA8], bytes, "MTP3 routing label bytes");
    Mtp3RoutingLabel decoded = Mtp3RoutingLabel.Decode(bytes);
    AssertEqual(0x1234U, decoded.DestinationPointCode, "MTP3 decoded DPC");
    AssertEqual(0x2345U, decoded.OriginatingPointCode, "MTP3 decoded OPC");
    AssertEqual((byte)0x0A, decoded.SignallingLinkSelection, "MTP3 decoded SLS");
}

static void SccpProtocolConstantsExposeConnectionlessClasses()
{
    AssertEqual((byte)0x09, (byte)SccpMessageType.Unitdata, "SCCP UDT message type");
    AssertEqual((byte)0x11, (byte)SccpMessageType.ExtendedUnitdata, "SCCP XUDT message type");
    AssertEqual((byte)0x13, (byte)SccpMessageType.LongUnitdata, "SCCP LUDT message type");

    SccpProtocolClass protocolClass = new(SccpConnectionlessClass.Class1, returnMessageOnError: true);
    AssertEqual((byte)0x81, protocolClass.Encode(), "SCCP protocol class encoding");
    SccpProtocolClass decoded = SccpProtocolClass.Decode(0x81);
    AssertEqual(SccpConnectionlessClass.Class1, decoded.ConnectionlessClass, "SCCP decoded protocol class");
    Assert(decoded.ReturnMessageOnError, "SCCP return-on-error flag");
}

static void SccpPartyAddressEncodesSsnAndGlobalTitle()
{
    SccpPartyAddress address = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        pointCode: 0x1234,
        globalTitle: new SccpGlobalTitle("44123456789", translationType: 0, numberingPlan: 1, natureOfAddress: 4));

    byte[] encoded = address.Encode();
    AssertEqual((byte)0x13, encoded[0], "SCCP party address indicator");
    AssertSequence([0x13, 0x34, 0x12, 0x06, 0x00, 0x11, 0x04, 0x44, 0x21, 0x43, 0x65, 0x87, 0xF9], encoded, "SCCP party address bytes");

    Assert(SccpPartyAddress.TryDecode(encoded, out SccpPartyAddress? decoded, out string? error), error ?? "SCCP party address decode failed");
    AssertEqual(SccpRoutingIndicator.RouteOnGlobalTitle, decoded!.RoutingIndicator, "SCCP decoded routing indicator");
    AssertEqual(SubsystemNumber.MAP, decoded.SubsystemNumber, "SCCP decoded SSN");
    AssertEqual((ushort)0x1234, decoded.PointCode, "SCCP decoded point code");
    AssertEqual("44123456789", decoded.GlobalTitle?.Digits, "SCCP decoded GT digits");
}

static void SccpUdtCodecUsesVariableParameterPointers()
{
    SccpPartyAddress called = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    SccpPartyAddress calling = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        subsystemNumber: SubsystemNumber.MSC,
        pointCode: 0x0102);

    SccpUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class0),
        called,
        calling,
        new byte[] { 0xCA, 0xFE });

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.Unitdata, encoded[0], "SCCP UDT message type");
    AssertEqual((byte)3, encoded[2], "SCCP called pointer");
    AssertEqual((byte)14, encoded[3], "SCCP calling pointer");
    AssertEqual((byte)18, encoded[4], "SCCP data pointer");

    Assert(SccpUnitdataMessage.TryDecode(encoded, out SccpUnitdataMessage? decoded, out string? error), error ?? "SCCP UDT decode failed");
    AssertEqual(SccpConnectionlessClass.Class0, decoded!.ProtocolClass.ConnectionlessClass, "SCCP decoded UDT protocol class");
    AssertEqual("44123456789", decoded.CalledParty.GlobalTitle?.Digits, "SCCP decoded UDT called GT");
    AssertEqual(SubsystemNumber.MSC, decoded.CallingParty.SubsystemNumber, "SCCP decoded UDT calling SSN");
    AssertSequence([0xCA, 0xFE], decoded.UserData.Span, "SCCP decoded UDT data");
}

static void SccpXudtCodecPreservesHopCounter()
{
    SccpPartyAddress called = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    SccpPartyAddress calling = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        subsystemNumber: SubsystemNumber.MSC,
        pointCode: 0x0102);

    SccpExtendedUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class1, returnMessageOnError: true),
        hopCounter: 12,
        called,
        calling,
        new byte[] { 0x01, 0x02, 0x03 });

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.ExtendedUnitdata, encoded[0], "SCCP XUDT message type");
    AssertEqual((byte)0x81, encoded[1], "SCCP XUDT protocol class");
    AssertEqual((byte)12, encoded[2], "SCCP XUDT hop counter");
    AssertEqual((byte)4, encoded[3], "SCCP XUDT called pointer");
    AssertEqual((byte)15, encoded[4], "SCCP XUDT calling pointer");
    AssertEqual((byte)19, encoded[5], "SCCP XUDT data pointer");
    AssertEqual((byte)0, encoded[6], "SCCP XUDT optional pointer");

    Assert(SccpExtendedUnitdataMessage.TryDecode(encoded, out SccpExtendedUnitdataMessage? decoded, out string? error), error ?? "SCCP XUDT decode failed");
    AssertEqual((byte)12, decoded!.HopCounter, "SCCP decoded XUDT hop counter");
    Assert(decoded.ProtocolClass.ReturnMessageOnError, "SCCP decoded XUDT return-on-error");
    AssertSequence([0x01, 0x02, 0x03], decoded.UserData.Span, "SCCP decoded XUDT data");
}

static void SccpSegmentationParameterRoundTrips()
{
    SccpSegmentationParameter segmentation = new(localReference: 0x00A1B2C3, remainingSegments: 3, firstSegment: true);
    Span<byte> encoded = stackalloc byte[SccpSegmentationParameter.EncodedLength];
    segmentation.Encode(encoded);
    AssertSequence([0x83, 0xA1, 0xB2, 0xC3], encoded, "SCCP segmentation bytes");

    SccpSegmentationParameter decoded = SccpSegmentationParameter.Decode(encoded);
    AssertEqual(0x00A1B2C3U, decoded.LocalReference, "SCCP decoded segmentation local reference");
    AssertEqual((byte)3, decoded.RemainingSegments, "SCCP decoded remaining segments");
    Assert(decoded.FirstSegment, "SCCP decoded first segment flag");
    Assert(decoded.Describe().Contains("remaining=3", StringComparison.Ordinal), decoded.Describe());
    AssertThrows<ArgumentOutOfRangeException>(() => new SccpSegmentationParameter(0x01000000, 0, false));
}

static void SccpXudtCarriesSegmentationOptionalParameter()
{
    SccpPartyAddress called = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    SccpPartyAddress calling = new(
        SccpRoutingIndicator.RouteOnSubsystemNumber,
        subsystemNumber: SubsystemNumber.MSC,
        pointCode: 0x0102);
    SccpSegmentationParameter segmentation = new(0x00010203, remainingSegments: 1, firstSegment: false);

    SccpExtendedUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class1),
        hopCounter: 10,
        called,
        calling,
        new byte[] { 0xAA, 0xBB, 0xCC },
        segmentation);

    byte[] encoded = message.Encode();
    AssertEqual((byte)22, encoded[6], "SCCP XUDT optional pointer");
    AssertEqual((byte)SccpOptionalParameterName.Segmentation, encoded[^7], "SCCP XUDT segmentation parameter name");
    AssertEqual((byte)SccpSegmentationParameter.EncodedLength, encoded[^6], "SCCP XUDT segmentation parameter length");
    AssertEqual((byte)SccpOptionalParameterName.EndOfOptionalParameters, encoded[^1], "SCCP XUDT optional end marker");

    Assert(SccpExtendedUnitdataMessage.TryDecode(encoded, out SccpExtendedUnitdataMessage? decoded, out string? error), error ?? "SCCP segmented XUDT decode failed");
    AssertEqual(0x00010203U, decoded!.Segmentation?.LocalReference, "SCCP decoded segmentation reference");
    AssertEqual((byte)1, decoded.Segmentation?.RemainingSegments, "SCCP decoded segmentation remaining");
    Assert(decoded.Segmentation?.FirstSegment == false, "SCCP decoded segmentation first flag");
}

static void SccpLudtCodecCarriesLongUserData()
{
    SccpPartyAddress called = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    SccpPartyAddress calling = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MSC, pointCode: 0x0102);
    byte[] payload = Enumerable.Range(0, 300).Select(i => (byte)i).ToArray();

    SccpLongUnitdataMessage message = new(
        new SccpProtocolClass(SccpConnectionlessClass.Class1),
        hopCounter: 9,
        called,
        calling,
        payload);

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.LongUnitdata, encoded[0], "SCCP LUDT message type");
    AssertSequence([0x00, 0x08], encoded.AsSpan(3, 2), "SCCP LUDT called pointer");
    AssertSequence([0x00, 0x0C], encoded.AsSpan(5, 2), "SCCP LUDT calling pointer");
    AssertSequence([0x00, 0x10], encoded.AsSpan(7, 2), "SCCP LUDT data pointer");
    AssertSequence([0x01, 0x2C], encoded.AsSpan(23, 2), "SCCP LUDT data length");

    Assert(SccpLongUnitdataMessage.TryDecode(encoded, out SccpLongUnitdataMessage? decoded, out string? error), error ?? "SCCP LUDT decode failed");
    AssertEqual((byte)9, decoded!.HopCounter, "SCCP decoded LUDT hop counter");
    AssertSequence(payload, decoded.UserData.Span, "SCCP decoded LUDT data");
}

static void SccpUdtsCodecCarriesReturnCause()
{
    SccpPartyAddress called = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    SccpPartyAddress calling = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MSC, pointCode: 0x0102);
    SccpUnitdataServiceMessage message = new(
        SccpReturnCause.SubsystemFailure,
        called,
        calling,
        new byte[] { 0xDE, 0xAD });

    byte[] encoded = message.Encode();
    AssertEqual((byte)SccpMessageType.UnitdataService, encoded[0], "SCCP UDTS message type");
    AssertEqual((byte)SccpReturnCause.SubsystemFailure, encoded[1], "SCCP UDTS return cause");
    AssertEqual((byte)3, encoded[2], "SCCP UDTS called pointer");
    AssertEqual((byte)7, encoded[3], "SCCP UDTS calling pointer");
    AssertEqual((byte)11, encoded[4], "SCCP UDTS data pointer");

    Assert(SccpUnitdataServiceMessage.TryDecode(encoded, out SccpUnitdataServiceMessage? decoded, out string? error), error ?? "SCCP UDTS decode failed");
    AssertEqual(SccpReturnCause.SubsystemFailure, decoded!.ReturnCause, "SCCP decoded UDTS cause");
    AssertSequence([0xDE, 0xAD], decoded.UserData.Span, "SCCP decoded UDTS data");
}

static void SccpRouteTableResolvesSsnAndGlobalTitleRoutes()
{
    SccpRouteTable table = new();
    table.Add(new SccpRoute("map-default", SccpRouteSelector.ForSubsystem(SubsystemNumber.MAP)));
    table.Add(new SccpRoute("smsc-uk", SccpRouteSelector.ForGlobalTitlePrefix("44123")));
    table.Add(new SccpRoute("smsc-uk-specific", SccpRouteSelector.ForGlobalTitlePrefix("4412345")));

    SccpPartyAddress ssnAddress = new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    Assert(table.TryResolve(ssnAddress, out SccpRoute? ssnRoute), "SCCP SSN route should resolve");
    AssertEqual("map-default", ssnRoute!.Name, "SCCP SSN route name");

    SccpPartyAddress gtAddress = new(
        SccpRoutingIndicator.RouteOnGlobalTitle,
        subsystemNumber: SubsystemNumber.MAP,
        globalTitle: new SccpGlobalTitle("44123456789"));
    Assert(table.TryResolve(gtAddress, out SccpRoute? gtRoute), "SCCP GT route should resolve");
    AssertEqual("smsc-uk-specific", gtRoute!.Name, "SCCP longest GT prefix route");
    AssertEqual(3, table.Snapshot().Count, "SCCP route snapshot count");
}

static void SccpPhase3ReadinessReportsFoundationStatus()
{
    AssertEqual("MTP3 and SCCP foundation", SccpPhase3Readiness.ReleaseLabel, "SCCP readiness label");
    AssertEqual(6, SccpPhase3Readiness.RequiredFoundationCapabilityCount, "SCCP readiness capability count");
    Assert(
        SccpPhase3Readiness.ProductionGateDescription.Contains("interoperability", StringComparison.Ordinal),
        SccpPhase3Readiness.ProductionGateDescription);

    SccpPhase3ReadinessReport report = SccpPhase3Readiness.GetReport();
    Assert(report.FoundationReady, "SCCP foundation should be ready");
    Assert(!report.IsProductionReady, "SCCP should not claim production readiness without interop vectors");
    AssertEqual(6, report.FoundationCapabilityCount, "SCCP completed foundation capabilities");
    Assert(report.Describe().Contains("foundationCapabilities=6/6", StringComparison.Ordinal), report.Describe());
}

static void SctpPayloadMetadataStoresStreamAndPpidValues()
{
    SctpPayloadMetadata metadata = new(streamId: 2, payloadProtocolIdentifier: 3, unordered: true);
    AssertEqual((ushort)2, metadata.StreamId, "SCTP metadata stream");
    AssertEqual((uint)3, metadata.PayloadProtocolIdentifier, "SCTP metadata PPID");
    Assert(metadata.Unordered, "SCTP metadata unordered flag");

    SctpReceiveResult result = new(bytesReceived: 12, metadata);
    AssertEqual(12, result.BytesReceived, "SCTP receive byte count");
    AssertEqual((ushort)2, result.Metadata.StreamId, "SCTP receive metadata stream");
}

static void SctpAssociationEventsDescribeLifecycleState()
{
    SctpAssociationEvent established = new(SctpAssociationEventType.Established, SctpAssociationState.Established, reason: "connected");
    AssertEqual(SctpAssociationEventType.Established, established.EventType, "SCTP event type");
    AssertEqual(SctpAssociationState.Established, established.State, "SCTP event state");
    AssertEqual("connected", established.Reason, "SCTP event reason");
}

static void SctpConnectionOptionsValidateEndpointsAndStreamCounts()
{
    SctpEndpoint remote = new("sg.example.net", 2905);
    SctpConnectionOptions options = new(
        remote,
        localEndpoint: new SctpEndpoint("0.0.0.0", 2905),
        outboundStreams: 8,
        inboundStreams: 8,
        defaultPayloadProtocolIdentifier: 3,
        connectTimeout: TimeSpan.FromSeconds(5));

    AssertEqual("sg.example.net:2905", remote.ToString(), "SCTP endpoint string");
    AssertEqual((ushort)8, options.OutboundStreams, "SCTP outbound streams");
    AssertEqual((ushort)8, options.InboundStreams, "SCTP inbound streams");
    AssertEqual((uint)3, options.DefaultPayloadProtocolIdentifier, "SCTP default PPID");
    AssertEqual(TimeSpan.FromSeconds(5), options.ConnectTimeout, "SCTP connect timeout");

    AssertThrows<ArgumentOutOfRangeException>(() => new SctpEndpoint("sg", 0));
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpConnectionOptions(remote, outboundStreams: 0));
}

static void SctpPpidHelpersRecognizeSigtranPayloadIdentifiers()
{
    AssertEqual((uint)3, SctpPayloadProtocolIdentifiers.M3ua, "M3UA PPID");
    Assert(SctpPayloadProtocolIdentifiers.IsKnown(SctpPayloadProtocolIdentifiers.M3ua), "M3UA PPID should be known");
    Assert(SctpPayloadProtocolIdentifiers.IsKnown(SctpPayloadProtocolIdentifiers.M2pa), "M2PA PPID should be known");
    Assert(SctpPayloadProtocolIdentifiers.TryRequireKnown(SctpPayloadProtocolIdentifiers.M3ua, out string? knownError), knownError ?? "known PPID failed");
    Assert(!SctpPayloadProtocolIdentifiers.TryRequireKnown(999, out string? unknownError), "unknown PPID should fail");
    Assert(unknownError?.Contains("999", StringComparison.Ordinal) == true, unknownError ?? "missing unknown PPID error");
}

static void SctpStreamSelectionPoliciesChooseOutboundStreams()
{
    SctpStreamSelectionPolicy fixedPolicy = new(SctpStreamSelectionMode.Fixed, streamCount: 4, fixedStreamId: 2);
    AssertEqual((ushort)2, fixedPolicy.SelectStream(sequence: 99), "fixed stream selection");

    SctpStreamSelectionPolicy roundRobin = new(SctpStreamSelectionMode.RoundRobin, streamCount: 4);
    AssertEqual((ushort)0, roundRobin.SelectStream(0), "round-robin first stream");
    AssertEqual((ushort)3, roundRobin.SelectStream(7), "round-robin modulo stream");
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpStreamSelectionPolicy(streamCount: 0));
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpStreamSelectionPolicy(streamCount: 2, fixedStreamId: 2));
}

static void SctpReconnectPoliciesComputeBoundedDelays()
{
    SctpReconnectPolicy policy = new(
        maxAttempts: 4,
        initialDelay: TimeSpan.FromMilliseconds(100),
        maxDelay: TimeSpan.FromMilliseconds(250),
        backoffMultiplier: 2.0);

    Assert(policy.IsEnabled, "reconnect should be enabled");
    AssertEqual(TimeSpan.FromMilliseconds(100), policy.GetDelay(1), "first reconnect delay");
    AssertEqual(TimeSpan.FromMilliseconds(200), policy.GetDelay(2), "second reconnect delay");
    AssertEqual(TimeSpan.FromMilliseconds(250), policy.GetDelay(3), "bounded reconnect delay");
    AssertThrows<ArgumentOutOfRangeException>(() => new SctpReconnectPolicy(maxAttempts: -1));
    AssertThrows<ArgumentOutOfRangeException>(() => policy.GetDelay(0));
}

static void SctpTransportHealthSnapshotsExposeAssociationDetails()
{
    SctpEndpoint remote = new("sg.example.net", 2905);
    SctpEndpoint local = new("0.0.0.0", 2905);
    SctpTransportHealth health = new(
        SctpAssociationState.Established,
        remote,
        local,
        outboundStreams: 8,
        inboundStreams: 8,
        defaultPayloadProtocolIdentifier: SctpPayloadProtocolIdentifiers.M3ua,
        sentMessages: 10,
        receivedMessages: 11);

    Assert(health.IsEstablished, "SCTP health should be established");
    AssertEqual(remote, health.RemoteEndpoint, "SCTP health remote endpoint");
    AssertEqual(local, health.LocalEndpoint, "SCTP health local endpoint");
    AssertEqual(10L, health.SentMessages, "SCTP health sent messages");
    AssertEqual(11L, health.ReceivedMessages, "SCTP health received messages");
}

static void TcpSctpAdapterExposesDevelopmentMetadataAndHealth()
{
    TcpListener listener = new(IPAddress.Loopback, 0);
    listener.Start();
    try
    {
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
        TcpClient client = new();
        client.Connect(IPAddress.Loopback, port);
        using TcpClient server = acceptTask.GetAwaiter().GetResult();

        using TcpSctpAdapter clientAdapter = new(client);
        using TcpSctpAdapter serverAdapter = new(server);
        ISctpMetadataSocket metadataSocket = serverAdapter;

        clientAdapter.SendAsync(new byte[] { 0x01, 0x02, 0x03 }).GetAwaiter().GetResult();
        byte[] receiveBuffer = new byte[16];
        SctpReceiveResult received = metadataSocket.ReceiveAsync(receiveBuffer).GetAwaiter().GetResult();

        AssertEqual(3, received.BytesReceived, "TCP adapter received byte count");
        AssertEqual(SctpPayloadProtocolIdentifiers.M3ua, received.Metadata.PayloadProtocolIdentifier, "TCP adapter metadata PPID");
        AssertSequence([0x01, 0x02, 0x03], receiveBuffer.AsSpan(0, received.BytesReceived), "TCP adapter received payload");

        SctpTransportHealth clientHealth = clientAdapter.GetHealthSnapshot(new SctpEndpoint("127.0.0.1", port));
        AssertEqual(1L, clientHealth.SentMessages, "TCP adapter sent health count");
        Assert(clientHealth.IsEstablished, "TCP adapter health should be established");
    }
    finally
    {
        listener.Stop();
    }
}

static void SctpTransportReadinessReportsFoundationStatus()
{
    AssertEqual("SCTP transport foundation", SctpTransportReadiness.ReleaseLabel, "SCTP readiness label");
    AssertEqual(5, SctpTransportReadiness.RequiredFoundationCapabilityCount, "SCTP foundation capability count");
    Assert(
        SctpTransportReadiness.ProductionGateDescription.Contains("Native SCTP", StringComparison.Ordinal),
        SctpTransportReadiness.ProductionGateDescription);
    SctpTransportReadinessReport report = SctpTransportReadiness.GetReport();
    AssertEqual(5, report.FoundationCapabilityCount, "Completed SCTP foundation capabilities");
    Assert(report.FoundationReady, "SCTP foundation should be ready");
    Assert(!report.IsProductionReady, "SCTP should not be production-ready without native implementation");
    Assert(report.Describe().Contains("foundationCapabilities=5/5", StringComparison.Ordinal), report.Describe());
    Assert(report.Describe().Contains("nativeSctp=False", StringComparison.Ordinal), report.Describe());
}

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

static void M3uaProtocolExposesPublicMetadata()
{
    AssertEqual("M3UA", M3uaProtocol.Name, "protocol name");
    AssertEqual("RFC 4666", M3uaProtocol.Specification, "protocol specification");
    AssertEqual("Sigtran.Net", M3uaProtocol.PackageName, "package name");
    AssertEqual((byte)1, M3uaProtocol.Version, "protocol version");
    AssertEqual(8, M3uaProtocol.HeaderLength, "protocol header length");
    AssertEqual(4, M3uaProtocol.ParameterHeaderLength, "protocol parameter header length");
    Span<byte> headerBuffer = stackalloc byte[8];
    headerBuffer[0] = 2;
    headerBuffer[1] = 1;
    headerBuffer[2] = (byte)M3uaMessageClass.Management;
    headerBuffer[3] = 0x7F;
    headerBuffer[7] = 8;
    Assert(M3uaProtocol.TryReadHeader(headerBuffer, out M3uaHeaderPreview preview, out string? previewError), previewError ?? "header preview failed");
    AssertEqual((byte)2, preview.Version, "header preview version");
    AssertEqual((byte)1, preview.Reserved, "header preview reserved");
    AssertEqual(M3uaMessageClass.Management, preview.MessageClass, "header preview class");
    AssertEqual((byte)0x7F, preview.MessageType, "header preview type");
    AssertEqual((uint)8, preview.MessageLength, "header preview length");
    Assert(!M3uaProtocol.TryReadHeader([0x01, 0x00], out _, out string? shortHeaderError), "short header preview should fail");
    Assert(shortHeaderError?.Contains("too short", StringComparison.Ordinal) == true, shortHeaderError ?? "missing short header error");
    Assert(M3uaProtocol.TryValidateMessageLength(8, availableLength: 8, out string? validLengthError), validLengthError ?? "valid length failed");
    Assert(!M3uaProtocol.TryValidateMessageLength(4, availableLength: 8, out string? shortLengthError), "short message length should fail");
    Assert(shortLengthError?.Contains("Invalid M3UA length 4", StringComparison.Ordinal) == true, shortLengthError ?? "missing short length error");
    Assert(!M3uaProtocol.TryValidateMessageLength(12, availableLength: 8, out string? oversizedLengthError), "oversized message length should fail");
    Assert(oversizedLengthError?.Contains("Invalid M3UA length 12", StringComparison.Ordinal) == true, oversizedLengthError ?? "missing oversized length error");
    Assert(!M3uaProtocol.TryValidateMessageLength(10, availableLength: 12, out string? unalignedLengthError), "unaligned message length should fail");
    Assert(unalignedLengthError?.Contains("not 32-bit aligned", StringComparison.Ordinal) == true, unalignedLengthError ?? "missing unaligned length error");

    M3uaProtocolCapabilities capabilities = M3uaProtocol.Capabilities;
    Assert(capabilities.SupportsPayloadData, "capabilities should include Payload Data");
    Assert(capabilities.SupportsAspLifecycle, "capabilities should include ASP lifecycle");
    Assert(capabilities.SupportsManagement, "capabilities should include Management");
    Assert(capabilities.SupportsSsnm, "capabilities should include SSNM");
    Assert(capabilities.SupportsRkm, "capabilities should include RKM");
    Assert(capabilities.SupportsTransportSession, "capabilities should include transport session");
}

static void M3uaAlphaReadinessReportDescribesReleaseGate()
{
    AssertEqual("M3UA alpha", M3uaAlphaReadiness.ReleaseLabel, "alpha release label");
    AssertEqual(3, M3uaAlphaReadiness.RequiredVerificationCommandCount, "alpha verification command count");
    M3uaAlphaReadinessReport report = M3uaAlphaReadiness.GetReport();
    Assert(report.IsReady, "M3UA alpha readiness report should be ready");
    Assert(report.HasPackageMetadata, "alpha report package metadata");
    Assert(report.RequiresXmlDocumentation, "alpha report XML docs");
    Assert(report.HasM3uaProtocolCoverage, "alpha report protocol coverage");
    Assert(report.HasTransportAbstraction, "alpha report transport abstraction");
    Assert(report.MarksUpperLayersExperimental, "alpha report experimental upper layers");
    Assert(
        report.Describe().Contains("m3uaAlphaReady=True", StringComparison.Ordinal),
        report.Describe());
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
    Assert(message.TryGetParameterCount(out int parameterCount, out string? countError), countError ?? "parameter count failed");
    AssertEqual(1, parameterCount, "message parameter count");
    Assert(message.HasParameter(M3uaParameterTag.ProtocolData), "Protocol Data should be present");
    Assert(!message.HasParameter(M3uaParameterTag.HeartbeatData), "Heartbeat Data should not be present");
    Assert(message.TryGetParameter(M3uaParameterTag.ProtocolData, out ReadOnlySpan<byte> genericProtocolData, out string? genericProtocolError), genericProtocolError ?? "generic Protocol Data missing");
    Assert(message.TryGetProtocolData(out ReadOnlySpan<byte> protocolData, out string? protocolError), protocolError ?? "Protocol Data missing");

    AssertSequence(genericProtocolData, protocolData, "generic Protocol Data value");
    AssertEqual(15, protocolData.Length, "Protocol Data value length");
    AssertSequence([0x00, 0x00, 0x00, 0x01], protocolData.Slice(0, 4), "decoded OPC");
    AssertSequence([0x00, 0x00, 0x00, 0x02], protocolData.Slice(4, 4), "decoded DPC");
    AssertSequence([3, 2, 0, 7], protocolData.Slice(8, 4), "decoded SI/NI/MP/SLS");
    AssertSequence(payload, protocolData.Slice(12), "decoded user payload");
    Assert(!message.TryGetParameter(M3uaParameterTag.HeartbeatData, out _, out string? missingError), "missing Heartbeat Data should not be found");
    Assert(missingError?.Contains("HeartbeatData", StringComparison.Ordinal) == true, missingError ?? "missing parameter error");
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

static void M3uaReportsSupportedTypedMessageKinds()
{
    Assert(
        M3uaTypedMessageParser.IsSupported(M3uaMessageClass.Transfer, (byte)M3uaTransferMessageType.PayloadData),
        "Payload Data should be supported");
    Assert(
        M3uaTypedMessageParser.IsSupported(M3uaMessageClass.Aspsm, (byte)M3uaAspsmMessageType.HeartbeatAck),
        "Heartbeat Ack should be supported");
    Assert(
        M3uaTypedMessageParser.IsSupported(M3uaMessageClass.RoutingKeyManagement, (byte)M3uaRoutingKeyManagementMessageType.RegistrationResponse),
        "Registration Response should be supported");
    Assert(
        !M3uaTypedMessageParser.IsSupported(M3uaMessageClass.Management, 0x7F),
        "Unknown Management message should not be supported");
    Assert(
        !M3uaTypedMessageParser.IsSupported((M3uaMessageClass)0x7F, 0x01),
        "Unknown message class should not be supported");
    Assert(
        M3uaTypedMessageParser.TryRequireSupported(M3uaMessageClass.Transfer, (byte)M3uaTransferMessageType.PayloadData, out string? supportedError),
        supportedError ?? "Payload Data should be required as supported");
    AssertEqual(null, supportedError, "supported require error");
    Assert(
        !M3uaTypedMessageParser.TryRequireSupported(M3uaMessageClass.Management, 0x7F, out string? unsupportedError),
        "Unknown Management message should not be required as supported");
    Assert(unsupportedError?.Contains("class=Management type=127", StringComparison.Ordinal) == true, unsupportedError ?? "missing unsupported require error");
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

static void M3uaRouteTableRemovesAndClearsRoutes()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: 5);

    Assert(table.IsEmpty, "new route table should be empty");
    Assert(table.TryAdd(first, out string? firstError), firstError ?? "first route add failed");
    Assert(table.TryAdd(second, out string? secondError), secondError ?? "second route add failed");
    AssertEqual(2, table.Count, "route table count after add");
    Assert(!table.IsEmpty, "route table should not be empty after add");

    M3uaPayloadRoute sameSelectors = new("renamed", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    Assert(table.TryRemove(sameSelectors, out string? removeError), removeError ?? "route remove failed");
    AssertEqual(1, table.Routes.Count, "route count after remove");
    AssertEqual(1, table.Count, "route table Count after remove");
    AssertEqual("second", table.Routes[0].Name, "remaining route");

    Assert(!table.TryRemove(sameSelectors, out string? missingError), "missing route remove should fail");
    Assert(missingError?.Contains("No route", StringComparison.Ordinal) == true, missingError ?? "missing route remove error");

    table.Clear();
    AssertEqual(0, table.Routes.Count, "route count after clear");
    AssertEqual(0, table.Count, "route table Count after clear");
    Assert(table.IsEmpty, "route table should be empty after clear");
}

static void M3uaRouteTableReplacesRoutesBySelector()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("old-name", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute replacement = new("new-name", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);

    Assert(table.TryAdd(first, out string? addError), addError ?? "route add failed");
    Assert(table.TryReplace(replacement, out string? replaceError), replaceError ?? "route replace failed");

    AssertEqual(1, table.Routes.Count, "route count after replace");
    AssertEqual("new-name", table.Routes[0].Name, "route name after replace");
    Assert(!table.TryFindByName("old-name", out _), "old route name should not be found after replace");
    Assert(table.TryFindByName("new-name", out _), "new route name should be found after replace");

    M3uaPayloadRoute missing = new("missing", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: null);
    Assert(!table.TryReplace(missing, out string? missingError), "missing selector replace should fail");
    Assert(missingError?.Contains("No route", StringComparison.Ordinal) == true, missingError ?? "missing replace error");
}

static void M3uaRouteTableAddsOrReplacesRoutesBySelector()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute replacement = new("replacement", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: null);

    table.AddOrReplace(first, out bool firstReplaced);
    table.AddOrReplace(replacement, out bool replacementReplaced);
    table.AddOrReplace(second, out bool secondReplaced);

    Assert(!firstReplaced, "first route should be added");
    Assert(replacementReplaced, "replacement route should replace same selectors");
    Assert(!secondReplaced, "second route should be added");
    M3uaPayloadRouteTableValidation validation = table.Validate();
    Assert(validation.IsValid, "route table should be valid");
    AssertEqual(2, validation.RouteCount, "route validation count");
    AssertEqual(0, validation.DuplicateNameCount, "route validation duplicate names");
    AssertEqual(2, table.Count, "add-or-replace route count");
    AssertEqual("replacement", table.Routes[0].Name, "replaced route name");
    AssertEqual("NA=7 RC=100 DPC=2 SI=3", table.Routes[0].DescribeSelectors(), "replaced route selectors");
    AssertEqual("second", table.Routes[1].Name, "added route name");
    AssertEqual("NA=* RC=200 DPC=* SI=*", table.Routes[1].DescribeSelectors(), "wildcard route selectors");

    table.AddOrReplace(new M3uaPayloadRoute("second", networkAppearance: null, routingContext: 201, destinationPointCode: null, serviceIndicator: null), out _);
    M3uaPayloadRouteTableValidation duplicateValidation = table.Validate();
    Assert(!duplicateValidation.IsValid, "duplicate route names should invalidate validation snapshot");
    Assert(duplicateValidation.HasDuplicateNames, "duplicate names should be reported");
}

static void M3uaRouteTableSnapshotsAndFindsRoutesByName()
{
    M3uaPayloadRouteTable table = new();
    M3uaPayloadRoute first = new("first", networkAppearance: 7, routingContext: 100, destinationPointCode: 2, serviceIndicator: 3);
    M3uaPayloadRoute second = new("second", networkAppearance: null, routingContext: 200, destinationPointCode: null, serviceIndicator: 5);

    Assert(table.TryAdd(first, out string? firstError), firstError ?? "first route add failed");
    Assert(table.TryAdd(second, out string? secondError), secondError ?? "second route add failed");

    M3uaPayloadRoute[] snapshot = table.Snapshot();
    AssertEqual(2, snapshot.Length, "snapshot route count");
    AssertEqual("first", snapshot[0].Name, "snapshot first route");
    table.Clear();
    AssertEqual(2, snapshot.Length, "snapshot should remain stable after clear");
    AssertEqual(0, table.Routes.Count, "table should be empty after clear");

    Assert(table.TryAdd(first, out string? readdError), readdError ?? "re-add route failed");
    Assert(table.TryFindByName("first", out M3uaPayloadRoute? found), "route by name should be found");
    AssertEqual((uint?)100, found!.RoutingContext, "found route Routing Context");
    Assert(!table.TryFindByName("missing", out _), "missing route by name should not be found");
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

    M3uaPayloadDataMessage typedInput = new(
        networkAppearance: 8,
        routingContext: 200,
        originatingPointCode: 3,
        destinationPointCode: 4,
        serviceIndicator: 5,
        networkIndicator: 2,
        messagePriority: 1,
        signallingLinkSelection: 9,
        userPayload: [0xBB],
        correlationId: 43);
    Assert(
        processor.TryBuildPayloadData(buffer, typedInput, out written, out string? typedBuildError),
        typedBuildError ?? "typed outbound DATA build failed");

    M3uaMessage typedMessage = DecodeMessage(buffer.Slice(0, written));
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(typedMessage, out M3uaPayloadDataMessage? typedOutput, out string? typedParseError),
        typedParseError ?? "typed outbound DATA parse failed");
    AssertEqual((uint?)8, typedOutput!.NetworkAppearance, "typed outbound DATA Network Appearance");
    AssertEqual((uint?)200, typedOutput.RoutingContext, "typed outbound DATA Routing Context");
    AssertEqual((uint?)43, typedOutput.CorrelationId, "typed outbound DATA Correlation Id");
    AssertSequence([0xBB], typedOutput.UserPayload, "typed outbound DATA payload");
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

    M3uaPayloadDataMessage typedOutbound = new(
        networkAppearance: 8,
        routingContext: 200,
        originatingPointCode: 3,
        destinationPointCode: 4,
        serviceIndicator: 5,
        networkIndicator: 2,
        messagePriority: 1,
        signallingLinkSelection: 9,
        userPayload: [0xAB, 0xCD],
        correlationId: 43);
    session.SendPayloadDataAsync(typedOutbound).GetAwaiter().GetResult();

    AssertEqual(2, socket.SentPackets.Count, "typed sent packet count");
    M3uaMessage typedSent = DecodeMessage(socket.SentPackets[1].Span);
    Assert(
        M3uaTypedMessageParser.TryParsePayloadData(typedSent, out M3uaPayloadDataMessage? parsedTypedOutbound, out string? typedParseError),
        typedParseError ?? "typed sent DATA parse failed");
    AssertEqual((uint?)8, parsedTypedOutbound!.NetworkAppearance, "typed sent DATA Network Appearance");
    AssertEqual((uint?)200, parsedTypedOutbound.RoutingContext, "typed sent DATA Routing Context");
    AssertEqual((uint?)43, parsedTypedOutbound.CorrelationId, "typed sent DATA Correlation Id");
    AssertSequence([0xAB, 0xCD], parsedTypedOutbound.UserPayload, "typed sent DATA payload");
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

static void M3uaTransportSessionWaitsForTypedMessages()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            (ushort)M3uaApplicationServerState.Active,
            aspIdentifier: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? notifyError),
        notifyError ?? "Notify build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaRegistrationResult[] results =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064)
    ];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, results, out written, out string? registrationError),
        registrationError ?? "REG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession session = new(socket, leaveOpen: true);
    M3uaInboundProcessingResult result = session.ReceiveUntilAsync(
        M3uaTypedMessageKind.RegistrationResponse,
        maxMessages: 2).GetAwaiter().GetResult();

    AssertEqual(M3uaTypedMessageKind.RegistrationResponse, result.TypedMessage.Kind, "waited typed kind");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, result.TypedMessage.As<M3uaRegistrationResponseMessage>().Results[0].Status, "waited registration status");
}

static void M3uaTransportSessionWaitsForAspTransitions()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildNotify(
            buffer,
            M3uaNotifyStatusType.ApplicationServerStateChange,
            (ushort)M3uaApplicationServerState.Inactive,
            aspIdentifier: null,
            ReadOnlySpan<uint>.Empty,
            ReadOnlySpan<byte>.Empty,
            out int written,
            out string? notifyError),
        notifyError ?? "Notify build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 0x0000002A, ReadOnlySpan<byte>.Empty, out written, out string? upAckError),
        upAckError ?? "ASP Up Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new();
    M3uaInboundProcessor inbound = new(aspSession);
    using M3uaTransportSession session = new(socket, inboundProcessor: inbound, leaveOpen: true);

    M3uaInboundProcessingResult result = session.ReceiveUntilTransitionAsync(
        M3uaAspEvent.AspUpAcknowledged,
        maxMessages: 2).GetAwaiter().GetResult();

    AssertEqual(M3uaAspEvent.AspUpAcknowledged, result.StateTransition!.Value.Event, "waited ASP transition event");
    AssertEqual(M3uaAspState.Inactive, aspSession.State, "waited ASP transition state");
}

static void M3uaTransportSessionDisposesOwnedSocket()
{
    FakeSctpSocket socket = new();
    M3uaTransportSession session = new(socket);
    session.Dispose();

    Assert(socket.Disposed, "owned socket should be disposed");
}

static void M3uaTransportSessionTracksCounters()
{
    Span<byte> buffer = stackalloc byte[64];
    FakeSctpSocket socket = new();
    Assert(
        M3uaMessageBuilder.BuildHeartbeat(buffer, [0x01], out int written, out string? buildError),
        buildError ?? "Heartbeat build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession session = new(socket, leaveOpen: true);
    session.SendHeartbeatAsync(new byte[] { 0x02 }).GetAwaiter().GetResult();
    M3uaInboundProcessingResult? result = session.ReceiveAsync().GetAwaiter().GetResult();
    Assert(result is not null, "counter receive result should not be null");

    InvalidOperationException exception = AssertThrows<InvalidOperationException>(() =>
        session.SendDestinationUnavailableAsync(
            networkAppearance: null,
            routingContexts: ReadOnlyMemory<uint>.Empty,
            affectedPointCodes: ReadOnlyMemory<M3uaAffectedPointCode>.Empty,
            infoString: ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult());
    Assert(exception.Message.Contains("Affected Point Code", StringComparison.Ordinal), exception.Message);

    M3uaTransportSessionCounters counters = session.Counters;
    AssertEqual(1L, counters.SentPdus, "counter sent PDUs");
    AssertEqual(1L, counters.ReceivedPdus, "counter received PDUs");
    AssertEqual(1L, counters.SendFailures, "counter send failures");
    AssertEqual(0L, counters.ReceiveFailures, "counter receive failures");

    M3uaTransportSessionHealth health = session.GetHealthSnapshot();
    AssertEqual(M3uaAspState.Down, health.AspState, "health ASP state");
    AssertEqual(1L, health.Counters.SentPdus, "health sent PDUs");
    AssertEqual(session.MaxPduSize, health.MaxPduSize, "health max PDU size");
    Assert(!health.IsDisposed, "health should show active session");
}

static void M3uaTransportSessionResetsCounters()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);
    session.SendHeartbeatAsync(new byte[] { 0x02 }).GetAwaiter().GetResult();

    M3uaTransportSessionCounters beforeReset = session.Counters;
    AssertEqual(1L, beforeReset.SentPdus, "counter sent PDUs before reset");

    session.ResetCounters();

    M3uaTransportSessionCounters afterReset = session.Counters;
    AssertEqual(0L, afterReset.SentPdus, "counter sent PDUs after reset");
    AssertEqual(0L, afterReset.ReceivedPdus, "counter received PDUs after reset");
    AssertEqual(0L, afterReset.SendFailures, "counter send failures after reset");
    AssertEqual(0L, afterReset.ReceiveFailures, "counter receive failures after reset");

    session.Dispose();
    Assert(session.GetHealthSnapshot().IsDisposed, "health should show disposed session");
}

static void M3uaTransportSessionNotifiesAspTransportLoss()
{
    FakeSctpSocket socket = new();
    M3uaAspSession aspSession = new(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    using M3uaTransportSession session = new(socket, inbound, leaveOpen: true);

    Assert(session.TryNotifyTransportLost(out M3uaAspStateTransition transition, out string? error), error ?? "transport loss notification failed");
    AssertEqual(M3uaAspEvent.TransportLost, transition.Event, "transport loss event");
    AssertEqual(M3uaAspState.Active, transition.From, "transport loss from state");
    AssertEqual(M3uaAspState.Down, transition.To, "transport loss to state");
    AssertEqual(M3uaAspState.Down, aspSession.State, "transport loss session state");
}

static void M3uaDiagnosticsFormatHexAndSummaries()
{
    byte[] bytes =
    [
        0x00, 0x01, 0x02, 0x03,
        0x04, 0x05, 0x06, 0x07,
        0x08, 0x09
    ];
    string dump = M3uaDiagnostics.FormatHexDump(bytes, bytesPerLine: 4);
    Assert(dump.Contains("0000: 00 01 02 03", StringComparison.Ordinal), dump);
    Assert(dump.Contains("0004: 04 05 06 07", StringComparison.Ordinal), dump);
    Assert(dump.Contains("0008: 08 09", StringComparison.Ordinal), dump);

    Span<byte> buffer = stackalloc byte[32];
    Assert(
        M3uaMessageBuilder.BuildHeartbeat(buffer, [0xAA, 0xBB], out int written, out string? buildError),
        buildError ?? "Heartbeat build failed");
    Assert(
        M3uaDiagnostics.TryFormatSummary(buffer.Slice(0, written), out string summary, out string? summaryError),
        summaryError ?? "summary format failed");
    AssertEqual("M3UA v1 class=Aspsm type=3 length=16 parameters=8", summary, "M3UA summary");
    Assert(
        M3uaDiagnostics.TryFormatTypedSummary(buffer.Slice(0, written), out string typedSummary, out string? typedSummaryError),
        typedSummaryError ?? "typed summary format failed");
    AssertEqual("M3UA v1 class=Aspsm type=3 kind=AspStateMaintenance length=16 parameters=8", typedSummary, "M3UA typed summary");
    Assert(
        M3uaDiagnostics.TryValidateSupportedPacket(buffer.Slice(0, written), out M3uaPacketValidationResult validation, out string? validationError),
        validationError ?? "supported packet validation failed");
    AssertEqual(M3uaMessageClass.Aspsm, validation.Header.MessageClass, "validation header class");
    AssertEqual(1, validation.ParameterCount, "validation parameter count");
    Assert(validation.DispatcherSupported, "validation dispatcher support");
    Assert(
        M3uaDiagnostics.TryFormatParameterInventory(buffer.Slice(0, written), out string inventory, out string? inventoryError),
        inventoryError ?? "parameter inventory failed");
    AssertEqual("M3UA parameters count=1 [HeartbeatData length=6 value=2 padded=8]", inventory, "M3UA parameter inventory");

    Span<byte> unsupported = stackalloc byte[8];
    unsupported[0] = 1;
    unsupported[2] = (byte)M3uaMessageClass.Management;
    unsupported[3] = 0x7F;
    unsupported[7] = 8;
    Assert(
        !M3uaDiagnostics.TryFormatTypedSummary(unsupported, out _, out string? unsupportedError),
        "unsupported typed summary should fail");
    Assert(unsupportedError?.Contains("Unsupported Management message type", StringComparison.Ordinal) == true, unsupportedError ?? "missing unsupported typed summary error");
    Assert(
        !M3uaDiagnostics.TryValidateSupportedPacket(unsupported, out _, out string? unsupportedValidationError),
        "unsupported packet validation should fail");
    Assert(unsupportedValidationError?.Contains("class=Management type=127", StringComparison.Ordinal) == true, unsupportedValidationError ?? "missing unsupported validation error");

    Assert(
        !M3uaDiagnostics.TryFormatSummary([0x01, 0x00], out _, out string? malformedError),
        "malformed summary should fail");
    Assert(malformedError?.Contains("too short", StringComparison.Ordinal) == true, malformedError ?? "missing malformed summary error");

    Span<byte> badParameter = stackalloc byte[12];
    badParameter[0] = 1;
    badParameter[2] = (byte)M3uaMessageClass.Aspsm;
    badParameter[3] = (byte)M3uaAspsmMessageType.Heartbeat;
    badParameter[7] = 12;
    badParameter[9] = (byte)M3uaParameterTag.HeartbeatData;
    badParameter[11] = 16;
    Assert(
        !M3uaDiagnostics.TryFormatParameterInventory(badParameter, out _, out string? badParameterError),
        "bad parameter inventory should fail");
    Assert(badParameterError?.Contains("exceeds remaining buffer", StringComparison.Ordinal) == true, badParameterError ?? "missing bad parameter error");

    M3uaAspSession session = new(M3uaAspState.Inactive);
    Span<byte> activeAckBuffer = stackalloc byte[64];
    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(activeAckBuffer, M3uaTrafficModeType.Loadshare, [100, 200], ReadOnlySpan<byte>.Empty, out int activeAckWritten, out string? activeAckError),
        activeAckError ?? "ASP Active Ack build failed");
    M3uaMessage activeAck = DecodeMessage(activeAckBuffer.Slice(0, activeAckWritten));
    Assert(session.TryApplyAcknowledgement(activeAck, out _, out string? sessionError), sessionError ?? "ASP session apply failed");
    AssertEqual("ASP state=Active aspId=none trafficMode=Loadshare routingContexts=100,200", M3uaDiagnostics.FormatAspSessionSummary(session), "ASP session summary");
}

static void M3uaAspClientCompletesStartupHandshake()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 42, ReadOnlySpan<byte>.Empty, out int written, out string? upAckError),
        upAckError ?? "ASP Up Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, [100], ReadOnlySpan<byte>.Empty, out written, out string? activeAckError),
        activeAckError ?? "ASP Active Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new();
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession, routingContext: 100);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaAspStartupResult result = client.StartAsync(new M3uaAspStartupOptions(
        aspIdentifier: 42,
        trafficModeType: M3uaTrafficModeType.Loadshare)).GetAwaiter().GetResult();

    AssertEqual(M3uaAspState.Active, aspSession.State, "ASP client final state");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, result.AspUpAcknowledgement.StateTransition!.Value.Event, "ASP client up transition");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, result.AspActiveAcknowledgement.StateTransition!.Value.Event, "ASP client active transition");
    AssertEqual(2, socket.SentPackets.Count, "ASP client sent packet count");
    AssertEqual((byte)M3uaAspsmMessageType.AspUp, DecodeMessage(socket.SentPackets[0].Span).MessageType, "ASP client first sent type");
    AssertEqual((byte)M3uaAsptmMessageType.AspActive, DecodeMessage(socket.SentPackets[1].Span).MessageType, "ASP client second sent type");
}

static void M3uaAspStartupOptionsValidateAndDescribeSettings()
{
    M3uaAspStartupOptions options = new(
        aspIdentifier: 42,
        trafficModeType: M3uaTrafficModeType.Loadshare,
        aspUpInfoString: new byte[] { 0x01, 0x02 },
        aspActiveInfoString: new byte[] { 0x03 },
        maxHandshakeMessages: 4);

    Assert(options.TryValidate(out string? validError), validError ?? "valid options should pass");
    AssertEqual("aspId=42 trafficMode=Loadshare upInfoBytes=2 activeInfoBytes=1 maxHandshakeMessages=4", options.Describe(), "startup options description");

    M3uaAspStartupOptions tooLarge = new(aspUpInfoString: new byte[ushort.MaxValue]);
    Assert(!tooLarge.TryValidate(out string? invalidError), "oversized Info String should fail validation");
    Assert(invalidError?.Contains("exceeds M3UA parameter value limit", StringComparison.Ordinal) == true, invalidError ?? "missing oversized Info String error");
}

static void M3uaAspClientResetsBeforeStartupHandshake()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, aspIdentifier: 77, ReadOnlySpan<byte>.Empty, out int written, out string? upAckError),
        upAckError ?? "ASP Up Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Override, [200], ReadOnlySpan<byte>.Empty, out written, out string? activeAckError),
        activeAckError ?? "ASP Active Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new(M3uaAspState.Active);
    aspSession.Reset(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession, routingContext: 200);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaAspStartupResult result = client.ResetAndStartAsync(new M3uaAspStartupOptions(
        aspIdentifier: 77,
        trafficModeType: M3uaTrafficModeType.Override)).GetAwaiter().GetResult();

    AssertEqual(M3uaAspState.Active, aspSession.State, "reset ASP client final state");
    AssertEqual((uint?)77, aspSession.AspIdentifier, "reset ASP Identifier");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Override, aspSession.TrafficModeType, "reset Traffic Mode");
    AssertSequence([0x00, 0x00, 0x00, 0xC8], UInt32SpanToBytes(aspSession.RoutingContexts), "reset Routing Context");
    AssertEqual(M3uaAspEvent.AspUpAcknowledged, result.AspUpAcknowledgement.StateTransition!.Value.Event, "reset up transition");
    AssertEqual(M3uaAspEvent.AspActiveAcknowledged, result.AspActiveAcknowledgement.StateTransition!.Value.Event, "reset active transition");
}

static void M3uaAspClientFailsWhenAcknowledgementIsMissing()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession transport = new(socket, leaveOpen: true);
    M3uaAspClient client = new(transport);

    InvalidOperationException exception = AssertThrows<InvalidOperationException>(() =>
        client.StartAsync(new M3uaAspStartupOptions(maxHandshakeMessages: 1)).GetAwaiter().GetResult());
    Assert(exception.Message.Contains("Transport closed", StringComparison.Ordinal), exception.Message);
}

static void M3uaTransportSessionSendsHeartbeat()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);

    session.SendHeartbeatAsync(new byte[] { 0x10, 0x20, 0x30 }).GetAwaiter().GetResult();

    AssertEqual(1, socket.SentPackets.Count, "Heartbeat sent packet count");
    M3uaMessage message = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual(M3uaMessageClass.Aspsm, message.MessageClass, "Heartbeat message class");
    AssertEqual((byte)M3uaAspsmMessageType.Heartbeat, message.MessageType, "Heartbeat message type");
    Assert(
        M3uaTypedMessageParser.TryParseAspsm(message, out M3uaAspStateMaintenanceMessage? typed, out string? parseError),
        parseError ?? "Heartbeat parse failed");
    AssertSequence([0x10, 0x20, 0x30], typed!.HeartbeatData.Span, "Heartbeat Data");
}

static void M3uaTransportSessionAcknowledgesInboundHeartbeat()
{
    Span<byte> buffer = stackalloc byte[64];
    FakeSctpSocket socket = new();
    byte[] heartbeatData = [0x01, 0x02, 0x03];
    Assert(
        M3uaMessageBuilder.BuildHeartbeat(buffer, heartbeatData, out int written, out string? buildError),
        buildError ?? "Heartbeat build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession session = new(socket, leaveOpen: true);
    M3uaInboundProcessingResult? result = session.ReceiveAndAcknowledgeHeartbeatAsync().GetAwaiter().GetResult();

    Assert(result is not null, "Heartbeat result should not be null");
    AssertEqual(M3uaTypedMessageKind.AspStateMaintenance, result!.TypedMessage.Kind, "Heartbeat typed kind");
    AssertEqual(1, socket.SentPackets.Count, "Heartbeat Ack sent packet count");

    M3uaMessage ack = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual(M3uaMessageClass.Aspsm, ack.MessageClass, "Heartbeat Ack class");
    AssertEqual((byte)M3uaAspsmMessageType.HeartbeatAck, ack.MessageType, "Heartbeat Ack type");
    Assert(
        M3uaTypedMessageParser.TryParseAspsm(ack, out M3uaAspStateMaintenanceMessage? typedAck, out string? parseError),
        parseError ?? "Heartbeat Ack parse failed");
    AssertSequence(heartbeatData, typedAck!.HeartbeatData.Span, "Heartbeat Ack data");
}

static void M3uaAspClientSendsHeartbeatAndWaitsForAck()
{
    Span<byte> buffer = stackalloc byte[64];
    FakeSctpSocket socket = new();
    byte[] heartbeatData = [0x01, 0x02, 0x03, 0x04];
    Assert(
        M3uaMessageBuilder.BuildHeartbeatAck(buffer, heartbeatData, out int written, out string? buildError),
        buildError ?? "Heartbeat Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaInboundProcessingResult result = client.SendHeartbeatAsync(heartbeatData).GetAwaiter().GetResult();

    AssertEqual(M3uaAspState.Active, aspSession.State, "Heartbeat should not change ASP state");
    AssertEqual(M3uaAspEvent.HeartbeatAcknowledged, result.StateTransition!.Value.Event, "Heartbeat event");
    Assert(!result.StateTransition.Value.Changed, "Heartbeat transition should not change state");
    AssertEqual(1, socket.SentPackets.Count, "Heartbeat client sent packet count");
    M3uaMessage sent = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual((byte)M3uaAspsmMessageType.Heartbeat, sent.MessageType, "Heartbeat client sent type");
}

static void M3uaAspClientDeactivatesAndStops()
{
    Span<byte> buffer = stackalloc byte[96];
    FakeSctpSocket socket = new();

    Assert(
        M3uaMessageBuilder.BuildAspInactiveAck(buffer, [100], ReadOnlySpan<byte>.Empty, out int written, out string? inactiveAckError),
        inactiveAckError ?? "ASP Inactive Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    Assert(
        M3uaMessageBuilder.BuildAspDownAck(buffer, ReadOnlySpan<byte>.Empty, out written, out string? downAckError),
        downAckError ?? "ASP Down Ack build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaAspSession aspSession = new(M3uaAspState.Active);
    M3uaInboundProcessor inbound = new(aspSession);
    M3uaOutboundProcessor outbound = new(aspSession, routingContext: 100);
    using M3uaTransportSession transport = new(socket, inbound, outbound, leaveOpen: true);
    M3uaAspClient client = new(transport);

    M3uaInboundProcessingResult inactive = client.DeactivateAsync().GetAwaiter().GetResult();
    M3uaInboundProcessingResult down = client.StopAsync().GetAwaiter().GetResult();

    AssertEqual(M3uaAspEvent.AspInactiveAcknowledged, inactive.StateTransition!.Value.Event, "ASP Inactive event");
    AssertEqual(M3uaAspState.Inactive, inactive.StateTransition.Value.To, "ASP Inactive final state");
    AssertEqual(M3uaAspEvent.AspDownAcknowledged, down.StateTransition!.Value.Event, "ASP Down event");
    AssertEqual(M3uaAspState.Down, aspSession.State, "ASP shutdown final state");
    AssertEqual(2, socket.SentPackets.Count, "ASP shutdown sent packet count");
    AssertEqual((byte)M3uaAsptmMessageType.AspInactive, DecodeMessage(socket.SentPackets[0].Span).MessageType, "ASP shutdown first sent type");
    AssertEqual((byte)M3uaAspsmMessageType.AspDown, DecodeMessage(socket.SentPackets[1].Span).MessageType, "ASP shutdown second sent type");
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
    Assert(M3uaParameterReader.TryCount(parameters, out int count, out string? countError), countError ?? "parameter count failed");
    AssertEqual(2, count, "parameter count");

    Span<byte> malformed = stackalloc byte[4];
    malformed[1] = (byte)M3uaParameterTag.InfoString;
    malformed[3] = 8;
    Assert(!M3uaParameterReader.TryCount(malformed, out _, out string? malformedCountError), "malformed count should fail");
    Assert(malformedCountError?.Contains("exceeds remaining buffer", StringComparison.Ordinal) == true, malformedCountError ?? "missing malformed count error");
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
    Assert(session.HasRoutingContext(100), "session should have Routing Context 100");
    Assert(session.HasRoutingContext(200), "session should have Routing Context 200");
    Assert(!session.HasRoutingContext(300), "session should not have Routing Context 300");

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
    Assert(!session.HasRoutingContext(100), "session should not have Routing Context after ASP Down Ack");
}

static void M3uaAspSessionResetsNegotiatedState()
{
    Span<byte> buffer = stackalloc byte[96];
    M3uaAspSession session = new();
    uint[] routingContexts = [0x00000064];

    Assert(
        M3uaMessageBuilder.BuildAspUpAck(buffer, 0x0000002A, ReadOnlySpan<byte>.Empty, out int written, out string? buildUpError),
        buildUpError ?? "ASP Up Ack build failed");
    Assert(
        session.TryApplyAcknowledgement(DecodeMessage(buffer.Slice(0, written)), out _, out string? upError),
        upError ?? "ASP Up Ack apply failed");

    Assert(
        M3uaMessageBuilder.BuildAspActiveAck(buffer, M3uaTrafficModeType.Loadshare, routingContexts, ReadOnlySpan<byte>.Empty, out written, out string? buildActiveError),
        buildActiveError ?? "ASP Active Ack build failed");
    Assert(
        session.TryApplyAcknowledgement(DecodeMessage(buffer.Slice(0, written)), out _, out string? activeError),
        activeError ?? "ASP Active Ack apply failed");

    AssertEqual(M3uaAspState.Active, session.State, "session state before reset");
    AssertEqual((uint?)0x0000002A, session.AspIdentifier, "session ASP Identifier before reset");
    AssertEqual((M3uaTrafficModeType?)M3uaTrafficModeType.Loadshare, session.TrafficModeType, "session Traffic Mode before reset");
    AssertEqual(1, session.RoutingContexts.Length, "session Routing Context count before reset");

    session.Reset();

    AssertEqual(M3uaAspState.Down, session.State, "session state after reset");
    AssertEqual(null, session.AspIdentifier, "session ASP Identifier after reset");
    AssertEqual(null, session.TrafficModeType, "session Traffic Mode after reset");
    AssertEqual(0, session.RoutingContexts.Length, "session Routing Context count after reset");

    session.Reset(M3uaAspState.Inactive);
    AssertEqual(M3uaAspState.Inactive, session.State, "session explicit reset state");
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

static void M3uaTransportSessionSendsManagementMessages()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);

    session.SendErrorAsync(
        M3uaErrorCode.InvalidRoutingContext,
        new uint[] { 0x00000011 },
        networkAppearance: 0x00000005,
        diagnosticInformation: new byte[] { 0xDE, 0xAD }).GetAwaiter().GetResult();
    session.SendNotifyAsync(
        M3uaNotifyStatusType.ApplicationServerStateChange,
        (ushort)M3uaApplicationServerState.Active,
        aspIdentifier: 0x0000002A,
        routingContexts: new uint[] { 0x00000033 },
        infoString: new byte[] { 0x61, 0x73 }).GetAwaiter().GetResult();

    AssertEqual(2, socket.SentPackets.Count, "Management sent packet count");

    M3uaMessage errorMessage = DecodeMessage(socket.SentPackets[0].Span);
    AssertEqual(M3uaMessageClass.Management, errorMessage.MessageClass, "sent Error class");
    AssertEqual((byte)M3uaManagementMessageType.Error, errorMessage.MessageType, "sent Error type");
    Assert(
        M3uaTypedMessageParser.TryParseError(errorMessage, out M3uaErrorMessage? error, out string? errorParseError),
        errorParseError ?? "sent Error parse failed");
    AssertEqual(M3uaErrorCode.InvalidRoutingContext, error!.ErrorCode, "sent Error code");
    AssertEqual((uint?)0x00000005, error.NetworkAppearance, "sent Error Network Appearance");
    AssertSequence([0x00, 0x00, 0x00, 0x11], UInt32SpanToBytes(error.RoutingContexts), "sent Error Routing Context");
    AssertSequence([0xDE, 0xAD], error.DiagnosticInformation.Span, "sent Error diagnostic");

    M3uaMessage notifyMessage = DecodeMessage(socket.SentPackets[1].Span);
    AssertEqual(M3uaMessageClass.Management, notifyMessage.MessageClass, "sent Notify class");
    AssertEqual((byte)M3uaManagementMessageType.Notify, notifyMessage.MessageType, "sent Notify type");
    Assert(
        M3uaTypedMessageParser.TryParseNotify(notifyMessage, out M3uaNotifyMessage? notify, out string? notifyParseError),
        notifyParseError ?? "sent Notify parse failed");
    AssertEqual(M3uaNotifyStatusType.ApplicationServerStateChange, notify!.StatusType, "sent Notify status type");
    AssertEqual((ushort)M3uaApplicationServerState.Active, notify.StatusInformation, "sent Notify status information");
    AssertEqual((uint?)0x0000002A, notify.AspIdentifier, "sent Notify ASP Identifier");
    AssertSequence([0x00, 0x00, 0x00, 0x33], UInt32SpanToBytes(notify.RoutingContexts), "sent Notify Routing Context");
    AssertSequence([0x61, 0x73], notify.InfoString.Span, "sent Notify Info String");
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

static void M3uaTransportSessionSendsSsnmMessages()
{
    FakeSctpSocket socket = new();
    using M3uaTransportSession session = new(socket, leaveOpen: true);
    uint[] routingContexts = [0x00000055];
    M3uaAffectedPointCode[] affectedPointCodes = [new(mask: 0, pointCode: 0x00112233)];

    session.SendDestinationUnavailableAsync(0x00000007, routingContexts, affectedPointCodes, new byte[] { 0x64, 0x75 }).GetAwaiter().GetResult();
    session.SendDestinationAvailableAsync(0x00000007, routingContexts, affectedPointCodes, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
    session.SendDestinationStateAuditAsync(null, ReadOnlyMemory<uint>.Empty, affectedPointCodes, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
    session.SendDestinationRestrictedAsync(null, ReadOnlyMemory<uint>.Empty, affectedPointCodes, ReadOnlyMemory<byte>.Empty).GetAwaiter().GetResult();
    session.SendDestinationUserPartUnavailableAsync(
        0x00000007,
        routingContexts,
        new M3uaAffectedPointCode(mask: 0, pointCode: 0x00012345),
        M3uaUserPartUnavailableCause.InaccessibleRemoteUser,
        M3uaMtp3UserIdentity.Sccp,
        new byte[] { 0x64, 0x75, 0x70, 0x75 }).GetAwaiter().GetResult();
    session.SendSignallingCongestionAsync(
        0x00000007,
        routingContexts,
        affectedPointCodes,
        concernedDestination: new M3uaAffectedPointCode(mask: 0, pointCode: 0x0000AAAA),
        congestionLevel: 2,
        infoString: new byte[] { 0x73, 0x63, 0x6F, 0x6E }).GetAwaiter().GetResult();

    AssertEqual(6, socket.SentPackets.Count, "SSNM sent packet count");
    AssertCommonSsnmPacket(socket.SentPackets[0].Span, M3uaSsnmMessageType.DestinationUnavailable, "sent DUNA");
    AssertCommonSsnmPacket(socket.SentPackets[1].Span, M3uaSsnmMessageType.DestinationAvailable, "sent DAVA");
    AssertCommonSsnmPacket(socket.SentPackets[2].Span, M3uaSsnmMessageType.DestinationStateAudit, "sent DAUD");
    AssertCommonSsnmPacket(socket.SentPackets[3].Span, M3uaSsnmMessageType.DestinationRestricted, "sent DRST");

    M3uaMessage dupuMessage = DecodeMessage(socket.SentPackets[4].Span);
    Assert(
        M3uaTypedMessageParser.TryParseDestinationUserPartUnavailable(dupuMessage, out M3uaDestinationUserPartUnavailableMessage? dupu, out string? dupuError),
        dupuError ?? "sent DUPU parse failed");
    AssertEqual(M3uaUserPartUnavailableCause.InaccessibleRemoteUser, dupu!.Cause, "sent DUPU cause");
    AssertEqual(M3uaMtp3UserIdentity.Sccp, dupu.UserIdentity, "sent DUPU user");

    M3uaMessage sconMessage = DecodeMessage(socket.SentPackets[5].Span);
    Assert(
        M3uaTypedMessageParser.TryParseSignallingCongestion(sconMessage, out M3uaSignallingCongestionMessage? scon, out string? sconError),
        sconError ?? "sent SCON parse failed");
    AssertEqual((uint?)2, scon!.CongestionLevel, "sent SCON congestion level");
    AssertEqual((uint)0x0000AAAA, scon.ConcernedDestination!.Value.PointCode, "sent SCON concerned destination");
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

static void M3uaExposesRkmResponseConvenienceHelpers()
{
    M3uaRegistrationResponseMessage registration = new(
    [
        new M3uaRegistrationResult(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064),
        new M3uaRegistrationResult(0x0000002B, M3uaRegistrationStatus.ErrorRoutingKeyAlreadyRegistered, 0)
    ]);
    Assert(!registration.AllSuccessful, "mixed registration results should not be all successful");
    Assert(registration.Results[0].IsSuccess, "first registration result should be success");
    Assert(!registration.Results[1].IsSuccess, "second registration result should be failure");
    Assert(registration.TryFindResult(0x0000002A, out M3uaRegistrationResult registrationResult), "registration result should be found");
    AssertEqual((uint)0x00000064, registrationResult.RoutingContext, "found registration Routing Context");
    Assert(registration.TryGetAssignedRoutingContext(0x0000002A, out uint assignedRoutingContext), "assigned Routing Context should be found");
    AssertEqual((uint)0x00000064, assignedRoutingContext, "assigned Routing Context value");
    Assert(!registration.TryGetAssignedRoutingContext(0x0000002B, out _), "failed registration should not expose assigned Routing Context");
    Assert(!registration.TryFindResult(0x0000002C, out _), "missing registration result should not be found");

    M3uaDeregistrationResponseMessage deregistration = new(
    [
        new M3uaDeregistrationResult(0x00000064, M3uaDeregistrationStatus.SuccessfullyDeregistered)
    ]);
    Assert(deregistration.AllSuccessful, "deregistration result should be all successful");
    Assert(deregistration.Results[0].IsSuccess, "deregistration result should be success");
    Assert(deregistration.TryFindResult(0x00000064, out M3uaDeregistrationResult deregistrationResult), "deregistration result should be found");
    AssertEqual(M3uaDeregistrationStatus.SuccessfullyDeregistered, deregistrationResult.Status, "found deregistration status");
    Assert(!deregistration.TryFindResult(0x00000065, out _), "missing deregistration result should not be found");
}

static void M3uaRkmClientRegistersAndDeregistersRoutingKeys()
{
    Span<byte> buffer = stackalloc byte[128];
    FakeSctpSocket socket = new();

    M3uaRegistrationResult[] registrationResults =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064)
    ];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, registrationResults, out int written, out string? registrationBuildError),
        registrationBuildError ?? "REG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaDeregistrationResult[] deregistrationResults =
    [
        new(0x00000064, M3uaDeregistrationStatus.SuccessfullyDeregistered)
    ];
    Assert(
        M3uaMessageBuilder.BuildDeregistrationResponse(buffer, deregistrationResults, out written, out string? deregistrationBuildError),
        deregistrationBuildError ?? "DEREG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession transport = new(socket, leaveOpen: true);
    M3uaRkmClient client = new(transport);
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 0x0000002A,
            routingContext: null,
            trafficModeType: M3uaTrafficModeType.Loadshare,
            destinationPointCodes: [new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)],
            networkAppearance: 0x00000007,
            serviceIndicators: [3],
            originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
    ];

    M3uaRegistrationResponseMessage registration = client.RegisterAsync(routingKeys).GetAwaiter().GetResult();
    M3uaDeregistrationResponseMessage deregistration = client.DeregisterAsync(new uint[] { 0x00000064 }).GetAwaiter().GetResult();

    AssertEqual(1, registration.Results.Length, "RKM client REG RSP result count");
    AssertEqual(M3uaRegistrationStatus.SuccessfullyRegistered, registration.Results[0].Status, "RKM client REG RSP status");
    AssertEqual((uint)0x00000064, registration.Results[0].RoutingContext, "RKM client REG RSP Routing Context");
    AssertEqual(1, deregistration.Results.Length, "RKM client DEREG RSP result count");
    AssertEqual(M3uaDeregistrationStatus.SuccessfullyDeregistered, deregistration.Results[0].Status, "RKM client DEREG RSP status");
    AssertEqual(2, socket.SentPackets.Count, "RKM client sent packet count");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.RegistrationRequest, DecodeMessage(socket.SentPackets[0].Span).MessageType, "RKM client first sent type");
    AssertEqual((byte)M3uaRoutingKeyManagementMessageType.DeregistrationRequest, DecodeMessage(socket.SentPackets[1].Span).MessageType, "RKM client second sent type");
}

static void M3uaRkmClientRequiresSuccessfulResponses()
{
    Span<byte> buffer = stackalloc byte[128];
    FakeSctpSocket socket = new();

    M3uaRegistrationResult[] registrationResults =
    [
        new(0x0000002A, M3uaRegistrationStatus.SuccessfullyRegistered, 0x00000064)
    ];
    Assert(
        M3uaMessageBuilder.BuildRegistrationResponse(buffer, registrationResults, out int written, out string? registrationBuildError),
        registrationBuildError ?? "REG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    M3uaDeregistrationResult[] deregistrationResults =
    [
        new(0x00000064, M3uaDeregistrationStatus.ErrorNotRegistered)
    ];
    Assert(
        M3uaMessageBuilder.BuildDeregistrationResponse(buffer, deregistrationResults, out written, out string? deregistrationBuildError),
        deregistrationBuildError ?? "DEREG RSP build failed");
    socket.QueueReceive(buffer.Slice(0, written).ToArray());

    using M3uaTransportSession transport = new(socket, leaveOpen: true);
    M3uaRkmClient client = new(transport);
    M3uaRoutingKey[] routingKeys =
    [
        new(
            localRoutingKeyIdentifier: 0x0000002A,
            routingContext: null,
            trafficModeType: M3uaTrafficModeType.Loadshare,
            destinationPointCodes: [new M3uaAffectedPointCode(mask: 0, pointCode: 0x00112233)],
            networkAppearance: null,
            serviceIndicators: [3],
            originatingPointCodes: ReadOnlySpan<M3uaAffectedPointCode>.Empty)
    ];

    M3uaRegistrationResponseMessage registration = client.RegisterAndRequireSuccessAsync(routingKeys).GetAwaiter().GetResult();
    Assert(registration.AllSuccessful, "strict registration should return successful response");

    InvalidOperationException exception = AssertThrows<InvalidOperationException>(() =>
        client.DeregisterAndRequireSuccessAsync(new uint[] { 0x00000064 }).GetAwaiter().GetResult());
    Assert(exception.Message.Contains("ErrorNotRegistered", StringComparison.Ordinal), exception.Message);
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

static void AssertCommonSsnmPacket(ReadOnlySpan<byte> packet, M3uaSsnmMessageType expectedType, string label)
{
    M3uaMessage message = DecodeMessage(packet);
    AssertEqual(M3uaMessageClass.Ssnm, message.MessageClass, $"{label} class");
    AssertEqual((byte)expectedType, message.MessageType, $"{label} type");
    Assert(
        M3uaTypedMessageParser.TryParseSsnm(message, out M3uaSsnmMessage? typed, out string? parseError),
        parseError ?? $"{label} parse failed");
    AssertEqual(expectedType, typed!.MessageType, $"{label} typed type");
    AssertEqual(1, typed.AffectedPointCodes.Length, $"{label} affected point-code count");
}

static TException AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException ex)
    {
        return ex;
    }

    throw new InvalidOperationException($"Expected exception {typeof(TException).Name}");
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
