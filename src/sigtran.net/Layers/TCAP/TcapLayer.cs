using sigtran.net.Core.Interfaces;
using sigtran.net.Core.Utilities;

namespace sigtran.net.Layers.TCAP;

/// <summary>
/// A minimal TCAP layer that sits between SCCP and the application (MAP).
/// It constructs and parses simplified TCAP components and manages a
/// collection of active dialogues.  This implementation supports only
/// single‑component messages and does not implement the ASN.1 Transaction
/// Portion or Dialogue Portion.
/// </summary>
public sealed class TcapLayer : ISigtranLayer
{
    private readonly ILayerConnector _connector;
    private readonly Dictionary<long, TcapDialogue> _dialogs = new();
    private long _nextDialogueId = 1;

    /// <summary>
    /// Constructs a new TCAP layer using the provided connector.
    /// </summary>
    public TcapLayer(ILayerConnector connector)
    {
        _connector = connector ?? throw new ArgumentNullException(nameof(connector));
    }

    /// <inheritdoc />
    public void ReceiveFromLower(ReadOnlySpan<byte> data)
    {
        // Parse a single component from the raw TCAP bytes.
        if (data.Length < 1)
        {
            return;
        }

        byte type = data[0];
        TcapComponent? comp = null;
        switch (type)
        {
            case 0xA1:
                TcapInvokeComponent inv = new(0, TcapOperationCode.None, ReadOnlyMemory<byte>.Empty);
                if (!inv.TryDecode(data))
                {
                    MetricsExporter.ErrorsTotal++;
                    throw new InvalidOperationException("Failed to decode TCAP Invoke");
                }
                comp = inv;
                break;
            case 0xA2:
                TcapReturnResultComponent res = new(0, TcapOperationCode.None, ReadOnlyMemory<byte>.Empty);
                if (!res.TryDecode(data))
                {
                    MetricsExporter.ErrorsTotal++;
                    throw new InvalidOperationException("Failed to decode TCAP ReturnResult");
                }
                comp = res;
                break;
            default:
                MetricsExporter.ErrorsTotal++;
                throw new InvalidOperationException($"Unknown TCAP component type 0x{type:X2}");
        }
        // For now we do not manage transaction portion; we always use dialogue ID 1
        const long dialogueId = 1;
        if (!_dialogs.TryGetValue(dialogueId, out TcapDialogue? dlg))
        {
            dlg = new(dialogueId);
            _dialogs[dialogueId] = dlg;
        }
        if (comp is TcapInvokeComponent iv)
        {
            dlg.HandleIncomingInvoke(iv);
            // Forward parameters to MAP (upper layer)
            _connector.SendUp(iv.Parameters.Span);
        }
        else if (comp is TcapReturnResultComponent rr)
        {
            dlg.HandleIncomingReturnResult(rr);
            // Forward parameters to MAP
            _connector.SendUp(rr.Parameters.Span);
        }
    }

    /// <inheritdoc />
    public void ReceiveFromUpper(ReadOnlySpan<byte> data)
    {
        // Application provides raw parameters.  Create a new dialogue and an invoke.
        long dialogueId = _nextDialogueId++;
        TcapDialogue dlg = new(dialogueId);
        _dialogs[dialogueId] = dlg;
        TcapInvokeComponent invoke = dlg.CreateInvoke(TcapOperationCode.MoForwardShortMessage, data.ToArray());
        byte[] encoded = invoke.Encode();
        // Send encoded component down to SCCP
        _connector.SendDown(encoded);
    }
}