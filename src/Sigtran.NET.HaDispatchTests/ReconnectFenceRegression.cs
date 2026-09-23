using System.Runtime.CompilerServices;

using Sigtran.NET.Layers.M3UA;
using Sigtran.NET.Layers.MTP3;

internal static class ReconnectFenceRegression
{
    [ModuleInitializer]
    internal static void Run()
    {
        ClosedGenerationRejectsBeforeSenderInvocationAsync().GetAwaiter().GetResult();
        AdministrativeFenceDrainsAdmittedWorkAsync().GetAwaiter().GetResult();
        AmbiguousFailureFencesGenerationAsync().GetAwaiter().GetResult();
        ProvenPreDispatchFailureKeepsGenerationOpenAsync().GetAwaiter().GetResult();
        PreInvocationCancellationIsKnownNotDispatchedAsync().GetAwaiter().GetResult();
    }

    private static async Task ClosedGenerationRejectsBeforeSenderInvocationAsync()
    {
        ScriptedSender inner = new("a");
        M3uaReconnectFencedAssociationSender sender = new(inner);

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);

        Equal(false, failure.DispatchMayHaveOccurred,
            "A sender without an activated transport generation must fail before dispatch ownership can transfer.");
        Equal(0, inner.Calls,
            "A closed generation must never invoke the underlying association sender.");
        Equal(false, sender.GetSnapshot().AcceptingDispatch,
            "A new reconnect-fenced sender starts fail-closed until a generation is explicitly activated.");

