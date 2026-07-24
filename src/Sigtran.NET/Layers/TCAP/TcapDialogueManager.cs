using System.Threading.Channels;

using Sigtran.NET.Layers.SCCP;

namespace Sigtran.NET.Layers.TCAP;

/// <summary>
/// Coordinates concurrent TCAP dialogues and invoke correlation over a stateful SCCP service.
/// </summary>
public sealed class TcapDialogueManager : ITcapComponentDialogues, IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly object _allocationSync = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly TcapDialogueManagerOptions _options;
    private readonly TcapTransactionIdAllocator _transactionIds;
    private readonly Dictionary<long, DialogueContext> _dialogues = [];
    private readonly Dictionary<string, long> _dialoguesByLocalTransactionId =
        new(StringComparer.Ordinal);
    private readonly Dictionary<InvokeKey, PendingInvoke> _invokeCompletions = [];
    private readonly Channel<TcapDialogueEvent> _events;
    private readonly Channel<TcapComponentIndication> _components;
    private CancellationTokenSource? _lifetime;
    private Task? _receiveTask;
    private Task? _timerTask;
    private Exception? _lastFailure;
    private long _nextDialogueId;
    private long _openedDialogues;
    private long _closedDialogues;
    private long _sentComponents;
    private long _receivedComponents;
    private long _timedOutInvokes;
    private long _rejectedComponents;
    private long _malformedTransactions;
    private bool _disposed;

    /// <summary>Creates a concurrent TCAP dialogue manager.</summary>
    /// <param name="sccp">The lower stateful SCCP service.</param>
    /// <param name="options">The dialogue manager options.</param>
    /// <param name="protocolClass">The SCCP class used for outbound TCAP traffic.</param>
    /// <param name="transactionIds">The optional transaction-id allocator.</param>
    public TcapDialogueManager(
        ISccpService sccp,
        TcapDialogueManagerOptions? options = null,
        SccpProtocolClass? protocolClass = null,
        TcapTransactionIdAllocator? transactionIds = null)
    {
        Sccp = sccp ?? throw new ArgumentNullException(nameof(sccp));
        _options = options ?? new();
        ProtocolClass = protocolClass
            ?? new(
                SccpConnectionlessClass.Class1,
                returnMessageOnError: true);
        _transactionIds = transactionIds ?? new();
        _events = Channel.CreateBounded<TcapDialogueEvent>(
            new BoundedChannelOptions(_options.EventQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });
        _components = Channel.CreateBounded<TcapComponentIndication>(
            new BoundedChannelOptions(_options.ComponentQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });
    }

    /// <inheritdoc />
    public ISccpService Sccp { get; }

    /// <summary>The SCCP protocol class used for outbound TCAP traffic.</summary>
    public SccpProtocolClass ProtocolClass { get; }

    /// <summary>Whether the receive and timer loops are active.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _receiveTask is not null
                    && !_receiveTask.IsCompleted
                    && _timerTask is not null
                    && !_timerTask.IsCompleted;
            }
        }
    }

    /// <summary>The most recent unhandled manager-loop failure.</summary>
    public Exception? LastFailure
    {
        get
        {
            lock (_sync)
            {
                return _lastFailure;
            }
        }
    }

    /// <summary>Starts SCCP receive ownership and the shared invoke timer.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the loops have started.</returns>
    public async ValueTask StartAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsRunning)
            {
                return;
            }

            if (_options.ManageSccpLifecycle)
            {
                await Sccp.StartAsync(ct).ConfigureAwait(false);
            }

            lock (_sync)
            {
                _lastFailure = null;
                _lifetime = new();
                _receiveTask = ReceiveLoopAsync(_lifetime.Token);
                _timerTask = TimerLoopAsync(_lifetime.Token);
            }

            await Task.Yield();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>Stops receive ownership and the shared invoke timer.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the loops have stopped.</returns>
    public async ValueTask StopAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            Task? receiveTask;
            Task? timerTask;
            CancellationTokenSource? lifetime;
            lock (_sync)
            {
                receiveTask = _receiveTask;
                timerTask = _timerTask;
                lifetime = _lifetime;
            }

            if (receiveTask is null && timerTask is null)
            {
                return;
            }

            lifetime?.Cancel();
            Task[] tasks = [receiveTask ?? Task.CompletedTask, timerTask ?? Task.CompletedTask];
            try
            {
                await Task.WhenAll(tasks).WaitAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (lifetime?.IsCancellationRequested == true)
            {
            }

            lock (_sync)
            {
                _receiveTask = null;
                _timerTask = null;
                _lifetime = null;
            }

            lifetime?.Dispose();
            if (_options.ManageSccpLifecycle)
            {
                await Sccp.StopAsync(ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<TcapDialogueHandle> BeginAsync(
        TcapBeginRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Transaction.PackageType != TcapPackageType.Begin)
        {
            throw new ArgumentException(
                "TCAP Begin request requires a Begin package.",
                nameof(request));
        }

        TcapTransactionId localId =
            request.Transaction.OriginatingTransactionId ?? AllocateTransactionId();
        DialogueContext context = CreateDialogue(
            request.CalledParty,
            request.CallingParty,
            localId,
            request.Transaction.DestinationTransactionId,
            TcapDialoguePhase.Open);
        TcapTransactionMessage transaction = new(
            TcapPackageType.Begin,
            localId,
            request.Transaction.DestinationTransactionId,
            request.Transaction.ComponentPortion,
            request.Transaction.DialoguePortion);
        try
        {
            await SendTransactionAsync(context, transaction, ct).ConfigureAwait(false);
            return context.Handle;
        }
        catch
        {
            CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask ContinueAsync(
        TcapDialogueHandle dialogue,
        TcapContinueRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DialogueContext context = GetDialogue(dialogue);
        TcapTransactionMessage transaction = NormalizeOutbound(
            context,
            TcapPackageType.Continue,
            request.Transaction);
        await SendTransactionAsync(context, transaction, ct).ConfigureAwait(false);
        lock (context.Sync)
        {
            context.Phase = TcapDialoguePhase.Continuing;
        }
    }

    /// <inheritdoc />
    public async ValueTask EndAsync(
        TcapDialogueHandle dialogue,
        TcapEndRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DialogueContext context = GetDialogue(dialogue);
        TcapTransactionMessage transaction = NormalizeOutbound(
            context,
            TcapPackageType.End,
            request.Transaction);
        await SendTransactionAsync(context, transaction, ct).ConfigureAwait(false);
        CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
    }

    /// <inheritdoc />
    public ValueTask<TcapDialogueEvent> ReceiveAsync(
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        EnsureRunning();
        return _events.Reader.ReadAsync(ct);
    }

    /// <summary>Begins a dialogue with one tracked Invoke component.</summary>
    /// <param name="request">The Begin/Invoke request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The tracked invoke handle.</returns>
    public async ValueTask<TcapInvokeHandle> BeginInvokeAsync(
        TcapBeginInvokeRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        TcapTransactionId localId = AllocateTransactionId();
        DialogueContext context = CreateDialogue(
            request.CalledParty,
            request.CallingParty,
            localId,
            remoteTransactionId: null,
            TcapDialoguePhase.Open);
        PendingInvoke pending = RegisterPendingInvoke(
            context,
            request.OperationCode,
            request.Timeout);
        TcapBerInvokeComponent component = new(
            pending.Handle.InvokeId,
            request.OperationCode,
            request.Parameters);
        TcapTransactionMessage transaction = new(
            TcapPackageType.Begin,
            localId,
            destinationTransactionId: null,
            component.Encode(),
            request.DialoguePortion);
        try
        {
            await SendTransactionAsync(context, transaction, ct).ConfigureAwait(false);
            Interlocked.Increment(ref _sentComponents);
            return pending.Handle;
        }
        catch
        {
            CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
            ForgetPending(pending);
            throw;
        }
    }

    /// <summary>Sends one tracked Invoke component on an existing dialogue.</summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="request">The Invoke request.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The tracked invoke handle.</returns>
    public async ValueTask<TcapInvokeHandle> InvokeAsync(
        TcapDialogueHandle dialogue,
        TcapInvokeRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        DialogueContext context = GetDialogue(dialogue);
        EnsureRemoteTransactionId(context);
        PendingInvoke pending = RegisterPendingInvoke(
            context,
            request.OperationCode,
            request.Timeout);
        TcapBerInvokeComponent component = new(
            pending.Handle.InvokeId,
            request.OperationCode,
            request.Parameters,
            request.LinkedInvokeId);
        TcapTransactionMessage transaction = CreateTransaction(
            context,
            TcapPackageType.Continue,
            component.Encode());
        try
        {
            await SendTransactionAsync(context, transaction, ct).ConfigureAwait(false);
            Interlocked.Increment(ref _sentComponents);
            return pending.Handle;
        }
        catch
        {
            RemovePendingInvoke(context, pending.Handle.InvokeId);
            CompletePending(pending, TcapInvokeOutcomeKind.DialogueClosed);
            ForgetPending(pending);
            throw;
        }
    }

    /// <summary>Waits for a tracked invoke to complete, fail, reject, or time out.</summary>
    /// <param name="invoke">The tracked invoke.</param>
    /// <param name="ct">A cancellation token that only cancels this waiter.</param>
    /// <returns>The terminal invoke outcome.</returns>
    public async ValueTask<TcapInvokeOutcome> WaitForInvokeAsync(
        TcapInvokeHandle invoke,
        CancellationToken ct = default)
    {
        PendingInvoke pending;
        InvokeKey key = new(invoke.Dialogue.DialogueId, invoke.InvokeId);
        lock (_sync)
        {
            if (!_invokeCompletions.TryGetValue(key, out pending!))
            {
                throw new InvalidOperationException(
                    $"TCAP invoke {invoke.InvokeId} on dialogue {invoke.Dialogue} is unknown.");
            }
        }

        TcapInvokeOutcome outcome =
            await pending.Completion.Task.WaitAsync(ct).ConfigureAwait(false);
        lock (_sync)
        {
            _invokeCompletions.Remove(key);
        }

        return outcome;
    }

    /// <summary>Receives one decoded inbound TCAP component.</summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The next component indication.</returns>
    public ValueTask<TcapComponentIndication> ReceiveComponentAsync(
        CancellationToken ct = default)
    {
        ThrowIfDisposed();
        EnsureRunning();
        return _components.Reader.ReadAsync(ct);
    }

    /// <summary>Sends a ReturnResult component for an inbound invoke.</summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="invokeId">The inbound invoke identifier.</param>
    /// <param name="operationCode">The optional result operation code.</param>
    /// <param name="parameters">The result parameters.</param>
    /// <param name="endDialogue">Whether to send End instead of Continue.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the result is sent.</returns>
    public ValueTask SendResultAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        TcapOperationCode? operationCode,
        ReadOnlyMemory<byte> parameters,
        bool endDialogue = false,
        CancellationToken ct = default)
    {
        TcapBerReturnResultComponent component =
            new(invokeId, operationCode, parameters);
        return SendInboundInvokeResponseAsync(
            dialogue,
            invokeId,
            component.Encode(),
            endDialogue,
            ct);
    }

    /// <summary>Sends a ReturnError component for an inbound invoke.</summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="invokeId">The inbound invoke identifier.</param>
    /// <param name="errorCode">The return error code.</param>
    /// <param name="parameters">The error parameters.</param>
    /// <param name="endDialogue">Whether to send End instead of Continue.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the error is sent.</returns>
    public ValueTask SendErrorAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        TcapReturnErrorCode errorCode,
        ReadOnlyMemory<byte> parameters,
        bool endDialogue = false,
        CancellationToken ct = default)
    {
        TcapBerReturnErrorComponent component =
            new(invokeId, errorCode, parameters);
        return SendInboundInvokeResponseAsync(
            dialogue,
            invokeId,
            component.Encode(),
            endDialogue,
            ct);
    }

    /// <summary>Sends a Reject component for an inbound invoke.</summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="invokeId">The inbound invoke identifier.</param>
    /// <param name="problemCode">The reject problem code.</param>
    /// <param name="endDialogue">Whether to send End instead of Continue.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when the reject is sent.</returns>
    public ValueTask SendRejectAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        TcapRejectProblemCode problemCode,
        bool endDialogue = false,
        CancellationToken ct = default)
    {
        TcapBerRejectComponent component = new(invokeId, problemCode);
        return SendInboundInvokeResponseAsync(
            dialogue,
            invokeId,
            component.Encode(),
            endDialogue,
            ct);
    }

    /// <summary>Aborts and removes an active dialogue.</summary>
    /// <param name="dialogue">The active dialogue.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>A value task that completes when Abort is sent.</returns>
    public async ValueTask AbortAsync(
        TcapDialogueHandle dialogue,
        CancellationToken ct = default)
    {
        DialogueContext context = GetDialogue(dialogue);
        bool canAddressPeer;
        lock (context.Sync)
        {
            canAddressPeer = context.RemoteTransactionId.HasValue;
            context.Phase = TcapDialoguePhase.Aborted;
        }

        if (canAddressPeer)
        {
            TcapTransactionMessage transaction = CreateTransaction(
                context,
                TcapPackageType.Abort,
                componentPortion: default);
            try
            {
                await SendTransactionAsync(context, transaction, ct)
                    .ConfigureAwait(false);
            }
            finally
            {
                CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
            }

            return;
        }

        CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
    }

    /// <summary>Returns a snapshot of active dialogues.</summary>
    /// <returns>The active dialogue snapshots.</returns>
    public IReadOnlyList<TcapDialogueSnapshot> SnapshotDialogues()
    {
        DialogueContext[] contexts;
        lock (_sync)
        {
            contexts = _dialogues.Values.ToArray();
        }

        return contexts
            .Select(context =>
            {
                lock (context.Sync)
                {
                    return new TcapDialogueSnapshot(
                        context.Handle,
                        context.Phase,
                        context.LocalTransactionId,
                        context.RemoteTransactionId,
                        context.PendingOutbound.Count,
                        context.ActiveInbound.Count);
                }
            })
            .ToArray();
    }

    /// <summary>Returns a point-in-time TCAP manager metrics snapshot.</summary>
    /// <returns>The current metrics.</returns>
    public TcapDialogueManagerMetrics GetMetrics()
    {
        int pending;
        int active;
        lock (_sync)
        {
            active = _dialogues.Count;
            pending = _dialogues.Values.Sum(context =>
            {
                lock (context.Sync)
                {
                    return context.PendingOutbound.Count;
                }
            });
        }

        return new(
            Interlocked.Read(ref _openedDialogues),
            Interlocked.Read(ref _closedDialogues),
            Interlocked.Read(ref _sentComponents),
            Interlocked.Read(ref _receivedComponents),
            Interlocked.Read(ref _timedOutInvokes),
            Interlocked.Read(ref _rejectedComponents),
            Interlocked.Read(ref _malformedTransactions),
            active,
            pending);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            await StopAsync().ConfigureAwait(false);
        }
        finally
        {
            DialogueContext[] contexts;
            lock (_sync)
            {
                contexts = _dialogues.Values.ToArray();
            }

            foreach (DialogueContext context in contexts)
            {
                CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
            }

            lock (_sync)
            {
                _invokeCompletions.Clear();
            }

            _disposed = true;
            _events.Writer.TryComplete();
            _components.Writer.TryComplete();
            _lifecycleGate.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                SccpDataIndication indication =
                    await Sccp.ReceiveAsync(ct).ConfigureAwait(false);
                if (!TcapTransactionMessage.TryDecode(
                        indication.UserData.Span,
                        out TcapTransactionMessage? transaction,
                        out _))
                {
                    Interlocked.Increment(ref _malformedTransactions);
                    continue;
                }

                await ProcessTransactionAsync(
                    indication,
                    transaction!,
                    ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _lastFailure = ex;
            }
        }
    }

    private async Task TimerLoopAsync(CancellationToken ct)
    {
        using PeriodicTimer timer = new(_options.TimerResolution);
        try
        {
            while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                ExpireInvokes(DateTimeOffset.UtcNow);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            lock (_sync)
            {
                _lastFailure = ex;
            }
        }
    }

    private async Task ProcessTransactionAsync(
        SccpDataIndication indication,
        TcapTransactionMessage transaction,
        CancellationToken ct)
    {
        DialogueContext? context;
        try
        {
            context = transaction.PackageType switch
            {
                TcapPackageType.Begin => CreateInboundDialogue(indication, transaction),
                TcapPackageType.Unidirectional => null,
                _ => FindDialogue(transaction.DestinationTransactionId)
            };
        }
        catch (InvalidOperationException)
        {
            Interlocked.Increment(ref _malformedTransactions);
            return;
        }

        if (transaction.PackageType != TcapPackageType.Unidirectional
            && context is null)
        {
            Interlocked.Increment(ref _malformedTransactions);
            return;
        }

        TcapDialogueHandle handle = context?.Handle ?? new(0);
        if (context is not null)
        {
            lock (context.Sync)
            {
                if (transaction.OriginatingTransactionId.HasValue)
                {
                    context.RemoteTransactionId =
                        transaction.OriginatingTransactionId;
                }

                context.Phase = transaction.PackageType switch
                {
                    TcapPackageType.Begin => TcapDialoguePhase.Open,
                    TcapPackageType.Continue => TcapDialoguePhase.Continuing,
                    TcapPackageType.End => TcapDialoguePhase.Ended,
                    TcapPackageType.Abort => TcapDialoguePhase.Aborted,
                    _ => context.Phase
                };
            }
        }

        await _events.Writer.WriteAsync(
            new(handle, transaction),
            ct).ConfigureAwait(false);
        if (context is not null && !transaction.ComponentPortion.IsEmpty)
        {
            await ProcessComponentsAsync(
                context,
                transaction.ComponentPortion,
                ct).ConfigureAwait(false);
        }

        if (context is not null
            && transaction.PackageType is TcapPackageType.End or TcapPackageType.Abort)
        {
            CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
        }
    }

    private async Task ProcessComponentsAsync(
        DialogueContext context,
        ReadOnlyMemory<byte> encodedComponents,
        CancellationToken ct)
    {
        ReadOnlyMemory<byte> remaining = encodedComponents;
        while (!remaining.IsEmpty)
        {
            if (!TcapBer.TryReadElement(
                    remaining.Span,
                    out TcapBerElement element,
                    out _))
            {
                Interlocked.Increment(ref _malformedTransactions);
                return;
            }

            ReadOnlyMemory<byte> encoded =
                remaining.Slice(0, element.TotalLength);
            await ProcessComponentAsync(context, element, encoded, ct)
                .ConfigureAwait(false);
            remaining = remaining.Slice(element.TotalLength);
        }
    }

    private async Task ProcessComponentAsync(
        DialogueContext context,
        TcapBerElement element,
        ReadOnlyMemory<byte> encoded,
        CancellationToken ct)
    {
        if (element.Tag.TagClass != TcapBerTagClass.ContextSpecific
            || !Enum.IsDefined(typeof(TcapComponentType), element.Tag.Number))
        {
            Interlocked.Increment(ref _malformedTransactions);
            return;
        }

        TcapComponentType type = (TcapComponentType)element.Tag.Number;
        TcapComponentIndication? indication = type switch
        {
            TcapComponentType.Invoke =>
                DecodeInvoke(context, encoded.Span),
            TcapComponentType.ReturnResultLast =>
                DecodeResult(context, encoded.Span),
            TcapComponentType.ReturnError =>
                DecodeError(context, encoded.Span),
            TcapComponentType.Reject =>
                DecodeReject(context, encoded.Span),
            _ => null
        };

        if (indication is null)
        {
            Interlocked.Increment(ref _malformedTransactions);
            return;
        }

        Interlocked.Increment(ref _receivedComponents);
        if (indication.ComponentType == TcapComponentType.Invoke)
        {
            await _components.Writer.WriteAsync(indication, ct).ConfigureAwait(false);
        }
    }

    private TcapComponentIndication? DecodeInvoke(
        DialogueContext context,
        ReadOnlySpan<byte> encoded)
    {
        if (!TcapBerInvokeComponent.TryDecode(
                encoded,
                out TcapBerInvokeComponent? component,
                out _))
        {
            return null;
        }

        lock (context.Sync)
        {
            if (!context.ActiveInbound.Add(component!.InvokeId))
            {
                Interlocked.Increment(ref _rejectedComponents);
                return null;
            }
        }

        return new(
            context.Handle,
            TcapComponentType.Invoke,
            component.InvokeId,
            component.OperationCode,
            component.Parameters,
            errorCode: null,
            problemCode: null);
    }

    private TcapComponentIndication? DecodeResult(
        DialogueContext context,
        ReadOnlySpan<byte> encoded)
    {
        if (!TcapBerReturnResultComponent.TryDecode(
                encoded,
                out TcapBerReturnResultComponent? component,
                out _))
        {
            return null;
        }

        PendingInvoke? pending =
            RemovePendingInvoke(context, component!.InvokeId);
        if (pending is not null)
        {
            CompletePending(
                pending,
                TcapInvokeOutcomeKind.Result,
                component.Parameters);
        }

        return new(
            context.Handle,
            TcapComponentType.ReturnResultLast,
            component.InvokeId,
            component.OperationCode,
            component.Parameters,
            errorCode: null,
            problemCode: null);
    }

    private TcapComponentIndication? DecodeError(
        DialogueContext context,
        ReadOnlySpan<byte> encoded)
    {
        if (!TcapBerReturnErrorComponent.TryDecode(
                encoded,
                out TcapBerReturnErrorComponent? component,
                out _))
        {
            return null;
        }

        PendingInvoke? pending =
            RemovePendingInvoke(context, component!.InvokeId);
        if (pending is not null)
        {
            CompletePending(
                pending,
                TcapInvokeOutcomeKind.Error,
                component.Parameters,
                component.ErrorCode);
        }

        return new(
            context.Handle,
            TcapComponentType.ReturnError,
            component.InvokeId,
            operationCode: null,
            component.Parameters,
            component.ErrorCode,
            problemCode: null);
    }

    private TcapComponentIndication? DecodeReject(
        DialogueContext context,
        ReadOnlySpan<byte> encoded)
    {
        if (!TcapBerRejectComponent.TryDecode(
                encoded,
                out TcapBerRejectComponent? component,
                out _))
        {
            return null;
        }

        PendingInvoke? pending =
            RemovePendingInvoke(context, component!.InvokeId);
        if (pending is not null)
        {
            CompletePending(
                pending,
                TcapInvokeOutcomeKind.Reject,
                parameters: default,
                errorCode: null,
                component.ProblemCode);
        }

        Interlocked.Increment(ref _rejectedComponents);
        return new(
            context.Handle,
            TcapComponentType.Reject,
            component.InvokeId,
            operationCode: null,
            parameters: default,
            errorCode: null,
            component.ProblemCode);
    }

    private async ValueTask SendInboundInvokeResponseAsync(
        TcapDialogueHandle dialogue,
        byte invokeId,
        ReadOnlyMemory<byte> component,
        bool endDialogue,
        CancellationToken ct)
    {
        DialogueContext context = GetDialogue(dialogue);
        lock (context.Sync)
        {
            if (!context.ActiveInbound.Remove(invokeId))
            {
                throw new InvalidOperationException(
                    $"TCAP inbound invoke {invokeId} is not active.");
            }
        }

        TcapPackageType packageType =
            endDialogue ? TcapPackageType.End : TcapPackageType.Continue;
        TcapTransactionMessage transaction =
            CreateTransaction(context, packageType, component);
        await SendTransactionAsync(context, transaction, ct).ConfigureAwait(false);
        Interlocked.Increment(ref _sentComponents);
        if (endDialogue)
        {
            CloseDialogue(context, TcapInvokeOutcomeKind.DialogueClosed);
        }
    }

    private async ValueTask SendTransactionAsync(
        DialogueContext context,
        TcapTransactionMessage transaction,
        CancellationToken ct)
    {
        await Sccp.SendAsync(
            new(
                ProtocolClass,
                context.CalledParty,
                context.CallingParty,
                transaction.Encode()),
            ct).ConfigureAwait(false);
    }

    private TcapTransactionMessage NormalizeOutbound(
        DialogueContext context,
        TcapPackageType packageType,
        TcapTransactionMessage transaction)
    {
        return new(
            packageType,
            packageType == TcapPackageType.Continue
                ? context.LocalTransactionId
                : null,
            context.RemoteTransactionId
                ?? transaction.DestinationTransactionId
                ?? throw new InvalidOperationException(
                    "TCAP peer transaction id is not known."),
            transaction.ComponentPortion,
            transaction.DialoguePortion);
    }

    private static TcapTransactionMessage CreateTransaction(
        DialogueContext context,
        TcapPackageType packageType,
        ReadOnlyMemory<byte> componentPortion)
    {
        lock (context.Sync)
        {
            TcapTransactionId remote = context.RemoteTransactionId
                ?? throw new InvalidOperationException(
                    "TCAP peer transaction id is not known.");
            return new(
                packageType,
                packageType == TcapPackageType.Continue
                    ? context.LocalTransactionId
                    : null,
                remote,
                componentPortion);
        }
    }

    private DialogueContext CreateDialogue(
        SccpPartyAddress calledParty,
        SccpPartyAddress callingParty,
        TcapTransactionId localTransactionId,
        TcapTransactionId? remoteTransactionId,
        TcapDialoguePhase phase)
    {
        TcapDialogueHandle handle =
            new(Interlocked.Increment(ref _nextDialogueId));
        DialogueContext context = new(
            handle,
            calledParty,
            callingParty,
            localTransactionId,
            remoteTransactionId,
            phase);
        lock (_sync)
        {
            if (_dialogues.Count >= _options.MaximumDialogues)
            {
                throw new InvalidOperationException(
                    "TCAP active dialogue capacity is exhausted.");
            }

            string key = localTransactionId.ToString();
            if (_dialoguesByLocalTransactionId.ContainsKey(key))
            {
                throw new InvalidOperationException(
                    $"TCAP local transaction id {key} is already active.");
            }

            _dialogues.Add(handle.DialogueId, context);
            _dialoguesByLocalTransactionId.Add(key, handle.DialogueId);
        }

        Interlocked.Increment(ref _openedDialogues);
        return context;
    }

    private DialogueContext? CreateInboundDialogue(
        SccpDataIndication indication,
        TcapTransactionMessage transaction)
    {
        if (!transaction.OriginatingTransactionId.HasValue)
        {
            return null;
        }

        return CreateDialogue(
            calledParty: indication.CallingParty,
            callingParty: indication.CalledParty,
            AllocateTransactionId(),
            transaction.OriginatingTransactionId,
            TcapDialoguePhase.Open);
    }

    private DialogueContext GetDialogue(TcapDialogueHandle handle)
    {
        lock (_sync)
        {
            if (!_dialogues.TryGetValue(handle.DialogueId, out DialogueContext? context))
            {
                throw new InvalidOperationException(
                    $"TCAP dialogue {handle.DialogueId} is not active.");
            }

            return context;
        }
    }

    private DialogueContext? FindDialogue(TcapTransactionId? destination)
    {
        if (!destination.HasValue)
        {
            return null;
        }

        lock (_sync)
        {
            return _dialoguesByLocalTransactionId.TryGetValue(
                    destination.Value.ToString(),
                    out long dialogueId)
                && _dialogues.TryGetValue(dialogueId, out DialogueContext? context)
                    ? context
                    : null;
        }
    }

    private PendingInvoke RegisterPendingInvoke(
        DialogueContext context,
        TcapOperationCode operationCode,
        TimeSpan? timeout)
    {
        PendingInvoke pending;
        lock (context.Sync)
        {
            if (context.PendingOutbound.Count
                >= _options.MaximumPendingInvokesPerDialogue)
            {
                throw new InvalidOperationException(
                    "TCAP pending invoke capacity is exhausted.");
            }

            byte invokeId = AllocateInvokeId(context);
            TcapInvokeHandle handle = new(context.Handle, invokeId);
            pending = new(
                handle,
                operationCode,
                DateTimeOffset.UtcNow + (timeout ?? _options.InvokeTimeout));
            context.PendingOutbound.Add(invokeId, pending);
        }

        lock (_sync)
        {
            _invokeCompletions.Add(
                new(context.Handle.DialogueId, pending.Handle.InvokeId),
                pending);
        }

        return pending;
    }

    private static byte AllocateInvokeId(DialogueContext context)
    {
        for (int index = 0; index < byte.MaxValue; index++)
        {
            byte candidate = context.NextInvokeId;
            context.NextInvokeId =
                context.NextInvokeId == byte.MaxValue
                    ? (byte)1
                    : (byte)(context.NextInvokeId + 1);
            if (!context.PendingOutbound.ContainsKey(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No TCAP invoke identifiers are available.");
    }

    private static PendingInvoke? RemovePendingInvoke(
        DialogueContext context,
        byte invokeId)
    {
        lock (context.Sync)
        {
            if (!context.PendingOutbound.Remove(
                    invokeId,
                    out PendingInvoke? pending))
            {
                return null;
            }

            return pending;
        }
    }

    private void ExpireInvokes(DateTimeOffset now)
    {
        DialogueContext[] contexts;
        lock (_sync)
        {
            contexts = _dialogues.Values.ToArray();
        }

        foreach (DialogueContext context in contexts)
        {
            PendingInvoke[] expired;
            lock (context.Sync)
            {
                expired = context.PendingOutbound.Values
                    .Where(pending => pending.DeadlineUtc <= now)
                    .ToArray();
                foreach (PendingInvoke pending in expired)
                {
                    context.PendingOutbound.Remove(pending.Handle.InvokeId);
                }
            }

            foreach (PendingInvoke pending in expired)
            {
                CompletePending(pending, TcapInvokeOutcomeKind.TimedOut);
                Interlocked.Increment(ref _timedOutInvokes);
            }
        }
    }

    private void CloseDialogue(
        DialogueContext context,
        TcapInvokeOutcomeKind pendingOutcome)
    {
        PendingInvoke[] pending;
        bool removed;
        lock (_sync)
        {
            removed = _dialogues.Remove(context.Handle.DialogueId);
            _dialoguesByLocalTransactionId.Remove(
                context.LocalTransactionId.ToString());
        }

        if (!removed)
        {
            return;
        }

        lock (context.Sync)
        {
            pending = context.PendingOutbound.Values.ToArray();
            context.PendingOutbound.Clear();
            context.ActiveInbound.Clear();
            if (context.Phase is not TcapDialoguePhase.Aborted)
            {
                context.Phase = TcapDialoguePhase.Ended;
            }
        }

        foreach (PendingInvoke invoke in pending)
        {
            CompletePending(invoke, pendingOutcome);
        }

        Interlocked.Increment(ref _closedDialogues);
    }

    private static void CompletePending(
        PendingInvoke pending,
        TcapInvokeOutcomeKind kind,
        ReadOnlyMemory<byte> parameters = default,
        TcapReturnErrorCode? errorCode = null,
        TcapRejectProblemCode? problemCode = null)
    {
        pending.Completion.TrySetResult(
            new(
                pending.Handle,
                kind,
                parameters,
                errorCode,
                problemCode));
    }

    private TcapTransactionId AllocateTransactionId()
    {
        for (int attempt = 0; attempt <= _options.MaximumDialogues; attempt++)
        {
            TcapTransactionId candidate;
            lock (_allocationSync)
            {
                candidate = _transactionIds.Allocate();
            }

            lock (_sync)
            {
                if (!_dialoguesByLocalTransactionId.ContainsKey(candidate.ToString()))
                {
                    return candidate;
                }
            }
        }

        throw new InvalidOperationException(
            "No unused TCAP transaction identifier is available.");
    }

    private void ForgetPending(PendingInvoke pending)
    {
        lock (_sync)
        {
            _invokeCompletions.Remove(
                new(
                    pending.Handle.Dialogue.DialogueId,
                    pending.Handle.InvokeId));
        }
    }

    private static void EnsureRemoteTransactionId(DialogueContext context)
    {
        lock (context.Sync)
        {
            if (!context.RemoteTransactionId.HasValue)
            {
                throw new InvalidOperationException(
                    "TCAP dialogue has not received a peer transaction id.");
            }
        }
    }

    private void EnsureRunning()
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException(
                "Start the TCAP dialogue manager before receiving events.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class DialogueContext
    {
        public DialogueContext(
            TcapDialogueHandle handle,
            SccpPartyAddress calledParty,
            SccpPartyAddress callingParty,
            TcapTransactionId localTransactionId,
            TcapTransactionId? remoteTransactionId,
            TcapDialoguePhase phase)
        {
            Handle = handle;
            CalledParty = calledParty;
            CallingParty = callingParty;
            LocalTransactionId = localTransactionId;
            RemoteTransactionId = remoteTransactionId;
            Phase = phase;
        }

        public object Sync { get; } = new();

        public TcapDialogueHandle Handle { get; }

        public SccpPartyAddress CalledParty { get; }

        public SccpPartyAddress CallingParty { get; }

        public TcapTransactionId LocalTransactionId { get; }

        public TcapTransactionId? RemoteTransactionId { get; set; }

        public TcapDialoguePhase Phase { get; set; }

        public Dictionary<byte, PendingInvoke> PendingOutbound { get; } = [];

        public HashSet<byte> ActiveInbound { get; } = [];

        public byte NextInvokeId { get; set; } = 1;
    }

    private sealed class PendingInvoke
    {
        public PendingInvoke(
            TcapInvokeHandle handle,
            TcapOperationCode operationCode,
            DateTimeOffset deadlineUtc)
        {
            Handle = handle;
            OperationCode = operationCode;
            DeadlineUtc = deadlineUtc;
            Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public TcapInvokeHandle Handle { get; }

        public TcapOperationCode OperationCode { get; }

        public DateTimeOffset DeadlineUtc { get; }

        public TaskCompletionSource<TcapInvokeOutcome> Completion { get; }
    }

    private readonly record struct InvokeKey(long DialogueId, byte InvokeId);
}
