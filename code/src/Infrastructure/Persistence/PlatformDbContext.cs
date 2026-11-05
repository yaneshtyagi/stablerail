using Microsoft.EntityFrameworkCore;
using StableRail.Infrastructure.Composition;

namespace StableRail.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public const string Schema = "platform";
    public const string MigrationsHistoryTable = "__ef_migrations";

    public DbSet<ModuleOwnershipRecord> ModuleOwnership => Set<ModuleOwnershipRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ModuleOwnershipRecord>(entity =>
        {
            entity.ToTable("module_ownership", Schema);
            entity.HasKey(record => record.Name);
            entity.Property(record => record.Name).HasColumnName("name").HasMaxLength(64);
            entity.Property(record => record.SchemaName).HasColumnName("schema_name").HasMaxLength(64);
            entity.Property(record => record.Responsibility).HasColumnName("responsibility");
            entity.Property(record => record.RecordedAt).HasColumnName("recorded_at").HasColumnType("timestamptz");
            entity.HasData(ModuleCatalog.All.Select(module => new ModuleOwnershipRecord
            {
                Name = module.Name,
                SchemaName = module.Schema,
                Responsibility = module.Responsibility,
                RecordedAt = ModuleCatalog.RecordedAt,
            }).ToArray());
        });
    }
}
