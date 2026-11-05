using System.Text.Json;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StableRail.Infrastructure.Composition;
using StableRail.Infrastructure.Configuration;
using StableRail.Infrastructure.Health;
using StableRail.Infrastructure.Http;
using StableRail.Infrastructure.Persistence;
using StableRail.Infrastructure.Queue;
using StableRail.Infrastructure.Telemetry;

namespace StableRail.Infrastructure.Hosting;

public static class StableRailHost
{
    public static WebApplicationBuilder AddStableRail(this WebApplicationBuilder builder, string serviceName)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
            options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
        });

        ConfigLocator.UseStableRailConfiguration(builder);
        var services = builder.Services;
        services.AddSingleton(new StableRailHostRole(serviceName));
        services.AddOptions<StableRailOptions>()
            .Bind(builder.Configuration.GetSection(StableRailOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<StableRailOptions>, ProductionConfigurationValidator>());
        services.AddStableRailTelemetry(builder, serviceName);
        services.AddDbContext<PlatformDbContext>((provider, options) =>
        {
            var connectionString = provider.GetRequiredService<IConfiguration>()["ConnectionStrings:PostgreSql"] ?? "";
            options.UsePlatformNpgsql(connectionString);
        });
        services.AddSingleton(provider => SqsClients.Create(provider.GetRequiredService<IOptions<StableRailOptions>>().Value.Queue));
        services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgresql", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
            .AddCheck<QueueHealthCheck>("queue", tags: ["ready"]);
        services.AddStableRailModules();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        });
        services.AddCors(options =>
        {
            options.AddPolicy("operations", policy =>
            {
                var origins = OperationsOrigins(
                    builder.Configuration[$"{StableRailOptions.SectionName}:Operations:Origin"],
                    builder.Environment.IsDevelopment());
                if (origins.Length > 0)
                    policy.WithOrigins(origins);
                policy.WithHeaders(
                        "Content-Type",
                        "X-Request-Id",
                        "traceparent",
                        "tracestate",
                        "X-StableRail-Intent-Id",
                        "X-StableRail-Route-Id",
                        "X-StableRail-Provider-Id",
                        "X-StableRail-Chain-Id",
                        "X-StableRail-Reconciliation-Id")
                    .WithMethods("GET", "OPTIONS")
                    .WithExposedHeaders("X-Request-Id", "X-Trace-Id");
            });
        });
        return builder;
    }

    private static string[] OperationsOrigins(string? origin, bool development)
    {
        if (string.IsNullOrWhiteSpace(origin) || !Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return [];

        if (!development)
            return [origin];

        var alias = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ? "127.0.0.1"
            : uri.Host.Equals("127.0.0.1", StringComparison.Ordinal) ? "localhost"
            : null;
        if (alias is null)
            return [origin];

        var alternate = new UriBuilder(uri) { Host = alias };
        return [origin, alternate.Uri.GetLeftPart(UriPartial.Authority)];
    }

    public static WebApplication UseStableRail(this WebApplication app)
    {
        app.UseMiddleware<RequestCorrelationMiddleware>();
        app.UseMiddleware<UnhandledExceptionMiddleware>();
        app.UseCors("operations");
        app.UseMiddleware<DevelopmentAuthenticationMiddleware>();
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthResponses.Write,
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = HealthResponses.Write,
        });
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var role = app.Services.GetRequiredService<StableRailHostRole>();
            app.Logger.LogInformation(
                "StableRail {ServiceName} started in {EnvironmentName}",
                role.Name,
                app.Environment.EnvironmentName);
        });
        return app;
    }
}
