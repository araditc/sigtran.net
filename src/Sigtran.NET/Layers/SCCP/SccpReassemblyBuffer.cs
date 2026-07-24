namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Identifies the result of adding one XUDT segment to a reassembly context.
/// </summary>
public enum SccpReassemblyStatus
{
    /// <summary>The segment was accepted and more segments are required.</summary>
    Pending,

    /// <summary>The final segment completed the payload.</summary>
    Complete,

    /// <summary>The segment did not follow the expected remaining-segment value.</summary>
    OutOfSequence,

    /// <summary>The configured context or payload limit was exceeded.</summary>
    CapacityExceeded,

    /// <summary>The segment did not contain a segmentation parameter.</summary>
    NotSegmented
}

/// <summary>
/// Represents the result of an SCCP XUDT reassembly operation.
/// </summary>
public sealed class SccpReassemblyResult
{
    private readonly byte[] _payload;

    internal SccpReassemblyResult(
        SccpReassemblyStatus status,
        uint? localReference,
        byte[]? payload,
        string? error)
    {
        Status = status;
        LocalReference = localReference;
        _payload = payload ?? [];
        Error = error;
    }

    /// <summary>The reassembly outcome.</summary>
    public SccpReassemblyStatus Status { get; }

    /// <summary>The 24-bit local segmentation reference, when present.</summary>
    public uint? LocalReference { get; }

    /// <summary>The completed payload, or an empty value while pending.</summary>
    public ReadOnlyMemory<byte> Payload => _payload;

    /// <summary>The validation error, when present.</summary>
    public string? Error { get; }
}

/// <summary>
/// Maintains bounded, expiring SCCP XUDT reassembly contexts.
/// </summary>
public sealed class SccpReassemblyBuffer
{
    private readonly object _sync = new();
    private readonly Dictionary<ReassemblyKey, ReassemblyContext> _contexts = [];

