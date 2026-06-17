using System.Text;

namespace sigtran.net.Layers.M3UA;

/// <summary>
/// Provides formatting helpers for M3UA diagnostics and protocol tracing.
/// </summary>
public static class M3uaDiagnostics
{
    /// <summary>
    /// Formats a compact one-line summary of an ASP session.
    /// </summary>
    /// <param name="session">The ASP session to summarize.</param>
    /// <returns>A compact ASP session summary.</returns>
    public static string FormatAspSessionSummary(M3uaAspSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        StringBuilder builder = new();
        builder.Append("ASP state=");
        builder.Append(session.State);
        builder.Append(" aspId=");
        builder.Append(session.AspIdentifier.HasValue ? session.AspIdentifier.Value.ToString() : "none");
        builder.Append(" trafficMode=");
        builder.Append(session.TrafficModeType.HasValue ? session.TrafficModeType.Value.ToString() : "none");
        builder.Append(" routingContexts=");
        if (session.RoutingContexts.IsEmpty)
        {
            builder.Append("none");
            return builder.ToString();
        }

        for (int i = 0; i < session.RoutingContexts.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            builder.Append(session.RoutingContexts[i]);
        }

        return builder.ToString();
    }

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

    /// <summary>
    /// Decodes an M3UA packet and formats a one-line inventory of its TLV parameters.
    /// </summary>
    /// <param name="packet">The encoded M3UA packet.</param>
    /// <param name="inventory">The formatted parameter inventory on success.</param>
    /// <param name="error">An error message when the packet or parameter block cannot be decoded.</param>
    /// <returns>True if the parameter inventory was formatted; otherwise false.</returns>
    public static bool TryFormatParameterInventory(ReadOnlySpan<byte> packet, out string inventory, out string? error)
    {
        M3uaMessage message = new();
        if (!message.TryDecode(packet, out error))
        {
            inventory = string.Empty;
            return false;
        }

        StringBuilder builder = new();
        builder.Append("M3UA parameters count=");
        int count = 0;
        M3uaParameterReader reader = new(message.Parameters.Span);
        while (reader.TryRead(out M3uaParameter parameter, out error))
        {
            if (count == 0)
            {
                builder.Append('0');
                builder.Append(" [");
            }
            else
            {
                builder.Append(", ");
            }

            builder.Append(parameter.Tag);
            builder.Append(" length=");
            builder.Append(parameter.Length);
            builder.Append(" value=");
            builder.Append(parameter.Value.Length);
            builder.Append(" padded=");
            builder.Append(parameter.PaddedLength);
            count++;
        }

        if (error is not null)
        {
            inventory = string.Empty;
            return false;
        }

        if (count == 0)
        {
            inventory = "M3UA parameters count=0";
            return true;
        }

        builder.Replace("count=0", $"count={count}", 0, "M3UA parameters count=0".Length);
        builder.Append(']');
        inventory = builder.ToString();
        return true;
    }
}
