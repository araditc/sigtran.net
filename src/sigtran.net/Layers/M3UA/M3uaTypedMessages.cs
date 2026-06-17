using System.Buffers.Binary;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Represents a typed ASP State Maintenance message decoded from M3UA.
/// </summary>
public sealed class M3uaAspStateMaintenanceMessage
{
    /// <summary>Creates a typed ASP State Maintenance message.</summary>
    /// <param name="messageType">The ASP State Maintenance message type.</param>
    /// <param name="aspIdentifier">The optional ASP Identifier value.</param>
    /// <param name="infoString">The optional Info String value.</param>
    /// <param name="heartbeatData">The optional Heartbeat Data value.</param>
    public M3uaAspStateMaintenanceMessage(
        M3uaAspsmMessageType messageType,
        uint? aspIdentifier,
        ReadOnlyMemory<byte> infoString,
        ReadOnlyMemory<byte> heartbeatData)
    {
        MessageType = messageType;
        AspIdentifier = aspIdentifier;
        InfoString = infoString;
        HeartbeatData = heartbeatData;
    }

    /// <summary>The ASP State Maintenance message type.</summary>
    public M3uaAspsmMessageType MessageType { get; }

    /// <summary>The optional ASP Identifier value.</summary>
    public uint? AspIdentifier { get; }

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }

    /// <summary>The optional Heartbeat Data value.</summary>
    public ReadOnlyMemory<byte> HeartbeatData { get; }
}

/// <summary>
/// Represents a typed ASP Traffic Maintenance message decoded from M3UA.
/// </summary>
public sealed class M3uaAspTrafficMaintenanceMessage
{
    private readonly uint[] _routingContexts;

    /// <summary>Creates a typed ASP Traffic Maintenance message.</summary>
    /// <param name="messageType">The ASP Traffic Maintenance message type.</param>
    /// <param name="trafficModeType">The optional Traffic Mode Type value.</param>
    /// <param name="routingContexts">The decoded Routing Context values.</param>
    /// <param name="infoString">The optional Info String value.</param>
    public M3uaAspTrafficMaintenanceMessage(
        M3uaAsptmMessageType messageType,
        M3uaTrafficModeType? trafficModeType,
        ReadOnlySpan<uint> routingContexts,
        ReadOnlyMemory<byte> infoString)
    {
        MessageType = messageType;
        TrafficModeType = trafficModeType;
        _routingContexts = routingContexts.ToArray();
        InfoString = infoString;
    }

    /// <summary>The ASP Traffic Maintenance message type.</summary>
    public M3uaAsptmMessageType MessageType { get; }

    /// <summary>The optional Traffic Mode Type value.</summary>
    public M3uaTrafficModeType? TrafficModeType { get; }

    /// <summary>The decoded Routing Context values.</summary>
    public ReadOnlySpan<uint> RoutingContexts => _routingContexts;

    /// <summary>The optional Info String value.</summary>
    public ReadOnlyMemory<byte> InfoString { get; }
}

