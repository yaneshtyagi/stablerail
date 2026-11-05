using StableRail.BuildingBlocks.Http;

namespace StableRail.Infrastructure.Http;

public sealed class UnhandledExceptionMiddleware(RequestDelegate next, ILogger<UnhandledExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var requestId = context.Items[RequestIds.ItemKey] as string ?? "";
            logger.LogError(
                exception,
                "Unhandled exception {ExceptionType} for request {RequestId}",
                exception.GetType().Name,
                requestId);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await Results.Json(
                ApiErrors.Body("internal_error", "The request could not be completed.", "internal", false, requestId),
                statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
        }
    }
}
