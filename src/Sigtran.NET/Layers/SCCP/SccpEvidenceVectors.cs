using Sigtran.NET.Core.Utilities;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>
/// Provides deterministic SCCP evidence vectors for byte-level codec validation.
/// </summary>
public static class SccpEvidenceVectors
{
    private const string Source = "SDK deterministic SCCP Q.713-style vector";

    private static readonly SigtranProtocolEvidenceVector[] Vectors =
    [
        new(
            "sccp/udt/route-on-ssn-map-to-msc",
            SigtranProtocolInteropSurface.Sccp,
            "SCCP UDT route-on-SSN payload from MAP SSN to MSC SSN.",
            new byte[]
            {
                0x09, 0x00, 0x03, 0x07, 0x0B,
                0x04, 0x43, 0x01, 0x01, 0x06,
                0x04, 0x43, 0x02, 0x01, 0x08,
                0x02, 0xCA, 0xFE
            },
            Source),
        new(
            "sccp/xudt/segmented-map-payload",
            SigtranProtocolInteropSurface.Sccp,
            "SCCP XUDT with segmentation optional parameter.",
            new byte[]
            {
                0x11, 0x81, 0x08, 0x04, 0x08, 0x0C, 0x0E,
                0x04, 0x43, 0x01, 0x01, 0x06,
                0x04, 0x43, 0x02, 0x01, 0x08,
                0x02, 0xBE, 0xEF,
                0x10, 0x04, 0x82, 0x01, 0x02, 0x03, 0x00
            },
            Source),
        new(
            "sccp/ludt/long-pointer-map-payload",
            SigtranProtocolInteropSurface.Sccp,
            "SCCP LUDT with 16-bit variable parameter pointers.",
            new byte[]
            {
                0x13, 0x01, 0x09, 0x00, 0x08, 0x00, 0x0C, 0x00, 0x10, 0x00, 0x00,
                0x00, 0x04, 0x43, 0x01, 0x01, 0x06,
                0x00, 0x04, 0x43, 0x02, 0x01, 0x08,
                0x00, 0x03, 0x01, 0x02, 0x03
            },
            Source),
        new(
            "sccp/udts/subsystem-failure",
            SigtranProtocolInteropSurface.Sccp,
            "SCCP UDTS carrying subsystem-failure return cause.",
            new byte[]
            {
                0x0A, 0x03, 0x03, 0x07, 0x0B,
                0x04, 0x43, 0x01, 0x01, 0x06,
                0x04, 0x43, 0x02, 0x01, 0x08,
                0x02, 0xDE, 0xAD
            },
            Source)
    ];

    /// <summary>Returns deterministic SCCP evidence vectors.</summary>
    /// <returns>The SCCP evidence vectors.</returns>
    public static IReadOnlyList<SigtranProtocolEvidenceVector> GetVectors()
    {
        return Vectors.ToArray();
    }

    /// <summary>Validates current SCCP encoders against deterministic evidence vectors.</summary>
    /// <returns>The byte-level validation reports.</returns>
    public static IReadOnlyList<SigtranProtocolEvidenceValidationReport> ValidateEncoders()
    {
        return Vectors.Select(vector => SigtranProtocolEvidenceValidator.Validate(vector, EncodeActualPayload(vector.Id))).ToArray();
    }

    private static byte[] EncodeActualPayload(string vectorId)
    {
        return vectorId switch
        {
            "sccp/udt/route-on-ssn-map-to-msc" => CreateUnitdata().Encode(),
            "sccp/xudt/segmented-map-payload" => CreateExtendedUnitdata().Encode(),
            "sccp/ludt/long-pointer-map-payload" => CreateLongUnitdata().Encode(),
            "sccp/udts/subsystem-failure" => CreateUnitdataService().Encode(),
            _ => throw new InvalidOperationException($"Unknown SCCP evidence vector '{vectorId}'.")
        };
    }

    private static SccpPartyAddress CreateCalledParty()
    {
        return new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MAP, pointCode: 0x0101);
    }

    private static SccpPartyAddress CreateCallingParty()
    {
        return new(SccpRoutingIndicator.RouteOnSubsystemNumber, subsystemNumber: SubsystemNumber.MSC, pointCode: 0x0102);
    }

    private static SccpUnitdataMessage CreateUnitdata()
    {
        return new(
            new SccpProtocolClass(SccpConnectionlessClass.Class0),
            CreateCalledParty(),
            CreateCallingParty(),
            new byte[] { 0xCA, 0xFE });
    }

    private static SccpExtendedUnitdataMessage CreateExtendedUnitdata()
    {
        return new(
            new SccpProtocolClass(SccpConnectionlessClass.Class1, returnMessageOnError: true),
            hopCounter: 8,
            CreateCalledParty(),
            CreateCallingParty(),
            new byte[] { 0xBE, 0xEF },
            new SccpSegmentationParameter(localReference: 0x010203, remainingSegments: 2, firstSegment: true));
    }

    private static SccpLongUnitdataMessage CreateLongUnitdata()
    {
        return new(
            new SccpProtocolClass(SccpConnectionlessClass.Class1),
            hopCounter: 9,
            CreateCalledParty(),
            CreateCallingParty(),
            new byte[] { 0x01, 0x02, 0x03 });
    }

    private static SccpUnitdataServiceMessage CreateUnitdataService()
    {
        return new(
            SccpReturnCause.SubsystemFailure,
            CreateCalledParty(),
            CreateCallingParty(),
            new byte[] { 0xDE, 0xAD });
    }
}
