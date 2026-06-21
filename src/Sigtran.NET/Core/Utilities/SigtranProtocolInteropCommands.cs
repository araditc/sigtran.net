namespace Sigtran.NET.Core.Utilities;

/// <summary>
/// Describes protocol interoperability vector command requirements.
/// </summary>
public sealed class SigtranProtocolInteropCommandSet
{
    /// <summary>Creates a protocol interoperability command set.</summary>
    /// <param name="commands">The commands.</param>
    /// <param name="requiresExternalVectorRoot">Whether an external vector root is required.</param>
    /// <param name="requiresComparisonReports">Whether comparison reports are required.</param>
    public SigtranProtocolInteropCommandSet(
        IReadOnlyList<string> commands,
        bool requiresExternalVectorRoot,
        bool requiresComparisonReports)
    {
        ArgumentNullException.ThrowIfNull(commands);
        Commands = commands.Count == 0 ? throw new ArgumentException("At least one command is required.", nameof(commands)) : commands.ToArray();
        RequiresExternalVectorRoot = requiresExternalVectorRoot;
        RequiresComparisonReports = requiresComparisonReports;
    }

    /// <summary>The commands.</summary>
    public IReadOnlyList<string> Commands { get; }

    /// <summary>Whether an external vector root is required.</summary>
    public bool RequiresExternalVectorRoot { get; }

    /// <summary>Whether comparison reports are required.</summary>
    public bool RequiresComparisonReports { get; }
}

/// <summary>
/// Provides protocol interoperability command helpers.
/// </summary>
public static class SigtranProtocolInteropCommands
{
    /// <summary>Creates the default protocol interoperability command set.</summary>
    /// <returns>The default protocol interoperability command set.</returns>
    public static SigtranProtocolInteropCommandSet CreateDefault()
    {
        return new(
            [
                "dotnet build src/Sigtran.NET.sln --configuration Release",
                "SIGTRAN_PROTOCOL_VECTOR_ROOT=artifacts/protocol-vectors dotnet run --project src/Sigtran.NET.Tests/Sigtran.NET.Tests.csproj --configuration Release",
                "dotnet sigtran-trace-compare artifacts/protocol-vectors"
            ],
            requiresExternalVectorRoot: true,
            requiresComparisonReports: true);
    }
}
