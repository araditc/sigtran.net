await RunAsync(
    "Tracked runtime send waits for lower-layer transport completion",
    RuntimeOutboundOwnershipRegressions.TrackedSendWaitsForTransportCompletionAsync);
await RunAsync(
    "Caller cancellation before transport invocation remains known not-dispatched",
    RuntimeOutboundOwnershipRegressions.CallerCancellationBeforeTransportInvocationIsKnownNotDispatchedAsync);
await RunAsync(
    "Failure after transport invocation remains ambiguous",
    RuntimeOutboundOwnershipRegressions.FailureAfterTransportInvocationIsAmbiguousAsync);
await RunAsync(
    "Replacement generation does not carry tracked work",
    RuntimeOutboundOwnershipRegressions.ReplacementGenerationDoesNotCarryTrackedWorkAsync);
await RunAsync(
    "Stale tracked work fails closed before transport",
    RuntimeOutboundOwnershipRegressions.StaleWorkItemFailsClosedWithoutTransportAsync);

static async Task RunAsync(string name, Func<Task> test)
{
    try
    {
        await test().ConfigureAwait(false);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"FAIL {name}: {ex}");
        Environment.ExitCode = 1;
    }
}
