using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using StableRail.BuildingBlocks.Http;
using StableRail.BuildingBlocks.Queue;
using StableRail.BuildingBlocks.Telemetry;
using StableRail.Infrastructure.Composition;
using StableRail.Infrastructure.Configuration;
using StableRail.Infrastructure.Http;
using StableRail.Infrastructure.Persistence;

namespace StableRail.Api;

public static class ControlPlaneEndpoints
{
    public static WebApplication MapControlPlane(this WebApplication app)
    {
        app.MapGet("/v1/system", (HttpContext context, IOptions<StableRailOptions> options, IHostEnvironment environment) =>
        {
            var settings = options.Value;
            var queue = settings.Queue;
            return Results.Ok(new SystemStatusResponse(
                environment.EnvironmentName,
                settings.Authentication.Mode,
                settings.Simulation.Enabled,
                settings.Providers.UseFakeAdapters,
                settings.Finality.Profile,
                "unavailable",
                new QueueStatusResponse(
                    DurableQueue.Product,
                    queue.Name,
                    queue.DeadLetterName,
                    queue.Region,
                    DurableQueue.Type,
                    string.IsNullOrWhiteSpace(queue.ServiceUrl) ? null : DurableQueue.Emulator)));
        });

        app.MapGet("/v1/modules", async (HttpContext context, PlatformDbContext db, CancellationToken cancellationToken) =>
        {
            var requestId = context.Items[RequestIds.ItemKey] as string ?? "";
            List<ModuleOwnershipRecord> rows;
            try
            {
                rows = await db.ModuleOwnership.AsNoTracking().OrderBy(row => row.Name).ToListAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is NpgsqlException or TimeoutException)
            {
                return Results.Json(
                    ApiErrors.Body(
                        "dependency_unavailable",
                        "Module ownership is not available.",
                        "dependency",
                        true,
                        requestId),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            var catalog = ModuleCatalog.All.OrderBy(module => module.Name).ToArray();
            if (rows.Count != catalog.Length
                || !rows.Select(row => (row.Name, row.SchemaName, row.Responsibility))
                    .SequenceEqual(catalog.Select(module => (module.Name, module.Schema, module.Responsibility))))
            {
                return Results.Json(
                    ApiErrors.Body(
                        "ownership_mismatch",
                        "Stored module ownership does not match the control plane.",
                        "internal",
                        false,
                        requestId),
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var requirements = catalog.ToDictionary(module => module.Name, module => module.Requirements);
            return Results.Ok(rows.Select(row => new ModuleOwnershipResponse(
                row.Name,
                row.SchemaName,
                row.Responsibility,
                requirements[row.Name],
                row.RecordedAt)));
        });

        app.MapGet("/v1/diagnostics/correlation", (HttpContext context, IOptions<StableRailOptions> options) =>
        {
            if (!options.Value.Diagnostics.ExposeTrace)
                return Results.NotFound();

            var requestId = context.Items[RequestIds.ItemKey] as string ?? "";
            var inspection = CorrelationProbe.Inspect(requestId, new Dictionary<string, string?>
            {
                [CorrelationProbe.IntentTag] = context.Request.Headers["X-StableRail-Intent-Id"],
                [CorrelationProbe.RouteTag] = context.Request.Headers["X-StableRail-Route-Id"],
                [CorrelationProbe.ProviderTag] = context.Request.Headers["X-StableRail-Provider-Id"],
                [CorrelationProbe.ChainTag] = context.Request.Headers["X-StableRail-Chain-Id"],
                [CorrelationProbe.ReconciliationTag] = context.Request.Headers["X-StableRail-Reconciliation-Id"],
            });
            return Results.Ok(new CorrelationResponse(inspection.RequestId, inspection.TraceId, inspection.Operations));
        });

        return app;
    }
}

public sealed record SystemStatusResponse(
    string Environment,
    string AuthenticationMode,
    bool SimulationEnabled,
    bool FakeAdaptersEnabled,
    string FinalityProfile,
    string Payments,
    QueueStatusResponse Queue);

public sealed record QueueStatusResponse(
    string Product,
    string Name,
    string DeadLetterName,
    string Region,
    string Type,
    string? Emulator);

public sealed record ModuleOwnershipResponse(
    string Name,
    string Schema,
    string Responsibility,
    IReadOnlyList<string> Requirements,
    DateTimeOffset RecordedAt);

public sealed record CorrelationResponse(string RequestId, string TraceId, IReadOnlyList<string> Operations);
