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
await RunAsync("Runtime binding rejects mid-session attach and sender mismatch", RuntimeGenerationBindingRegression.BindingRejectsMidSessionAttachAndIdentityMismatchAsync);
await RunAsync("Stale shutdown cannot revoke a replacement runtime epoch", RuntimeShutdownOrderingRegression.StaleShutdownCannotRevokeReplacementEpochAsync);

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
