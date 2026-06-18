namespace sigtran.net.Layers.MAP;

/// <summary>
/// MAP SMS error codes used by the SDK profile.
/// </summary>
public enum MapSmsErrorCode : byte
{
    /// <summary>Unknown subscriber.</summary>
    UnknownSubscriber = 1,

    /// <summary>Absent subscriber for SMS.</summary>
    AbsentSubscriberForShortMessage = 6,

    /// <summary>Call barred.</summary>
    CallBarred = 13,

    /// <summary>Facility not supported.</summary>
    FacilityNotSupported = 21,

    /// <summary>System failure.</summary>
    SystemFailure = 34,

    /// <summary>Data missing.</summary>
    DataMissing = 35,

    /// <summary>Unexpected data value.</summary>
    UnexpectedDataValue = 36
}

/// <summary>
/// Maps MAP SMS errors to delivery status categories.
/// </summary>
public static class MapSmsErrorMapper
{
    /// <summary>Maps a MAP SMS error to a delivery status.</summary>
    /// <param name="errorCode">The MAP SMS error code.</param>
    /// <returns>The mapped delivery status.</returns>
    public static MapSmsDeliveryStatus ToDeliveryStatus(MapSmsErrorCode errorCode)
    {
        return errorCode switch
        {
            MapSmsErrorCode.AbsentSubscriberForShortMessage => MapSmsDeliveryStatus.AbsentSubscriber,
            MapSmsErrorCode.SystemFailure => MapSmsDeliveryStatus.UnknownFailure,
            MapSmsErrorCode.DataMissing => MapSmsDeliveryStatus.EquipmentProtocolError,
            MapSmsErrorCode.UnexpectedDataValue => MapSmsDeliveryStatus.EquipmentProtocolError,
            _ => MapSmsDeliveryStatus.UnknownFailure
        };
    }
}

/// <summary>
/// Represents MAP SMS extension parameters.
/// </summary>
public sealed class MapSmsExtensionContainer
{
    private readonly MapSmsParameterSet _parameters = new();

    /// <summary>Adds an extension parameter.</summary>
    /// <param name="tagNumber">The context-specific extension tag.</param>
    /// <param name="value">The extension value bytes.</param>
    public void Add(byte tagNumber, ReadOnlySpan<byte> value)
    {
        _parameters.Add(tagNumber, value);
    }

    /// <summary>Encodes the extension container.</summary>
    /// <returns>The encoded extension bytes.</returns>
    public byte[] Encode()
    {
        return _parameters.Encode();
    }

    /// <summary>Returns extension parameter snapshot.</summary>
    /// <returns>The extension parameters.</returns>
    public IReadOnlyList<MapSmsParameter> Snapshot()
    {
        return _parameters.Snapshot();
    }

    /// <summary>Attempts to decode an extension container.</summary>
    /// <param name="data">The encoded extension bytes.</param>
    /// <param name="container">The decoded container on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True when decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapSmsExtensionContainer? container, out string? error)
    {
        container = null;
        if (!MapSmsParameterSet.TryDecode(data, out MapSmsParameterSet? set, out error))
        {
            return false;
        }

        container = new();
        foreach (MapSmsParameter parameter in set!.Snapshot())
        {
            container.Add(parameter.TagNumber, parameter.Value.Span);
        }

        return true;
    }
}
