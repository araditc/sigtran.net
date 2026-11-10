using Sigtran.NET.Layers.MAP;

/// <summary>Checks TS 29.002 fields against literal ASN.1 profile vectors.</summary>
internal static class MapSmsWireTests
{
    internal static void Fields()
    {
        var subscriber = new MapSmsAddress(MapSmsAddressKind.Msisdn, "989121234567");
        var centre = new MapSmsAddress(MapSmsAddressKind.ServiceCentre, "441234");
        var imsi = new MapSmsAddress(MapSmsAddressKind.Imsi, "432109876543210");
        byte[] sri = Convert.FromHexString("30148007918919123254768101FF8204914421438700");
        var request = new MapSendRoutingInfoForShortMessage(subscriber, centre, gprsSupportIndicator: true, smRpPriority: true);
        Require(request.Encode().AsSpan().SequenceEqual(sri), "SRI-SM BOOLEAN or NULL tag differs");
        Require(MapSendRoutingInfoForShortMessage.TryDecode(sri, out var decoded, out _)
            && decoded is { GprsSupportIndicator: true, SmRpPriority: true }, "SRI-SM flags were not decoded");

        byte[] result = Convert.FromHexString("3012040834129078563412F0A006810491442143");
        var response = new MapSmsRoutingInfo(imsi, new(MapSmsAddressKind.Msisdn, centre.Digits));
        Require(response.Encode().AsSpan().SequenceEqual(result)
            && MapSmsRoutingInfo.TryDecode(result, out var parsed, out _)
            && parsed!.Imsi.Digits == imsi.Digits, "SRI-SM mandatory result fields differ");
        Require(!MapSmsRoutingInfo.TryDecode([0x30, 0], out _, out _), "Empty SRI-SM result accepted");

        byte[] duplicate = [.. sri, 0x87, 0];
        duplicate[1] += 2;
        Require(!MapSendRoutingInfoForShortMessage.TryDecode(duplicate, out _, out _), "Duplicate GPRS indicator accepted");
        sri[5] = 0xF9;
        Require(!MapSendRoutingInfoForShortMessage.TryDecode(sri, out _, out _), "Nonfinal TBCD filler accepted");

        byte[] report = Convert.FromHexString("30120407918919123254760404914421430A0102");
        Require(new MapReportShortMessageDeliveryStatus(subscriber, centre, MapSmsDeliveryStatus.Delivered)
            .Encode().AsSpan().SequenceEqual(report), "Delivered must encode as successfulTransfer(2)");
        report[^1] = 3;
        Require(!MapReportShortMessageDeliveryStatus.TryDecode(report, out _, out _), "Unknown delivery outcome accepted");
        Require(!MapAlertServiceCentre.TryDecode([0x30, 0x03, 0x04, 0x01, 0x91], out _, out _), "Alert with missing digits accepted");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
