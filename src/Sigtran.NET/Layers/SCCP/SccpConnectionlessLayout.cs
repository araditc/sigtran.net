using System.Buffers.Binary;

namespace Sigtran.NET.Layers.SCCP;

/// <summary>Reads Q.713 connectionless variable parts without trusting pointer order.</summary>
internal static class SccpConnectionlessLayout
{
    internal static bool TryRead(ReadOnlySpan<byte> data, int fixedLength, int firstPointer, bool longPointers,
        out ReadOnlySpan<byte> called, out ReadOnlySpan<byte> calling, out ReadOnlySpan<byte> payload,
        out ReadOnlySpan<byte> optional, out string? error)
    {
        called = calling = payload = optional = default;
        error = "Invalid SCCP variable parameter layout.";
        if (data.Length < fixedLength)
        {
            return false;
        }

        int width = longPointers ? 2 : 1;
        int payloadEnd = data.Length;
        if (fixedLength != 5)
        {
            int optionalPointer = firstPointer + 3 * width;
            int distance = ReadPointer(data, optionalPointer, longPointers);
            if (distance != 0)
            {
                payloadEnd = optionalPointer + width - 1 + distance;
                if (payloadEnd < fixedLength || payloadEnd >= data.Length)
                {
                    return false;
                }
            }
        }

        Span<(int Start, int End)> ranges = stackalloc (int, int)[3];
        for (int field = 0; field < ranges.Length; field++)
        {
            int pointer = firstPointer + field * width;
            int distance = ReadPointer(data, pointer, longPointers);
            // Q.713 counts from the most significant octet, even for a little-endian pointer.
            int start = pointer + width - 1 + distance;
            int lengthWidth = field == 2 ? width : 1;
            if (distance == 0 || start < fixedLength || start > payloadEnd - lengthWidth)
            {
                return false;
            }

            int length = lengthWidth == 2 ? BinaryPrimitives.ReadUInt16LittleEndian(data[start..]) : data[start];
            if (length == 0 || length > payloadEnd - start - lengthWidth)
            {
                return false;
            }

            int end = start + lengthWidth + length;
            ranges[field] = (start, end);
            foreach (var previous in ranges[..field])
            {
                if (start < previous.End && previous.Start < end)
                {
                    return false;
                }
            }
        }

        // Non-overlapping mandatory parameters must account for the whole variable part.
        if ((ranges[0].End - ranges[0].Start) + (ranges[1].End - ranges[1].Start)
            + (ranges[2].End - ranges[2].Start) != payloadEnd - fixedLength)
        {
            return false;
        }

        called = data[(ranges[0].Start + 1)..ranges[0].End];
        calling = data[(ranges[1].Start + 1)..ranges[1].End];
        payload = data[(ranges[2].Start + width)..ranges[2].End];
        optional = data[payloadEnd..];
        error = null;
        return true;
    }

    private static int ReadPointer(ReadOnlySpan<byte> data, int offset, bool longPointers)
        => longPointers ? BinaryPrimitives.ReadUInt16LittleEndian(data[offset..]) : data[offset];
}
