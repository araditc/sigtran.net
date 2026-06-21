using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// MAP SMS operation codes used by the SDK SMS profile.
/// </summary>
public enum MapSmsOperationCode : byte
{
    /// <summary>MT-ForwardSM operation.</summary>
    MtForwardShortMessage = 44,

    /// <summary>SendRoutingInfoForSM operation.</summary>
    SendRoutingInfoForShortMessage = 45,

    /// <summary>MO-ForwardSM operation.</summary>
    MoForwardShortMessage = 46,

    /// <summary>ReportSM-DeliveryStatus operation.</summary>
    ReportShortMessageDeliveryStatus = 47,

    /// <summary>AlertServiceCentre operation.</summary>
    AlertServiceCentre = 64
}

/// <summary>
/// Describes one MAP SMS operation.
/// </summary>
public readonly struct MapSmsOperationMetadata
{
    /// <summary>Creates MAP SMS operation metadata.</summary>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="name">The operation name.</param>
    /// <param name="expectsResult">Whether the operation expects a result.</param>
    public MapSmsOperationMetadata(MapSmsOperationCode operationCode, string name, bool expectsResult)
    {
        OperationCode = operationCode;
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Operation name is required.", nameof(name)) : name;
        ExpectsResult = expectsResult;
    }

    /// <summary>The operation code.</summary>
    public MapSmsOperationCode OperationCode { get; }

    /// <summary>The operation name.</summary>
    public string Name { get; }

    /// <summary>Whether the operation expects a result.</summary>
    public bool ExpectsResult { get; }
}

/// <summary>
/// Provides MAP SMS operation metadata.
/// </summary>
public static class MapSmsOperationCatalog
{
    private static readonly MapSmsOperationMetadata[] Operations =
    [
        new(MapSmsOperationCode.MtForwardShortMessage, "mt-ForwardSM", expectsResult: true),
        new(MapSmsOperationCode.SendRoutingInfoForShortMessage, "sendRoutingInfoForSM", expectsResult: true),
        new(MapSmsOperationCode.MoForwardShortMessage, "mo-ForwardSM", expectsResult: true),
        new(MapSmsOperationCode.ReportShortMessageDeliveryStatus, "reportSM-DeliveryStatus", expectsResult: true),
        new(MapSmsOperationCode.AlertServiceCentre, "alertServiceCentre", expectsResult: true)
    ];

    /// <summary>Gets all supported MAP SMS operations.</summary>
    /// <returns>The supported operation metadata.</returns>
    public static IReadOnlyList<MapSmsOperationMetadata> GetSupportedOperations()
    {
        return Operations;
    }

    /// <summary>Attempts to find operation metadata by operation code.</summary>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="metadata">The metadata on success.</param>
    /// <returns>True when the operation is known; otherwise false.</returns>
    public static bool TryGet(MapSmsOperationCode operationCode, out MapSmsOperationMetadata metadata)
    {
        foreach (MapSmsOperationMetadata candidate in Operations)
        {
            if (candidate.OperationCode == operationCode)
            {
                metadata = candidate;
                return true;
            }
        }

        metadata = default;
        return false;
    }
}

/// <summary>
/// Represents a BER context-specific MAP SMS parameter.
/// </summary>
public readonly struct MapSmsParameter
{
    /// <summary>Creates a MAP SMS parameter.</summary>
    /// <param name="tagNumber">The context-specific tag number.</param>
    /// <param name="value">The parameter value bytes.</param>
    public MapSmsParameter(byte tagNumber, ReadOnlyMemory<byte> value)
    {
        if (tagNumber > 30)
        {
            throw new ArgumentOutOfRangeException(nameof(tagNumber), "MAP SMS parameters use BER short-form context tags.");
        }

        TagNumber = tagNumber;
        Value = value;
    }

    /// <summary>The context-specific tag number.</summary>
    public byte TagNumber { get; }

    /// <summary>The parameter value bytes.</summary>
    public ReadOnlyMemory<byte> Value { get; }
}

/// <summary>
/// Represents an ordered set of MAP SMS parameters.
/// </summary>
public sealed class MapSmsParameterSet
{
    private readonly List<MapSmsParameter> _parameters = [];

    /// <summary>Adds a parameter to the set.</summary>
    /// <param name="tagNumber">The context-specific tag number.</param>
    /// <param name="value">The parameter value bytes.</param>
    public void Add(byte tagNumber, ReadOnlySpan<byte> value)
    {
        _parameters.Add(new MapSmsParameter(tagNumber, value.ToArray()));
    }

    /// <summary>Returns the parameter snapshot.</summary>
    /// <returns>The parameter snapshot.</returns>
    public IReadOnlyList<MapSmsParameter> Snapshot()
    {
        return _parameters.ToArray();
    }

    /// <summary>Encodes the parameter set as BER context-specific TLVs.</summary>
    /// <returns>The encoded parameter bytes.</returns>
    public byte[] Encode()
    {
        byte[] result = new byte[1024];
        int offset = 0;
        foreach (MapSmsParameter parameter in _parameters)
        {
            TcapBerTag tag = new(TcapBerTagClass.ContextSpecific, constructed: false, parameter.TagNumber);
            if (!TcapBer.TryWriteElement(result.AsSpan(offset), tag, parameter.Value.Span, out int written, out string? error))
            {
                throw new InvalidOperationException(error);
            }

            offset += written;
        }

        Array.Resize(ref result, offset);
        return result;
    }

    /// <summary>Attempts to decode a parameter set.</summary>
    /// <param name="data">The encoded parameter bytes.</param>
    /// <param name="parameters">The decoded parameter set on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out MapSmsParameterSet? parameters, out string? error)
    {
        parameters = new();
        error = null;
        while (!data.IsEmpty)
        {
            if (!TcapBer.TryReadElement(data, out TcapBerElement element, out error))
            {
                parameters = null;
                return false;
            }

            if (element.Tag.TagClass != TcapBerTagClass.ContextSpecific || element.Tag.Constructed)
            {
                error = "MAP SMS parameter must use a primitive context-specific tag.";
                parameters = null;
                return false;
            }

            parameters.Add(element.Tag.Number, element.Value.Span);
            data = data[element.TotalLength..];
        }

        return true;
    }
}
