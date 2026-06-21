namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Builds common TCAP BER transaction messages for higher-level APIs.
/// </summary>
public sealed class TcapSessionBuilder
{
    private readonly TcapTransactionIdAllocator _transactionIds;
    private readonly TcapInvokeRegistry _invokeIds;

    /// <summary>Creates a TCAP session builder.</summary>
    /// <param name="transactionIds">The transaction id allocator.</param>
    /// <param name="invokeIds">The invoke id registry.</param>
    public TcapSessionBuilder(TcapTransactionIdAllocator? transactionIds = null, TcapInvokeRegistry? invokeIds = null)
    {
        _transactionIds = transactionIds ?? new TcapTransactionIdAllocator();
        _invokeIds = invokeIds ?? new TcapInvokeRegistry();
    }

    /// <summary>Builds a TCAP Begin package containing one Invoke component.</summary>
    /// <param name="applicationContext">The dialogue application context.</param>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="parameters">The operation parameters.</param>
    /// <returns>The encoded transaction message and allocated identifiers.</returns>
    public TcapBuiltInvoke BeginInvoke(TcapObjectIdentifier applicationContext, TcapOperationCode operationCode, ReadOnlyMemory<byte> parameters)
    {
        TcapTransactionId transactionId = _transactionIds.Allocate();
        byte invokeId = _invokeIds.Allocate();
        TcapDialoguePortion dialogue = new(applicationContext);
        TcapBerInvokeComponent invoke = new(invokeId, operationCode, parameters);
        TcapTransactionMessage message = new(
            TcapPackageType.Begin,
            originatingTransactionId: transactionId,
            dialoguePortion: dialogue.Encode(),
            componentPortion: invoke.Encode());

        return new(message.Encode(), transactionId, invokeId);
    }

    /// <summary>Builds a TCAP End package containing one ReturnResult component.</summary>
    /// <param name="destinationTransactionId">The destination transaction id.</param>
    /// <param name="invokeId">The invoke id being completed.</param>
    /// <param name="operationCode">The operation code.</param>
    /// <param name="parameters">The result parameters.</param>
    /// <returns>The encoded End message.</returns>
    public byte[] EndResult(TcapTransactionId destinationTransactionId, byte invokeId, TcapOperationCode operationCode, ReadOnlyMemory<byte> parameters)
    {
        _invokeIds.Complete(invokeId);
        TcapBerReturnResultComponent result = new(invokeId, operationCode, parameters);
        TcapTransactionMessage message = new(
            TcapPackageType.End,
            destinationTransactionId: destinationTransactionId,
            componentPortion: result.Encode());

        return message.Encode();
    }
}

/// <summary>
/// Describes a built TCAP Invoke transaction.
/// </summary>
public readonly struct TcapBuiltInvoke
{
    /// <summary>Creates a built invoke result.</summary>
    /// <param name="encodedMessage">The encoded TCAP message.</param>
    /// <param name="originatingTransactionId">The originating transaction id.</param>
    /// <param name="invokeId">The invoke id.</param>
    public TcapBuiltInvoke(byte[] encodedMessage, TcapTransactionId originatingTransactionId, byte invokeId)
    {
        EncodedMessage = encodedMessage;
        OriginatingTransactionId = originatingTransactionId;
        InvokeId = invokeId;
    }

    /// <summary>The encoded TCAP message.</summary>
    public byte[] EncodedMessage { get; }

    /// <summary>The originating transaction id.</summary>
    public TcapTransactionId OriginatingTransactionId { get; }

    /// <summary>The invoke id.</summary>
    public byte InvokeId { get; }
}
