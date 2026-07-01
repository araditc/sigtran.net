namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Identifies reference external peer lab traffic vector kinds.
/// </summary>
public enum SigtranReferencePeerLabTrafficVectorKind
{
    /// <summary>M3UA ASP lifecycle traffic.</summary>
    AspLifecycle,

    /// <summary>M3UA heartbeat traffic.</summary>
    Heartbeat,

    /// <summary>M3UA DATA traffic.</summary>
    PayloadData
}

/// <summary>
/// Describes one reference external peer lab traffic vector.
/// </summary>
public sealed class SigtranReferencePeerLabTrafficVector
{
    /// <summary>Creates a reference external peer lab traffic vector.</summary>
    /// <param name="id">The stable vector id.</param>
    /// <param name="kind">The traffic vector kind.</param>
    /// <param name="expectedMessages">The expected ordered message names.</param>
    /// <param name="requiresPayload">Whether the vector requires M3UA DATA payload.</param>
    public SigtranReferencePeerLabTrafficVector(
        string id,
        SigtranReferencePeerLabTrafficVectorKind kind,
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
    public SigtranReferencePeerLabTrafficVectorKind Kind { get; }

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
/// Provides reference external peer lab traffic vectors.
/// </summary>
public static class SigtranReferencePeerLabTrafficVectors
{
    /// <summary>Returns the default reference external peer traffic vectors.</summary>
    /// <returns>The default reference external peer traffic vectors.</returns>
    public static IReadOnlyList<SigtranReferencePeerLabTrafficVector> GetDefault()
    {
        return
        [
            new(
                "m3ua-asp-lifecycle",
                SigtranReferencePeerLabTrafficVectorKind.AspLifecycle,
                ["ASPUP", "ASPUP_ACK", "ASPAC", "ASPAC_ACK", "ASPIA", "ASPIA_ACK", "ASPDN", "ASPDN_ACK"],
                requiresPayload: false),
            new(
                "m3ua-heartbeat",
                SigtranReferencePeerLabTrafficVectorKind.Heartbeat,
                ["BEAT", "BEAT_ACK"],
                requiresPayload: false),
            new(
                "m3ua-payload-data",
                SigtranReferencePeerLabTrafficVectorKind.PayloadData,
                ["DATA"],
                requiresPayload: true)
        ];
    }

    /// <summary>Returns the flattened expected reference peer message sequence.</summary>
    /// <returns>The expected reference peer message sequence.</returns>
    public static IReadOnlyList<string> GetExpectedMessageSequence()
    {
        return GetDefault()
            .SelectMany(static vector => vector.ExpectedMessages)
            .ToArray();
    }
}
