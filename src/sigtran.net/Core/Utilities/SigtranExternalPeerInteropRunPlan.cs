namespace sigtran.net.Core.Utilities;

/// <summary>
/// Describes an OpenSS7/IPSS7 interoperability execution plan.
/// </summary>
public sealed class SigtranExternalPeerInteropRunPlan
{
    /// <summary>Creates an OpenSS7/IPSS7 interoperability execution plan.</summary>
    /// <param name="template">The interop lab template.</param>
    /// <param name="environment">The required execution environment.</param>
    /// <param name="configuration">The ASP-to-SG configuration.</param>
    /// <param name="expectations">The trace expectations.</param>
    public SigtranExternalPeerInteropRunPlan(
        SigtranInteropLabTemplate template,
        SigtranExternalPeerInteropEnvironment environment,
        SigtranExternalPeerInteropConfiguration configuration,
        SigtranExternalPeerInteropTraceExpectations expectations)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        Expectations = expectations ?? throw new ArgumentNullException(nameof(expectations));
    }

    /// <summary>The interop lab template.</summary>
    public SigtranInteropLabTemplate Template { get; }

    /// <summary>The required execution environment.</summary>
    public SigtranExternalPeerInteropEnvironment Environment { get; }

    /// <summary>The ASP-to-SG configuration.</summary>
    public SigtranExternalPeerInteropConfiguration Configuration { get; }

    /// <summary>The trace expectations.</summary>
    public SigtranExternalPeerInteropTraceExpectations Expectations { get; }

    /// <summary>Whether the run plan has the required execution contracts.</summary>
    public bool IsExecutable => Environment.HasMinimumLabPrerequisites
        && Configuration.IsAspToSgReady
        && Expectations.CoversAspLifecycle
        && Expectations.RequiresDataTransfer;
}

/// <summary>
/// Provides OpenSS7/IPSS7 interoperability execution plan helpers.
/// </summary>
public static class SigtranExternalPeerInteropRunPlans
{
    /// <summary>Creates the default OpenSS7/IPSS7 ASP-to-SG execution plan.</summary>
    /// <returns>The default OpenSS7/IPSS7 ASP-to-SG execution plan.</returns>
    public static SigtranExternalPeerInteropRunPlan CreateDefaultAspToSg()
    {
        return new(
            SigtranInteropPeerProfiles.CreateExternalPeerM3uaAspToSgTemplate(),
            SigtranExternalPeerInteropEnvironments.CreateDefault(),
            SigtranExternalPeerInteropConfigurations.CreateDefaultAspToSg(),
            SigtranExternalPeerInteropTraceExpectationsCatalog.CreateAspToSg());
    }
}
