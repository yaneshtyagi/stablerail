using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StableRail.BuildingBlocks.Queue;
using StableRail.Infrastructure.Composition;
using StableRail.Infrastructure.Configuration;

namespace StableRail.Infrastructure.Tests;

public sealed class ProductionRulesTests
{
    [Fact]
    public void Development_configuration_is_rejected_when_the_host_is_production()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(CodeRoot(), "config", "appsettings.Development.json"))
            .Build();

        var problems = ProductionRules.Evaluate(new TestHostEnvironment("Production"), configuration);

        Assert.Contains(problems, problem => problem.Contains("Development authentication", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("Simulation is rejected", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("Fake provider adapters", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("dev-only finality profile", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("ElasticMQ", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("access keys", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("Trace diagnostics", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("local development password", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Closed_environments_reject_development_authentication(string environment)
    {
        var problems = ProductionRules.Evaluate(new TestHostEnvironment(environment), Configuration(new Dictionary<string, string?>
        {
            ["StableRail:Authentication:Mode"] = "Development",
        }));

        Assert.Contains(problems, problem => problem.Contains("Development authentication", StringComparison.Ordinal));
    }

    [Fact]
    public void Test_environment_rejects_development_authentication_and_allows_a_disabled_mode()
    {
        var development = ProductionRules.Evaluate(new TestHostEnvironment("Test"), Configuration(new Dictionary<string, string?>
        {
            ["StableRail:Authentication:Mode"] = "Development",
            ["StableRail:Simulation:Enabled"] = "true",
        }));
        var disabled = ProductionRules.Evaluate(new TestHostEnvironment("Test"), Configuration(new Dictionary<string, string?>
        {
            ["StableRail:Authentication:Mode"] = "Disabled",
            ["StableRail:Simulation:Enabled"] = "true",
        }));

        Assert.Contains(development, problem => problem.Contains("Development authentication", StringComparison.Ordinal));
        Assert.Empty(disabled);
    }

    [Fact]
    public void Complete_non_local_configuration_passes()
    {
        var problems = ProductionRules.Evaluate(new TestHostEnvironment("Production"), Configuration(Complete()));
        Assert.Empty(problems);
    }

    [Fact]
    public void Missing_critical_configuration_is_rejected()
    {
        var problems = ProductionRules.Evaluate(new TestHostEnvironment("Production"), Configuration([]));
        Assert.Contains(problems, problem => problem.Contains("ConnectionStrings:PostgreSql is required", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("ConnectionStrings:Redis is required", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("StableRail:Queue:Name", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("StableRail:Telemetry:OtlpEndpoint is required", StringComparison.Ordinal));
        Assert.Contains(problems, problem => problem.Contains("Mode must be Required", StringComparison.Ordinal));
    }

    [Fact]
    public void Registered_modules_match_the_catalog()
    {
        var services = new ServiceCollection();
        services.AddStableRailModules();
        using var provider = services.BuildServiceProvider();
        var names = provider.GetServices<StableRail.BuildingBlocks.Modules.IModule>().Select(module => module.Name).OrderBy(name => name);
        Assert.Equal(ModuleCatalog.All.Select(module => module.Name), names);
    }

    private static Dictionary<string, string?> Complete() => new()
    {
        ["ConnectionStrings:PostgreSql"] = "Host=db.internal;Port=5432;Database=stablerail;Username=stablerail_app;Password=change-me;SSL Mode=Require",
        ["ConnectionStrings:Redis"] = "redis.internal:6379,password=change-me,ssl=true",
        ["StableRail:Authentication:Mode"] = "Required",
        ["StableRail:Simulation:Enabled"] = "false",
        ["StableRail:Providers:UseFakeAdapters"] = "false",
        ["StableRail:Finality:Profile"] = "",
        ["StableRail:Queue:Name"] = DurableQueue.QueueName,
        ["StableRail:Queue:DeadLetterName"] = DurableQueue.DeadLetterQueueName,
        ["StableRail:Queue:Region"] = DurableQueue.Region,
        ["StableRail:Queue:ServiceUrl"] = "",
        ["StableRail:Queue:AccessKey"] = "",
        ["StableRail:Queue:SecretKey"] = "",
        ["StableRail:Telemetry:OtlpEndpoint"] = "https://otel.internal:4317",
        ["StableRail:Telemetry:ConsoleExporter"] = "false",
        ["StableRail:Diagnostics:ExposeTrace"] = "false",
    };

    private static IConfiguration Configuration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

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

    private sealed class TestHostEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "StableRail.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
