namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Identifies SCTP transport operations that require timeout and cancellation handling.
/// </summary>
public enum SctpOperationKind
{
    /// <summary>Association connect operation.</summary>
    Connect,

    /// <summary>User message send operation.</summary>
    Send,

    /// <summary>User message receive operation.</summary>
    Receive,

    /// <summary>Association reconnect operation.</summary>
    Reconnect,

    /// <summary>Graceful shutdown operation.</summary>
    Shutdown
}

/// <summary>
/// Describes the cancellation and timeout budget for one SCTP operation.
/// </summary>
public sealed class SctpOperationCancellationBudget
{
    /// <summary>Creates an SCTP operation cancellation budget.</summary>
    /// <param name="operationKind">The operation kind.</param>
    /// <param name="startedUtc">The UTC operation start time.</param>
    /// <param name="timeout">The operation timeout.</param>
    /// <param name="callerCancellationToken">The caller cancellation token.</param>
    public SctpOperationCancellationBudget(
        SctpOperationKind operationKind,
        DateTimeOffset startedUtc,
        TimeSpan timeout,
        CancellationToken callerCancellationToken = default)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "SCTP operation timeout must be positive.");
        }

        OperationKind = operationKind;
        StartedUtc = startedUtc;
        Timeout = timeout;
        CallerCancellationToken = callerCancellationToken;
    }

    /// <summary>The operation kind.</summary>
    public SctpOperationKind OperationKind { get; }

    /// <summary>The UTC operation start time.</summary>
    public DateTimeOffset StartedUtc { get; }

    /// <summary>The operation timeout.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>The caller cancellation token.</summary>
    public CancellationToken CallerCancellationToken { get; }

    /// <summary>The UTC deadline for the operation.</summary>
    public DateTimeOffset DeadlineUtc => StartedUtc + Timeout;

    /// <summary>Whether the caller cancellation token is currently cancelled.</summary>
    public bool CallerCancellationRequested => CallerCancellationToken.IsCancellationRequested;

    /// <summary>Returns whether the operation is timed out at a given UTC timestamp.</summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <returns><c>true</c> when the operation deadline has passed; otherwise <c>false</c>.</returns>
    public bool IsTimedOut(DateTimeOffset nowUtc)
    {
        return nowUtc >= DeadlineUtc;
    }

    /// <summary>Formats a compact cancellation budget summary.</summary>
    /// <returns>The cancellation budget summary.</returns>
    public string Describe()
    {
        return $"operation={OperationKind} timeout={Timeout} deadline={DeadlineUtc:O} callerCancelled={CallerCancellationRequested}";
    }
}

/// <summary>
/// Defines SCTP operation timeout values and creates cancellation budgets.
/// </summary>
public sealed class SctpOperationTimeoutPolicy
{
    /// <summary>Creates an SCTP operation timeout policy.</summary>
    /// <param name="connectTimeout">The connect timeout.</param>
    /// <param name="sendTimeout">The send timeout.</param>
    /// <param name="receiveTimeout">The receive timeout.</param>
    /// <param name="reconnectTimeout">The reconnect timeout.</param>
    /// <param name="shutdownTimeout">The shutdown timeout.</param>
    public SctpOperationTimeoutPolicy(
        TimeSpan? connectTimeout = null,
        TimeSpan? sendTimeout = null,
        TimeSpan? receiveTimeout = null,
        TimeSpan? reconnectTimeout = null,
        TimeSpan? shutdownTimeout = null)
    {
        ConnectTimeout = Validate(connectTimeout ?? TimeSpan.FromSeconds(10), nameof(connectTimeout));
        SendTimeout = Validate(sendTimeout ?? TimeSpan.FromSeconds(5), nameof(sendTimeout));
        ReceiveTimeout = Validate(receiveTimeout ?? TimeSpan.FromSeconds(30), nameof(receiveTimeout));
        ReconnectTimeout = Validate(reconnectTimeout ?? TimeSpan.FromSeconds(10), nameof(reconnectTimeout));
        ShutdownTimeout = Validate(shutdownTimeout ?? TimeSpan.FromSeconds(5), nameof(shutdownTimeout));
    }

    /// <summary>The connect timeout.</summary>
    public TimeSpan ConnectTimeout { get; }

    /// <summary>The send timeout.</summary>
    public TimeSpan SendTimeout { get; }

    /// <summary>The receive timeout.</summary>
    public TimeSpan ReceiveTimeout { get; }

    /// <summary>The reconnect timeout.</summary>
    public TimeSpan ReconnectTimeout { get; }

    /// <summary>The shutdown timeout.</summary>
    public TimeSpan ShutdownTimeout { get; }

    /// <summary>Returns the timeout for an operation kind.</summary>
    /// <param name="operationKind">The operation kind.</param>
    /// <returns>The operation timeout.</returns>
    public TimeSpan GetTimeout(SctpOperationKind operationKind)
    {
        return operationKind switch
        {
            SctpOperationKind.Connect => ConnectTimeout,
            SctpOperationKind.Send => SendTimeout,
            SctpOperationKind.Receive => ReceiveTimeout,
            SctpOperationKind.Reconnect => ReconnectTimeout,
            SctpOperationKind.Shutdown => ShutdownTimeout,
            _ => SendTimeout
        };
    }

    /// <summary>Creates a cancellation budget for an operation.</summary>
    /// <param name="operationKind">The operation kind.</param>
    /// <param name="startedUtc">The UTC operation start time.</param>
    /// <param name="callerCancellationToken">The caller cancellation token.</param>
    /// <returns>The cancellation budget.</returns>
    public SctpOperationCancellationBudget CreateBudget(
        SctpOperationKind operationKind,
        DateTimeOffset startedUtc,
        CancellationToken callerCancellationToken = default)
    {
        return new(operationKind, startedUtc, GetTimeout(operationKind), callerCancellationToken);
    }

    private static TimeSpan Validate(TimeSpan timeout, string parameterName)
    {
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(parameterName, "SCTP operation timeout must be positive.");
        }

        return timeout;
    }
}
