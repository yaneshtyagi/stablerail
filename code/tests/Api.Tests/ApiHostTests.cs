using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using StableRail.BuildingBlocks.Telemetry;

namespace StableRail.Api.Tests;

public sealed class ApiHostTests
{
    [Fact]
    public void Production_startup_rejects_missing_critical_configuration()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSql"] = "",
                    ["ConnectionStrings:Redis"] = "",
                    ["StableRail:Authentication:Mode"] = "Required",
                    ["StableRail:Queue:Name"] = "",
                    ["StableRail:Queue:DeadLetterName"] = "",
                    ["StableRail:Queue:Region"] = "",
                    ["StableRail:Telemetry:OtlpEndpoint"] = "",
                });
            });
        });

        var exception = Record.Exception(() => factory.CreateClient());
        Assert.NotNull(exception);
        Assert.Contains("Production startup rejected configuration", exception.ToString(), StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings:PostgreSql is required", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Production_startup_rejects_development_authentication_and_simulation()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddJsonFile(Path.Combine(CodeRoot(), "config", "appsettings.Development.json"));
            });
        });

        var exception = Record.Exception(() => factory.CreateClient());
        Assert.NotNull(exception);
        var text = exception.ToString();
        Assert.Contains("Development authentication", text, StringComparison.Ordinal);
        Assert.Contains("Simulation is rejected", text, StringComparison.Ordinal);
        Assert.Contains("Fake provider adapters", text, StringComparison.Ordinal);
        Assert.Contains("dev-only finality profile", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_id_is_returned_and_system_status_has_no_payment_volume()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/system");
        request.Headers.Add("X-Request-Id", "req_api_test_1234");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("req_api_test_1234", response.Headers.GetValues("X-Request-Id").Single());
        Assert.Equal("Development", body.GetProperty("authentication_mode").GetString());
        Assert.True(body.GetProperty("simulation_enabled").GetBoolean());
        Assert.Equal("unavailable", body.GetProperty("payments").GetString());
        Assert.Equal("AmazonSqs", body.GetProperty("queue").GetProperty("product").GetString());
        Assert.Equal("Standard", body.GetProperty("queue").GetProperty("type").GetString());
        Assert.Equal("ElasticMQ", body.GetProperty("queue").GetProperty("emulator").GetString());
        Assert.False(body.ToString().Contains("100000", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Correlation_spans_share_the_incoming_trace_and_request()
    {
        var observed = new List<(string Name, string TraceId, string? RequestId, string? Intent, string? Route, string? Provider, string? Chain, string? Reconciliation)>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => observed.Add((
                activity.OperationName,
                activity.TraceId.ToString(),
                activity.GetTagItem(CorrelationProbe.RequestIdTag)?.ToString(),
                activity.GetTagItem(CorrelationProbe.IntentTag)?.ToString(),
                activity.GetTagItem(CorrelationProbe.RouteTag)?.ToString(),
                activity.GetTagItem(CorrelationProbe.ProviderTag)?.ToString(),
                activity.GetTagItem(CorrelationProbe.ChainTag)?.ToString(),
                activity.GetTagItem(CorrelationProbe.ReconciliationTag)?.ToString())),
        };
        ActivitySource.AddActivityListener(listener);

        var traceId = ActivityTraceId.CreateRandom();
        var parent = ActivitySpanId.CreateRandom();
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/diagnostics/correlation");
        request.Headers.TryAddWithoutValidation("traceparent", $"00-{traceId}-{parent}-01");
        request.Headers.TryAddWithoutValidation("X-Request-Id", "req_trace_test_01");
        request.Headers.TryAddWithoutValidation("X-StableRail-Intent-Id", "intent_1");
        request.Headers.TryAddWithoutValidation("X-StableRail-Route-Id", "route_1");
        request.Headers.TryAddWithoutValidation("X-StableRail-Provider-Id", "provider_1");
        request.Headers.TryAddWithoutValidation("X-StableRail-Chain-Id", "chain_1");
        request.Headers.TryAddWithoutValidation("X-StableRail-Reconciliation-Id", "recon_1");

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(traceId.ToString(), body.GetProperty("trace_id").GetString());
        Assert.Equal("req_trace_test_01", body.GetProperty("request_id").GetString());
        foreach (var operation in CorrelationProbe.Operations)
        {
            var match = observed.Single(item => item.Name == operation);
            Assert.Equal(traceId.ToString(), match.TraceId);
            Assert.Equal("req_trace_test_01", match.RequestId);
            Assert.Equal("intent_1", match.Intent);
            Assert.Equal("route_1", match.Route);
            Assert.Equal("provider_1", match.Provider);
            Assert.Equal("chain_1", match.Chain);
            Assert.Equal("recon_1", match.Reconciliation);
        }
    }

    [Fact]
    public async Task Readiness_reports_postgresql_redis_and_queue()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/ready");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
        var names = body.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString() ?? "").OrderBy(name => name).ToArray();
        Assert.Equal(["postgresql", "queue", "redis"], names);
        Assert.All(body.GetProperty("checks").EnumerateArray(), check => Assert.Equal("Healthy", check.GetProperty("status").GetString()));
    }

    private static string CodeRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "StableRail.slnx")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("StableRail.slnx was not found.");
    }
}
