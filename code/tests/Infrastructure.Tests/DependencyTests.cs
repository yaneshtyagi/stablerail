using Microsoft.EntityFrameworkCore;
using StableRail.BuildingBlocks.Queue;
using StableRail.Infrastructure.Composition;
using StableRail.Infrastructure.Configuration;
using StableRail.Infrastructure.Health;
using StableRail.Infrastructure.Persistence;
using StableRail.Infrastructure.Queue;

namespace StableRail.Infrastructure.Tests;

public sealed class DependencyTests
{
    [Fact]
    public async Task Migrations_create_module_schemas_and_ownership()
    {
        await TestDatabase.EnsureMigratedAsync();
        await using var context = TestDatabase.Open(applicationRole: false);
        await context.Database.MigrateAsync();

        var schemas = await context.Database.SqlQueryRaw<string>("select schema_name as \"Value\" from information_schema.schemata")
            .ToListAsync();
        foreach (var module in ModuleCatalog.All)
            Assert.Contains(module.Schema, schemas);
        Assert.Contains(PlatformDbContext.Schema, schemas);

        var rows = await context.ModuleOwnership.AsNoTracking().OrderBy(row => row.Name).ToListAsync();
        Assert.Equal(
            ModuleCatalog.All.Select(module => (module.Name, module.Schema, module.Responsibility)),
            rows.Select(row => (row.Name, row.SchemaName, row.Responsibility)));
    }

    [Fact]
    public async Task Redis_responds_to_ping()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? "localhost:63799";
        await DependencyProbes.PingRedisAsync(connectionString, CancellationToken.None);
    }

    [Fact]
    public async Task Durable_queue_round_trip_uses_the_recorded_queues()
    {
        await QueueSmoke.RoundTripAsync(new QueueOptions
        {
            Name = DurableQueue.QueueName,
            DeadLetterName = DurableQueue.DeadLetterQueueName,
            Region = DurableQueue.Region,
            ServiceUrl = DurableQueue.LocalServiceUrl,
            AccessKey = DurableQueue.LocalAccessKey,
            SecretKey = DurableQueue.LocalSecretKey,
        });
    }
}
