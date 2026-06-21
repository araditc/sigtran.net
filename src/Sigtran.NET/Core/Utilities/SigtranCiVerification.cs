namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes one command in the official SDK verification profile.
/// </summary>
public sealed class SigtranCiVerificationStep
{
    /// <summary>Creates a CI verification step.</summary>
    /// <param name="name">The step name.</param>
    /// <param name="command">The command to run.</param>
    public SigtranCiVerificationStep(string name, string command)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Step name is required.", nameof(name)) : name;
        Command = string.IsNullOrWhiteSpace(command) ? throw new ArgumentException("Step command is required.", nameof(command)) : command;
    }

    /// <summary>The step name.</summary>
    public string Name { get; }

    /// <summary>The command to run.</summary>
    public string Command { get; }
}

/// <summary>
/// Describes the official SDK CI verification profile.
/// </summary>
public sealed class SigtranCiVerificationProfile
{
    /// <summary>Creates a CI verification profile.</summary>
    /// <param name="dotNetVersion">The .NET SDK version pattern.</param>
    /// <param name="steps">The verification steps.</param>
    public SigtranCiVerificationProfile(string dotNetVersion, IReadOnlyList<SigtranCiVerificationStep> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        DotNetVersion = string.IsNullOrWhiteSpace(dotNetVersion) ? throw new ArgumentException(".NET version is required.", nameof(dotNetVersion)) : dotNetVersion;
        Steps = steps.Count == 0 ? throw new ArgumentException("At least one CI step is required.", nameof(steps)) : steps.ToArray();
    }

    /// <summary>The .NET SDK version pattern.</summary>
    public string DotNetVersion { get; }

    /// <summary>The verification steps.</summary>
    public IReadOnlyList<SigtranCiVerificationStep> Steps { get; }

    /// <summary>Returns the commands in execution order.</summary>
    /// <returns>The ordered command list.</returns>
    public IReadOnlyList<string> GetCommands()
    {
        return Steps.Select(static step => step.Command).ToArray();
    }
}

/// <summary>
/// Provides the official verification profile for local and CI use.
/// </summary>
public static class SigtranCiVerification
{
    /// <summary>Creates the default SDK verification profile.</summary>
    /// <returns>The default verification profile.</returns>
    public static SigtranCiVerificationProfile CreateDefaultProfile()
    {
        return new(
            "10.0.x",
            [
                new SigtranCiVerificationStep("Build", "dotnet build src/Sigtran.NET.sln --configuration Release"),
                new SigtranCiVerificationStep("Test", "dotnet run --project src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj --configuration Release"),
                new SigtranCiVerificationStep("Pack", "dotnet pack src/Sigtran.NET/Sigtran.NET.csproj --configuration Release --no-build")
            ]);
    }
}
