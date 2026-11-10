using Sigtran.NET.Layers.SCCP;

/// <summary>Exercises independent Q.713 layouts and malformed pointer boundaries.</summary>
internal static class SccpWireTests
{
    internal static void ConnectionlessLayout()
    {
        byte[] reordered = Convert.FromHexString("09000B050102CAFE04430201080443010106");
        Require(SccpUnitdataMessage.TryDecode(reordered, out var unitdata, out _)
            && unitdata!.CalledParty.PointCode == 0x0101 && unitdata.CallingParty.PointCode == 0x0102
            && unitdata.UserData.Span.SequenceEqual(new byte[] { 0xCA, 0xFE }), "Legal Q.713 reordering rejected");
        byte[] ludt = Convert.FromHexString("13010907000A000D000000044301010604430201080300010203");
        Require(SccpLongUnitdataMessage.TryDecode(ludt, out var decoded, out _)
            && decoded!.Encode().AsSpan().SequenceEqual(ludt), "Q.713 LUDT vector differs");
        foreach (int offset in new[] { 3, 5, 7, 21 })
        {
            byte[] bad = ludt.ToArray();
            bad[offset] = 0;
            Require(!SccpLongUnitdataMessage.TryDecode(bad, out _, out _), $"Invalid LUDT field at {offset} accepted");
        }

        byte[] aliased = reordered.ToArray();
        aliased[3] = 10;
        Require(!SccpUnitdataMessage.TryDecode(aliased, out _, out _), "Aliased addresses accepted");
        byte[] unsupportedClass = reordered.ToArray();
        unsupportedClass[1] = 2;
        Require(!SccpUnitdataMessage.TryDecode(unsupportedClass, out _, out _), "Connection-oriented class accepted");
        Require(!SccpUnitdataMessage.TryDecode([.. reordered, 0], out _, out _), "Trailing UDT bytes accepted");
        Require(!SccpPartyAddress.TryDecode([0x43, 0, 0x80, 6], out _, out _), "Non-ITU point code accepted");
        Require(!SccpPartyAddress.TryDecode([0x10, 0, 0x11, 4, 0xF1], out _, out _), "MAP-style SCCP filler accepted");

        byte[] xudt = Convert.FromHexString("11810804080C0E0443010106044302010802BEEF10048201020300");
        Require(SccpExtendedUnitdataMessage.TryDecode(xudt, out _, out _), "Valid XUDT optional block rejected");
        xudt[21] = 3;
        Require(!SccpExtendedUnitdataMessage.TryDecode(xudt, out _, out _), "Invalid segmentation length accepted");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
