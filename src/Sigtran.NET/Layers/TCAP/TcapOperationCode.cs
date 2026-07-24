namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Enumerates legacy local operation tokens retained for source and wire-test
/// compatibility. Application profiles use explicitly cast local operation
/// values because TCAP operation codes are defined by the upper-layer context.
/// </summary>
public enum TcapOperationCode : byte
{
    /// <summary>Unknown or unspecified operation.</summary>
    None = 0,
    /// <summary>Legacy token used by MO-ForwardSM TCAP codec tests.</summary>
    MoForwardShortMessage = 1,
    /// <summary>Legacy token used by MT-ForwardSM TCAP codec tests.</summary>
    MtForwardShortMessage = 2,
    /// <summary>Legacy token used by AlertServiceCentre TCAP codec tests.</summary>
    AlertServiceCentre = 3,
    /// <summary>Legacy token used by ReportSM-DeliveryStatus TCAP codec tests.</summary>
    ReportSmDeliveryStatus = 4
}
