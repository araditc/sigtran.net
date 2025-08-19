namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Common subsystem numbers used in SCCP addressing.  These values are
/// drawn from typical GSM/SS7 deployments.  The actual numbers may vary
/// between networks; adjust as needed.
/// </summary>
public enum SubsystemNumber : byte
{
    /// <summary>Unknown or not specified.</summary>
    Unknown = 0,
    /// <summary>Mobile Application Part (MAP).</summary>
    MAP = 6,
    /// <summary>Visitor Location Register (VLR).</summary>
    VLR = 7,
    /// <summary>Mobile Switching Centre (MSC).</summary>
    MSC = 8,
    /// <summary>Serving GPRS Support Node (SGSN).</summary>
    SGSN = 149,
    /// <summary>Gateway Mobile Location Centre (GMLC).</summary>
    GMLC = 145,
    /// <summary>Short Message Service Centre (SMSC) – example value.</summary>
    SMSC = 254
}