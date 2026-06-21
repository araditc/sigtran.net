using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Provides MAP SMS application context object identifiers.
/// </summary>
public static class MapSmsApplicationContexts
{
    /// <summary>MAP SMS application context placeholder used by the SDK SMS profile foundation.</summary>
    public static TcapObjectIdentifier SmsApplicationContextV3 => new(0, 0, 17, 773, 1, 1, 1);
}

/// <summary>
/// Builds high-level MAP SMS TCAP transactions.
/// </summary>
public sealed class MapSmsTcapClient
{
    private readonly TcapSessionBuilder _builder;
    private readonly TcapObjectIdentifier _applicationContext;

    /// <summary>Creates a MAP SMS TCAP client.</summary>
    /// <param name="builder">The TCAP session builder.</param>
    /// <param name="applicationContext">The MAP SMS application context.</param>
    public MapSmsTcapClient(TcapSessionBuilder? builder = null, TcapObjectIdentifier? applicationContext = null)
    {
        _builder = builder ?? new TcapSessionBuilder();
        _applicationContext = applicationContext ?? MapSmsApplicationContexts.SmsApplicationContextV3;
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

    private TcapBuiltInvoke Begin(MapSmsOperationCode operationCode, byte[] parameters)
    {
        return _builder.BeginInvoke(_applicationContext, (TcapOperationCode)operationCode, parameters);
    }
}