/// <summary>
/// Converts generic M3UA messages into typed messages used by session state machines.
/// </summary>
public static class M3uaTypedMessageParser
{
    /// <summary>
    /// Parses an ASP State Maintenance message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseAspsm(
        M3uaMessage message,
        out M3uaAspStateMaintenanceMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Aspsm)
        {
            error = $"Expected ASPSM message class, got {message.MessageClass}";
            return false;
        }

        if (!Enum.IsDefined(typeof(M3uaAspsmMessageType), message.MessageType))
        {
            error = $"Unknown ASPSM message type {message.MessageType}";
            return false;
        }

        M3uaAspsmMessageType messageType = (M3uaAspsmMessageType)message.MessageType;
        uint? aspIdentifier = null;
        byte[] infoString = Array.Empty<byte>();
        byte[] heartbeatData = Array.Empty<byte>();
        bool hasInfoString = false;
        bool hasHeartbeatData = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.AspIdentifier:
                    if (aspIdentifier.HasValue)
                    {
                        error = "Duplicate ASP Identifier parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint id, out error))
                    {
                        return false;
                    }

                    aspIdentifier = id;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;

                case M3uaParameterTag.HeartbeatData:
                    if (hasHeartbeatData)
                    {
                        error = "Duplicate Heartbeat Data parameter";
                        return false;
                    }

                    hasHeartbeatData = true;
                    heartbeatData = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        typedMessage = new(messageType, aspIdentifier, infoString, heartbeatData);
        return true;
    }

    /// <summary>
    /// Parses an ASP Traffic Maintenance message.
    /// </summary>
    /// <param name="message">The decoded M3UA message.</param>
    /// <param name="typedMessage">The typed message on success.</param>
    /// <param name="error">An error message if parsing fails.</param>
    /// <returns>True if the message was parsed; otherwise false.</returns>
    public static bool TryParseAsptm(
        M3uaMessage message,
        out M3uaAspTrafficMaintenanceMessage? typedMessage,
        out string? error)
    {
        typedMessage = null;
        error = null;

        if (message.MessageClass != M3uaMessageClass.Asptm)
        {
            error = $"Expected ASPTM message class, got {message.MessageClass}";
            return false;
        }

        if (!Enum.IsDefined(typeof(M3uaAsptmMessageType), message.MessageType))
        {
            error = $"Unknown ASPTM message type {message.MessageType}";
            return false;
        }

        M3uaAsptmMessageType messageType = (M3uaAsptmMessageType)message.MessageType;
        M3uaTrafficModeType? trafficModeType = null;
        uint[] routingContexts = Array.Empty<uint>();
        byte[] infoString = Array.Empty<byte>();
        bool hasRoutingContext = false;
        bool hasInfoString = false;

        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            switch (parameter.Tag)
            {
                case M3uaParameterTag.TrafficModeType:
                    if (trafficModeType.HasValue)
                    {
                        error = "Duplicate Traffic Mode Type parameter";
                        return false;
                    }

                    if (!TryReadUInt32(parameter.Value, parameter.Tag, out uint trafficMode, out error))
                    {
                        return false;
                    }

                    if (!Enum.IsDefined(typeof(M3uaTrafficModeType), trafficMode))
                    {
                        error = $"Unknown Traffic Mode Type {trafficMode}";
                        return false;
                    }

                    trafficModeType = (M3uaTrafficModeType)trafficMode;
                    break;

                case M3uaParameterTag.RoutingContext:
                    if (hasRoutingContext)
                    {
                        error = "Duplicate Routing Context parameter";
                        return false;
                    }

                    if (!TryReadUInt32List(parameter.Value, parameter.Tag, out routingContexts, out error))
                    {
                        return false;
                    }

                    hasRoutingContext = true;
                    break;

                case M3uaParameterTag.InfoString:
                    if (hasInfoString)
                    {
                        error = "Duplicate Info String parameter";
                        return false;
                    }

                    hasInfoString = true;
                    infoString = parameter.Value.ToArray();
                    break;
            }
        }

        if (error is not null)
        {
            return false;
        }

        typedMessage = new(messageType, trafficModeType, routingContexts, infoString);
        return true;
    }

    private static bool TryReadUInt32(
        ReadOnlySpan<byte> value,
        M3uaParameterTag tag,
        out uint result,
        out string? error)
    {
        result = 0;
        error = null;
        if (value.Length != sizeof(uint))
        {
            error = $"{tag} parameter must contain exactly 4 bytes";
            return false;
        }

        result = BinaryPrimitives.ReadUInt32BigEndian(value);
        return true;
    }

    private static bool TryReadUInt32List(
        ReadOnlySpan<byte> value,
        M3uaParameterTag tag,
        out uint[] result,
        out string? error)
    {
        result = Array.Empty<uint>();
        error = null;

        if (value.IsEmpty || (value.Length % sizeof(uint)) != 0)
        {
            error = $"{tag} parameter length must be a non-empty multiple of 4 bytes";
            return false;
        }

        result = new uint[value.Length / sizeof(uint)];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = BinaryPrimitives.ReadUInt32BigEndian(value.Slice(i * sizeof(uint), sizeof(uint)));
        }

        return true;
    }
}
