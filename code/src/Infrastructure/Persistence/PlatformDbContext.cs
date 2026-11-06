using Microsoft.EntityFrameworkCore;
using StableRail.Infrastructure.Composition;
using StableRail.Modules.Compliance.Domain;
using StableRail.Modules.Finance.Domain;
using StableRail.Modules.Identity.Domain;
using StableRail.Modules.Monitoring.Domain;
using StableRail.Modules.Payments.Domain;
using StableRail.Modules.Providers.Domain;
using StableRail.Modules.Webhooks.Domain;

namespace StableRail.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public const string Schema = "platform";
    public const string MigrationsHistoryTable = "__ef_migrations";

    public DbSet<ModuleOwnershipRecord> ModuleOwnership => Set<ModuleOwnershipRecord>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Credential> Credentials => Set<Credential>();
    public DbSet<MerchantPolicyVersion> MerchantPolicyVersions => Set<MerchantPolicyVersion>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ProviderAccount> ProviderAccounts => Set<ProviderAccount>();
    public DbSet<RoutingPolicyVersion> RoutingPolicyVersions => Set<RoutingPolicyVersion>();
    public DbSet<RoutingDecision> RoutingDecisions => Set<RoutingDecision>();
    public DbSet<PaymentIntent> PaymentIntents => Set<PaymentIntent>();
    public DbSet<PaymentOption> PaymentOptions => Set<PaymentOption>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<AcceptanceDecision> AcceptanceDecisions => Set<AcceptanceDecision>();
    public DbSet<StatusEdge> StatusEdges => Set<StatusEdge>();
    public DbSet<BlockchainObservation> BlockchainObservations => Set<BlockchainObservation>();
    public DbSet<ObservationEvidence> ObservationEvidence => Set<ObservationEvidence>();
    public DbSet<FinancialEvent> FinancialEvents => Set<FinancialEvent>();
    public DbSet<EffectSource> EffectSources => Set<EffectSource>();
    public DbSet<SettlementRecord> SettlementRecords => Set<SettlementRecord>();
    public DbSet<ReconciliationRecord> ReconciliationRecords => Set<ReconciliationRecord>();
    public DbSet<ComplianceDecision> ComplianceDecisions => Set<ComplianceDecision>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

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
        JournalModel.Configure(modelBuilder);
    }
}
