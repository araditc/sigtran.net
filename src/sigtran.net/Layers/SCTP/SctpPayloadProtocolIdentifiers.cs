namespace sigtran.net.Layers.SCTP;

/// <summary>
/// SCTP Payload Protocol Identifier values commonly used by SIGTRAN adaptation layers.
/// </summary>
public static class SctpPayloadProtocolIdentifiers
{
    /// <summary>M3UA Payload Protocol Identifier.</summary>
    public const uint M3ua = 3;

    /// <summary>M2PA Payload Protocol Identifier.</summary>
    public const uint M2pa = 5;

    /// <summary>
    /// Determines whether the PPID is currently recognized by the SDK.
    /// </summary>
    /// <param name="payloadProtocolIdentifier">The PPID value to check.</param>
    /// <returns>True if the PPID is known; otherwise false.</returns>
    public static bool IsKnown(uint payloadProtocolIdentifier)
    {
        return payloadProtocolIdentifier is M3ua or M2pa;
    }

    /// <summary>
    /// Requires a known SIGTRAN PPID.
    /// </summary>
    /// <param name="payloadProtocolIdentifier">The PPID value to check.</param>
    /// <param name="error">An error message when the PPID is unknown.</param>
    /// <returns>True if the PPID is known; otherwise false.</returns>
    public static bool TryRequireKnown(uint payloadProtocolIdentifier, out string? error)
    {
        if (IsKnown(payloadProtocolIdentifier))
        {
            error = null;
            return true;
        }

        error = $"Unsupported SCTP Payload Protocol Identifier {payloadProtocolIdentifier}.";
        return false;
    }
}
