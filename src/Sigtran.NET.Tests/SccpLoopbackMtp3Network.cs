using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

internal sealed class SccpLoopbackMtp3Network : IMtp3Network
{
    private readonly Channel<Mtp3TransferMessage> _inbound =
        Channel.CreateUnbounded<Mtp3TransferMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

    public List<Mtp3TransferMessage> SentTransfers { get; } = [];

    public ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        SentTransfers.Add(message);
        _inbound.Writer.TryWrite(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Mtp3TransferMessage> ReceiveAsync(
        CancellationToken ct = default)
    {
        return _inbound.Reader.ReadAsync(ct);
    }

    public void QueueInbound(Mtp3TransferMessage message)
    {
        _inbound.Writer.TryWrite(message);
    }
}