        Console.WriteLine("PASS Reconnect fence rejects dispatch before explicit generation activation");
    }

    private static async Task AdministrativeFenceDrainsAdmittedWorkAsync()
    {
        ScriptedSender inner = new("a", gate: true);
        M3uaReconnectFencedAssociationSender sender = new(inner);
        Equal(1L, sender.ActivateNextGeneration(),
            "The first explicit transport generation must start at one.");

        Task first = sender.SendAsync(CreateTransfer()).AsTask();
        await inner.WaitUntilEnteredAsync().ConfigureAwait(false);

        Task draining = sender
            .FenceAsync(M3uaAssociationFenceReason.RuntimeUnavailable)
            .AsTask();
        Equal(false, draining.IsCompleted,
            "Fencing must wait for work already admitted to the current generation.");

        M3uaAssociationSendException rejected = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(false, rejected.DispatchMayHaveOccurred,
            "New work after the fence closes must be known not-dispatched.");
        Equal(1, inner.Calls,
            "The fenced generation must not invoke the underlying sender for new work.");

        Throws<InvalidOperationException>(() => sender.ActivateNextGeneration(),
            "A replacement generation must not overlap unresolved work from the previous generation.");

        inner.Release();
        await first.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        await draining.WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);

        M3uaAssociationFenceSnapshot drained = sender.GetSnapshot();
        Equal(false, drained.AcceptingDispatch,
            "A drained fence remains closed until replacement activation is explicit.");
        Equal(0, drained.InFlightDispatches,
            "The fenced generation must reconcile all admitted work before replacement activation.");
        Equal(M3uaAssociationFenceReason.RuntimeUnavailable, drained.Reason,
            "The administrative/runtime fence reason must remain observable after drain.");
        Equal(2L, sender.ActivateNextGeneration(),
            "Replacement activation must advance the association generation after drain.");

        Console.WriteLine("PASS Reconnect fence closes admission and drains old generation before replacement");
    }

    private static async Task AmbiguousFailureFencesGenerationAsync()
    {
        ScriptedSender inner = new("a")
        {
            Behavior = ScriptedSendBehavior.AmbiguousFailure
        };
        M3uaReconnectFencedAssociationSender sender = new(inner);
        sender.ActivateNextGeneration();

        M3uaAssociationSendException ambiguous = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(true, ambiguous.DispatchMayHaveOccurred,
            "The scripted transport failure must remain ownership-ambiguous.");

        M3uaAssociationFenceSnapshot fenced = sender.GetSnapshot();
        Equal(false, fenced.AcceptingDispatch,
            "An ambiguous outcome must fence the current transport generation immediately.");
        Equal(M3uaAssociationFenceReason.AmbiguousOutcome, fenced.Reason,
            "Ambiguous ownership must retain stronger fence precedence than routine runtime unavailability.");
        Equal(0, fenced.InFlightDispatches,
            "The failed send lease must still be released after ambiguity is surfaced.");

        M3uaAssociationSendException rejected = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(false, rejected.DispatchMayHaveOccurred,
            "Once ambiguity fences the generation, later work must be rejected before sender invocation.");
        Equal(1, inner.Calls,
            "No second transport invocation is allowed on an ambiguity-fenced generation.");

        await sender.FenceAsync(M3uaAssociationFenceReason.RuntimeUnavailable)
            .ConfigureAwait(false);
        Equal(M3uaAssociationFenceReason.AmbiguousOutcome, sender.GetSnapshot().Reason,
            "A weaker runtime fence must not erase an ambiguity fence.");
        Equal(2L, sender.ActivateNextGeneration(),
            "Only explicit replacement activation may reopen dispatch after an ambiguity-fenced generation has drained.");

        Console.WriteLine("PASS Ambiguous send fences generation without blind replay");
    }

    private static async Task ProvenPreDispatchFailureKeepsGenerationOpenAsync()
    {
        ScriptedSender inner = new("a")
        {
            Behavior = ScriptedSendBehavior.KnownNotDispatched
        };
        M3uaReconnectFencedAssociationSender sender = new(inner);
        long generation = sender.ActivateNextGeneration();

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer()).AsTask()).ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "The scripted failure must remain positively pre-dispatch.");

        M3uaAssociationFenceSnapshot snapshot = sender.GetSnapshot();
        Equal(generation, snapshot.Generation,
            "A proven pre-dispatch failure must not manufacture a transport reconnect generation.");
        Equal(true, snapshot.AcceptingDispatch,
            "Generation fencing is reserved for uncertain/explicit transport loss, not proven pre-dispatch rejection.");
        Equal(M3uaAssociationFenceReason.None, snapshot.Reason,
            "No association-generation fence should be created by a proven pre-dispatch failure.");

        inner.Behavior = ScriptedSendBehavior.Success;
        await sender.SendAsync(CreateTransfer()).ConfigureAwait(false);
        Equal(2, inner.Calls,
            "The same generation may continue when higher-level routing policy deliberately retries after a proven pre-dispatch failure.");

        Console.WriteLine("PASS Proven pre-dispatch failure does not over-fence transport generation");
    }

    private static async Task PreInvocationCancellationIsKnownNotDispatchedAsync()
    {
        ScriptedSender inner = new("a");
        M3uaReconnectFencedAssociationSender sender = new(inner);
        sender.ActivateNextGeneration();
        using CancellationTokenSource canceled = new();
        canceled.Cancel();

        M3uaAssociationSendException failure = await CaptureSendFailureAsync(
            sender.SendAsync(CreateTransfer(), canceled.Token).AsTask()).ConfigureAwait(false);
        Equal(false, failure.DispatchMayHaveOccurred,
            "Cancellation observed before inner sender invocation must remain a known not-dispatched outcome.");
        Equal(0, inner.Calls,
            "Pre-invocation cancellation must not reach the transport sender.");
        Equal(true, sender.GetSnapshot().AcceptingDispatch,
            "Pre-invocation cancellation must not fence an otherwise healthy generation.");

        Console.WriteLine("PASS Pre-invocation cancellation preserves known-not-dispatched ownership");
    }

    private static async Task<M3uaAssociationSendException> CaptureSendFailureAsync(Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (M3uaAssociationSendException ex)
        {
            return ex;
        }

        throw new InvalidOperationException("Expected M3uaAssociationSendException.");
    }

    private static void Throws<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static Mtp3TransferMessage CreateTransfer() => new(
        new Mtp3ServiceInformationOctet(
            Mtp3ServiceIndicator.Sccp,
            networkIndicator: 2),
        new Mtp3RoutingLabel(
            destinationPointCode: 1234,
            originatingPointCode: 4321,
            signallingLinkSelection: 3),
        new byte[] { 0x01, 0x02 },
        routingContext: 100);

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{message} Expected={expected}; Actual={actual}.");
        }
    }

    private enum ScriptedSendBehavior
    {
        Success,
        KnownNotDispatched,
        AmbiguousFailure
    }

    private sealed class ScriptedSender : IM3uaAssociationSender
    {
        private readonly bool _gate;
        private readonly TaskCompletionSource<bool> _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        internal ScriptedSender(string associationName, bool gate = false)
        {
            AssociationName = associationName;
            _gate = gate;
        }

        public string AssociationName { get; }

        internal ScriptedSendBehavior Behavior { get; set; }

        internal int Calls => Volatile.Read(ref _calls);

        internal Task WaitUntilEnteredAsync() =>
            _entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        internal void Release() => _release.TrySetResult(true);

        public async ValueTask SendAsync(
            Mtp3TransferMessage message,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(message);
            Interlocked.Increment(ref _calls);
            _entered.TrySetResult(true);

            if (_gate)
            {
                await _release.Task.WaitAsync(TimeSpan.FromSeconds(2), ct)
                    .ConfigureAwait(false);
            }

            switch (Behavior)
            {
                case ScriptedSendBehavior.Success:
                    return;
                case ScriptedSendBehavior.KnownNotDispatched:
                    throw new M3uaAssociationSendException(
                        "synthetic known pre-dispatch failure",
                        dispatchMayHaveOccurred: false);
                case ScriptedSendBehavior.AmbiguousFailure:
                    throw new M3uaAssociationSendException(
                        "synthetic ambiguous transport failure",
                        dispatchMayHaveOccurred: true);
                default:
                    throw new InvalidOperationException(
                        $"Unsupported scripted behavior '{Behavior}'.");
            }
        }
    }
}
