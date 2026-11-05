using Microsoft.Extensions.Options;
using StableRail.BuildingBlocks.Http;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Http;

public sealed class DevelopmentAuthenticationMiddleware(RequestDelegate next, IOptions<StableRailOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        var mode = options.Value.Authentication.Mode;
        var allowsLocalBypass = mode is AuthenticationOptions.DevelopmentMode or AuthenticationOptions.DisabledMode;
        if (context.Request.Path.StartsWithSegments("/v1") && !allowsLocalBypass)
        {
            var requestId = context.Items[RequestIds.ItemKey] as string ?? "";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await Results.Json(
                ApiErrors.Body(
                    "authentication_required",
                    "A required operator credential is not present.",
                    "authentication",
                    false,
                    requestId),
                statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
