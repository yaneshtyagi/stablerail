using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StableRail.Worker.Tests;

public sealed class WorkerHostTests
{
    [Fact]
    public void Production_startup_rejects_development_simulation()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSql"] = "Host=db.internal;Port=5432;Database=stablerail;Username=stablerail_app;Password=change-me",
                    ["ConnectionStrings:Redis"] = "redis.internal:6379,password=change-me",
                    ["StableRail:Authentication:Mode"] = "Development",
                    ["StableRail:Simulation:Enabled"] = "true",
                    ["StableRail:Providers:UseFakeAdapters"] = "true",
                    ["StableRail:Finality:Profile"] = "dev-only",
                    ["StableRail:Queue:Name"] = "stablerail-control-plane",
                    ["StableRail:Queue:DeadLetterName"] = "stablerail-control-plane-dlq",
                    ["StableRail:Queue:Region"] = "us-east-1",
                    ["StableRail:Queue:ServiceUrl"] = "http://localhost:9324",
                    ["StableRail:Queue:AccessKey"] = "local",
                    ["StableRail:Queue:SecretKey"] = "local",
                    ["StableRail:Telemetry:OtlpEndpoint"] = "https://otel.internal:4317",
                    ["StableRail:Diagnostics:ExposeTrace"] = "true",
                });
            });
        });

        var exception = Record.Exception(() => factory.CreateClient());
        Assert.NotNull(exception);
        var text = exception.ToString();
        Assert.Contains("Development authentication", text, StringComparison.Ordinal);
        Assert.Contains("Simulation is rejected", text, StringComparison.Ordinal);
        Assert.Contains("Fake provider adapters", text, StringComparison.Ordinal);
        Assert.Contains("dev-only finality profile", text, StringComparison.Ordinal);
        Assert.Contains("ElasticMQ", text, StringComparison.Ordinal);
        Assert.Contains("access keys", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Live_endpoint_does_not_require_dependencies()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/health/live");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.GetProperty("status").GetString());
    }
}
