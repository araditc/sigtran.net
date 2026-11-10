using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sigtran.NET.Layers.M3UA;

namespace Sigtran.NET.OperationsHost;

/// <summary>Exposes only the local, authenticated MAP SMS reference workload.</summary>
internal static class ReferenceSmsEndpoint
{
    private const int MaximumBodyBytes = 4096;
    private static readonly JsonSerializerOptions RequestJson = new(JsonSerializerDefaults.Web)
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        MaxDepth = 4
    };

    internal static void Map(WebApplication app)
    {
        app.MapPost("/v1/map-sms/{operation}", (HttpContext context, string operation, ReferenceOperationExecutor executor,
            ReferenceServiceOptions options, ReferenceMapStack stack, M3uaRuntime runtime) =>
            HandleAsync(context, operation, executor, options, () => stack.IsRunning && runtime.State == M3uaRuntimeState.Active));
    }

    /// <summary>Authenticates the real socket peer and bounds the body before dispatching a typed operation.</summary>
    internal static async Task<IResult> HandleAsync(HttpContext context, string operation, ReferenceOperationExecutor executor,
        ReferenceServiceOptions options, Func<bool> ready)
    {
        context.Response.Headers.CacheControl = "no-store";
        IPAddress? remote = context.Connection.RemoteIpAddress;
        if (remote is null || !IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote))
        {
            return Results.NotFound();
        }

        if (!Authorized(context.Request, options))
        {
            return Results.Unauthorized();
        }

        if (!ready())
        {
            return Reply(new("Unavailable"), StatusCodes.Status503ServiceUnavailable);
        }

        if (!context.Request.HasJsonContentType())
        {
            return Reply(new("InvalidRequest"), StatusCodes.Status415UnsupportedMediaType);
        }

        if (context.Request.ContentLength > MaximumBodyBytes)
        {
            return Reply(new("RequestTooLarge"), StatusCodes.Status413PayloadTooLarge);
        }

        CancellationToken ct = context.RequestAborted;
        byte[] body = new byte[MaximumBodyBytes + 1];
        int count;
        try
        {
            count = await context.Request.Body.ReadAtLeastAsync(body, body.Length, false, ct)
                .ConfigureAwait(false);
        }
        catch (BadHttpRequestException error)
        {
            return Reply(new(error.StatusCode == 413 ? "RequestTooLarge" : "InvalidRequest"), error.StatusCode);
        }

        if (count > MaximumBodyBytes)
        {
            return Reply(new("RequestTooLarge"), StatusCodes.Status413PayloadTooLarge);
        }

        ReferenceSmsRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<ReferenceSmsRequest>(body.AsSpan(0, count), RequestJson);
        }
        catch (JsonException)
        {
            return Reply(new("InvalidRequest"), StatusCodes.Status400BadRequest);
        }

        if (request is null)
        {
            return Reply(new("InvalidRequest"), StatusCodes.Status400BadRequest);
        }

        ReferenceSmsResult result = await executor.ExecuteAsync(operation, request, ct).ConfigureAwait(false);
        return Reply(result, result.Outcome switch
        {
            "Result" => StatusCodes.Status200OK,
            "Error" => StatusCodes.Status422UnprocessableEntity,
            "InvalidRequest" => StatusCodes.Status400BadRequest,
            "OperationNotAllowed" => StatusCodes.Status403Forbidden,
            "Busy" => StatusCodes.Status429TooManyRequests,
            "Unavailable" => StatusCodes.Status503ServiceUnavailable,
            "TimedOut" => StatusCodes.Status504GatewayTimeout,
            _ => StatusCodes.Status502BadGateway
        });
    }

    private static IResult Reply(ReferenceSmsResult value, int status) => Results.Json(value, statusCode: status);

    private static bool Authorized(HttpRequest request, ReferenceServiceOptions options)
    {
        const string prefix = "Bearer ";
        var headers = request.Headers.Authorization;
        if (headers.Count != 1 || headers[0] is not { } value || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string token = value[prefix.Length..];
        return ReferenceServiceOptions.IsToken(token)
            && CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.ASCII.GetBytes(token)), options.TokenDigest);
    }
}
