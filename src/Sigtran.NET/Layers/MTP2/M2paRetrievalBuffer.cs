namespace Sigtran.NET.Layers.MTP2;

/// <summary>
/// Represents one unacknowledged M2PA User Data message retained for changeover retrieval.
/// </summary>
public sealed class M2paRetrievalEntry
{
    /// <summary>Creates a retrieval entry.</summary>
    /// <param name="forwardSequenceNumber">The User Data FSN.</param>
    /// <param name="payload">The retained MTP3 service data unit.</param>
    /// <param name="sentAtUtc">The UTC send time.</param>
    public M2paRetrievalEntry(
        uint forwardSequenceNumber,
        ReadOnlyMemory<byte> payload,
        DateTimeOffset sentAtUtc)
    {
        M2paProtocol.ValidateSequenceNumber(
            forwardSequenceNumber,
            nameof(forwardSequenceNumber));
        if (payload.IsEmpty)
        {
            throw new ArgumentException(
                "Retrieval entries require non-empty User Data.",
                nameof(payload));
        }

        if (sentAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Send time must use UTC.", nameof(sentAtUtc));
        }

        ForwardSequenceNumber = forwardSequenceNumber;
        Payload = payload.ToArray();
        SentAtUtc = sentAtUtc;
    }

    /// <summary>The User Data FSN.</summary>
    public uint ForwardSequenceNumber { get; }

    /// <summary>The retained MTP3 service data unit.</summary>
    public ReadOnlyMemory<byte> Payload { get; }

    /// <summary>The UTC send time.</summary>
    public DateTimeOffset SentAtUtc { get; }
}

/// <summary>
/// Retains sent M2PA User Data until the peer acknowledges it.
/// </summary>
public sealed class M2paRetrievalBuffer
{
    private readonly object _sync = new();
    private readonly LinkedList<M2paRetrievalEntry> _entries = [];

    /// <summary>Creates a bounded retrieval buffer.</summary>
    /// <param name="capacity">The maximum unacknowledged User Data messages.</param>
    public M2paRetrievalBuffer(int capacity = 4096)
    {
        Capacity = capacity > 0
            ? capacity
            : throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Retrieval capacity must be positive.");
    }

    /// <summary>The maximum unacknowledged User Data messages.</summary>
    public int Capacity { get; }

    /// <summary>The current unacknowledged message count.</summary>
    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>Adds one sent User Data message.</summary>
    /// <param name="entry">The retrieval entry.</param>
    public void Add(M2paRetrievalEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        lock (_sync)
        {
            if (_entries.Count >= Capacity)
            {
                throw new InvalidOperationException(
                    $"M2PA retrieval buffer capacity {Capacity} is exhausted.");
            }

            if (_entries.Any(existing =>
                    existing.ForwardSequenceNumber == entry.ForwardSequenceNumber))
            {
                throw new InvalidOperationException(
                    $"M2PA FSN {entry.ForwardSequenceNumber} is already retained.");
            }

            _entries.AddLast(entry);
        }
    }

    /// <summary>Removes entries up to and including a peer BSN.</summary>
    /// <param name="backwardSequenceNumber">The peer acknowledgement BSN.</param>
    /// <returns>The number of acknowledged entries removed.</returns>
    public int AcknowledgeThrough(uint backwardSequenceNumber)
    {
        M2paProtocol.ValidateSequenceNumber(
            backwardSequenceNumber,
            nameof(backwardSequenceNumber));
        lock (_sync)
        {
            LinkedListNode<M2paRetrievalEntry>? acknowledged =
                _entries.First;
            while (acknowledged is not null
                && acknowledged.Value.ForwardSequenceNumber
                    != backwardSequenceNumber)
            {
                acknowledged = acknowledged.Next;
            }

            if (acknowledged is null)
            {
                return 0;
            }

            int removed = 0;
            while (_entries.First is not null)
            {
                uint fsn = _entries.First.Value.ForwardSequenceNumber;
                _entries.RemoveFirst();
                removed++;
                if (fsn == backwardSequenceNumber)
                {
                    break;
                }
            }

            return removed;
        }
    }

    /// <summary>Returns unacknowledged User Data in transmission order.</summary>
    /// <returns>The retained retrieval entries.</returns>
    public IReadOnlyList<M2paRetrievalEntry> Snapshot()
    {
        lock (_sync)
        {
            return _entries.ToArray();
        }
    }
}
