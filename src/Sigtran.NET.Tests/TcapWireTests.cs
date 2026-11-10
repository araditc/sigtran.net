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

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
