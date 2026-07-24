using System.Threading.Channels;

using Sigtran.NET.Layers.MTP3;

internal sealed class PairedMtp3Network : IMtp3Network
{
    private readonly Channel<Mtp3TransferMessage> _inbound =
        Channel.CreateUnbounded<Mtp3TransferMessage>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });
    private PairedMtp3Network? _peer;

    public List<Mtp3TransferMessage> SentTransfers { get; } = [];

    public static (PairedMtp3Network First, PairedMtp3Network Second) CreatePair()
    {
        PairedMtp3Network first = new();
        PairedMtp3Network second = new();
        first._peer = second;
        second._peer = first;
        return (first, second);
    }

    public ValueTask SendAsync(
        Mtp3TransferMessage message,
        CancellationToken ct = default)
    {
        SentTransfers.Add(message);
        PairedMtp3Network peer = _peer
            ?? throw new InvalidOperationException("MTP3 test peer is not connected.");
        peer._inbound.Writer.TryWrite(message);
        return ValueTask.CompletedTask;
    }

    public ValueTask<Mtp3TransferMessage> ReceiveAsync(
        CancellationToken ct = default)
    {
        return _inbound.Reader.ReadAsync(ct);
    }
}
