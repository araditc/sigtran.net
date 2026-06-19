using sigtran.net.Layers.MAP;
using sigtran.net.Layers.TCAP;

namespace sigtran.net.Core.Utilities;

/// <summary>
/// Builds deterministic MAP SMS simulator flows backed by the SDK MAP and TCAP encoders.
/// </summary>
public sealed class MapSmsSimulatorFlowBuilder
{
    private readonly SigtranSimulatorScript _script = new();
    private readonly TcapSessionBuilder _tcap = new();
    private readonly SigtranSimulatorEndpoint _msc;
    private readonly SigtranSimulatorEndpoint _hlr;
    private readonly SigtranSimulatorEndpoint _smsc;

    /// <summary>Creates a MAP SMS simulator flow builder.</summary>
    /// <param name="mscName">The MSC endpoint name.</param>
    /// <param name="hlrName">The HLR endpoint name.</param>
    /// <param name="smscName">The SMSC endpoint name.</param>
    public MapSmsSimulatorFlowBuilder(string mscName = "msc", string hlrName = "hlr", string smscName = "smsc")
    {
        _msc = new SigtranSimulatorEndpoint(mscName, SigtranSimulatorRole.Asp);
        _hlr = new SigtranSimulatorEndpoint(hlrName, SigtranSimulatorRole.SignallingGateway);
        _smsc = new SigtranSimulatorEndpoint(smscName, SigtranSimulatorRole.Asp);
    }

    /// <summary>Adds a mobile-originated ForwardSM invoke from MSC to SMSC.</summary>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="originatingAddress">The originating subscriber address.</param>
    /// <param name="shortMessageTpdu">The short-message TPDU bytes.</param>
    /// <returns>The current builder.</returns>
    public MapSmsSimulatorFlowBuilder AddMobileOriginatedForwardShortMessage(
        MapSmsAddress serviceCentreAddress,
        MapSmsAddress originatingAddress,
        ReadOnlySpan<byte> shortMessageTpdu)
    {
        MapMoForwardShortMessage message = new(serviceCentreAddress, originatingAddress, shortMessageTpdu.ToArray());
        TcapBuiltInvoke invoke = _tcap.BeginInvoke(
            MapSmsApplicationContexts.SmsApplicationContextV3,
            (TcapOperationCode)MapSmsOperationCode.MoForwardShortMessage,
            message.Encode());

        Add(_msc, _smsc, invoke.EncodedMessage, "MAP mo-ForwardSM");
        return this;
    }

    /// <summary>Adds a SendRoutingInfoForSM invoke from SMSC to HLR.</summary>
    /// <param name="subscriberAddress">The destination subscriber address.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="gprsSupportIndicator">Whether GPRS delivery information is requested.</param>
    /// <returns>The current builder.</returns>
    public MapSmsSimulatorFlowBuilder AddSendRoutingInfoForShortMessage(
        MapSmsAddress subscriberAddress,
        MapSmsAddress serviceCentreAddress,
        bool gprsSupportIndicator = false)
    {
        MapSendRoutingInfoForShortMessage message = new(subscriberAddress, serviceCentreAddress, gprsSupportIndicator);
        TcapBuiltInvoke invoke = _tcap.BeginInvoke(
            MapSmsApplicationContexts.SmsApplicationContextV3,
            (TcapOperationCode)MapSmsOperationCode.SendRoutingInfoForShortMessage,
            message.Encode());

        Add(_smsc, _hlr, invoke.EncodedMessage, "MAP sendRoutingInfoForSM");
        return this;
    }

    /// <summary>Adds a mobile-terminated ForwardSM invoke from SMSC to MSC.</summary>
    /// <param name="subscriberIdentity">The destination subscriber identity.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="shortMessageTpdu">The short-message TPDU bytes.</param>
    /// <returns>The current builder.</returns>
    public MapSmsSimulatorFlowBuilder AddMobileTerminatedForwardShortMessage(
        MapSmsAddress subscriberIdentity,
        MapSmsAddress serviceCentreAddress,
        ReadOnlySpan<byte> shortMessageTpdu)
    {
        MapMtForwardShortMessage message = new(subscriberIdentity, serviceCentreAddress, shortMessageTpdu.ToArray());
        TcapBuiltInvoke invoke = _tcap.BeginInvoke(
            MapSmsApplicationContexts.SmsApplicationContextV3,
            (TcapOperationCode)MapSmsOperationCode.MtForwardShortMessage,
            message.Encode());

        Add(_smsc, _msc, invoke.EncodedMessage, "MAP mt-ForwardSM");
        return this;
    }

    /// <summary>Adds a ReportSM-DeliveryStatus invoke from HLR to SMSC.</summary>
    /// <param name="subscriberAddress">The subscriber address.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <param name="status">The delivery status.</param>
    /// <returns>The current builder.</returns>
    public MapSmsSimulatorFlowBuilder AddReportShortMessageDeliveryStatus(
        MapSmsAddress subscriberAddress,
        MapSmsAddress serviceCentreAddress,
        MapSmsDeliveryStatus status)
    {
        MapReportShortMessageDeliveryStatus message = new(subscriberAddress, serviceCentreAddress, status);
        TcapBuiltInvoke invoke = _tcap.BeginInvoke(
            MapSmsApplicationContexts.SmsApplicationContextV3,
            (TcapOperationCode)MapSmsOperationCode.ReportShortMessageDeliveryStatus,
            message.Encode());

        Add(_hlr, _smsc, invoke.EncodedMessage, "MAP reportSM-DeliveryStatus");
        return this;
    }

    /// <summary>Adds an AlertServiceCentre invoke from HLR to SMSC.</summary>
    /// <param name="subscriberAddress">The subscriber address.</param>
    /// <param name="serviceCentreAddress">The service centre address.</param>
    /// <returns>The current builder.</returns>
    public MapSmsSimulatorFlowBuilder AddAlertServiceCentre(
        MapSmsAddress subscriberAddress,
        MapSmsAddress serviceCentreAddress)
    {
        MapAlertServiceCentre message = new(subscriberAddress, serviceCentreAddress);
        TcapBuiltInvoke invoke = _tcap.BeginInvoke(
            MapSmsApplicationContexts.SmsApplicationContextV3,
            (TcapOperationCode)MapSmsOperationCode.AlertServiceCentre,
            message.Encode());

        Add(_hlr, _smsc, invoke.EncodedMessage, "MAP alertServiceCentre");
        return this;
    }

    /// <summary>Builds the simulator script.</summary>
    /// <returns>The deterministic simulator script.</returns>
    public SigtranSimulatorScript Build()
    {
        SigtranSimulatorScript copy = new();
        foreach (SigtranSimulatorStep step in _script.Snapshot())
        {
            copy.Add(step);
        }

        return copy;
    }

    private void Add(SigtranSimulatorEndpoint from, SigtranSimulatorEndpoint to, ReadOnlyMemory<byte> payload, string description)
    {
        _script.Add(new SigtranSimulatorStep(from, to, "TCAP/MAP", payload, description));
    }
}
