using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StableRail.BuildingBlocks.Telemetry;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Telemetry;

public static class TelemetryRegistration
{
    public static IServiceCollection AddStableRailTelemetry(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        string serviceName)
    {
        var telemetry = builder.Configuration.GetSection(StableRailOptions.SectionName).Get<StableRailOptions>()?.Telemetry
            ?? new TelemetryOptions();
        var endpoint = ResolveEndpoint(telemetry.OtlpEndpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceNamespace: "stablerail")
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(StableRailTelemetry.ActivitySourceName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                if (endpoint is not null)
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
                if (telemetry.ConsoleExporter)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(StableRailTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation();
                if (endpoint is not null)
                    metrics.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
            });

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            if (endpoint is not null)
                logging.AddOtlpExporter(exporter => exporter.Endpoint = endpoint);
        });

        return services;
    }

    private static Uri? ResolveEndpoint(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var endpoint) ? endpoint : null;
}
