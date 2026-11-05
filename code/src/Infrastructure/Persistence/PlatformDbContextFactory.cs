using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StableRail.Infrastructure.Persistence;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public const string LocalConnectionString =
        "Host=localhost;Port=54329;Database=stablerail;Username=stablerail;Password=stablerail";

    public const string LocalApplicationConnectionString =
        "Host=localhost;Port=54329;Database=stablerail;Username=stablerail_app;Password=stablerail_app";

    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSql");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = LocalConnectionString;

        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UsePlatformNpgsql(connectionString)
            .Options;
        return new PlatformDbContext(options);
    }
}

public static class PlatformDbContextOptions
{
    public static DbContextOptionsBuilder UsePlatformNpgsql(this DbContextOptionsBuilder builder, string connectionString) =>
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable(PlatformDbContext.MigrationsHistoryTable, PlatformDbContext.Schema));

    public static DbContextOptionsBuilder<PlatformDbContext> UsePlatformNpgsql(
        this DbContextOptionsBuilder<PlatformDbContext> builder,
        string connectionString) =>
        builder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsHistoryTable(PlatformDbContext.MigrationsHistoryTable, PlatformDbContext.Schema));
}
