using System.Formats.Asn1;

namespace Sigtran.NET.Core.Utilities;

/// <summary>Reuses one small standard BER writer per synchronous encoding thread.</summary>
internal static class BerWriterCache
{
    [ThreadStatic] private static AsnWriter? _writer;

    internal static BerWriterScope Rent()
    {
        AsnWriter writer = _writer ?? new(AsnEncodingRules.BER);
        _writer = null; // Nested encoding must never borrow the writer already in use.
        return new(writer);
    }

    internal static void Return(AsnWriter writer)
    {
        int size;
        try { size = writer.GetEncodedLength(); }
        catch (InvalidOperationException) { size = int.MaxValue; }
        writer.Reset(); // Clear encoded data and outstanding scopes even after an encoding failure.
        if (size <= 4096 && _writer is null) _writer = writer;
    }
}

/// <summary>A stack-bound lease; only owned copies returned by AsnWriter.Encode may escape.</summary>
internal readonly ref struct BerWriterScope(AsnWriter writer)
{
    internal AsnWriter Writer { get; } = writer;
    public void Dispose() => BerWriterCache.Return(Writer);
}
