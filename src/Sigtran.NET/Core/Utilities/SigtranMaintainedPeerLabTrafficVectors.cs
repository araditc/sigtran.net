namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies maintained external peer lab traffic vector kinds.
/// </summary>
public enum SigtranMaintainedPeerLabTrafficVectorKind
{
    /// <summary>M3UA ASP lifecycle traffic.</summary>
    AspLifecycle,

    /// <summary>M3UA heartbeat traffic.</summary>
    Heartbeat,

    /// <summary>M3UA DATA traffic.</summary>
    PayloadData
}

/// <summary>
/// Describes one maintained external peer lab traffic vector.
/// </summary>
public sealed class SigtranMaintainedPeerLabTrafficVector
{
    /// <summary>Creates a maintained external peer lab traffic vector.</summary>
    /// <param name="id">The stable vector id.</param>
    /// <param name="kind">The traffic vector kind.</param>
    /// <param name="expectedMessages">The expected ordered message names.</param>
    /// <param name="requiresPayload">Whether the vector requires M3UA DATA payload.</param>
    public SigtranMaintainedPeerLabTrafficVector(
        string id,
        SigtranMaintainedPeerLabTrafficVectorKind kind,
        IReadOnlyList<string> expectedMessages,
        bool requiresPayload)
    {
        ArgumentNullException.ThrowIfNull(expectedMessages);
        Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Traffic vector id is required.", nameof(id)) : id;
        Kind = kind;
        ExpectedMessages = expectedMessages.Count == 0 ? throw new ArgumentException("At least one expected message is required.", nameof(expectedMessages)) : expectedMessages.ToArray();
        RequiresPayload = requiresPayload;
    }

    /// <summary>The stable vector id.</summary>
    public string Id { get; }

    /// <summary>The traffic vector kind.</summary>
    public SigtranMaintainedPeerLabTrafficVectorKind Kind { get; }

    /// <summary>The expected ordered message names.</summary>
    public IReadOnlyList<string> ExpectedMessages { get; }

    /// <summary>Whether the vector requires M3UA DATA payload.</summary>
    public bool RequiresPayload { get; }

    /// <summary>Whether the vector contains enough expected messages for comparison.</summary>
    public bool IsComparable => ExpectedMessages.Count > 0;

    /// <summary>Formats a compact traffic vector summary.</summary>
    /// <returns>The traffic vector summary.</returns>
    public string Describe()
    {
        return $"vector={Id} kind={Kind} messages={ExpectedMessages.Count} payload={RequiresPayload}";
    }
}

/// <summary>
/// Provides maintained external peer lab traffic vectors.
/// </summary>
public static class SigtranMaintainedPeerLabTrafficVectors
{
    /// <summary>Returns the default maintained external peer traffic vectors.</summary>
    /// <returns>The default maintained external peer traffic vectors.</returns>
    public static IReadOnlyList<SigtranMaintainedPeerLabTrafficVector> GetDefault()
    {
        return
        [
            new(
                "m3ua-asp-lifecycle",
                SigtranMaintainedPeerLabTrafficVectorKind.AspLifecycle,
                ["ASPUP", "ASPUP_ACK", "ASPAC", "ASPAC_ACK", "ASPIA", "ASPIA_ACK", "ASPDN", "ASPDN_ACK"],
                requiresPayload: false),
            new(
                "m3ua-heartbeat",
                SigtranMaintainedPeerLabTrafficVectorKind.Heartbeat,
                ["BEAT", "BEAT_ACK"],
                requiresPayload: false),
            new(
                "m3ua-payload-data",
                SigtranMaintainedPeerLabTrafficVectorKind.PayloadData,
                ["DATA"],
                requiresPayload: true)
        ];
    }

    /// <summary>Returns the flattened expected maintained peer message sequence.</summary>
    /// <returns>The expected maintained peer message sequence.</returns>
    public static IReadOnlyList<string> GetExpectedMessageSequence()
    {
        return GetDefault()
            .SelectMany(static vector => vector.ExpectedMessages)
            .ToArray();
    }
}
