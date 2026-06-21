namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// SCTP outbound stream selection modes.
/// </summary>
public enum SctpStreamSelectionMode
{
    /// <summary>Always use one configured stream id.</summary>
    Fixed,
    /// <summary>Select streams by applying modulo to a caller-provided sequence value.</summary>
    RoundRobin
}

/// <summary>
/// Defines how outbound SCTP streams are selected for user messages.
/// </summary>
public sealed class SctpStreamSelectionPolicy
{
    /// <summary>Creates an SCTP stream selection policy.</summary>
    /// <param name="mode">The stream selection mode.</param>
    /// <param name="streamCount">The number of outbound streams available.</param>
    /// <param name="fixedStreamId">The fixed stream id used by fixed mode.</param>
    public SctpStreamSelectionPolicy(
        SctpStreamSelectionMode mode = SctpStreamSelectionMode.Fixed,
        ushort streamCount = 1,
        ushort fixedStreamId = 0)
    {
        if (streamCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(streamCount), "Stream count must be positive.");
        }

        if (fixedStreamId >= streamCount)
        {
            throw new ArgumentOutOfRangeException(nameof(fixedStreamId), "Fixed stream id must be less than stream count.");
        }

        Mode = mode;
        StreamCount = streamCount;
        FixedStreamId = fixedStreamId;
    }

    /// <summary>The stream selection mode.</summary>
    public SctpStreamSelectionMode Mode { get; }

    /// <summary>The number of outbound streams available.</summary>
    public ushort StreamCount { get; }

    /// <summary>The fixed stream id used by fixed mode.</summary>
    public ushort FixedStreamId { get; }

    /// <summary>
    /// Selects an outbound stream id.
    /// </summary>
    /// <param name="sequence">A caller-provided sequence value for round-robin mode.</param>
    /// <returns>The selected outbound stream id.</returns>
    public ushort SelectStream(uint sequence = 0)
    {
        return Mode switch
        {
            SctpStreamSelectionMode.Fixed => FixedStreamId,
            SctpStreamSelectionMode.RoundRobin => (ushort)(sequence % StreamCount),
            _ => FixedStreamId
        };
    }
}
