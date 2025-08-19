namespace sigtran.net.Core.Utilities;

/// <summary>
/// Provides hexadecimal dump output for protocol data units.  This helper
/// can be used during development to visualise byte streams that are
/// otherwise opaque.  It writes to the console in a Wireshark‑friendly
/// format.  This class is static because it maintains no state.
/// </summary>
public static class PduLogger
{
    /// <summary>
    /// Writes a labelled hex dump of the given data to the console.  Each
    /// line shows an offset and sixteen bytes.  The dump is purely for
    /// diagnostic use and has no effect on protocol logic.
    /// </summary>
    /// <param name="label">A label to prepend to the dump.</param>
    /// <param name="data">The data to dump.</param>
    public static void LogHex(string label, ReadOnlySpan<byte> data)
    {
        Console.WriteLine($"--- {label} ---");
        for (int i = 0; i < data.Length; i += 16)
        {
            ReadOnlySpan<byte> line = data.Slice(i, Math.Min(16, data.Length - i));
            Console.Write($"{i:X4}: ");
            foreach (byte b in line)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
        }
    }
}