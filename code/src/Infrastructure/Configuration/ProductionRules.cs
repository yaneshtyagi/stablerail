using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Npgsql;
using StableRail.BuildingBlocks.Queue;
using StableRail.BuildingBlocks.Runtime;
using StackExchange.Redis;

namespace StableRail.Infrastructure.Configuration;

public static class ProductionRules
{
    public static IReadOnlyList<string> Evaluate(IHostEnvironment environment, IConfiguration configuration)
    {
        var options = new StableRailOptions();
        configuration.GetSection(StableRailOptions.SectionName).Bind(options);
        var problems = new List<string>();

        if (string.Equals(options.Authentication.Mode, AuthenticationOptions.DevelopmentMode, StringComparison.Ordinal)
            && !environment.IsDevelopment())
        {
            problems.Add("Development authentication is rejected outside the Development environment.");
        }

        if (environment.IsDevelopment() || environment.IsEnvironment("Test"))
            return problems;

        problems.AddRange(ClosedEnvironmentProblems(configuration, options));
        return problems;
    }

    private static IEnumerable<string> ClosedEnvironmentProblems(IConfiguration configuration, StableRailOptions options)
    {
        if (!string.Equals(options.Authentication.Mode, AuthenticationOptions.RequiredMode, StringComparison.Ordinal))
        {
            yield return "Development authentication and disabled authentication are rejected. StableRail:Authentication:Mode must be Required.";
        }

        if (options.Simulation.Enabled)
            yield return "Simulation is rejected in this environment.";

        if (options.Providers.UseFakeAdapters)
            yield return "Fake provider adapters are rejected in this environment.";

        if (string.Equals(options.Finality.Profile, FinalityOptions.DevOnlyProfile, StringComparison.OrdinalIgnoreCase))
            yield return "The dev-only finality profile is rejected in this environment.";

        if (options.Diagnostics.ExposeTrace)
            yield return "Trace diagnostics are rejected in this environment.";

        if (options.Telemetry.ConsoleExporter)
            yield return "The console trace exporter is rejected in this environment.";

        foreach (var problem in PostgresProblems(configuration["ConnectionStrings:PostgreSql"]))
            yield return problem;

        foreach (var problem in RedisProblems(configuration["ConnectionStrings:Redis"]))
            yield return problem;

        foreach (var problem in QueueProblems(options.Queue))
            yield return problem;

        foreach (var problem in TelemetryProblems(options.Telemetry.OtlpEndpoint))
            yield return problem;
    }

    private static IEnumerable<string> PostgresProblems(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return ["ConnectionStrings:PostgreSql is required."];

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            return ["ConnectionStrings:PostgreSql is not a valid PostgreSQL connection string."];
        }

        var problems = new List<string>();
        if (IsLocalHost(builder.Host) || builder.Port == RuntimePins.LocalPostgresPort)
            problems.Add("ConnectionStrings:PostgreSql must not use the local development host or port.");

        if (string.IsNullOrWhiteSpace(builder.Password) || builder.Password is DurableQueue.LocalSecretKey or "stablerail" or "stablerail_app")
            problems.Add("ConnectionStrings:PostgreSql must not use the local development password.");

        return problems;
    }

    private static IEnumerable<string> RedisProblems(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return ["ConnectionStrings:Redis is required."];

        ConfigurationOptions parsed;
        try
        {
            parsed = ConfigurationOptions.Parse(connectionString);
        }
        catch (Exception)
        {
            return ["ConnectionStrings:Redis is not a valid Redis connection string."];
        }

        var problems = new List<string>();
        if (parsed.EndPoints.Count == 0)
            problems.Add("ConnectionStrings:Redis must name an endpoint.");

        foreach (var endpoint in parsed.EndPoints)
        {
            var (host, port) = ReadEndpoint(endpoint);
            if (IsLocalHost(host) || port == RuntimePins.LocalRedisPort)
                problems.Add("ConnectionStrings:Redis must not use the local development host or port.");
        }

        if (string.IsNullOrWhiteSpace(parsed.Password) || parsed.Password is DurableQueue.LocalSecretKey or "stablerail")
            problems.Add("ConnectionStrings:Redis must not use the local development password.");

        return problems;
    }

    private static IEnumerable<string> QueueProblems(QueueOptions queue)
    {
        if (!IsQueueName(queue.Name))
            yield return "StableRail:Queue:Name is required and must be a standard SQS queue name.";

        if (!IsQueueName(queue.DeadLetterName))
            yield return "StableRail:Queue:DeadLetterName is required and must be a standard SQS queue name.";

        if (string.IsNullOrWhiteSpace(queue.Region) || queue.Region.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
            yield return "StableRail:Queue:Region is required.";

        if (!string.IsNullOrWhiteSpace(queue.ServiceUrl))
        {
            if (!Uri.TryCreate(queue.ServiceUrl, UriKind.Absolute, out var serviceUri)
                || IsLocalHost(serviceUri.Host)
                || serviceUri.Port == DurableQueue.LocalApiPort)
            {
                yield return "StableRail:Queue:ServiceUrl must not point at the local ElasticMQ emulator.";
            }
        }

        if (!string.IsNullOrWhiteSpace(queue.AccessKey) || !string.IsNullOrWhiteSpace(queue.SecretKey))
            yield return "StableRail:Queue access keys must not be set in application configuration. Use the AWS credential chain.";
    }

    private static IEnumerable<string> TelemetryProblems(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            yield return "StableRail:Telemetry:OtlpEndpoint is required.";
            yield break;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
        {
            yield return "StableRail:Telemetry:OtlpEndpoint must be an absolute http or https URI.";
            yield break;
        }

        if (IsLocalHost(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            yield return "StableRail:Telemetry:OtlpEndpoint must not use a local host or embedded credentials.";
    }

    private static bool IsQueueName(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Length <= 80
        && name.All(static character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_');

    private static bool IsLocalHost(string? host) =>
        host is not null
        && (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.Ordinal)
            || host.Equals("::1", StringComparison.Ordinal)
            || host.Equals("[::1]", StringComparison.Ordinal)
            || host.Equals("host.docker.internal", StringComparison.OrdinalIgnoreCase));

    private static (string Host, int Port) ReadEndpoint(EndPoint endpoint) => endpoint switch
    {
        DnsEndPoint dns => (dns.Host, dns.Port),
        IPEndPoint ip => (ip.Address.ToString(), ip.Port),
        _ => ("", 0),
    };

}
