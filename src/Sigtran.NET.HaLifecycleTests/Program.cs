await RunAsync("Runtime binding opens exactly one generation on first ASP activation", RuntimeGenerationBindingRegression.FirstAspActivationOpensExactlyOneGenerationAsync);
await RunAsync("Ambiguous generation requires an explicit replacement transport epoch", RuntimeGenerationBindingRegression.AmbiguousSessionRequiresReplacementEpochAsync);
await RunAsync("Runtime reconnect waits for prior generation drain", RuntimeGenerationBindingRegression.ReconnectActivationWaitsForPriorGenerationDrainAsync);
await RunAsync("Route admission waits for transport-generation readiness", RuntimeGenerationBindingRegression.RouteAdmissionWaitsForGenerationReadinessAsync);
await RunAsync("Runtime binding rejects unexpected association identity", RuntimeGenerationBindingRegression.UnexpectedAssociationCannotOpenGenerationAsync);
await RunAsync("Runtime binding rejects missing association identity", RuntimeGenerationBindingRegression.MissingAssociationIdentityCannotOpenGenerationAsync);
await RunAsync("Runtime binding disposal closes admission and drains", RuntimeGenerationBindingRegression.DisposalClosesAdmissionAndWaitsForInFlightGenerationAsync);
await RunAsync("Runtime binding rejects an already-open sender", RuntimeGenerationBindingRegression.BindingRejectsAlreadyOpenSenderAsync);
await RunAsync("Runtime binding fences external sender activation", RuntimeGenerationBindingRegression.ExternalSenderActivationBeforeAspFailsClosedAsync);
await RunAsync("Runtime publishes Starting before synchronous session open and restart", RuntimeGenerationBindingRegression.RuntimeStartingEventPrecedesSessionOpenAcrossRestartAsync);
await RunAsync("Runtime binding ignores stale shutdown after reentrant restart", RuntimeGenerationBindingRegression.StaleShutdownCompletedCannotFenceReentrantRestartAsync);
await RunAsync("Runtime binding rejects mid-session attach and sender mismatch", RuntimeGenerationBindingRegression.BindingRejectsMidSessionAttachAndIdentityMismatchAsync);
await RunAsync("HA topology snapshot reconciles runtime generation and dispatch state", TopologyDiagnosticsRegression.SnapshotReconcilesRuntimeGenerationAndDispatchAsync);
await RunAsync("HA topology diagnostics reject association membership drift", TopologyDiagnosticsRegression.MembershipMismatchFailsClosedAsync);
await RunAsync("HA topology diagnostics reject same-name foreign runtime ownership", TopologyDiagnosticsRegression.BindingRuntimeOwnershipMismatchFailsClosedAsync);
await RunAsync("HA topology diagnostics reject foreign coordinator route pool", TopologyDiagnosticsRegression.CoordinatorPoolOwnershipMismatchFailsClosedAsync);
await RunAsync("HA topology diagnostics reject foreign dispatch sender ownership", TopologyDiagnosticsRegression.DispatchSenderOwnershipMismatchFailsClosedAsync);
await RunAsync("HA topology diagnostics reject dual runtime route-health publishing", TopologyDiagnosticsRegression.SupervisorRouteHealthPublisherMismatchFailsClosedAsync);
await RunAsync("HA runtime rejects two production lanes aliasing one runtime", TopologyDiagnosticsRegression.DuplicateProductionRuntimeAliasFailsClosedAsync);
await RunAsync("HA runtime accepts distinct production runtime identities", TopologyDiagnosticsRegression.DistinctProductionRuntimesAreAcceptedAsync);

static async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex.Message}");
        Environment.ExitCode = 1;
    }
}
