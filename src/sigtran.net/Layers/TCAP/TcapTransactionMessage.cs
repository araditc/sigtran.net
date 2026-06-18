namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Represents a BER-encoded TCAP transaction message.
/// </summary>
public sealed class TcapTransactionMessage
{
    /// <summary>Creates a TCAP transaction message.</summary>
    /// <param name="packageType">The package type.</param>
    /// <param name="originatingTransactionId">The optional originating transaction id.</param>
    /// <param name="destinationTransactionId">The optional destination transaction id.</param>
    /// <param name="componentPortion">The optional encoded component portion.</param>
    /// <param name="dialoguePortion">The optional encoded dialogue portion.</param>
    public TcapTransactionMessage(
        TcapPackageType packageType,
        TcapTransactionId? originatingTransactionId = null,
        TcapTransactionId? destinationTransactionId = null,
        ReadOnlyMemory<byte> componentPortion = default,
        ReadOnlyMemory<byte> dialoguePortion = default)
    {
        PackageType = packageType;
        OriginatingTransactionId = originatingTransactionId;
        DestinationTransactionId = destinationTransactionId;
        ComponentPortion = componentPortion;
        DialoguePortion = dialoguePortion;
    }

    /// <summary>The package type.</summary>
    public TcapPackageType PackageType { get; }

    /// <summary>The optional originating transaction id.</summary>
    public TcapTransactionId? OriginatingTransactionId { get; }

    /// <summary>The optional destination transaction id.</summary>
    public TcapTransactionId? DestinationTransactionId { get; }

    /// <summary>The optional encoded component portion.</summary>
    public ReadOnlyMemory<byte> ComponentPortion { get; }

    /// <summary>The optional encoded dialogue portion.</summary>
    public ReadOnlyMemory<byte> DialoguePortion { get; }

    /// <summary>Encodes this TCAP transaction message.</summary>
    /// <returns>The encoded transaction message.</returns>
    public byte[] Encode()
    {
        byte[] content = new byte[2048];
        int offset = 0;
        if (OriginatingTransactionId.HasValue)
        {
            WriteElement(content, ref offset, TcapTransactionTags.TransactionId(originating: true), OriginatingTransactionId.Value.ToArray());
        }

        if (DestinationTransactionId.HasValue)
        {
            WriteElement(content, ref offset, TcapTransactionTags.TransactionId(originating: false), DestinationTransactionId.Value.ToArray());
        }

        if (!DialoguePortion.IsEmpty)
        {
            WriteElement(content, ref offset, new TcapBerTag(TcapBerTagClass.ContextSpecific, constructed: true, TcapTransactionTags.DialoguePortion), DialoguePortion.Span);
        }

        if (!ComponentPortion.IsEmpty)
        {
            WriteElement(content, ref offset, new TcapBerTag(TcapBerTagClass.ContextSpecific, constructed: true, TcapTransactionTags.ComponentPortion), ComponentPortion.Span);
        }

        byte[] result = new byte[offset + 4];
        if (!TcapBer.TryWriteElement(result, TcapTransactionTags.Package(PackageType), content.AsSpan(0, offset), out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        Array.Resize(ref result, written);
        return result;
    }

    /// <summary>Attempts to decode a TCAP transaction message.</summary>
    /// <param name="data">The encoded transaction bytes.</param>
    /// <param name="message">The decoded message on success.</param>
    /// <param name="error">An error message on failure.</param>
    /// <returns>True if decoding succeeded; otherwise false.</returns>
    public static bool TryDecode(ReadOnlySpan<byte> data, out TcapTransactionMessage? message, out string? error)
    {
        message = null;
        error = null;
        if (!TcapBer.TryReadElement(data, out TcapBerElement wrapper, out error))
        {
            return false;
        }

        if (wrapper.Tag.TagClass != TcapBerTagClass.Application || !wrapper.Tag.Constructed)
        {
            error = "TCAP transaction message must use a constructed application tag";
            return false;
        }

        TcapTransactionId? originating = null;
        TcapTransactionId? destination = null;
        ReadOnlyMemory<byte> dialogue = default;
        ReadOnlyMemory<byte> components = default;
        ReadOnlySpan<byte> content = wrapper.Value.Span;
        while (!content.IsEmpty)
        {
            if (!TcapBer.TryReadElement(content, out TcapBerElement element, out error))
            {
                return false;
            }

            if (element.Tag.TagClass == TcapBerTagClass.ContextSpecific && element.Tag.Number == TcapTransactionTags.OriginatingTransactionId)
            {
                originating = new TcapTransactionId(element.Value.Span);
            }
            else if (element.Tag.TagClass == TcapBerTagClass.ContextSpecific && element.Tag.Number == TcapTransactionTags.DestinationTransactionId)
            {
                destination = new TcapTransactionId(element.Value.Span);
            }
            else if (element.Tag.TagClass == TcapBerTagClass.ContextSpecific && element.Tag.Number == TcapTransactionTags.DialoguePortion)
            {
                dialogue = element.Value;
            }
            else if (element.Tag.TagClass == TcapBerTagClass.ContextSpecific && element.Tag.Number == TcapTransactionTags.ComponentPortion)
            {
                components = element.Value;
            }

            content = content[element.TotalLength..];
        }

        message = new((TcapPackageType)wrapper.Tag.Number, originating, destination, components, dialogue);
        return true;
    }

    private static void WriteElement(byte[] destination, ref int offset, TcapBerTag tag, ReadOnlySpan<byte> value)
    {
        if (!TcapBer.TryWriteElement(destination.AsSpan(offset), tag, value, out int written, out string? error))
        {
            throw new InvalidOperationException(error);
        }

        offset += written;
    }
}
