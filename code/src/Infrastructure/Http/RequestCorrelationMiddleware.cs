using System.Diagnostics;
using StableRail.BuildingBlocks.Http;
using StableRail.BuildingBlocks.Telemetry;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Http;

public sealed class RequestCorrelationMiddleware(RequestDelegate next, ILogger<RequestCorrelationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[RequestIds.HeaderName].ToString();
        var requestId = RequestIds.IsAcceptable(incoming) ? incoming : RequestIds.Create();
        context.Items[RequestIds.ItemKey] = requestId;
        context.Response.Headers[RequestIds.HeaderName] = requestId;

        if (Activity.Current is { } activity)
        {
            activity.SetTag(CorrelationProbe.RequestIdTag, requestId);
            if (activity.TraceId != default)
                context.Response.Headers[RequestIds.TraceHeaderName] = activity.TraceId.ToString();
        }

        var level = context.Request.Path.StartsWithSegments("/health") ? LogLevel.Debug : LogLevel.Information;
        var started = Stopwatch.GetTimestamp();
        var role = context.RequestServices.GetRequiredService<StableRailHostRole>().Name;
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["TraceId"] = Activity.Current?.TraceId.ToString() ?? "",
            ["ServiceName"] = role,
        }))
        {
            try
            {
                await next(context);
            }
            finally
            {
                logger.Log(
                    level,
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms",
                    context.Request.Method,
                    context.Request.Path.Value ?? "",
                    context.Response.StatusCode,
                    Math.Round(Stopwatch.GetElapsedTime(started).TotalMilliseconds, 3));
            }
        }
    }
}
