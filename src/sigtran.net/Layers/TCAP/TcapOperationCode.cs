namespace sigtran.net.Layers.TCAP;

/// <summary>
/// Enumerates simplified TCAP/MAP operation codes.  These values are
/// placeholders used for early testing and do not correspond to real
/// ASN.1 object identifiers.  Extend this enumeration as needed for
/// additional MAP services.
/// </summary>
public enum TcapOperationCode : byte
{
    /// <summary>Unknown or unspecified operation.</summary>
    None = 0,
    /// <summary>Mobile‑originated forward short message (MO‑ForwardSM).</summary>
    MoForwardShortMessage = 1,
    /// <summary>Mobile‑terminated forward short message (MT‑ForwardSM).</summary>
    MtForwardShortMessage = 2,
    /// <summary>Alert Service Centre.</summary>
    AlertServiceCentre = 3,
    /// <summary>Report short message delivery status.</summary>
    ReportSmDeliveryStatus = 4
}