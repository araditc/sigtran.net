using Sigtran.NET.Layers.TCAP;

namespace Sigtran.NET.Layers.MAP;

/// <summary>
/// Identifies the terminal outcome of a MAP SMS invoke.
/// </summary>
public enum MapSmsOperationOutcomeKind
{
    /// <summary>The peer returned a successful result.</summary>
    Result,

    /// <summary>The peer returned a MAP user error.</summary>
    Error,

    /// <summary>TCAP rejected the component.</summary>
    Reject,

    /// <summary>The invoke exceeded its configured timeout.</summary>
    TimedOut,

    /// <summary>The TCAP dialogue closed before a result arrived.</summary>
    DialogueClosed
}

/// <summary>
/// Represents the correlated terminal outcome of a MAP SMS operation.
/// </summary>
public sealed class MapSmsOperationResult
{
    private readonly byte[] _parameters;

    internal MapSmsOperationResult(
        MapSmsOperationCode operationCode,
        TcapInvokeOutcome outcome)
    {
        OperationCode = operationCode;
        Dialogue = outcome.Invoke.Dialogue;
        InvokeId = outcome.Invoke.InvokeId;
        Outcome = outcome.Kind switch
        {
            TcapInvokeOutcomeKind.Result => MapSmsOperationOutcomeKind.Result,
            TcapInvokeOutcomeKind.Error => MapSmsOperationOutcomeKind.Error,
            TcapInvokeOutcomeKind.Reject => MapSmsOperationOutcomeKind.Reject,
            TcapInvokeOutcomeKind.TimedOut => MapSmsOperationOutcomeKind.TimedOut,
            _ => MapSmsOperationOutcomeKind.DialogueClosed
        };
        _parameters = outcome.Parameters.ToArray();
        ErrorCode = outcome.ErrorCode.HasValue
            ? (MapSmsErrorCode?)(byte)outcome.ErrorCode.Value
            : null;
        RejectProblem = outcome.ProblemCode;
    }

    /// <summary>The MAP SMS operation code.</summary>
    public MapSmsOperationCode OperationCode { get; }

    /// <summary>The TCAP dialogue that carried the operation.</summary>
    public TcapDialogueHandle Dialogue { get; }

    /// <summary>The TCAP invoke identifier.</summary>
    public byte InvokeId { get; }

    /// <summary>The terminal outcome category.</summary>
    public MapSmsOperationOutcomeKind Outcome { get; }

    /// <summary>The returned result or error parameters.</summary>
    public ReadOnlyMemory<byte> Parameters => _parameters;

    /// <summary>The MAP user error code, when a ReturnError was received.</summary>
    public MapSmsErrorCode? ErrorCode { get; }

    /// <summary>The TCAP reject problem, when a Reject was received.</summary>
    public TcapRejectProblemCode? RejectProblem { get; }

    /// <summary>Whether the peer returned a successful result.</summary>
    public bool IsSuccess => Outcome == MapSmsOperationOutcomeKind.Result;
}
