using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a rendered maintained external peer lab environment file.
/// </summary>
public sealed class SigtranMaintainedPeerLabEnvironmentFile
{
    private readonly SortedDictionary<string, string> _variables;

    /// <summary>Creates a maintained external peer lab environment file.</summary>
    /// <param name="variables">The environment variables.</param>
    public SigtranMaintainedPeerLabEnvironmentFile(IReadOnlyDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        if (variables.Count == 0)
        {
            throw new ArgumentException("At least one environment variable is required.", nameof(variables));
        }

        _variables = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> variable in variables)
        {
            _variables[variable.Key] = variable.Value;
        }
    }

    /// <summary>The environment variables.</summary>
    public IReadOnlyDictionary<string, string> Variables => new SortedDictionary<string, string>(_variables, StringComparer.Ordinal);

    /// <summary>Renders the environment file content.</summary>
    /// <returns>The environment file content.</returns>
    public string Render()
    {
        StringBuilder builder = new();
        builder.AppendLine("# SIGTRAN.NET maintained peer lab environment");

        foreach (KeyValuePair<string, string> variable in _variables)
        {
            builder.Append(variable.Key);
            builder.Append('=');
            builder.AppendLine(Escape(variable.Value));
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact environment file summary.</summary>
    /// <returns>The environment file summary.</returns>
    public string Describe()
    {
        return $"variables={_variables.Count} rendered={Render().Length}";
    }

    private static string Escape(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }
}

/// <summary>
/// Provides maintained external peer lab environment file helpers.
/// </summary>
public static class SigtranMaintainedPeerLabEnvironmentFiles
{
    /// <summary>Creates an environment file from a maintained peer lab run manifest.</summary>
    /// <param name="manifest">The maintained peer lab run manifest.</param>
    /// <returns>The maintained peer lab environment file.</returns>
    public static SigtranMaintainedPeerLabEnvironmentFile FromManifest(SigtranMaintainedPeerLabRunManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        Dictionary<string, string> variables = new(StringComparer.Ordinal)
        {
            ["ARTIFACT_ROOT"] = manifest.Configuration.ArtifactRoot,
            ["DPC"] = manifest.Configuration.DestinationPointCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["LOCAL_IP"] = manifest.Configuration.LocalIp,
            ["LOCAL_SCTP_PORT"] = manifest.Configuration.LocalSctpPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["NETWORK_INDICATOR"] = manifest.Configuration.NetworkIndicator.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["OPC"] = manifest.Configuration.OriginatingPointCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["PEER_NAME"] = manifest.Configuration.PeerName,
            ["REMOTE_IP"] = manifest.Configuration.RemoteIp,
            ["REMOTE_SCTP_PORT"] = manifest.Configuration.RemoteSctpPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ROUTING_CONTEXT"] = manifest.Configuration.RoutingContext.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["SERVICE_INDICATOR"] = manifest.Configuration.ServiceIndicator.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["SIGTRAN_ADAPTATION"] = manifest.Configuration.Adaptation,
            ["TRAFFIC_MODE"] = manifest.Configuration.TrafficMode,
            ["SIGTRAN_LAB_RUN_ID"] = manifest.RunId
        };

        foreach (KeyValuePair<string, string> variable in manifest.Binding.EnvironmentVariables)
        {
            variables[variable.Key] = variable.Value;
        }

        return new SigtranMaintainedPeerLabEnvironmentFile(variables);
    }
}
