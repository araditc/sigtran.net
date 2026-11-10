using System.Text.Json.Serialization;
using Sigtran.NET.Layers.MAP;

namespace Sigtran.NET.OperationsHost;

/// <summary>The bounded set of business inputs understood by the reference endpoint.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ReferenceSmsRequest(
    string? Msisdn = null, string? ServiceCentre = null, string? Imsi = null,
    string? TpduHex = null, string? DeliveryStatus = null, bool? GprsSupport = null);

/// <summary>A terminal application result without transport frames, raw BER or subscriber logging.</summary>
internal sealed record ReferenceSmsResult(string Outcome, byte? ErrorCode = null, byte? RejectProblem = null,
    string? Imsi = null, string? NetworkNodeNumber = null);

internal delegate ValueTask<MapSmsOperationResult> MapInvocation(IMapSmsService map, TimeSpan timeout, CancellationToken ct);

/// <summary>Converts validated operation inputs to one typed SDK invocation.</summary>
internal static class ReferenceMapOperations
{
    internal static MapInvocation? Create(MapSmsOperationCode operation, ReferenceSmsRequest request)
    {
        if (!IsDigits(request.ServiceCentre)
            || (operation != MapSmsOperationCode.SendRoutingInfoForShortMessage && request.GprsSupport is not null)
            || (operation != MapSmsOperationCode.ReportShortMessageDeliveryStatus && request.DeliveryStatus is not null)
            || (operation != MapSmsOperationCode.MtForwardShortMessage && request.Imsi is not null))
        {
            return null;
        }

        bool forward = operation is MapSmsOperationCode.MoForwardShortMessage or MapSmsOperationCode.MtForwardShortMessage;
        byte[]? tpdu = forward ? DecodeTpdu(request.TpduHex) : null;
        if ((forward && tpdu is null) || (!forward && request.TpduHex is not null))
        {
            return null;
        }

        MapSmsAddress centre = new(MapSmsAddressKind.ServiceCentre, request.ServiceCentre!);
        if (operation == MapSmsOperationCode.MtForwardShortMessage)
        {
            if (!IsDigits(request.Imsi) || request.Msisdn is not null)
            {
                return null;
            }

            MapMtForwardShortMessage mt = new(new(MapSmsAddressKind.Imsi, request.Imsi!), centre, tpdu!);
            return (map, timeout, ct) => map.InvokeMtForwardShortMessageAsync(mt, timeout, ct);
        }

        if (!IsDigits(request.Msisdn))
        {
            return null;
        }

        MapSmsAddress subscriber = new(MapSmsAddressKind.Msisdn, request.Msisdn!);
        return operation switch
        {
            MapSmsOperationCode.SendRoutingInfoForShortMessage => (map, timeout, ct) =>
                map.InvokeRoutingInfoForShortMessageAsync(new(subscriber, centre, request.GprsSupport ?? false), timeout, ct),
            MapSmsOperationCode.MoForwardShortMessage => (map, timeout, ct) =>
                map.InvokeMoForwardShortMessageAsync(new(centre, subscriber, tpdu!), timeout, ct),
            MapSmsOperationCode.ReportShortMessageDeliveryStatus when DeliveryStatus(request.DeliveryStatus) is { } status =>
                (map, timeout, ct) => map.InvokeReportShortMessageDeliveryStatusAsync(new(subscriber, centre, status), timeout, ct),
            MapSmsOperationCode.AlertServiceCentre => (map, timeout, ct) =>
                map.InvokeAlertServiceCentreAsync(new(subscriber, centre), timeout, ct),
            _ => null
        };
    }

    private static bool IsDigits(string? value) => value is { Length: >= 1 and <= 15 }
        && value.All(char.IsAsciiDigit);

    private static MapSmsDeliveryStatus? DeliveryStatus(string? name) => name switch
    {
        "delivered" => MapSmsDeliveryStatus.Delivered,
        "absent-subscriber" => MapSmsDeliveryStatus.AbsentSubscriber,
        "memory-capacity-exceeded" => MapSmsDeliveryStatus.MemoryCapacityExceeded,
        _ => null
    };

    private static byte[]? DecodeTpdu(string? hex)
    {
        if (hex is not { Length: >= 2 and <= 400 } || (hex.Length & 1) != 0)
        {
            return null;
        }

        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

/// <summary>Applies the allowlist, non-queuing admission bound and timeout to typed MAP work.</summary>
internal sealed partial class ReferenceOperationExecutor(IMapSmsService map, ReferenceServiceOptions options)
{
    private readonly SemaphoreSlim _slots = new(options.MaxInFlight, options.MaxInFlight);

    internal async ValueTask<ReferenceSmsResult> ExecuteAsync(string operationName, ReferenceSmsRequest request, CancellationToken ct)
    {
        if (!ReferenceServiceOptions.TryGetOperation(operationName, out MapSmsOperationCode operation)
            || !options.Operations.Contains(operation))
        {
            return new("OperationNotAllowed");
        }

        MapInvocation? invoke = ReferenceMapOperations.Create(operation, request);
        if (invoke is null)
        {
            return new("InvalidRequest");
        }

        ct.ThrowIfCancellationRequested();
        if (!_slots.Wait(0))
        {
            return new("Busy");
        }

        using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        // The TCAP timer owns the normal timeout; also bound queue admission and transport waiting.
        deadline.CancelAfter(options.Timeout + TimeSpan.FromMilliseconds(250));
        try
        {
            MapSmsOperationResult result = await invoke(map, options.Timeout, deadline.Token).ConfigureAwait(false);
            return new(result.Outcome.ToString(), (byte?)result.ErrorCode, (byte?)result.RejectProblem,
                result.RoutingInfo?.Imsi.Digits, result.RoutingInfo?.NetworkNodeNumber.Digits);
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return new("TimedOut");
        }
        finally
        {
            _slots.Release();
        }
    }
}