    /// <summary>Creates a bounded reassembly buffer.</summary>
    /// <param name="maximumContexts">The maximum concurrent incomplete messages.</param>
    /// <param name="maximumPayloadBytes">The maximum bytes in one completed payload.</param>
    /// <param name="timeout">The inactive context timeout.</param>
    public SccpReassemblyBuffer(
        int maximumContexts = 1024,
        int maximumPayloadBytes = 65535,
        TimeSpan? timeout = null)
    {
        if (maximumContexts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumContexts));
        }

        if (maximumPayloadBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPayloadBytes));
        }

        TimeSpan actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        if (actualTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        MaximumContexts = maximumContexts;
        MaximumPayloadBytes = maximumPayloadBytes;
        Timeout = actualTimeout;
    }

    /// <summary>The maximum concurrent incomplete messages.</summary>
    public int MaximumContexts { get; }

    /// <summary>The maximum bytes in one completed payload.</summary>
    public int MaximumPayloadBytes { get; }

    /// <summary>The inactive context timeout.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>The current incomplete context count.</summary>
    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _contexts.Count;
            }
        }
    }

    /// <summary>Adds one decoded XUDT segment.</summary>
    /// <param name="sourcePointCode">The source MTP3 point code.</param>
    /// <param name="message">The decoded XUDT message.</param>
    /// <param name="observedAtUtc">The UTC receive time.</param>
    /// <returns>The reassembly result.</returns>
    public SccpReassemblyResult Add(
        uint sourcePointCode,
        SccpExtendedUnitdataMessage message,
        DateTimeOffset observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observation time must use UTC.", nameof(observedAtUtc));
        }

        SccpSegmentationParameter? segmentation = message.Segmentation;
        if (!segmentation.HasValue)
        {
            return new(
                SccpReassemblyStatus.NotSegmented,
                localReference: null,
                message.UserData.ToArray(),
                error: null);
        }

        SccpSegmentationParameter segment = segmentation.Value;
        ReassemblyKey key = new(
            sourcePointCode,
            segment.LocalReference,
            Convert.ToHexString(message.CalledParty.Encode()),
            Convert.ToHexString(message.CallingParty.Encode()));

        lock (_sync)
        {
            CleanupExpiredCore(observedAtUtc);
            if (segment.FirstSegment)
            {
                if (_contexts.Count >= MaximumContexts && !_contexts.ContainsKey(key))
                {
                    return new(
                        SccpReassemblyStatus.CapacityExceeded,
                        segment.LocalReference,
                        payload: null,
                        "SCCP reassembly context capacity is exhausted.");
                }

                if (message.UserData.Length > MaximumPayloadBytes)
                {
                    return new(
                        SccpReassemblyStatus.CapacityExceeded,
                        segment.LocalReference,
                        payload: null,
                        "SCCP reassembled payload limit was exceeded.");
                }

                if (segment.RemainingSegments == 0)
                {
                    return new(
                        SccpReassemblyStatus.Complete,
                        segment.LocalReference,
                        message.UserData.ToArray(),
                        error: null);
                }

                _contexts[key] = new(
                    message.UserData.ToArray(),
                    expectedRemaining: (byte)(segment.RemainingSegments - 1),
                    observedAtUtc);
                return new(
                    SccpReassemblyStatus.Pending,
                    segment.LocalReference,
                    payload: null,
                    error: null);
            }

            if (!_contexts.TryGetValue(key, out ReassemblyContext? context)
                || context.ExpectedRemaining != segment.RemainingSegments)
            {
                _contexts.Remove(key);
                return new(
                    SccpReassemblyStatus.OutOfSequence,
                    segment.LocalReference,
                    payload: null,
                    "SCCP segment did not match an active ordered reassembly context.");
            }

            if (context.Payload.Count + message.UserData.Length > MaximumPayloadBytes)
            {
                _contexts.Remove(key);
                return new(
                    SccpReassemblyStatus.CapacityExceeded,
                    segment.LocalReference,
                    payload: null,
                    "SCCP reassembled payload limit was exceeded.");
            }

            context.Payload.AddRange(message.UserData.ToArray());
            context.LastObservedAtUtc = observedAtUtc;
            if (segment.RemainingSegments == 0)
            {
                _contexts.Remove(key);
                return new(
                    SccpReassemblyStatus.Complete,
                    segment.LocalReference,
                    context.Payload.ToArray(),
                    error: null);
            }

            context.ExpectedRemaining--;
            return new(
                SccpReassemblyStatus.Pending,
                segment.LocalReference,
                payload: null,
                error: null);
        }
    }

    /// <summary>Removes inactive reassembly contexts.</summary>
    /// <param name="observedAtUtc">The current UTC time.</param>
    /// <returns>The number of expired contexts removed.</returns>
    public int CleanupExpired(DateTimeOffset observedAtUtc)
    {
        if (observedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Observation time must use UTC.", nameof(observedAtUtc));
        }

        lock (_sync)
        {
            return CleanupExpiredCore(observedAtUtc);
        }
    }

    private int CleanupExpiredCore(DateTimeOffset observedAtUtc)
    {
        ReassemblyKey[] expired = _contexts
            .Where(pair => observedAtUtc - pair.Value.LastObservedAtUtc >= Timeout)
            .Select(pair => pair.Key)
            .ToArray();
        foreach (ReassemblyKey key in expired)
        {
            _contexts.Remove(key);
        }

        return expired.Length;
    }

    private sealed class ReassemblyContext
    {
        public ReassemblyContext(
            byte[] firstSegment,
            byte expectedRemaining,
            DateTimeOffset lastObservedAtUtc)
        {
            Payload = new(firstSegment);
            ExpectedRemaining = expectedRemaining;
            LastObservedAtUtc = lastObservedAtUtc;
        }

        public List<byte> Payload { get; }

        public byte ExpectedRemaining { get; set; }

        public DateTimeOffset LastObservedAtUtc { get; set; }
    }

    private readonly record struct ReassemblyKey(
        uint SourcePointCode,
        uint LocalReference,
        string CalledParty,
        string CallingParty);
}
