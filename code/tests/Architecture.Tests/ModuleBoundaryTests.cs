using System.Reflection;
using System.Xml.Linq;
using StableRail.BuildingBlocks.Modules;
using StableRail.BuildingBlocks.Queue;
using StableRail.BuildingBlocks.Runtime;
using StableRail.Infrastructure.Composition;

namespace StableRail.Architecture.Tests;

public sealed class ModuleBoundaryTests
{
    private static readonly string[] Modules =
    [
        "Compliance",
        "Finance",
        "Identity",
        "Monitoring",
        "Payments",
        "Providers",
        "Webhooks",
    ];

    [Fact]
    public void Module_projects_only_reference_their_own_layer()
    {
        var root = RepoPaths.CodeRoot();
        var buildingBlocks = Project(root, "src/BuildingBlocks/StableRail.BuildingBlocks.csproj");
        Assert.Empty(References(buildingBlocks));

        foreach (var module in Modules)
        {
            var domain = Project(root, $"src/Modules/{module}/Domain/StableRail.Modules.{module}.Domain.csproj");
            var application = Project(root, $"src/Modules/{module}/Application/StableRail.Modules.{module}.Application.csproj");
            Assert.Equal([buildingBlocks], References(domain));
            Assert.Equal([buildingBlocks, domain], References(application).OrderBy(path => path).ToArray());
            Assert.Empty(PackageReferences(domain));
            Assert.Equal(["Microsoft.Extensions.DependencyInjection.Abstractions"], PackageReferences(application));
        }
    }

    [Fact]
    public void Hosts_compose_modules_through_infrastructure()
    {
        var root = RepoPaths.CodeRoot();
        var infrastructure = Project(root, "src/Infrastructure/StableRail.Infrastructure.csproj");
        var references = References(infrastructure);
        Assert.Contains(Project(root, "src/BuildingBlocks/StableRail.BuildingBlocks.csproj"), references);
        foreach (var module in Modules)
        {
            Assert.Contains(Project(root, $"src/Modules/{module}/Domain/StableRail.Modules.{module}.Domain.csproj"), references);
            Assert.Contains(Project(root, $"src/Modules/{module}/Application/StableRail.Modules.{module}.Application.csproj"), references);
        }

        Assert.Equal(
            [infrastructure],
            References(Project(root, "src/Api/StableRail.Api.csproj")));
        Assert.Equal(
            [infrastructure],
            References(Project(root, "src/Worker/StableRail.Worker.csproj")));
    }

    [Fact]
    public void Catalog_matches_module_ownership()
    {
        var root = RepoPaths.CodeRoot();
        var folders = Directory.GetDirectories(Path.Combine(root, "src", "Modules"))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();
        Assert.Equal(Modules, folders);

        var names = new List<string>();
        foreach (var module in Modules)
        {
            var assembly = Assembly.Load($"StableRail.Modules.{module}.Domain");
            var ownership = assembly.GetTypes()
                .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.Static))
                .Single(field => field.FieldType == typeof(ModuleOwnership))
                .GetValue(null) as ModuleOwnership;
            Assert.NotNull(ownership);
            Assert.Equal(module.ToLowerInvariant(), ownership.Name);
            Assert.Equal(ownership.Name, ownership.Schema);
            Assert.NotEmpty(ownership.Requirements);
            names.Add(ownership.Name);
        }

        Assert.Equal(names.OrderBy(name => name), ModuleCatalog.All.Select(module => module.Name));
    }

    [Fact]
    public void Recorded_runtime_pins_match_local_configuration()
    {
        var root = RepoPaths.CodeRoot();
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.yml"));
        var queueConfig = File.ReadAllText(Path.Combine(root, "config", "elasticmq.conf"));
        var development = File.ReadAllText(Path.Combine(root, "config", "appsettings.Development.json"));
        var node = File.ReadAllText(Path.Combine(root, "operations", ".node-version")).Trim();

        Assert.Contains($"image: {RuntimePins.PostgresImage}", compose);
        Assert.Contains($"image: {RuntimePins.RedisImage}", compose);
        Assert.Contains($"image: {DurableQueue.EmulatorImage}", compose);
        Assert.Contains($"\"{RuntimePins.LocalPostgresPort}:5432\"", compose);
        Assert.Contains($"\"{RuntimePins.LocalRedisPort}:6379\"", compose);
        Assert.Contains(DurableQueue.QueueName, queueConfig);
        Assert.Contains(DurableQueue.DeadLetterQueueName, queueConfig);
        Assert.Contains($"maxReceiveCount = {DurableQueue.MaxReceiveCount}", queueConfig);
        Assert.Contains($"defaultVisibilityTimeout = {DurableQueue.VisibilityTimeoutSeconds} seconds", queueConfig);
        Assert.Contains($"receiveMessageWait = {DurableQueue.ReceiveWaitTimeSeconds} seconds", queueConfig);
        Assert.Contains($"region = {DurableQueue.Region}", queueConfig);
        Assert.Contains($"accountId = {DurableQueue.AccountId}", queueConfig);
        Assert.Contains("fifo = false", queueConfig);
        Assert.Contains(DurableQueue.QueueName, development);
        Assert.Contains(DurableQueue.LocalServiceUrl, development);
        Assert.Contains(DurableQueue.LocalAccessKey, development);
        Assert.Equal(RuntimePins.Node, node);
        Assert.Equal("net10.0", RuntimePins.TargetFramework);
    }

    private static string Project(string root, string relative) =>
        Path.GetFullPath(Path.Combine(root, relative));

    private static string[] References(string project) =>
        XDocument.Load(project)
            .Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value ?? "")
            .Select(relative => relative.Replace('\\', Path.DirectorySeparatorChar))
            .Select(relative => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, relative)))
            .OrderBy(path => path)
            .ToArray();

    private static string[] PackageReferences(string project) =>
        XDocument.Load(project)
            .Descendants("PackageReference")
            .Select(element => element.Attribute("Include")?.Value ?? "")
            .OrderBy(name => name)
            .ToArray();
}

internal static class RepoPaths
{
    public static string CodeRoot()
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
