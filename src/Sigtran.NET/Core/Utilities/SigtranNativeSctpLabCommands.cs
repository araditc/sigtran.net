namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes native SCTP lab command requirements.
/// </summary>
public sealed class SigtranNativeSctpLabCommandSet
{
    /// <summary>Creates a native SCTP lab command set.</summary>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresLinux">Whether Linux is required.</param>
    /// <param name="requiresLksctpTools">Whether lksctp-tools are required.</param>
    public SigtranNativeSctpLabCommandSet(
        IReadOnlyList<string> commands,
        bool requiresLinux,
        bool requiresLksctpTools)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresLinux = requiresLinux;
        RequiresLksctpTools = requiresLksctpTools;
    }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether Linux is required.</summary>
    public bool RequiresLinux { get; }

    /// <summary>Whether lksctp-tools are required.</summary>
    public bool RequiresLksctpTools { get; }
}

/// <summary>
/// Provides native SCTP lab command helpers.
/// </summary>
public static class SigtranNativeSctpLabCommands
{
    /// <summary>Creates the default native SCTP lab command set.</summary>
    /// <returns>The default native SCTP lab command set.</returns>
    public static SigtranNativeSctpLabCommandSet CreateDefault()
    {
        return new(
            [
                "uname -a",
                "modprobe sctp || true",
                "sysctl net.sctp || true",
                "dotnet build src/Sigtran.NET.sln --configuration Release",
                "SIGTRAN_NATIVE_SCTP_LAB=1 dotnet run --project src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj --configuration Release"
            ],
            requiresLinux: true,
            requiresLksctpTools: true);
    }
}
