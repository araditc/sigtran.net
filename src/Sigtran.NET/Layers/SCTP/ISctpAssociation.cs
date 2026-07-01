namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Exposes the lifecycle state of one SCTP association without binding higher layers to a concrete socket implementation.
/// </summary>
public interface ISctpAssociation
{
    /// <summary>The current association lifecycle state.</summary>
    SctpAssociationState State { get; }

    /// <summary>
    /// Returns a point-in-time snapshot of timestamped lifecycle events.
    /// </summary>
    /// <returns>The recorded lifecycle events.</returns>
    IReadOnlyList<SctpAssociationJournalEntry> SnapshotEvents();
}
