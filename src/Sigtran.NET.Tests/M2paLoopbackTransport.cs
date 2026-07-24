using System.Threading.Channels;

using Sigtran.NET.Layers.MTP2;
using Sigtran.NET.Layers.SCTP;

internal sealed class M2paLoopbackTransport : ISctpTransport
{
    private readonly Channel<M2paLoopbackPacket> _receivePackets =
        Channel.CreateUnbounded<M2paLoopbackPacket>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    private uint _peerForwardSequenceNumber = M2paProtocol.MaximumSequenceNumber;
    private bool _alignmentReadySent;
    private bool _disposed;

    public ISctpAssociation Association { get; } = new M3uaLoopbackAssociation();

    public List<SctpPayloadMetadata> SentMetadata { get; } = [];

    public ValueTask SendAsync(
        SctpOutboundMessage message,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!M2paMessage.TryDecode(
                message.Payload.Span,
                out M2paMessage? decoded,
                out string? decodeError))
        {
            throw new InvalidOperationException(decodeError);
        }

        if (!M2paProtocol.TryValidateSctpMetadata(
                decoded!,
                message.Metadata,
                out string? metadataError))
        {
            throw new InvalidOperationException(metadataError);
        }

        SentMetadata.Add(message.Metadata);
        M2paLoopbackPacket? response = BuildResponse(decoded!, message.Metadata.StreamId);
        if (response is not null)
        {
            _receivePackets.Writer.TryWrite(response.Value);
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask<SctpReceiveResult> ReceiveAsync(
        Memory<byte> buffer,
        CancellationToken ct = default)
    {
        M2paLoopbackPacket packet =
            await _receivePackets.Reader.ReadAsync(ct).ConfigureAwait(false);
        packet.Payload.CopyTo(buffer);
        return new(packet.Payload.Length, packet.Metadata);
    }

    public void QueueLinkStatus(M2paLinkStatus status)
    {
        ushort stream = M2paProtocol.GetStream(status);
        _receivePackets.Writer.TryWrite(
            EncodeLinkStatus(status, stream));
    }

    public void Dispose()
    {
        _disposed = true;
        _receivePackets.Writer.TryComplete();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private M2paLoopbackPacket? BuildResponse(
        M2paMessage message,
        ushort streamId)
    {
        if (message.MessageType == M2paMessageType.UserData)
        {
            if (message.IsAcknowledgementOnly)
            {
                return null;
            }

            _peerForwardSequenceNumber =
                M2paProtocol.NextSequenceNumber(_peerForwardSequenceNumber);
            byte[] encoded = new byte[
                M2paProtocol.MinimumMessageLength + message.Payload.Length];
            if (!M2paMessage.TryEncodeUserData(
                    encoded,
                    backwardSequenceNumber: message.ForwardSequenceNumber,
                    forwardSequenceNumber: _peerForwardSequenceNumber,
                    payload: message.Payload.Span,
                    out int written,
                    out string? error))
            {
                throw new InvalidOperationException(error);
            }

            return new(
                encoded.AsSpan(0, written).ToArray(),
                new(
                    M2paProtocol.UserDataStream,
                    SctpPayloadProtocolIdentifiers.M2pa));
        }

        return message.LinkStatus switch
        {
            M2paLinkStatus.Alignment =>
                EncodeLinkStatus(M2paLinkStatus.Alignment, M2paProtocol.LinkStatusStream),
            M2paLinkStatus.ProvingNormal =>
                EncodeLinkStatus(M2paLinkStatus.ProvingNormal, M2paProtocol.LinkStatusStream),
            M2paLinkStatus.ProvingEmergency =>
                EncodeLinkStatus(M2paLinkStatus.ProvingEmergency, M2paProtocol.LinkStatusStream),
            M2paLinkStatus.Ready when streamId == M2paProtocol.LinkStatusStream
                && !_alignmentReadySent => MarkAlignmentReady(),
            M2paLinkStatus.ProcessorRecovered =>
                EncodeLinkStatus(M2paLinkStatus.Ready, M2paProtocol.UserDataStream),
            _ => null
        };
    }

    private M2paLoopbackPacket MarkAlignmentReady()
    {
        _alignmentReadySent = true;
        return EncodeLinkStatus(M2paLinkStatus.Ready, M2paProtocol.LinkStatusStream);
    }

    private M2paLoopbackPacket EncodeLinkStatus(
        M2paLinkStatus status,
        ushort streamId)
    {
        byte[] encoded = new byte[M2paProtocol.MinimumMessageLength + sizeof(uint)];
        if (!M2paMessage.TryEncodeLinkStatus(
                encoded,
                backwardSequenceNumber: M2paProtocol.MaximumSequenceNumber,
                forwardSequenceNumber: _peerForwardSequenceNumber,
                status,
                ReadOnlySpan<byte>.Empty,
                out int written,
                out string? error))
        {
            throw new InvalidOperationException(error);
        }

        return new(
            encoded.AsSpan(0, written).ToArray(),
            new(streamId, SctpPayloadProtocolIdentifiers.M2pa));
    }
}

internal readonly record struct M2paLoopbackPacket(
    byte[] Payload,
    SctpPayloadMetadata Metadata);
