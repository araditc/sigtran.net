namespace Sigtran.NET.Layers.SCTP;

/// <summary>Tracks one SCTP user record while recvmsg may return more than one fragment.</summary>
internal struct NativeSctpRecord
{
    internal int BytesWritten { get; private set; }
    internal bool InProgress { get; private set; }
    internal bool Discarded { get; private set; }
    internal SctpPayloadMetadata Metadata { get; private set; }

    internal bool Observe(NativeSctpReceiveFrame frame, int capacity)
    {
        if (!InProgress)
        {
            Metadata = frame.Metadata;
        }
        else if (Metadata.StreamId != frame.Metadata.StreamId
            || Metadata.PayloadProtocolIdentifier != frame.Metadata.PayloadProtocolIdentifier
            || Metadata.Unordered != frame.Metadata.Unordered)
        {
            throw new InvalidDataException("Native SCTP metadata changed before the end of a user record.");
        }

        InProgress = !frame.IsEndOfRecord;
        Discarded |= frame.IsTruncated || frame.ByteCount > capacity - BytesWritten;
        if (Discarded)
        {
            return false;
        }

        BytesWritten += frame.ByteCount;
        return true;
    }
}
