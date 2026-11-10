using Sigtran.NET.Layers.MTP3;
using Sigtran.NET.Layers.SCCP;

/// <summary>Defines the lab's synthetic point-code and subsystem-routed SCCP workload.</summary>
internal static class Mtp3LabTraffic
{
    internal static SccpConnectionlessService CreateService(IMtp3Network network, ushort local, ushort remote, byte sls)
        => new(network, new(remote, local, sls), networkIndicator: 2);

    internal static SccpUnitdataMessage CreateUnitdata(ushort local, ushort remote, byte marker, byte sequence)
        => new(new(SccpConnectionlessClass.Class0), Address(remote), Address(local), new byte[] { marker, sequence });

    internal static void ValidateUnitdata(SccpUnitdataMessage message, ushort local, ushort remote, byte marker, byte sequence)
    {
        if (message.ProtocolClass.Encode() != 0 || message.CalledParty.PointCode != local || message.CallingParty.PointCode != remote
            || message.CalledParty.SubsystemNumber != Subsystem(local) || message.CallingParty.SubsystemNumber != Subsystem(remote)
            || !message.UserData.Span.SequenceEqual(new byte[] { marker, sequence }))
        {
            throw new InvalidDataException($"Unexpected SCCP UDT at sequence {sequence}.");
        }
    }

    private static SubsystemNumber Subsystem(ushort pointCode) => pointCode == 1 ? (SubsystemNumber)7 : (SubsystemNumber)6;

    private static SccpPartyAddress Address(ushort pointCode)
        => new(SccpRoutingIndicator.RouteOnSubsystemNumber, Subsystem(pointCode), pointCode);
}
