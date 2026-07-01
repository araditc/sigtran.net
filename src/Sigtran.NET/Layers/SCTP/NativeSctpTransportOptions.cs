namespace Sigtran.NET.Layers.SCTP;

/// <summary>
/// Configures production native SCTP transport behavior around metadata, backpressure, timeouts, and reconnect.
/// </summary>
public sealed class NativeSctpTransportOptions
{
    /// <summary>Creates native SCTP transport options.</summary>
    /// <param name="backpressurePolicy">The send backpressure policy.</param>
    /// <param name="timeoutPolicy">The operation timeout policy.</param>
    /// <param name="reconnectPolicy">The reconnect policy.</param>
    /// <param name="requireKernelMetadata">Whether send/receive operations must use Linux SCTP metadata APIs.</param>
    public NativeSctpTransportOptions(
        SctpBackpressurePolicy? backpressurePolicy = null,
        SctpOperationTimeoutPolicy? timeoutPolicy = null,
        SctpReconnectPolicy? reconnectPolicy = null,
        bool requireKernelMetadata = true)
    {
        BackpressurePolicy = backpressurePolicy ?? new SctpBackpressurePolicy();
        TimeoutPolicy = timeoutPolicy ?? new SctpOperationTimeoutPolicy();
        ReconnectPolicy = reconnectPolicy ?? new SctpReconnectPolicy();
        RequireKernelMetadata = requireKernelMetadata;
    }

    /// <summary>The send backpressure policy.</summary>
    public SctpBackpressurePolicy BackpressurePolicy { get; }

    /// <summary>The operation timeout policy.</summary>
    public SctpOperationTimeoutPolicy TimeoutPolicy { get; }

    /// <summary>The reconnect policy.</summary>
    public SctpReconnectPolicy ReconnectPolicy { get; }

    /// <summary>Whether send/receive operations must use Linux SCTP metadata APIs.</summary>
    public bool RequireKernelMetadata { get; }
}
