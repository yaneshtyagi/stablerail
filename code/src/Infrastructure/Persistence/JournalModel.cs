using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StableRail.BuildingBlocks.Ledger;
using StableRail.Modules.Compliance.Domain;
using StableRail.Modules.Finance.Domain;
using StableRail.Modules.Identity.Domain;
using StableRail.Modules.Monitoring.Domain;
using StableRail.Modules.Payments.Domain;
using StableRail.Modules.Providers.Domain;
using StableRail.Modules.Webhooks.Domain;

namespace StableRail.Infrastructure.Persistence;

public static class JournalModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureIdentity(modelBuilder);
        ConfigureProviders(modelBuilder);
        ConfigurePayments(modelBuilder);
        ConfigureMonitoring(modelBuilder);
        ConfigureFinance(modelBuilder);
        ConfigureCompliance(modelBuilder);
        ConfigureWebhooks(modelBuilder);
        UseSnakeCaseColumns(modelBuilder);
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void ConfigureIdentity(ModelBuilder modelBuilder)
    {
        var tenant = modelBuilder.Entity<Tenant>();
        tenant.ToTable("tenants", "identity");
        tenant.HasKey(row => row.Id);
        tenant.Property(row => row.CreatedAt).HasColumnType("timestamptz");

        var merchant = modelBuilder.Entity<Merchant>();
        merchant.ToTable("merchants", "identity");
        merchant.HasKey(row => row.Id);
        merchant.HasAlternateKey(row => new { row.TenantId, row.Id });
        merchant.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId);
        merchant.Property(row => row.CreatedAt).HasColumnType("timestamptz");

        var credential = modelBuilder.Entity<Credential>();
        credential.ToTable("credentials", "identity");
        credential.HasKey(row => row.Id);
        credential.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId);
        credential.HasOne<Merchant>().WithMany().HasForeignKey(row => new { row.TenantId, row.MerchantId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);
        credential.Property(row => row.CreatedAt).HasColumnType("timestamptz");
        credential.Property(row => row.RevokedAt).HasColumnType("timestamptz");

        var policy = modelBuilder.Entity<MerchantPolicyVersion>();
        policy.ToTable("merchant_policy_versions", "identity");
        policy.HasKey(row => row.Id);
        policy.HasAlternateKey(row => new { row.TenantId, row.Id });
        policy.HasOne<Merchant>().WithMany().HasForeignKey(row => new { row.TenantId, row.MerchantId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        policy.Property(row => row.CreatedAt).HasColumnType("timestamptz");

        var audit = modelBuilder.Entity<AuditEvent>();
        audit.ToTable("audit_events", "identity");
        audit.HasKey(row => row.Id);
        audit.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId);
        audit.Property(row => row.RecordedAt).HasColumnType("timestamptz");
    }

    private static void ConfigureProviders(ModelBuilder modelBuilder)
    {
        var provider = modelBuilder.Entity<Provider>();
        provider.ToTable("providers", "providers");
        provider.HasKey(row => row.Id);
        provider.Property(row => row.CreatedAt).HasColumnType("timestamptz");

        var account = modelBuilder.Entity<ProviderAccount>();
        account.ToTable("provider_accounts", "providers");
        account.HasKey(row => row.Id);
        account.HasAlternateKey(row => new { row.TenantId, row.Id });
        account.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId);
        account.HasOne<Provider>().WithMany().HasForeignKey(row => row.ProviderId);
        account.Property(row => row.CreatedAt).HasColumnType("timestamptz");

        var policy = modelBuilder.Entity<RoutingPolicyVersion>();
        policy.ToTable("routing_policy_versions", "providers");
        policy.HasKey(row => row.Id);
        policy.HasAlternateKey(row => new { row.TenantId, row.Id });
        policy.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId);
        policy.Property(row => row.CreatedAt).HasColumnType("timestamptz");

        var decision = modelBuilder.Entity<RoutingDecision>();
        decision.ToTable("routing_decisions", "providers");
        decision.HasKey(row => row.Id);
        decision.HasAlternateKey(row => new { row.TenantId, row.Id });
        decision.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        decision.HasOne<RoutingPolicyVersion>().WithMany().HasForeignKey(row => new { row.TenantId, row.PolicyVersionId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        decision.HasOne<ProviderAccount>().WithMany().HasForeignKey(row => new { row.TenantId, row.SelectedProviderAccountId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);
        decision.Property(row => row.RecordedAt).HasColumnType("timestamptz");
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        var intent = modelBuilder.Entity<PaymentIntent>();
        intent.ToTable("payment_intents", "payments", table =>
        {
            table.HasCheckConstraint("ck_intent_scales", "obligation_scale BETWEEN 0 AND 18 AND recognized_scale BETWEEN 0 AND 18 AND tolerance_scale BETWEEN 0 AND 18 AND (quoted_fiat_scale IS NULL OR quoted_fiat_scale BETWEEN 0 AND 18)");
            table.HasCheckConstraint("ck_intent_minor", "obligation_minor ~ '^(0|[1-9][0-9]{0,37})$' AND recognized_minor ~ '^(0|[1-9][0-9]{0,37})$' AND tolerance_minor ~ '^(0|[1-9][0-9]{0,37})$' AND (quoted_fiat_minor IS NULL OR quoted_fiat_minor ~ '^(0|[1-9][0-9]{0,37})$')");
            table.HasCheckConstraint("ck_intent_quote", "(quoted_fiat_minor IS NULL AND quoted_fiat_scale IS NULL AND quoted_fiat_text IS NULL AND quoted_fiat_currency IS NULL AND quote_reference IS NULL) OR (quoted_fiat_minor IS NOT NULL AND quoted_fiat_scale IS NOT NULL AND quoted_fiat_text IS NOT NULL AND quoted_fiat_currency IS NOT NULL AND quote_reference IS NOT NULL)");
        });
        intent.HasKey(row => row.Id);
        intent.HasAlternateKey(row => new { row.TenantId, row.Id });
        intent.Property(row => row.Version).IsConcurrencyToken();
        intent.Property(row => row.AcceptanceStatus).HasConversion<string>();
        intent.Property(row => row.CompliancePosture).HasConversion<string>();
        intent.Property(row => row.SettlementPosture).HasConversion<string>();
        intent.Property(row => row.ObligationMinor).MinorUnits();
        intent.Property(row => row.RecognizedMinor).MinorUnits();
        intent.Property(row => row.ToleranceMinor).MinorUnits();
        intent.Property(row => row.QuotedFiatMinor).MinorUnits();
        intent.Property(row => row.ExpiresAt).HasColumnType("timestamptz");
        intent.Property(row => row.CreatedAt).HasColumnType("timestamptz");
        intent.Property(row => row.UpdatedAt).HasColumnType("timestamptz");
        intent.HasOne<Merchant>().WithMany().HasForeignKey(row => new { row.TenantId, row.MerchantId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        intent.HasOne<MerchantPolicyVersion>().WithMany().HasForeignKey(row => new { row.TenantId, row.TolerancePolicyVersionId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        intent.Ignore(row => row.Obligation);
        intent.Ignore(row => row.Tolerance);
        intent.Ignore(row => row.Recognized);

        var option = modelBuilder.Entity<PaymentOption>();
        option.ToTable("payment_options", "payments", table =>
        {
            table.HasCheckConstraint("ck_option_scale", "amount_scale BETWEEN 0 AND 18");
            table.HasCheckConstraint("ck_option_minor", "amount_minor ~ '^(0|[1-9][0-9]{0,37})$'");
        });
        option.HasKey(row => row.Id);
        option.HasAlternateKey(row => new { row.TenantId, row.Id });
        option.Property(row => row.Status).HasConversion<string>();
        option.Property(row => row.AmountMinor).MinorUnits();
        option.Property(row => row.CreatedAt).HasColumnType("timestamptz");
        option.Property(row => row.InstructionsIssuedAt).HasColumnType("timestamptz");
        option.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        option.HasOne<ProviderAccount>().WithMany().HasForeignKey(row => new { row.TenantId, row.ProviderAccountId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        option.HasOne<RoutingDecision>().WithMany().HasForeignKey(row => new { row.TenantId, row.RouteDecisionId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);

        var attempt = modelBuilder.Entity<PaymentAttempt>();
        attempt.ToTable("payment_attempts", "payments");
        attempt.HasKey(row => row.Id);
        attempt.Property(row => row.Version).IsConcurrencyToken();
        attempt.Property(row => row.Outcome).HasConversion<string>();
        attempt.Property(row => row.PreparedAt).HasColumnType("timestamptz");
        attempt.Property(row => row.ResolvedAt).HasColumnType("timestamptz");
        attempt.HasOne<PaymentOption>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentOptionId }).HasPrincipalKey(row => new { row.TenantId, row.Id });

        var decision = modelBuilder.Entity<AcceptanceDecision>();
        decision.ToTable("acceptance_decisions", "payments");
        decision.HasKey(row => row.Id);
        decision.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        decision.HasIndex(row => new { row.TenantId, row.PaymentIntentId, row.Kind }).IsUnique();
        decision.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id });

        var edge = modelBuilder.Entity<StatusEdge>();
        edge.ToTable("status_edges", "payments");
        edge.HasKey(row => row.Id);
        edge.Property(row => row.FromStatus).HasConversion<string>();
        edge.Property(row => row.ToStatus).HasConversion<string>();
        edge.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        edge.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
    }

    private static void ConfigureMonitoring(ModelBuilder modelBuilder)
    {
        var observation = modelBuilder.Entity<BlockchainObservation>();
        observation.ToTable("blockchain_observations", "monitoring", table =>
        {
            table.HasCheckConstraint("ck_observation_scale", "amount_scale BETWEEN 0 AND 18");
            table.HasCheckConstraint("ck_observation_minor", "amount_minor ~ '^(0|[1-9][0-9]{0,37})$'");
            table.HasCheckConstraint("ck_observation_log", "log_index >= 0 AND chain_id > 0");
        });
        observation.HasKey(row => row.Id);
        observation.HasAlternateKey(row => new { row.TenantId, row.Id });
        observation.HasIndex(row => new { row.ChainId, row.TransactionHash, row.LogIndex }).IsUnique();
        observation.Property(row => row.AmountMinor).MinorUnits();
        observation.Property(row => row.ObservedAt).HasColumnType("timestamptz");
        observation.HasOne<Tenant>().WithMany().HasForeignKey(row => row.TenantId);

        var evidence = modelBuilder.Entity<ObservationEvidence>();
        evidence.ToTable("observation_evidence", "monitoring");
        evidence.HasKey(row => row.Id);
        evidence.Property(row => row.Lifecycle).HasConversion<string>();
        evidence.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        evidence.HasOne<BlockchainObservation>().WithMany().HasForeignKey(row => new { row.TenantId, row.ObservationId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
    }

    private static void ConfigureFinance(ModelBuilder modelBuilder)
    {
        var financial = modelBuilder.Entity<FinancialEvent>();
        financial.ToTable("financial_events", "finance", table =>
        {
            table.HasCheckConstraint("ck_event_scale", "amount_scale BETWEEN 0 AND 18");
            table.HasCheckConstraint("ck_event_minor", "amount_minor ~ '^-?(0|[1-9][0-9]{0,37})$'");
            table.HasCheckConstraint(
                "ck_event_effect",
                "(kind = 'recognition' AND affects_recognized_value AND amount_minor::numeric > 0) OR (kind = 'compensation' AND affects_recognized_value AND amount_minor::numeric < 0) OR (kind = 'unallocated' AND NOT affects_recognized_value AND amount_minor::numeric > 0) OR (kind = 'source_conflict' AND NOT affects_recognized_value)");
        });
        financial.HasKey(row => row.Id);
        financial.HasAlternateKey(row => new { row.TenantId, row.Id });
        financial.Property(row => row.Kind).HasConversion<string>();
        financial.Property(row => row.AmountMinor).MinorUnits();
        financial.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        financial.HasIndex(row => row.ObservationId).IsUnique().HasFilter("kind IN ('recognition', 'unallocated') AND observation_id IS NOT NULL");
        financial.HasIndex(row => row.CompensatesEventId).IsUnique().HasFilter("kind = 'compensation'");
        financial.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);
        financial.HasOne<PaymentOption>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentOptionId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);
        financial.HasOne<BlockchainObservation>().WithMany().HasForeignKey(row => new { row.TenantId, row.ObservationId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);

        var source = modelBuilder.Entity<EffectSource>();
        source.ToTable("effect_sources", "finance");
        source.HasKey(row => row.Id);
        source.HasIndex(row => new { row.SourceKind, row.SourceKey }).IsUnique();
        source.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        source.HasOne<FinancialEvent>().WithMany().HasForeignKey(row => new { row.TenantId, row.FinancialEventId }).HasPrincipalKey(row => new { row.TenantId, row.Id });

        var settlement = modelBuilder.Entity<SettlementRecord>();
        settlement.ToTable("settlement_records", "finance", table =>
        {
            table.HasCheckConstraint("ck_settlement_scale", "amount_scale BETWEEN 0 AND 18");
            table.HasCheckConstraint("ck_settlement_minor", "amount_minor ~ '^(0|[1-9][0-9]{0,37})$'");
        });
        settlement.HasKey(row => row.Id);
        settlement.HasAlternateKey(row => new { row.TenantId, row.Id });
        settlement.HasIndex(row => new { row.ProviderAccountId, row.ProviderReference }).IsUnique();
        settlement.Property(row => row.AmountMinor).MinorUnits();
        settlement.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        settlement.HasOne<ProviderAccount>().WithMany().HasForeignKey(row => new { row.TenantId, row.ProviderAccountId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        settlement.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);

        var reconciliation = modelBuilder.Entity<ReconciliationRecord>();
        reconciliation.ToTable("reconciliation_records", "finance");
        reconciliation.HasKey(row => row.Id);
        reconciliation.Property(row => row.Posture).HasConversion<string>();
        reconciliation.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        reconciliation.HasOne<SettlementRecord>().WithMany().HasForeignKey(row => new { row.TenantId, row.SettlementRecordId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        reconciliation.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);
    }

    private static void ConfigureCompliance(ModelBuilder modelBuilder)
    {
        var decision = modelBuilder.Entity<ComplianceDecision>();
        decision.ToTable("compliance_decisions", "compliance");
        decision.HasKey(row => row.Id);
        decision.Property(row => row.Posture).HasConversion<string>();
        decision.Property(row => row.DecidedAt).HasColumnType("timestamptz");
        decision.Property(row => row.FreshUntil).HasColumnType("timestamptz");
        decision.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        decision.HasOne<Merchant>().WithMany().HasForeignKey(row => new { row.TenantId, row.MerchantId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
        decision.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);
        decision.HasOne<MerchantPolicyVersion>().WithMany().HasForeignKey(row => new { row.TenantId, row.PolicyVersionId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
    }

    private static void ConfigureWebhooks(ModelBuilder modelBuilder)
    {
        var webhook = modelBuilder.Entity<WebhookEvent>();
        webhook.ToTable("webhook_events", "webhooks");
        webhook.HasKey(row => row.Id);
        webhook.HasAlternateKey(row => new { row.TenantId, row.Id });
        webhook.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        webhook.HasOne<PaymentIntent>().WithMany().HasForeignKey(row => new { row.TenantId, row.PaymentIntentId }).HasPrincipalKey(row => new { row.TenantId, row.Id }).IsRequired(false);

        var delivery = modelBuilder.Entity<WebhookDelivery>();
        delivery.ToTable("webhook_deliveries", "webhooks");
        delivery.HasKey(row => row.Id);
        delivery.HasIndex(row => new { row.WebhookEventId, row.AttemptNumber }).IsUnique();
        delivery.Property(row => row.RecordedAt).HasColumnType("timestamptz");
        delivery.HasOne<WebhookEvent>().WithMany().HasForeignKey(row => new { row.TenantId, row.WebhookEventId }).HasPrincipalKey(row => new { row.TenantId, row.Id });
    }

    private static void UseSnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (tableName is null)
                continue;
            var table = StoreObjectIdentifier.Table(tableName, entity.GetSchema());
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(table);
                if (column is not null && column.Any(char.IsUpper))
                    property.SetColumnName(Snake(column));
            }
        }
    }

    private static string Snake(string value)
    {
        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0)
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
