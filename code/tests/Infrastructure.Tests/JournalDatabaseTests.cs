using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;
using StableRail.Infrastructure.Persistence;
using StableRail.Infrastructure.Persistence.Journal;
using StableRail.Modules.Finance.Domain;
using StableRail.Modules.Monitoring.Domain;
using StableRail.Modules.Payments.Domain;

namespace StableRail.Infrastructure.Tests;

public sealed class JournalDatabaseTests
{
    private static DateTimeOffset At(int minutes) =>
        new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero).AddMinutes(minutes);

    [Fact]
    public async Task Forbidden_transitions_leave_the_projection_unchanged()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        var prepared = await IssuePayment(issue: false);
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);

        var early = await Assert.ThrowsAsync<LedgerRuleException>(() => journal.Expire(issued.TenantId, issued.IntentId, At(1), CancellationToken.None));
        Assert.Equal("forbidden_transition", early.Code);
        await journal.ResolveAttempt(prepared.TenantId, prepared.AttemptId, AttemptOutcome.unknown, At(2), CancellationToken.None);
        var blocked = await Assert.ThrowsAsync<LedgerRuleException>(() =>
            journal.Cancel(prepared.TenantId, prepared.IntentId, true, At(3), CancellationToken.None));
        Assert.Equal("forbidden_transition", blocked.Code);
        Assert.Equal(AcceptanceStatus.created, (await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == prepared.IntentId)).AcceptanceStatus);
        Assert.Equal(AcceptanceStatus.requires_payment, (await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId)).AcceptanceStatus);
        Assert.Equal(0, await db.WebhookEvents.CountAsync(row => row.PaymentIntentId == prepared.IntentId && row.EventType == "cancelled"));

        var jump = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE payments.payment_intents SET acceptance_status = 'succeeded' WHERE id = {prepared.IntentId}"));
        Assert.Contains("forbidden acceptance transition", jump.MessageText, StringComparison.Ordinal);
        Assert.Equal(AcceptanceStatus.created, (await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == prepared.IntentId)).AcceptanceStatus);
    }

    [Fact]
    public async Task Precision_boundaries_round_trip_as_exact_numeric_units()
    {
        await TestDatabase.EnsureMigratedAsync();
        var largest = new string('9', 38);
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var at = At(0);
        var tenant = await journal.CreateTenant("Precision " + Guid.NewGuid().ToString("N")[..8], at, CancellationToken.None);
        var merchant = await journal.CreateMerchant(tenant.Id, "Precision merchant", at, CancellationToken.None);
        var policy = await journal.CreateMerchantPolicy(tenant.Id, merchant.Id, "tolerance", "{\"tolerance\":\"0\"}", at, CancellationToken.None);
        var intent = await journal.CreateIntent(
            tenant.Id,
            merchant.Id,
            "precision-" + Guid.NewGuid().ToString("N"),
            AssetAmount.Parse(largest, 0),
            "SYN",
            AssetAmount.Zero(0),
            policy.Id,
            At(30),
            at,
            new FiatQuote("usd", AssetAmount.Parse("10.00", 2), "quote-dev", null),
            CancellationToken.None);

        var stored = await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == intent.Id);
        Assert.Equal(largest, stored.ObligationText);
        Assert.Equal("10.00", stored.QuotedFiatText);
        Assert.Equal("USD", stored.QuotedFiatCurrency);
        Assert.NotEqual(stored.ObligationMinor, stored.QuotedFiatMinor);
        var units = await db.Database.SqlQuery<string>(
            $"select obligation_units::text as \"Value\" from payments.payment_intents where id = {intent.Id}").SingleAsync();
        Assert.Equal(largest, units);

        var scale = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE payments.payment_intents SET recognized_scale = 19 WHERE id = {intent.Id}"));
        Assert.Equal(PostgresErrorCodes.CheckViolation, scale.SqlState);
    }

    [Fact]
    public async Task Competing_updates_do_not_merge_the_projection()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        await using var first = TestDatabase.Open(applicationRole: true);
        await using var second = TestDatabase.Open(applicationRole: true);
        var left = await first.PaymentIntents.SingleAsync(row => row.Id == issued.IntentId);
        var right = await second.PaymentIntents.SingleAsync(row => row.Id == issued.IntentId);
        left.ExceptionFlags = "late_transfer";
        left.Version++;
        right.ExceptionFlags = "wrong_token";
        right.Version++;

        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        await using var check = TestDatabase.Open(applicationRole: true);
        var stored = await check.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal("late_transfer", stored.ExceptionFlags);
        Assert.Equal(left.Version, stored.Version);
        Assert.Equal(AcceptanceStatus.requires_payment, stored.AcceptanceStatus);
    }

    [Fact]
    public async Task Duplicate_chain_and_provider_notifications_post_one_financial_effect()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        var key = ChainObservationKey.Create(issued.ChainId, "abc123def456" + Guid.NewGuid().ToString("N"), 4);
        var request = Recognition(issued, key, "100.000000", "chain", key.SourceKey);
        var gate = new Barrier(2);
        async Task<RecognitionResult> Once()
        {
            await using var db = TestDatabase.Open(applicationRole: true);
            gate.SignalAndWait(TimeSpan.FromSeconds(15));
            return await new Journal(db).Recognize(request, CancellationToken.None);
        }

        var results = await Task.WhenAll(Once(), Once());
        Assert.Equal(1, results.Count(result => result.PostedValue));

        await using var check = TestDatabase.Open(applicationRole: true);
        Assert.Equal(1, await check.BlockchainObservations.CountAsync(row => row.TransactionHash == key.TransactionHash && row.LogIndex == key.LogIndex));
        var events = await check.FinancialEvents.Where(row => row.PaymentIntentId == issued.IntentId && row.Kind == FinancialEventKind.recognition).ToListAsync();
        Assert.Single(events);
        var provider = Recognition(issued, key, "100.000000", "provider", "provider:" + issued.ProviderAccountId + ":notice-1");
        var linked = await new Journal(check).Recognize(provider, CancellationToken.None);
        Assert.False(linked.PostedValue);
        Assert.Equal(events[0].Id, linked.FinancialEventId);
        var conflict = Recognition(issued, key, "101.000000", "provider", "provider:" + issued.ProviderAccountId + ":notice-2");
        var conflicted = await new Journal(check).Recognize(conflict, CancellationToken.None);
        Assert.False(conflicted.PostedValue);
        var intent = await check.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal("100.000000", intent.RecognizedText);
        Assert.Equal(AcceptanceStatus.detected, intent.AcceptanceStatus);
        Assert.Contains("underpayment", intent.ExceptionFlags, StringComparison.Ordinal);
        Assert.Equal(1, await check.FinancialEvents.CountAsync(row => row.PaymentIntentId == issued.IntentId && row.Kind == FinancialEventKind.recognition));
        Assert.Equal(1, await check.FinancialEvents.CountAsync(row => row.PaymentIntentId == issued.IntentId && row.Kind == FinancialEventKind.source_conflict));
    }

    [Fact]
    public async Task Compensation_appends_the_opposite_amount_and_rebuild_restores_the_cache()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var key = ChainObservationKey.Create(issued.ChainId, "fedcba987654" + Guid.NewGuid().ToString("N"), 1);
        var posted = await journal.Recognize(Recognition(issued, key, "250.10", "chain", key.SourceKey), CancellationToken.None);
        var observationId = await db.BlockchainObservations.Where(row => row.TransactionHash == key.TransactionHash).Select(row => row.Id).SingleAsync();
        await journal.RecordLifecycle(issued.TenantId, observationId, ObservationLifecycle.finalized, "blockhash1", At(8), CancellationToken.None);
        var succeeded = await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal(AcceptanceStatus.succeeded, succeeded.AcceptanceStatus);
        Assert.Equal(SettlementPosture.pending, succeeded.SettlementPosture);
        Assert.Equal("250.10", succeeded.ObligationText);
        Assert.Equal("250.100000", succeeded.RecognizedText);
        Assert.Equal("10.00", succeeded.QuotedFiatText);

        var original = await db.FinancialEvents.AsNoTracking().SingleAsync(row => row.Id == posted.FinancialEventId);
        var compensation = await journal.Compensate(issued.TenantId, original.Id, At(9), CancellationToken.None);
        var reread = await db.FinancialEvents.AsNoTracking().SingleAsync(row => row.Id == original.Id);
        Assert.Equal(original.AmountMinor, reread.AmountMinor);
        Assert.Equal(original.AmountText, reread.AmountText);
        Assert.Equal(-original.AmountMinor, compensation.AmountMinor);
        var corrected = await db.PaymentIntents.SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal(AcceptanceStatus.detected, corrected.AcceptanceStatus);
        Assert.Equal(AssetAmount.Zero(6).CanonicalText, corrected.RecognizedText);

        corrected.RecognizedMinor = 1;
        corrected.RecognizedText = "0.000001";
        corrected.AcceptanceStatus = AcceptanceStatus.confirming;
        await db.SaveChangesAsync();
        var history = await db.FinancialEvents.AsNoTracking()
            .Where(row => row.PaymentIntentId == issued.IntentId)
            .OrderBy(row => row.Id)
            .Select(row => new { row.Id, row.AmountMinor, row.AmountText, row.Kind })
            .ToListAsync();
        var rebuilt = await journal.Rebuild(issued.TenantId, issued.IntentId, At(10), CancellationToken.None);
        var after = await db.FinancialEvents.AsNoTracking()
            .Where(row => row.PaymentIntentId == issued.IntentId)
            .OrderBy(row => row.Id)
            .Select(row => new { row.Id, row.AmountMinor, row.AmountText, row.Kind })
            .ToListAsync();
        Assert.Equal(history, after);
        Assert.Equal(AcceptanceStatus.detected, rebuilt.AcceptanceStatus);
        Assert.Equal(BigInteger.Zero, rebuilt.RecognizedMinor);
        Assert.Equal("250.10", rebuilt.ObligationText);
        Assert.Equal("10.00", rebuilt.QuotedFiatText);
    }

    [Fact]
    public async Task Late_wrong_token_and_unknown_attempts_do_not_create_success()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment(amountText: "250.000000", quote: null);
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var wrong = ChainObservationKey.Create(issued.ChainId, "wrongtoken" + Guid.NewGuid().ToString("N"), 0);
        await journal.Recognize(Recognition(issued, wrong, "250.000000", "chain", wrong.SourceKey) with { TokenContract = "othertoken" }, CancellationToken.None);
        var uncredited = await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal(AcceptanceStatus.requires_payment, uncredited.AcceptanceStatus);
        Assert.Equal("0.000000", uncredited.RecognizedText);
        Assert.Contains("wrong_token", uncredited.ExceptionFlags, StringComparison.Ordinal);

        await journal.Expire(issued.TenantId, issued.IntentId, At(31), CancellationToken.None);
        var lateKey = ChainObservationKey.Create(issued.ChainId, "latetransfer" + Guid.NewGuid().ToString("N"), 2);
        var late = Recognition(issued, lateKey, "250.000000", "chain", lateKey.SourceKey) with { ObservedAt = At(32) };
        await journal.Recognize(late, CancellationToken.None);
        var expired = await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal(AcceptanceStatus.expired, expired.AcceptanceStatus);
        Assert.Contains("late_transfer", expired.ExceptionFlags, StringComparison.Ordinal);
        Assert.Equal(1, await db.PaymentOptions.CountAsync(row => row.PaymentIntentId == issued.IntentId));
        Assert.Equal(0, await db.WebhookEvents.CountAsync(row => row.PaymentIntentId == issued.IntentId && row.EventType == "succeeded"));
    }

    [Fact]
    public async Task Historical_route_and_issued_instructions_remain_after_later_changes()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var first = await db.RoutingDecisions.AsNoTracking().SingleAsync(row => row.PaymentIntentId == issued.IntentId);
        var later = await journal.CreateRoutingPolicy(issued.TenantId, "{\"label\":\"later-development-fixture\"}", At(6), CancellationToken.None);
        var option = await db.PaymentOptions.AsNoTracking().SingleAsync(row => row.Id == issued.OptionId);
        var reread = await db.RoutingDecisions.AsNoTracking().SingleAsync(row => row.Id == first.Id);
        Assert.Equal(first.PolicyVersionId, reread.PolicyVersionId);
        Assert.NotEqual(later.Id, reread.PolicyVersionId);
        Assert.Equal(OptionStatus.issued, option.Status);
        Assert.NotNull(option.Instructions);

        var replaced = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE payments.payment_options SET destination = 'replacedestination' WHERE id = {issued.OptionId}"));
        Assert.Contains("issued payment instructions cannot be replaced", replaced.MessageText, StringComparison.Ordinal);
        Assert.Equal(option.Destination, (await db.PaymentOptions.AsNoTracking().SingleAsync(row => row.Id == issued.OptionId)).Destination);
    }

    [Fact]
    public async Task Application_role_cannot_update_or_delete_immutable_evidence()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        await using var app = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(app);
        var audit = await journal.RecordAudit(issued.TenantId, "operator-1", "read", "payment_intent", issued.IntentId, "allowed", "{\"scope\":\"tenant\"}", At(4), CancellationToken.None);
        var key = ChainObservationKey.Create(issued.ChainId, "roleevent" + Guid.NewGuid().ToString("N"), 3);
        var posted = await journal.Recognize(Recognition(issued, key, "1.000000", "chain", key.SourceKey), CancellationToken.None);
        await using var deliveryDb = TestDatabase.Open(applicationRole: true);
        var webhookId = await deliveryDb.WebhookEvents.Where(row => row.PaymentIntentId == issued.IntentId).Select(row => row.Id).FirstAsync();
        var delivery = await new Journal(deliveryDb).RecordDelivery(issued.TenantId, webhookId, 1, "not_delivered", At(5), CancellationToken.None);

        await using var connection = new NpgsqlConnection(PlatformDbContextFactory.LocalApplicationConnectionString);
        await connection.OpenAsync();
        var update = new NpgsqlCommand("UPDATE finance.financial_events SET amount_text = '9.000000' WHERE id = @id", connection);
        update.Parameters.AddWithValue("id", posted.FinancialEventId!.Value);
        var denied = await Assert.ThrowsAsync<PostgresException>(() => update.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, denied.SqlState);
        var delete = new NpgsqlCommand("DELETE FROM identity.audit_events WHERE id = @id", connection);
        delete.Parameters.AddWithValue("id", audit.Id);
        var deleted = await Assert.ThrowsAsync<PostgresException>(() => delete.ExecuteNonQueryAsync());
        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, deleted.SqlState);

        await using var owner = new NpgsqlConnection(PlatformDbContextFactory.LocalConnectionString);
        await owner.OpenAsync();
        var rewrite = new NpgsqlCommand("UPDATE finance.financial_events SET amount_text = amount_text WHERE id = @id", owner);
        rewrite.Parameters.AddWithValue("id", posted.FinancialEventId!.Value);
        var trigger = await Assert.ThrowsAsync<PostgresException>(() => rewrite.ExecuteNonQueryAsync());
        Assert.Contains("immutable evidence cannot be updated or deleted", trigger.MessageText, StringComparison.Ordinal);
        var deliveryUpdate = new NpgsqlCommand("UPDATE webhooks.webhook_deliveries SET outcome = 'delivered' WHERE id = @id", owner);
        deliveryUpdate.Parameters.AddWithValue("id", delivery.Id);
        var deliveryTrigger = await Assert.ThrowsAsync<PostgresException>(() => deliveryUpdate.ExecuteNonQueryAsync());
        Assert.Contains("immutable evidence cannot be updated or deleted", deliveryTrigger.MessageText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Tenant_ownership_rejects_a_cross_tenant_option()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var other = await journal.CreateTenant("Other " + Guid.NewGuid().ToString("N")[..8], At(0), CancellationToken.None);
        var option = PaymentOption.Create(other.Id, issued.IntentId, "SYN", issued.ChainId, issued.Contract, issued.Destination, issued.ProviderAccountId, AssetAmount.Parse("1", 6), At(1));
        db.Add(option);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Settlement_mismatch_does_not_change_acceptance()
    {
        await TestDatabase.EnsureMigratedAsync();
        var issued = await IssuePayment();
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var key = ChainObservationKey.Create(issued.ChainId, "settleevent" + Guid.NewGuid().ToString("N"), 7);
        await journal.Recognize(Recognition(issued, key, "250.10", "chain", key.SourceKey), CancellationToken.None);
        var observationId = await db.BlockchainObservations.Where(row => row.TransactionHash == key.TransactionHash).Select(row => row.Id).SingleAsync();
        await journal.RecordLifecycle(issued.TenantId, observationId, ObservationLifecycle.finalized, "blockhash2", At(8), CancellationToken.None);
        await journal.RecordSettlement(
            issued.TenantId,
            issued.ProviderAccountId,
            "statement-" + Guid.NewGuid().ToString("N"),
            "{\"provider_reference\":\"development-fixture\"}",
            AssetAmount.Parse("1.00", 6),
            issued.IntentId,
            SettlementPosture.mismatch,
            At(9),
            CancellationToken.None);
        var intent = await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == issued.IntentId);
        Assert.Equal(AcceptanceStatus.succeeded, intent.AcceptanceStatus);
        Assert.Equal(SettlementPosture.mismatch, intent.SettlementPosture);
    }

    private static async Task<IssuedPayment> IssuePayment(string amountText = "250.10", string? quote = "10.00", bool issue = true)
    {
        await using var db = TestDatabase.Open(applicationRole: true);
        var journal = new Journal(db);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = await journal.CreateTenant("Tenant " + suffix, At(0), CancellationToken.None);
        var merchant = await journal.CreateMerchant(tenant.Id, "Merchant " + suffix, At(0), CancellationToken.None);
        var policy = await journal.CreateMerchantPolicy(tenant.Id, merchant.Id, "tolerance", "{\"tolerance\":\"0\",\"label\":\"development-default\"}", At(0), CancellationToken.None);
        var provider = await journal.CreateProvider("synthetic-provider-" + suffix, At(0), CancellationToken.None);
        var account = await journal.CreateProviderAccount(tenant.Id, provider.Id, "development", "uncertified", "uncertified", At(0), CancellationToken.None);
        var amount = AssetAmount.Parse(amountText, 6);
        FiatQuote? fiat = quote is null ? null : new FiatQuote("usd", AssetAmount.Parse(quote, 2), "quote-dev", null);
        var intent = await journal.CreateIntent(tenant.Id, merchant.Id, "order-" + suffix, amount, "SYN", AssetAmount.Zero(6), policy.Id, At(30), At(0), fiat, CancellationToken.None);
        Assert.Equal(AcceptanceStatus.created, intent.AcceptanceStatus);
        const string contract = "syntheticcontract";
        const string destination = "syntheticdestination";
        const long chainId = 990001;
        var option = await journal.AddOption(tenant.Id, intent.Id, "SYN", chainId, contract, destination, account.Id, amount, At(1), CancellationToken.None);
        var routingPolicy = await journal.CreateRoutingPolicy(tenant.Id, "{\"label\":\"development-fixture\"}", At(1), CancellationToken.None);
        await journal.RecordRoute(tenant.Id, merchant.Id, intent.Id, option.Id, routingPolicy.Id, "{\"label\":\"development-fixture\"}", "{\"excluded\":[]}", account.Id, At(1), CancellationToken.None);
        await journal.RecordCompliance(tenant.Id, merchant.Id, intent.Id, CompliancePosture.clear, "development-fixture", At(1), policy.Id, At(1000), "clear", null, At(1), CancellationToken.None);
        var attempt = await journal.PrepareAttempt(tenant.Id, option.Id, "correlation-" + suffix, At(2), CancellationToken.None);
        if (!issue)
            return new IssuedPayment(tenant.Id, merchant.Id, intent.Id, option.Id, attempt.Id, account.Id, contract, chainId, destination);
        var premature = await Assert.ThrowsAsync<LedgerRuleException>(() =>
            journal.Issue(tenant.Id, intent.Id, option.Id, attempt.Id, "{\"destination\":\"syntheticdestination\"}", At(2), CancellationToken.None));
        Assert.Equal("forbidden_transition", premature.Code);
        await journal.ResolveAttempt(tenant.Id, attempt.Id, AttemptOutcome.committed, At(3), CancellationToken.None);
        await journal.Issue(tenant.Id, intent.Id, option.Id, attempt.Id, "{\"destination\":\"syntheticdestination\"}", At(4), CancellationToken.None);
        var stored = await db.PaymentIntents.AsNoTracking().SingleAsync(row => row.Id == intent.Id);
        Assert.Equal(AcceptanceStatus.requires_payment, stored.AcceptanceStatus);
        return new IssuedPayment(tenant.Id, merchant.Id, intent.Id, option.Id, attempt.Id, account.Id, contract, chainId, destination);
    }

    private static RecognitionRequest Recognition(IssuedPayment issued, ChainObservationKey key, string amount, string sourceKind, string sourceKey) =>
        new(
            issued.TenantId,
            issued.IntentId,
            issued.OptionId,
            key,
            "SYN",
            issued.Contract,
            issued.Destination,
            AssetAmount.Parse(amount, 6),
            sourceKind,
            sourceKey,
            10,
            "blockhash1",
            "{\"source\":\"development-fixture\"}",
            At(7));

    private sealed record IssuedPayment(
        Guid TenantId,
        Guid MerchantId,
        Guid IntentId,
        Guid OptionId,
        Guid AttemptId,
        Guid ProviderAccountId,
        string Contract,
        long ChainId,
        string Destination);
}
