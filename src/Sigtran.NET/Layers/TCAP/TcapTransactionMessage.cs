namespace Sigtran.NET.Layers.TCAP;

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
        : this(packageType, originatingTransactionId, destinationTransactionId, componentPortion, dialoguePortion, null)
    {
    }

    /// <summary>Creates a TCAP transaction message with an optional provider abort cause.</summary>
    /// <param name="packageType">The package type.</param>
    /// <param name="originatingTransactionId">The optional originating transaction ID.</param>
    /// <param name="destinationTransactionId">The optional destination transaction ID.</param>
    /// <param name="componentPortion">The optional encoded component portion.</param>
    /// <param name="dialoguePortion">The optional encoded dialogue portion.</param>
    /// <param name="providerAbortCause">The provider cause, only for Abort without a dialogue portion.</param>
    public TcapTransactionMessage(
        TcapPackageType packageType,
        TcapTransactionId? originatingTransactionId,
        TcapTransactionId? destinationTransactionId,
        ReadOnlyMemory<byte> componentPortion,
        ReadOnlyMemory<byte> dialoguePortion,
        TcapProviderAbortCause? providerAbortCause)
    {
        PackageType = packageType;
        OriginatingTransactionId = originatingTransactionId;
        DestinationTransactionId = destinationTransactionId;
        ComponentPortion = componentPortion;
        DialoguePortion = dialoguePortion;
        ProviderAbortCause = providerAbortCause;
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

    /// <summary>The optional transaction-provider abort cause.</summary>
    public TcapProviderAbortCause? ProviderAbortCause { get; }

    /// <summary>Encodes this TCAP transaction message.</summary>
    /// <returns>The encoded transaction message.</returns>
    public byte[] Encode()
    {
        if (!HasValidFields(PackageType, OriginatingTransactionId, DestinationTransactionId,
                ComponentPortion, DialoguePortion, ProviderAbortCause))
        {
            throw new InvalidOperationException("Invalid TCAP transaction fields for the package type.");
        }

        int contentLength = (OriginatingTransactionId is { } origin ? TcapBer.GetEncodedLength(origin.Length) : 0)
            + (DestinationTransactionId is { } destination ? TcapBer.GetEncodedLength(destination.Length) : 0)
            + (ProviderAbortCause.HasValue ? TcapBer.GetEncodedLength(1) : 0)
            + (DialoguePortion.IsEmpty ? 0 : TcapBer.GetEncodedLength(DialoguePortion.Length))
            + (ComponentPortion.IsEmpty ? 0 : TcapBer.GetEncodedLength(ComponentPortion.Length));
        byte[] result = new byte[TcapBer.GetEncodedLength(contentLength)];
        int headerLength = result.Length - contentLength;
        int offset = headerLength;
        if (OriginatingTransactionId.HasValue)
        {
            WriteElement(result, ref offset, TcapTransactionTags.TransactionId(originating: true), OriginatingTransactionId.Value.Bytes);
        }

        if (DestinationTransactionId.HasValue)
        {
            WriteElement(result, ref offset, TcapTransactionTags.TransactionId(originating: false), DestinationTransactionId.Value.Bytes);
        }

        if (ProviderAbortCause is { } cause)
        {
            WriteElement(result, ref offset, new TcapBerTag(TcapBerTagClass.Application, false, TcapTransactionTags.ProviderAbortCause), [(byte)cause]);
        }

        if (!DialoguePortion.IsEmpty)
        {
            WriteElement(result, ref offset, new TcapBerTag(TcapBerTagClass.Application, constructed: true, TcapTransactionTags.DialoguePortion), DialoguePortion.Span);
        }

        if (!ComponentPortion.IsEmpty)
        {
            WriteElement(result, ref offset, new TcapBerTag(TcapBerTagClass.Application, constructed: true, TcapTransactionTags.ComponentPortion), ComponentPortion.Span);
        }

        // Fields already occupy their final range; writing the outer header copies the value onto itself.
        if (!TcapBer.TryWriteElement(result, TcapTransactionTags.Package(PackageType), result.AsSpan(headerLength), out _, out string? error))
        {
            throw new InvalidOperationException(error);
        }

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

        if (wrapper.TotalLength != data.Length
            || wrapper.Tag.TagClass != TcapBerTagClass.Application || !wrapper.Tag.Constructed)
        {
            error = "Expected exactly one constructed TCAP application element.";
            return false;
        }

        TcapTransactionId? originating = null;
        TcapTransactionId? destination = null;
        ReadOnlyMemory<byte> dialogue = default;
        ReadOnlyMemory<byte> components = default;
        TcapProviderAbortCause? cause = null;
        int previousTag = 0;
        ReadOnlySpan<byte> content = wrapper.Value.Span;
        while (!content.IsEmpty)
        {
            if (!TcapBer.TryReadElement(content, out TcapBerElement element, out error))
            {
                return false;
            }

            if (element.Tag.TagClass != TcapBerTagClass.Application || element.Tag.Number <= previousTag)
            {
                error = "Invalid, duplicate, or out-of-order TCAP transaction field.";
                return false;
            }

            switch (element.Tag.Encode())
            {
                case 0x48 when element.Value.Length is >= 1 and <= TcapTransactionId.MaxLength:
                    originating = new TcapTransactionId(element.Value.Span);
                    break;
                case 0x49 when element.Value.Length is >= 1 and <= TcapTransactionId.MaxLength:
                    destination = new TcapTransactionId(element.Value.Span);
                    break;
                case 0x4A when element.Value.Length == 1 && element.Value.Span[0] <= (byte)TcapProviderAbortCause.ResourceLimitation:
                    cause = (TcapProviderAbortCause)element.Value.Span[0];
                    break;
                case 0x6B when !element.Value.IsEmpty:
                    dialogue = element.Value;
                    break;
                case 0x6C when !element.Value.IsEmpty:
                    components = element.Value;
                    break;
                default:
                    error = "Invalid TCAP transaction field tag or size.";
                    return false;
            }

            previousTag = element.Tag.Number;
            content = content[element.TotalLength..];
        }

        TcapPackageType packageType = (TcapPackageType)wrapper.Tag.Number;
        if (!HasValidFields(packageType, originating, destination, components, dialogue, cause))
        {
            error = "Invalid TCAP transaction fields for the package type.";
            return false;
        }

        message = new(packageType, originating, destination, components, dialogue, cause);
        return true;
    }

    private static bool HasValidFields(TcapPackageType packageType, TcapTransactionId? originating,
        TcapTransactionId? destination, ReadOnlyMemory<byte> components, ReadOnlyMemory<byte> dialogue,
        TcapProviderAbortCause? cause)
    {
        if (originating is { Length: 0 } || destination is { Length: 0 }
            || cause > TcapProviderAbortCause.ResourceLimitation)
        {
            return false;
        }

        return packageType switch
        {
            TcapPackageType.Unidirectional => originating is null && destination is null && !components.IsEmpty && cause is null,
            TcapPackageType.Begin => originating is not null && destination is null && cause is null,
            TcapPackageType.Continue => originating is not null && destination is not null && cause is null,
            TcapPackageType.End => originating is null && destination is not null && cause is null,
            TcapPackageType.Abort => originating is null && destination is not null && components.IsEmpty
                && (cause is null || dialogue.IsEmpty),
            _ => false
        };
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
