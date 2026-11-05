using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Npgsql;
using StackExchange.Redis;
using StableRail.Infrastructure.Configuration;
using StableRail.Infrastructure.Http;
using IAmazonSQS = Amazon.SQS.IAmazonSQS;
using GetQueueUrlRequest = Amazon.SQS.Model.GetQueueUrlRequest;

namespace StableRail.Infrastructure.Health;

public static class DependencyProbes
{
    public static async Task PingPostgresAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = 2,
            CommandTimeout = 2,
        };
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    public static async Task PingRedisAsync(string connectionString, CancellationToken cancellationToken)
    {
        var options = ConfigurationOptions.Parse(connectionString);
        options.AbortOnConnectFail = true;
        options.ConnectTimeout = 2000;
        using var connection = await ConnectionMultiplexer.ConnectAsync(options);
        cancellationToken.ThrowIfCancellationRequested();
        _ = await connection.GetDatabase().PingAsync();
    }
}

public sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var connectionString = configuration["ConnectionStrings:PostgreSql"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy();

        try
        {
            await DependencyProbes.PingPostgresAsync(connectionString, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

public sealed class RedisHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        var connectionString = configuration["ConnectionStrings:Redis"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy();

        try
        {
            await DependencyProbes.PingRedisAsync(connectionString, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

public sealed class QueueHealthCheck(IAmazonSQS sqs, IOptions<StableRailOptions> options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.Queue.Name))
            return HealthCheckResult.Unhealthy();

        try
        {
            _ = await sqs.GetQueueUrlAsync(new GetQueueUrlRequest { QueueName = options.Value.Queue.Name }, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}

public static class HealthResponses
{
    public static Task Write(HttpContext context, HealthReport report)
    {
        var payload = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
            }),
        };
        return context.Response.WriteAsJsonAsync(payload, StableRailJson.Serializer, context.RequestAborted);
    }
}
