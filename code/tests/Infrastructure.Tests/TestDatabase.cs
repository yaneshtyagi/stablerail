using Microsoft.EntityFrameworkCore;
using StableRail.Infrastructure.Persistence;

namespace StableRail.Infrastructure.Tests;

internal static class TestDatabase
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task EnsureMigratedAsync()
    {
        await Gate.WaitAsync();
        try
        {
            await using var db = Open(applicationRole: false);
            await db.Database.MigrateAsync();
            await db.Database.ExecuteSqlRawAsync("ALTER ROLE stablerail_app WITH LOGIN PASSWORD 'stablerail_app'");
        }
        finally
        {
            Gate.Release();
        }
    }

    public static PlatformDbContext Open(bool applicationRole)
    {
        var connectionString = applicationRole
            ? PlatformDbContextFactory.LocalApplicationConnectionString
            : PlatformDbContextFactory.LocalConnectionString;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UsePlatformNpgsql(connectionString).Options;
        return new PlatformDbContext(options);
    }
}
