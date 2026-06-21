using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes a maintained external peer lab workflow template.
/// </summary>
public sealed class SigtranMaintainedPeerLabWorkflowTemplate
{
    /// <summary>Creates a maintained peer lab workflow template.</summary>
    /// <param name="name">The workflow name.</param>
    /// <param name="path">The workflow file path.</param>
    /// <param name="ciProfile">The maintained peer lab CI profile.</param>
    public SigtranMaintainedPeerLabWorkflowTemplate(
        string name,
        string path,
        SigtranMaintainedPeerLabCiProfile ciProfile)
    {
        ArgumentNullException.ThrowIfNull(ciProfile);
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Workflow name is required.", nameof(name)) : name;
        Path = string.IsNullOrWhiteSpace(path) ? throw new ArgumentException("Workflow path is required.", nameof(path)) : path;
        CiProfile = ciProfile;
    }

    /// <summary>The workflow name.</summary>
    public string Name { get; }

    /// <summary>The workflow file path.</summary>
    public string Path { get; }

    /// <summary>The maintained peer lab CI profile.</summary>
    public SigtranMaintainedPeerLabCiProfile CiProfile { get; }

    /// <summary>Whether the workflow is manual-dispatch only.</summary>
    public bool ManualDispatchOnly => CiProfile.ManualDispatchOnly;

    /// <summary>Whether the workflow requires a self-hosted Linux runner.</summary>
    public bool RequiresSelfHostedLinux => CiProfile.RequiresSelfHostedLinux;

    /// <summary>The required environment variable names.</summary>
    public IReadOnlyList<string> RequiredEnvironmentVariables => CiProfile.RequiredEnvironmentVariables;

    /// <summary>The retained artifact upload patterns.</summary>
    public IReadOnlyList<string> ArtifactPatterns => CiProfile.ArtifactPatterns;

    /// <summary>Whether this workflow is safe for default pull request CI.</summary>
    public bool SafeForDefaultCi => CiProfile.SafeForDefaultCi;

    /// <summary>Whether the template is ready to be rendered for a guarded lab workflow.</summary>
    public bool IsReady => Path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
        && ManualDispatchOnly
        && RequiresSelfHostedLinux
        && RequiredEnvironmentVariables.Contains("ROUTING_CONTEXT")
        && ArtifactPatterns.Any(static pattern => pattern.Contains("/pcap/", StringComparison.Ordinal))
        && ArtifactPatterns.Any(static pattern => pattern.Contains("/reports/", StringComparison.Ordinal));

    /// <summary>Renders the GitHub Actions workflow YAML.</summary>
    /// <returns>The rendered workflow YAML.</returns>
    public string RenderYaml()
    {
        StringBuilder builder = new();
        builder.AppendLine($"name: {Name}");
        builder.AppendLine();
        builder.AppendLine("on:");
        builder.AppendLine("  workflow_dispatch:");
        builder.AppendLine("    inputs:");
        builder.AppendLine("      run-id:");
        builder.AppendLine("        description: Maintained peer lab run id");
        builder.AppendLine("        required: true");
        builder.AppendLine("        default: maintained-peer-lab-run");
        builder.AppendLine();
        builder.AppendLine("permissions:");
        builder.AppendLine("  contents: read");
        builder.AppendLine();
        builder.AppendLine("jobs:");
        builder.AppendLine("  maintained-peer-lab:");
        builder.AppendLine("    runs-on:");
        builder.AppendLine("      - self-hosted");
        builder.AppendLine("      - linux");
        builder.AppendLine("    env:");
        builder.AppendLine("      SIGTRAN_LAB_RUN_ID: ${{ inputs.run-id }}");

        foreach (string variable in RequiredEnvironmentVariables)
        {
            builder.Append("      ");
            builder.Append(variable);
            builder.Append(": ${{ vars.");
            builder.Append(variable);
            builder.AppendLine(" }}");
        }

        builder.AppendLine("    steps:");
        builder.AppendLine("      - name: Checkout");
        builder.AppendLine("        uses: actions/checkout@v4");
        builder.AppendLine("      - name: Setup .NET");
        builder.AppendLine("        uses: actions/setup-dotnet@v4");
        builder.AppendLine("        with:");
        builder.AppendLine("          dotnet-version: 10.0.x");
        builder.AppendLine("      - name: Run maintained peer lab");
        builder.AppendLine("        shell: bash");
        builder.AppendLine("        run: ./scripts/maintained-peer-lab/run.sh \"${SIGTRAN_LAB_RUN_ID}\"");
        builder.AppendLine("      - name: Upload maintained peer evidence");
        builder.AppendLine("        uses: actions/upload-artifact@v4");
        builder.AppendLine("        with:");
        builder.AppendLine("          name: maintained-peer-lab-evidence");
        builder.AppendLine("          path: |");

        foreach (string pattern in ArtifactPatterns)
        {
            builder.Append("            ");
            builder.AppendLine(pattern);
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact workflow template summary.</summary>
    /// <returns>The workflow template summary.</returns>
    public string Describe()
    {
        return $"name={Name} path={Path} manual={ManualDispatchOnly} selfHostedLinux={RequiresSelfHostedLinux} artifacts={ArtifactPatterns.Count} ready={IsReady}";
    }
}

/// <summary>
/// Provides maintained external peer lab workflow template helpers.
/// </summary>
public static class SigtranMaintainedPeerLabWorkflows
{
    /// <summary>Creates the default maintained external peer lab workflow template.</summary>
    /// <returns>The default maintained peer lab workflow template.</returns>
    public static SigtranMaintainedPeerLabWorkflowTemplate CreateDefault()
    {
        return new(
            "maintained-peer-lab",
            ".github/workflows/maintained-peer-lab.yml",
            SigtranMaintainedPeerLabCi.CreateDefault());
    }
}
