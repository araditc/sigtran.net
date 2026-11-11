using System.Formats.Asn1;
using Sigtran.NET.Core.Utilities;
using Sigtran.NET.Layers.TCAP;

/// <summary>Checks exact BER boundaries and ownership independently of wall-clock benchmark speed.</summary>
internal static class EncodingAllocationTests
{
    internal static void TransactionLengths()
    {
        foreach (int size in new[] { 0, 120, 127, 128, 255, 256, 2049, 60000 })
        {
            AsnWriter component = new(AsnEncodingRules.BER);
            component.WriteOctetString(new byte[size]);
            byte[] argument = component.Encode();
            TcapTransactionId id = new(new byte[] { 1, 2, 3, 4 });
            TcapTransactionMessage message = new(TcapPackageType.Begin, id, componentPortion: argument);
            AsnWriter expected = new(AsnEncodingRules.BER);
            using (expected.PushSequence(new Asn1Tag(TagClass.Application, 2, true)))
            {
                expected.WriteOctetString(id.Bytes, new Asn1Tag(TagClass.Application, 8));
                using (expected.PushSequence(new Asn1Tag(TagClass.Application, 12, true)))
                    expected.WriteEncodedValue(argument);
            }
            byte[] actual = message.Encode();
            Require(actual.AsSpan().SequenceEqual(expected.Encode()), "Transaction BER differs from the standard encoder");
            Require(TcapTransactionMessage.TryDecode(actual, out var decoded, out _)
                && decoded!.ComponentPortion.Span.SequenceEqual(argument), "Long-form lengths must round trip");
            byte[] next = message.Encode();
            next[^1] ^= 1;
            Require(actual.AsSpan().SequenceEqual(expected.Encode()), "Every encoding must own its output");
        }
    }

    internal static void WriterLeases()
    {
        using (var outer = BerWriterCache.Rent())
        {
            outer.Writer.WriteInteger(1);
            using (var inner = BerWriterCache.Rent())
            {
                inner.Writer.WriteInteger(2);
                Require(inner.Writer.Encode().AsSpan().SequenceEqual(new byte[] { 2, 1, 2 }), "Nested encoders must be isolated");
            }
            Require(outer.Writer.Encode().AsSpan().SequenceEqual(new byte[] { 2, 1, 1 }), "Nested return must not reset an active writer");
        }
        using (var broken = BerWriterCache.Rent()) broken.Writer.PushSequence();
        using (var clean = BerWriterCache.Rent()) Require(clean.Writer.GetEncodedLength() == 0, "An unfinished writer must not poison reuse");
        Parallel.For(0, 128, index =>
        {
            using var encoding = BerWriterCache.Rent();
            encoding.Writer.WriteInteger(index);
            Require(new AsnReader(encoding.Writer.Encode(), AsnEncodingRules.BER).TryReadInt32(out int value)
                && value == index, "Writers must not cross thread boundaries");
        });
    }

    internal static void TransactionAllocation()
    {
        TcapTransactionMessage transaction = new(TcapPackageType.Begin, new TcapTransactionId(new byte[] { 1 }),
            componentPortion: new byte[] { 0xa1, 0 });
        for (int i = 0; i < 100; i++) _ = transaction.Encode();
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) _ = transaction.Encode();
        Require(GC.GetAllocatedBytesForCurrentThread() - before < 100 * 256,
            "Small transaction encode must not recreate a 2 KiB scratch buffer");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
