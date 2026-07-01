using System.Text;

namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes reference external peer lab runner file materialization.
/// </summary>
public sealed class SigtranReferencePeerLabRunnerFileMaterializationPlan
{
    private readonly string[] _directories;
    private readonly SigtranReferencePeerLabRunnerInputFile[] _inputFiles;

    /// <summary>Creates a reference peer lab runner file materialization plan.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <param name="directories">The directories to create.</param>
    /// <param name="inputFiles">The input files to create.</param>
    public SigtranReferencePeerLabRunnerFileMaterializationPlan(
        SigtranReferencePeerLabRunnerInputBundle inputBundle,
        IReadOnlyList<string> directories,
        IReadOnlyList<SigtranReferencePeerLabRunnerInputFile> inputFiles)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        ArgumentNullException.ThrowIfNull(directories);
        ArgumentNullException.ThrowIfNull(inputFiles);

        InputBundle = inputBundle;
        _directories = directories.Count == 0 ? throw new ArgumentException("At least one directory is required.", nameof(directories)) : directories.ToArray();
        _inputFiles = inputFiles.Count == 0 ? throw new ArgumentException("At least one input file is required.", nameof(inputFiles)) : inputFiles.ToArray();
    }

    /// <summary>The runner input bundle.</summary>
    public SigtranReferencePeerLabRunnerInputBundle InputBundle { get; }

    /// <summary>The directories to create.</summary>
    public IReadOnlyList<string> Directories => _directories.ToArray();

    /// <summary>The input files to create.</summary>
    public IReadOnlyList<SigtranReferencePeerLabRunnerInputFile> InputFiles => _inputFiles.ToArray();

    /// <summary>Whether directories cover the workspace required directory list.</summary>
    public bool CoversRequiredDirectories => InputBundle.Workspace.GetRequiredDirectories()
        .All(directory => _directories.Contains(directory, StringComparer.Ordinal));

    /// <summary>Whether input files cover the runner input bundle.</summary>
    public bool CoversInputFiles => InputBundle.GetInputFiles()
        .All(input => _inputFiles.Any(file => file.Path == input.Path && file.Content == input.Content));

    /// <summary>Whether the materialization plan is ready to render.</summary>
    public bool IsMaterializationReady => InputBundle.IsMaterializationReady
        && CoversRequiredDirectories
        && CoversInputFiles
        && _directories.All(static directory => !directory.Contains('\\', StringComparison.Ordinal))
        && _inputFiles.All(static file => !file.Path.Contains('\\', StringComparison.Ordinal));

    /// <summary>Renders a shell script that materializes the runner files.</summary>
    /// <returns>The shell script.</returns>
    public string RenderShellScript()
    {
        StringBuilder builder = new();
        builder.AppendLine("#!/usr/bin/env bash");
        builder.AppendLine("set -euo pipefail");
        builder.AppendLine();

        foreach (string directory in _directories)
        {
            builder.Append("mkdir -p ");
            builder.AppendLine(Quote(directory));
        }

        builder.AppendLine();
        foreach (SigtranReferencePeerLabRunnerInputFile file in _inputFiles)
        {
            builder.Append("cat > ");
            builder.Append(Quote(file.Path));
            builder.AppendLine(" <<'EOF'");
            builder.Append(file.Content);
            if (!file.Content.EndsWith('\n'))
            {
                builder.AppendLine();
            }

            builder.AppendLine("EOF");
            if (file.Path.EndsWith(".sh", StringComparison.Ordinal))
            {
                builder.Append("chmod +x ");
                builder.AppendLine(Quote(file.Path));
            }
        }

        return builder.ToString();
    }

    /// <summary>Formats a compact file materialization summary.</summary>
    /// <returns>The file materialization summary.</returns>
    public string Describe()
    {
        return $"run={InputBundle.Workspace.RunManifest.RunId} directories={_directories.Length} inputs={_inputFiles.Length} ready={IsMaterializationReady}";
    }

    private static string Quote(string value)
    {
        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }
}

/// <summary>
/// Provides reference external peer lab runner file materialization helpers.
/// </summary>
public static class SigtranReferencePeerLabRunnerFileMaterialization
{
    /// <summary>Creates the default runner file materialization plan.</summary>
    /// <param name="inputBundle">The runner input bundle.</param>
    /// <returns>The default runner file materialization plan.</returns>
    public static SigtranReferencePeerLabRunnerFileMaterializationPlan CreateDefault(SigtranReferencePeerLabRunnerInputBundle inputBundle)
    {
        ArgumentNullException.ThrowIfNull(inputBundle);
        return new(inputBundle, inputBundle.Workspace.GetRequiredDirectories(), inputBundle.GetInputFiles());
    }
}
