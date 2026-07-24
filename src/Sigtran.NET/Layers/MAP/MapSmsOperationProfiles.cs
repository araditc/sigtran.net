using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Defines the MAP application context and timeout policy for one SMS operation.
/// </summary>
public sealed class MapSmsOperationProfile
{
    /// <summary>Creates a MAP SMS operation profile.</summary>
    /// <param name="operationCode">The MAP local operation code.</param>
    /// <param name="name">The standardized operation name.</param>
    /// <param name="applicationContext">The MAP application-context-name.</param>
    /// <param name="timeout">The default invoke timeout.</param>
    public MapSmsOperationProfile(
        MapSmsOperationCode operationCode,
        string name,
        TcapObjectIdentifier applicationContext,
        TimeSpan timeout)
    {
        OperationCode = operationCode;
        Name = string.IsNullOrWhiteSpace(name)
            ? throw new ArgumentException("Operation name is required.", nameof(name))
            : name;
        ApplicationContext = applicationContext
            ?? throw new ArgumentNullException(nameof(applicationContext));
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        Timeout = timeout;
    }

    /// <summary>The MAP local operation code.</summary>
    public MapSmsOperationCode OperationCode { get; }

    /// <summary>The standardized operation name.</summary>
    public string Name { get; }

    /// <summary>The MAP application-context-name.</summary>
    public TcapObjectIdentifier ApplicationContext { get; }

    /// <summary>The default invoke timeout.</summary>
    public TimeSpan Timeout { get; }
}

/// <summary>
/// Provides standardized MAP SMS operation profiles.
/// </summary>
public static class MapSmsOperationProfiles
{
    private static readonly IReadOnlyDictionary<MapSmsOperationCode, MapSmsOperationProfile>
        Profiles = new Dictionary<MapSmsOperationCode, MapSmsOperationProfile>
        {
            [MapSmsOperationCode.SendRoutingInfoForShortMessage] = new(
                MapSmsOperationCode.SendRoutingInfoForShortMessage,
                "sendRoutingInfoForSM",
                MapSmsApplicationContexts.ShortMessageGatewayV3,
                TimeSpan.FromSeconds(15)),
            [MapSmsOperationCode.ReportShortMessageDeliveryStatus] = new(
                MapSmsOperationCode.ReportShortMessageDeliveryStatus,
                "reportSM-DeliveryStatus",
                MapSmsApplicationContexts.ShortMessageGatewayV3,
                TimeSpan.FromSeconds(15)),
            [MapSmsOperationCode.MoForwardShortMessage] = new(
                MapSmsOperationCode.MoForwardShortMessage,
                "mo-ForwardSM",
                MapSmsApplicationContexts.ShortMessageMobileOriginatedRelayV3,
                TimeSpan.FromSeconds(30)),
            [MapSmsOperationCode.MtForwardShortMessage] = new(
                MapSmsOperationCode.MtForwardShortMessage,
                "mt-ForwardSM",
                MapSmsApplicationContexts.ShortMessageMobileTerminatedRelayV3,
                TimeSpan.FromSeconds(30)),
            [MapSmsOperationCode.AlertServiceCentre] = new(
                MapSmsOperationCode.AlertServiceCentre,
                "alertServiceCentre",
                MapSmsApplicationContexts.ShortMessageAlertV2,
                TimeSpan.FromSeconds(15))
        };

    /// <summary>Gets all built-in MAP SMS operation profiles.</summary>
    /// <returns>The immutable profile snapshot.</returns>
    public static IReadOnlyList<MapSmsOperationProfile> GetAll()
    {
        return Profiles.Values.OrderBy(profile => profile.OperationCode).ToArray();
    }

    /// <summary>Gets the profile for a MAP SMS operation.</summary>
    /// <param name="operationCode">The operation code.</param>
    /// <returns>The matching operation profile.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the operation is not part of the built-in SMS profile.
    /// </exception>
    public static MapSmsOperationProfile Get(MapSmsOperationCode operationCode)
    {
        return Profiles.TryGetValue(operationCode, out MapSmsOperationProfile? profile)
            ? profile
            : throw new ArgumentOutOfRangeException(
                nameof(operationCode),
                operationCode,
                "The MAP SMS operation is not supported.");
    }

    /// <summary>Attempts to get a profile from a TCAP local operation code.</summary>
    /// <param name="operationCode">The TCAP local operation code.</param>
    /// <param name="profile">The matching profile on success.</param>
    /// <returns>True when the operation belongs to the built-in SMS profile.</returns>
    public static bool TryGet(
        TcapOperationCode operationCode,
        out MapSmsOperationProfile? profile)
    {
        return Profiles.TryGetValue((MapSmsOperationCode)operationCode, out profile);
    }
}
