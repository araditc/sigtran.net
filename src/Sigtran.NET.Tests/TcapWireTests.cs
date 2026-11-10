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

    internal static void InvokeFields()
    {
        byte[] vector = Convert.FromHexString("A10C0201FF800180020200803000");
        var invoke = new TcapBerInvokeComponent(255, (TcapOperationCode)128, new byte[] { 0x30, 0 }, linkedInvokeId: 128);
        Require(invoke.Encode().AsSpan().SequenceEqual(vector), "Invoke wire bytes differ");
        Require(TcapBerInvokeComponent.TryDecode(vector, out var decoded, out var error)
            && decoded?.LinkedInvokeId == 128 && decoded.InvokeId == 255 && (byte)decoded.OperationCode == 128,
            error ?? "Invoke IDs or local code changed");
        byte[] noArgument = Convert.FromHexString("A10602010102012D");
        Require(TcapBerInvokeComponent.TryDecode(noArgument, out var absent, out error) && absent!.Parameters.IsEmpty,
            error ?? "Optional argument rejected");
        foreach (string hex in new[] { "810602010102012D", "A10B02010102010202012D3000", "A107020200FF02012D", "A1060201010201FF", "A10602010102012D00" })
        {
            Require(!TcapBerInvokeComponent.TryDecode(Convert.FromHexString(hex), out _, out _), $"Malformed Invoke accepted: {hex}");
        }
    }

    internal static void OutcomeFields()
    {
        byte[] emptyResult = Convert.FromHexString("A2030201FF");
        var result = new TcapBerReturnResultComponent(255, (TcapOperationCode)45, default);
        Require(result.Encode().AsSpan().SequenceEqual(emptyResult)
            && TcapBerReturnResultComponent.TryDecode(emptyResult, out var decoded, out _)
            && decoded?.OperationCode is null, "ID-only result did not round-trip");

        byte[] error = Convert.FromHexString("A309020101020124040101");
        Require(TcapBerReturnErrorComponent.TryDecode(error, out var decodedError, out _)
            && (byte)decodedError!.ErrorCode == 36 && decodedError.Encode().AsSpan().SequenceEqual(error),
            "ReturnError open type changed");

        byte[] uncorrelated = Convert.FromHexString("A4050500820102");
        Require(TcapBerRejectComponent.TryDecode(uncorrelated, out var reject, out _)
            && reject?.CorrelatedInvokeId is null && reject!.ProblemCode == TcapRejectProblemCode.MistypedResult
            && reject.Encode().AsSpan().SequenceEqual(uncorrelated), "Uncorrelated result problem changed");

        Require(!TcapBerReturnResultComponent.TryDecode(Convert.FromHexString("A20602010102012D"), out _, out _),
            "Flattened result accepted");
        Require(!TcapBerRejectComponent.TryDecode(Convert.FromHexString("A406020101810108"), out _, out _),
            "Unknown problem code accepted");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
