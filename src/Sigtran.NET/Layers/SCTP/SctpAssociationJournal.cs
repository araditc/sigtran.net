namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Describes one timestamped SCTP association lifecycle journal entry.
/// </summary>
public readonly struct SctpAssociationJournalEntry
{
    /// <summary>Creates an SCTP association journal entry.</summary>
    /// <param name="timestampUtc">The UTC timestamp for the lifecycle event.</param>
    /// <param name="associationEvent">The association lifecycle event.</param>
    public SctpAssociationJournalEntry(DateTimeOffset timestampUtc, SctpAssociationEvent associationEvent)
    {
        TimestampUtc = timestampUtc;
        AssociationEvent = associationEvent;
    }

    /// <summary>The UTC timestamp for the lifecycle event.</summary>
    public DateTimeOffset TimestampUtc { get; }

    /// <summary>The association lifecycle event.</summary>
    public SctpAssociationEvent AssociationEvent { get; }

    /// <summary>Formats a compact lifecycle journal entry summary.</summary>
    /// <returns>The lifecycle journal entry summary.</returns>
    public string Describe()
    {
        return $"{TimestampUtc:O} event={AssociationEvent.EventType} state={AssociationEvent.State} reason={AssociationEvent.Reason ?? "-"}";
    }
}

/// <summary>
/// Records timestamped SCTP association lifecycle events for diagnostics and recovery decisions.
/// </summary>
public sealed class SctpAssociationJournal
{
    private readonly List<SctpAssociationJournalEntry> _entries = [];

    /// <summary>The recorded lifecycle event count.</summary>
    public int Count => _entries.Count;

    /// <summary>The current association state inferred from the latest journal entry.</summary>
    public SctpAssociationState CurrentState => _entries.Count == 0 ? SctpAssociationState.Closed : _entries[^1].AssociationEvent.State;

    /// <summary>Whether a failure event has been recorded.</summary>
    public bool HasFailure => _entries.Any(static entry => entry.AssociationEvent.EventType == SctpAssociationEventType.Failed);

    /// <summary>The latest recorded failure reason.</summary>
    public string? LastFailureReason => _entries
        .Where(static entry => entry.AssociationEvent.EventType == SctpAssociationEventType.Failed)
        .Select(static entry => entry.AssociationEvent.Reason)
        .LastOrDefault();

    /// <summary>Records a lifecycle event with the current UTC timestamp.</summary>
    /// <param name="associationEvent">The association lifecycle event.</param>
    public void Record(SctpAssociationEvent associationEvent)
    {
        Record(associationEvent, DateTimeOffset.UtcNow);
    }

    /// <summary>Records a lifecycle event with a caller-provided UTC timestamp.</summary>
    /// <param name="associationEvent">The association lifecycle event.</param>
    /// <param name="timestampUtc">The UTC timestamp for the lifecycle event.</param>
    public void Record(SctpAssociationEvent associationEvent, DateTimeOffset timestampUtc)
    {
        _entries.Add(new(timestampUtc, associationEvent));
    }

    /// <summary>Returns a point-in-time snapshot of the journal entries.</summary>
    /// <returns>The journal entries.</returns>
    public IReadOnlyList<SctpAssociationJournalEntry> Snapshot()
    {
        return _entries.ToArray();
    }

    /// <summary>Formats a compact lifecycle journal summary.</summary>
    /// <returns>The lifecycle journal summary.</returns>
    public string Describe()
    {
        return $"events={_entries.Count} current={CurrentState} failed={HasFailure} reason={LastFailureReason ?? "-"}";
    }
}
