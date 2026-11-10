using System.Formats.Asn1;
using Sigtran.NET.Layers.TCAP;

/// <summary>Checks Q.773 wire fields against an encoder independent of the SDK.</summary>
internal static class TcapWireTests
{
    internal static void TransactionTags()
    {
        AsnWriter reference = new(AsnEncodingRules.BER);
        using (reference.PushSequence(new Asn1Tag(TagClass.Application, 5, isConstructed: true)))
        {
            reference.WriteOctetString([0, 0xFE], new Asn1Tag(TagClass.Application, 8));
            reference.WriteOctetString([0xA0, 0xFF, 0xFF, 0xFF], new Asn1Tag(TagClass.Application, 9));
        }

        byte[] vector = Convert.FromHexString("650A480200FE4904A0FFFFFF");
        Require(reference.Encode().AsSpan().SequenceEqual(vector), "Independent Continue vector differs");
        var message = new TcapTransactionMessage(TcapPackageType.Continue,
            new TcapTransactionId([0, 0xFE]), new TcapTransactionId([0xA0, 0xFF, 0xFF, 0xFF]));
        Require(message.Encode().AsSpan().SequenceEqual(vector), "SDK Continue bytes differ");
        Require(TcapTransactionMessage.TryDecode(vector, out var decoded, out var error), error ?? "Continue decode failed");
        Require(decoded?.OriginatingTransactionId?.ToString() == "00FE"
            && decoded.DestinationTransactionId?.ToString() == "A0FFFFFF", "Opaque transaction IDs changed");
    }

    internal static void TransactionFields()
    {
        byte[] abort = Convert.FromHexString("67064901FF4A0101");
        var transaction = new TcapTransactionMessage(TcapPackageType.Abort, null, new TcapTransactionId([0xFF]),
            default, default, TcapProviderAbortCause.UnrecognizedTransactionId);
        Require(transaction.Encode().AsSpan().SequenceEqual(abort), "Provider Abort bytes differ");
        Require(TcapTransactionMessage.TryDecode(abort, out var decoded, out var error)
            && decoded?.ProviderAbortCause == TcapProviderAbortCause.UnrecognizedTransactionId, error ?? "Provider cause lost");

        string[] malformed =
        [
            "6200", "62024800", "620748050000000000", "6203680101", "6203880101",
            "6206480101480102", "6203490101", "6506490101480102",
            "620348010100", "6303480101", "6100", "67064901014A0105",
            "67064901016C0100", "67094901014A01016B0100"
        ];
        foreach (string hex in malformed)
        {
            Require(!TcapTransactionMessage.TryDecode(Convert.FromHexString(hex), out var result, out error)
                && result is null && error is not null, $"Malformed transaction accepted: {hex}");
        }
    }

    internal static void BerBoundaries()
    {
        AsnWriter reference = new(AsnEncodingRules.BER);
        reference.WriteObjectIdentifier("2.4294967295.0.128");
        var oid = new TcapObjectIdentifier(2, uint.MaxValue, 0, 128);
        byte[] encoded = reference.Encode();
        Require(encoded.AsSpan(2).SequenceEqual(oid.EncodeValue()), "OID bytes differ from independent BER");
        Require(TcapObjectIdentifier.DecodeValue(encoded.AsSpan(2)).ToString() == "2.4294967295.0.128", "Large OID arc changed");

        foreach (string hex in new[] { "1F0100", "30800000", "0080", "048201", "0483000001" })
        {
            Require(!TcapBer.TryReadElement(Convert.FromHexString(hex), out _, out _), $"Unsupported BER accepted: {hex}");
        }

        foreach (string hex in new[] { "0600", "060180", "06022A80", "06072A908080808000" })
        {
            Require(!TcapDialoguePortion.TryDecode(Convert.FromHexString(hex), out _, out _), $"Malformed OID accepted: {hex}");
        }

        var octetString = new TcapBerTag(TcapBerTagClass.Universal, false, 4);
        byte[] maximum = new byte[ushort.MaxValue];
        byte[] destination = new byte[maximum.Length + 4];
        Require(TcapBer.TryWriteElement(destination, octetString, maximum, out int count, out _)
            && count == destination.Length && destination.AsSpan(0, 4).SequenceEqual(new byte[] { 4, 0x82, 0xFF, 0xFF }),
            "Maximum supported BER length did not encode");
        Require(!TcapBer.TryWriteElement(new byte[65540], octetString, new byte[65536], out _, out _),
            "Oversize BER value silently truncated");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
