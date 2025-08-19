namespace sigtran.net.Layers.SCCP;

/// <summary>
/// Represents the numbering plan used in a Global Title.  This limited
/// subset supports only E.164 as required for basic MAP testing.  Full
/// implementations should cover the full set defined in Q.713.
/// </summary>
public enum GlobalTitlePlan : byte
{
    /// <summary>No global title present.</summary>
    None = 0,
    /// <summary>E.164 numbering plan (international telephone numbers).</summary>
    E164 = 1
}