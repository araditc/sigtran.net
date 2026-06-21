using Sigtran.NET.Core.Utilities;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Provides deterministic MAP SMS evidence vectors for byte-level operation parameter validation.
/// </summary>
public static class MapSmsEvidenceVectors
{
    private const string Source = "SDK deterministic MAP SMS BER-shaped vector";

    private static readonly SigtranProtocolEvidenceVector[] Vectors =
    [
        new(
            "map/sms/mo-forward-sm",
            SigtranProtocolInteropSurface.MapSms,
            "MAP MO-ForwardSM with service centre, MSISDN, and TPDU.",
            new byte[]
            {
                0x80, 0x06, 0x03, 0x04, 0x01, 0x44, 0x21, 0x43,
                0x81, 0x09, 0x01, 0x04, 0x01, 0x89, 0x19, 0x12, 0x32, 0x54, 0x76,
                0x82, 0x02, 0x11, 0x22
            },
            Source),
        new(
            "map/sms/mt-forward-sm",
            SigtranProtocolInteropSurface.MapSms,
            "MAP MT-ForwardSM with IMSI, service centre, and TPDU.",
            new byte[]
            {
                0x80, 0x0B, 0x02, 0x04, 0x01, 0x34, 0x12, 0x90, 0x78, 0x56, 0x34, 0x12, 0xF0,
                0x81, 0x06, 0x03, 0x04, 0x01, 0x44, 0x21, 0x43,
                0x82, 0x02, 0x21, 0x43
            },
            Source),
        new(
            "map/sms/send-routing-info-for-sm",
            SigtranProtocolInteropSurface.MapSms,
            "MAP SendRoutingInfoForSM with MSISDN, service centre, and GPRS support indicator.",
            new byte[]
            {
                0x80, 0x09, 0x01, 0x04, 0x01, 0x89, 0x19, 0x12, 0x32, 0x54, 0x76,
                0x81, 0x06, 0x03, 0x04, 0x01, 0x44, 0x21, 0x43,
                0x82, 0x01, 0x01
            },
            Source),
        new(
            "map/sms/report-sm-delivery-status",
            SigtranProtocolInteropSurface.MapSms,
            "MAP ReportSM-DeliveryStatus with MSISDN, service centre, and delivery status.",
            new byte[]
            {
                0x80, 0x09, 0x01, 0x04, 0x01, 0x89, 0x19, 0x12, 0x32, 0x54, 0x76,
                0x81, 0x06, 0x03, 0x04, 0x01, 0x44, 0x21, 0x43,
                0x82, 0x01, 0x02
            },
            Source),
        new(
            "map/sms/alert-service-centre",
            SigtranProtocolInteropSurface.MapSms,
            "MAP AlertServiceCentre with MSISDN and service centre address.",
            new byte[]
            {
                0x80, 0x09, 0x01, 0x04, 0x01, 0x89, 0x19, 0x12, 0x32, 0x54, 0x76,
                0x81, 0x06, 0x03, 0x04, 0x01, 0x44, 0x21, 0x43
            },
            Source)
    ];

    /// <summary>Returns deterministic MAP SMS evidence vectors.</summary>
    /// <returns>The MAP SMS evidence vectors.</returns>
    public static IReadOnlyList<SigtranProtocolEvidenceVector> GetVectors()
    {
        return Vectors.ToArray();
    }

    /// <summary>Validates current MAP SMS encoders against deterministic evidence vectors.</summary>
    /// <returns>The byte-level validation reports.</returns>
    public static IReadOnlyList<SigtranProtocolEvidenceValidationReport> ValidateEncoders()
    {
        return Vectors.Select(vector => SigtranProtocolEvidenceValidator.Validate(vector, EncodeActualPayload(vector.Id))).ToArray();
    }

    private static byte[] EncodeActualPayload(string vectorId)
    {
        return vectorId switch
        {
            "map/sms/mo-forward-sm" => CreateMoForwardSm().Encode(),
            "map/sms/mt-forward-sm" => CreateMtForwardSm().Encode(),
            "map/sms/send-routing-info-for-sm" => CreateSendRoutingInfoForSm().Encode(),
            "map/sms/report-sm-delivery-status" => CreateReportSmDeliveryStatus().Encode(),
            "map/sms/alert-service-centre" => CreateAlertServiceCentre().Encode(),
            _ => throw new InvalidOperationException($"Unknown MAP SMS evidence vector '{vectorId}'.")
        };
    }

    private static MapSmsAddress CreateServiceCentreAddress()
    {
        return new(MapSmsAddressKind.ServiceCentre, "441234");
    }

    private static MapSmsAddress CreateMsisdnAddress()
    {
        return new(MapSmsAddressKind.Msisdn, "989121234567");
    }

    private static MapMoForwardShortMessage CreateMoForwardSm()
    {
        return new(CreateServiceCentreAddress(), CreateMsisdnAddress(), new byte[] { 0x11, 0x22 });
    }

    private static MapMtForwardShortMessage CreateMtForwardSm()
    {
        return new(
            new MapSmsAddress(MapSmsAddressKind.Imsi, "432109876543210"),
            CreateServiceCentreAddress(),
            new byte[] { 0x21, 0x43 });
    }

    private static MapSendRoutingInfoForShortMessage CreateSendRoutingInfoForSm()
    {
        return new(CreateMsisdnAddress(), CreateServiceCentreAddress(), gprsSupportIndicator: true);
    }

    private static MapReportShortMessageDeliveryStatus CreateReportSmDeliveryStatus()
    {
        return new(CreateMsisdnAddress(), CreateServiceCentreAddress(), MapSmsDeliveryStatus.MemoryCapacityExceeded);
    }

    private static MapAlertServiceCentre CreateAlertServiceCentre()
    {
        return new(CreateMsisdnAddress(), CreateServiceCentreAddress());
    }
}
