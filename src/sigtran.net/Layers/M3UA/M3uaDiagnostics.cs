using System.Text;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Provides formatting helpers for M3UA diagnostics and protocol tracing.
/// </summary>
public static class M3uaDiagnostics
{
    /// <summary>
    /// Formats bytes as an offset-based hexadecimal dump without writing to a console or logger.
    /// </summary>
    /// <param name="data">The bytes to format.</param>
    /// <param name="bytesPerLine">The number of bytes to include on each output line.</param>
    /// <returns>An offset-based hexadecimal dump.</returns>
    public static string FormatHexDump(ReadOnlySpan<byte> data, int bytesPerLine = 16)
    {
        if (bytesPerLine <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesPerLine), "Bytes per line must be positive.");
        }

        StringBuilder builder = new();
        for (int offset = 0; offset < data.Length; offset += bytesPerLine)
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            ReadOnlySpan<byte> line = data.Slice(offset, Math.Min(bytesPerLine, data.Length - offset));
            builder.Append(offset.ToString("X4"));
            builder.Append(": ");
            for (int i = 0; i < line.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(line[i].ToString("X2"));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Decodes an M3UA packet and formats a one-line summary of its common header.
    /// </summary>
    /// <param name="packet">The encoded M3UA packet.</param>
    /// <param name="summary">The formatted summary on success.</param>
    /// <param name="error">An error message when the packet cannot be decoded.</param>
    /// <returns>True if the summary was formatted; otherwise false.</returns>
    public static bool TryFormatSummary(ReadOnlySpan<byte> packet, out string summary, out string? error)
    {
        M3uaMessage message = new();
        if (!message.TryDecode(packet, out error))
        {
            summary = string.Empty;
            return false;
        }

        summary = $"M3UA v{message.Version} class={message.MessageClass} type={message.MessageType} length={message.MessageLength} parameters={message.Parameters.Length}";
        return true;
    }

    /// <summary>
    /// Decodes an M3UA packet, parses it as a supported typed message, and formats a one-line summary.
    /// </summary>
    /// <param name="packet">The encoded M3UA packet.</param>
    /// <param name="summary">The formatted typed summary on success.</param>
    /// <param name="error">An error message when the packet cannot be decoded or typed.</param>
    /// <returns>True if the typed summary was formatted; otherwise false.</returns>
    public static bool TryFormatTypedSummary(ReadOnlySpan<byte> packet, out string summary, out string? error)
    {
        M3uaMessage message = new();
        if (!message.TryDecode(packet, out error))
        {
            summary = string.Empty;
            return false;
        }

        if (!M3uaTypedMessageParser.TryParseKnown(message, out M3uaTypedMessage? typedMessage, out error))
        {
            summary = string.Empty;
            return false;
        }

        summary = $"M3UA v{message.Version} class={message.MessageClass} type={message.MessageType} kind={typedMessage!.Kind} length={message.MessageLength} parameters={message.Parameters.Length}";
        return true;
    }
}
