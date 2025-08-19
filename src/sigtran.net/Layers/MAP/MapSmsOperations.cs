namespace sigtran.net.Layers.MAP;

/// <summary>
/// Provides helper methods for constructing simplified MAP SMS messages.
/// In a complete implementation these methods would build ASN.1 BER
/// encoded MAP operations (e.g. MT‑ForwardSM, MO‑ForwardSM).  Here we
/// simply return the user payload for testing TCAP transport.
/// </summary>
public static class MapSmsOperations
{
    /// <summary>
    /// Creates a simplified Mobile‑Originated Forward Short Message
    /// request.  The real MAP operation carries numerous fields such as
    /// SM‑RP‑DA, SM‑RP‑OA and user data.  This method returns only the
    /// user data for demonstration.
    /// </summary>
    /// <param name="userData">The short message user data (e.g. GSM 03.40).</param>
    /// <returns>A byte array representing the operation parameters.</returns>
    public static byte[] CreateMoForwardSm(ReadOnlySpan<byte> userData)
    {
        return userData.ToArray();
    }

    /// <summary>
    /// Creates a simplified Mobile‑Terminated Forward Short Message request.
    /// </summary>
    public static byte[] CreateMtForwardSm(ReadOnlySpan<byte> userData)
    {
        return userData.ToArray();
    }
}