using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Provides MAP SMS application context object identifiers.
/// </summary>
public static class MapSmsApplicationContexts
{
    /// <summary>The MAP shortMsgGatewayContext-v3 object identifier.</summary>
    public static TcapObjectIdentifier ShortMessageGatewayV3 =>
        new(0, 4, 0, 0, 1, 0, 20, 3);

    /// <summary>The MAP shortMsgMO-RelayContext-v3 object identifier.</summary>
    public static TcapObjectIdentifier ShortMessageMobileOriginatedRelayV3 =>
        new(0, 4, 0, 0, 1, 0, 21, 3);

    /// <summary>The MAP shortMsgAlertContext-v2 object identifier.</summary>
    public static TcapObjectIdentifier ShortMessageAlertV2 =>
        new(0, 4, 0, 0, 1, 0, 23, 2);

    /// <summary>The MAP shortMsgMT-RelayContext-v3 object identifier.</summary>
    public static TcapObjectIdentifier ShortMessageMobileTerminatedRelayV3 =>
        new(0, 4, 0, 0, 1, 0, 25, 3);

    /// <summary>
    /// Gets the legacy default MAP SMS context, retained for source compatibility.
    /// </summary>
    public static TcapObjectIdentifier SmsApplicationContextV3 =>
        ShortMessageGatewayV3;
}

/// <summary>
/// Builds high-level MAP SMS TCAP transactions.
/// </summary>
public sealed class MapSmsTcapClient
{
    private readonly TcapSessionBuilder _builder;
    private readonly TcapObjectIdentifier? _applicationContextOverride;

    /// <summary>Creates a MAP SMS TCAP client.</summary>
    /// <param name="builder">The TCAP session builder.</param>
    /// <param name="applicationContext">The MAP SMS application context.</param>
    public MapSmsTcapClient(TcapSessionBuilder? builder = null, TcapObjectIdentifier? applicationContext = null)
    {
        _builder = builder ?? new TcapSessionBuilder();
        _applicationContextOverride = applicationContext;
    }

    /// <summary>Builds a MO-ForwardSM Begin/Invoke transaction.</summary>
    /// <param name="message">The MO-ForwardSM parameters.</param>
    /// <returns>The built TCAP invoke transaction.</returns>
    public TcapBuiltInvoke BeginMoForwardShortMessage(MapMoForwardShortMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Begin(MapSmsOperationCode.MoForwardShortMessage, message.Encode());
    }

    /// <summary>Builds a MT-ForwardSM Begin/Invoke transaction.</summary>
    /// <param name="message">The MT-ForwardSM parameters.</param>
    /// <returns>The built TCAP invoke transaction.</returns>
    public TcapBuiltInvoke BeginMtForwardShortMessage(MapMtForwardShortMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Begin(MapSmsOperationCode.MtForwardShortMessage, message.Encode());
    }

    /// <summary>Builds a SendRoutingInfoForSM Begin/Invoke transaction.</summary>
    /// <param name="message">The SRI-SM parameters.</param>
    /// <returns>The built TCAP invoke transaction.</returns>
    public TcapBuiltInvoke BeginSendRoutingInfoForShortMessage(MapSendRoutingInfoForShortMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Begin(MapSmsOperationCode.SendRoutingInfoForShortMessage, message.Encode());
    }

    /// <summary>Builds an AlertServiceCentre Begin/Invoke transaction.</summary>
    /// <param name="message">The AlertServiceCentre parameters.</param>
    /// <returns>The built TCAP invoke transaction.</returns>
    public TcapBuiltInvoke BeginAlertServiceCentre(MapAlertServiceCentre message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Begin(MapSmsOperationCode.AlertServiceCentre, message.Encode());
    }

    /// <summary>Builds a ReportSM-DeliveryStatus Begin/Invoke transaction.</summary>
    /// <param name="message">The ReportSM-DeliveryStatus parameters.</param>
    /// <returns>The built TCAP invoke transaction.</returns>
    public TcapBuiltInvoke BeginReportShortMessageDeliveryStatus(
        MapReportShortMessageDeliveryStatus message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return Begin(
            MapSmsOperationCode.ReportShortMessageDeliveryStatus,
            message.Encode());
    }

    private TcapBuiltInvoke Begin(MapSmsOperationCode operationCode, byte[] parameters)
    {
        TcapObjectIdentifier applicationContext =
            _applicationContextOverride
            ?? MapSmsOperationProfiles.Get(operationCode).ApplicationContext;
        return _builder.BeginInvoke(
            applicationContext,
            (TcapOperationCode)operationCode,
            parameters);
    }
}
