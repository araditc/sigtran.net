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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
