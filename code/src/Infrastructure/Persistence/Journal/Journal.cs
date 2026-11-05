using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;
using StableRail.BuildingBlocks.Time;
using StableRail.Modules.Compliance.Domain;
using StableRail.Modules.Finance.Domain;
using StableRail.Modules.Identity.Domain;
using StableRail.Modules.Monitoring.Domain;
using StableRail.Modules.Payments.Domain;
using StableRail.Modules.Providers.Domain;
using StableRail.Modules.Webhooks.Domain;

namespace StableRail.Infrastructure.Persistence.Journal;

public sealed record RecognitionRequest(
    Guid TenantId,
    Guid PaymentIntentId,
    Guid PaymentOptionId,
    ChainObservationKey Key,
    string AssetCode,
    string TokenContract,
    string Destination,
    AssetAmount Amount,
    string SourceKind,
    string SourceKey,
    long? BlockNumber,
    string? BlockHash,
    string RawProvenance,
    DateTimeOffset ObservedAt);

public sealed record RecognitionResult(Guid? FinancialEventId, bool PostedValue, bool Duplicate);

public sealed class Journal(PlatformDbContext db)
{
    public async Task<Tenant> CreateTenant(string name, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var tenant = Tenant.Create(name, at);
            db.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
            return tenant;
        }, cancellationToken);

    public async Task<Merchant> CreateMerchant(Guid tenantId, string name, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await LoadTenant(tenantId, cancellationToken);
            var merchant = Merchant.Create(tenantId, name, at);
            db.Add(merchant);
            await db.SaveChangesAsync(cancellationToken);
            return merchant;
        }, cancellationToken);

    public async Task<Credential> CreateCredential(
        Guid tenantId,
        Guid? merchantId,
        string environmentName,
        string label,
        string verifierHash,
        string scopes,
        DateTimeOffset at,
        Guid? rotatedFromId,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            if (merchantId is Guid merchant)
                _ = await LoadMerchant(tenantId, merchant, cancellationToken);
            var credential = Credential.Create(tenantId, merchantId, environmentName, label, verifierHash, scopes, at, rotatedFromId);
            db.Add(credential);
            await db.SaveChangesAsync(cancellationToken);
            return credential;
        }, cancellationToken);

    public async Task RevokeCredential(Guid tenantId, Guid credentialId, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var credential = await db.Credentials.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == credentialId, cancellationToken)
                ?? throw new LedgerRuleException("not_found", "Credential was not found in this tenant.");
            credential.RevokedAt = UtcTime.Require(at);
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public async Task<MerchantPolicyVersion> CreateMerchantPolicy(
        Guid tenantId,
        Guid merchantId,
        string kind,
        string document,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await LoadMerchant(tenantId, merchantId, cancellationToken);
            var version = MerchantPolicyVersion.Create(tenantId, merchantId, kind, document, at);
            db.Add(version);
            await db.SaveChangesAsync(cancellationToken);
            return version;
        }, cancellationToken);

    public async Task<AuditEvent> RecordAudit(
        Guid tenantId,
        string actorId,
        string action,
        string objectType,
        Guid? objectId,
        string result,
        string details,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var audit = AuditEvent.Create(tenantId, actorId, action, objectType, objectId, result, details, at);
            db.Add(audit);
            await db.SaveChangesAsync(cancellationToken);
            return audit;
        }, cancellationToken);

    public async Task<Provider> CreateProvider(string name, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var provider = Provider.Create(name, at);
            db.Add(provider);
            await db.SaveChangesAsync(cancellationToken);
            return provider;
        }, cancellationToken);

    public async Task<ProviderAccount> CreateProviderAccount(
        Guid tenantId,
        Guid providerId,
        string environmentName,
        string label,
        string status,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await LoadTenant(tenantId, cancellationToken);
            _ = await db.Providers.SingleOrDefaultAsync(row => row.Id == providerId, cancellationToken)
                ?? throw new LedgerRuleException("not_found", "Provider was not found.");
            var account = ProviderAccount.Create(tenantId, providerId, environmentName, label, status, at);
            db.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            return account;
        }, cancellationToken);

    public async Task<RoutingPolicyVersion> CreateRoutingPolicy(Guid tenantId, string document, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await LoadTenant(tenantId, cancellationToken);
            var version = RoutingPolicyVersion.Create(tenantId, document, at);
            db.Add(version);
            await db.SaveChangesAsync(cancellationToken);
            return version;
        }, cancellationToken);

    public async Task<PaymentIntent> CreateIntent(
        Guid tenantId,
        Guid merchantId,
        string merchantReference,
        AssetAmount obligation,
        string obligationAsset,
        AssetAmount tolerance,
        Guid tolerancePolicyVersionId,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        FiatQuote? quotedFiat,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await LoadMerchant(tenantId, merchantId, cancellationToken);
            _ = await db.MerchantPolicyVersions.SingleOrDefaultAsync(
                row => row.TenantId == tenantId && row.Id == tolerancePolicyVersionId,
                cancellationToken) ?? throw new LedgerRuleException("not_found", "Tolerance policy was not found in this tenant.");
            var intent = PaymentIntent.Create(
                tenantId,
                merchantId,
                merchantReference,
                obligation,
                obligationAsset,
                tolerance,
                tolerancePolicyVersionId,
                expiresAt,
                createdAt,
                quotedFiat);
            db.Add(intent);
            db.Add(WebhookEvent.Create(
                tenantId,
                intent.Id,
                "payment_intent.created",
                Payload("payment_intent.created", intent.Id, AcceptanceStatus.created),
                createdAt));
            await db.SaveChangesAsync(cancellationToken);
            return intent;
        }, cancellationToken);

    public async Task<PaymentOption> AddOption(
        Guid tenantId,
        Guid paymentIntentId,
        string assetCode,
        long chainId,
        string tokenContract,
        string destination,
        Guid providerAccountId,
        AssetAmount amount,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            _ = await db.ProviderAccounts.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == providerAccountId, cancellationToken)
                ?? throw new LedgerRuleException("not_found", "Provider account was not found in this tenant.");
            var option = PaymentOption.Create(tenantId, paymentIntentId, assetCode, chainId, tokenContract, destination, providerAccountId, amount, at);
            db.Add(option);
            await db.SaveChangesAsync(cancellationToken);
            return option;
        }, cancellationToken);

    public async Task<RoutingDecision> RecordRoute(
        Guid tenantId,
        Guid merchantId,
        Guid paymentIntentId,
        Guid paymentOptionId,
        Guid policyVersionId,
        string inputs,
        string candidates,
        Guid selectedProviderAccountId,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var intent = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            var option = await LoadOption(tenantId, paymentOptionId, cancellationToken);
            if (option.PaymentIntentId != paymentIntentId || intent.MerchantId != merchantId)
                throw new LedgerRuleException("tenant_mismatch", "Payment option was not found on this intent.");
            if (option.ProviderAccountId != selectedProviderAccountId)
                throw new LedgerRuleException("validation", "The routing decision must name the option's provider account.");
            if (option.Status == OptionStatus.issued)
                throw new LedgerRuleException("forbidden_transition", "Issued instructions keep their recorded route.");
            if (option.RouteDecisionId is not null)
                throw new LedgerRuleException("forbidden_transition", "The option already has a routing decision.");
            var decision = RoutingDecision.Create(
                tenantId,
                merchantId,
                paymentIntentId,
                policyVersionId,
                inputs,
                candidates,
                selectedProviderAccountId,
                at);
            db.Add(decision);
            await db.SaveChangesAsync(cancellationToken);
            option.RouteDecisionId = decision.Id;
            await db.SaveChangesAsync(cancellationToken);
            return decision;
        }, cancellationToken);

    public async Task<ComplianceDecision> RecordCompliance(
        Guid tenantId,
        Guid merchantId,
        Guid paymentIntentId,
        CompliancePosture posture,
        string source,
        DateTimeOffset decidedAt,
        Guid policyVersionId,
        DateTimeOffset freshUntil,
        string outcome,
        string? caseReference,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var intent = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            var decision = ComplianceDecision.Create(
                tenantId,
                merchantId,
                paymentIntentId,
                posture,
                source,
                decidedAt,
                policyVersionId,
                freshUntil,
                outcome,
                caseReference,
                recordedAt);
            db.Add(decision);
            await db.SaveChangesAsync(cancellationToken);
            await ApplyProjection(intent, "compliance", recordedAt, cancellationToken);
            return decision;
        }, cancellationToken);

    public async Task<PaymentAttempt> PrepareAttempt(
        Guid tenantId,
        Guid paymentOptionId,
        string correlationReference,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var option = await LoadOption(tenantId, paymentOptionId, cancellationToken);
            if (option.Status is not (OptionStatus.draft or OptionStatus.issuing))
                throw new LedgerRuleException("forbidden_transition", "An attempt cannot be prepared for this option.");
            if (option.Status == OptionStatus.draft)
                option.Status = OptionStatus.issuing;
            var attempt = PaymentAttempt.Prepare(tenantId, paymentOptionId, correlationReference, at);
            db.Add(attempt);
            await db.SaveChangesAsync(cancellationToken);
            var intent = await LoadIntent(tenantId, option.PaymentIntentId, cancellationToken);
            await ApplyProjection(intent, "attempt", at, cancellationToken);
            return attempt;
        }, cancellationToken);

    public async Task ResolveAttempt(
        Guid tenantId,
        Guid attemptId,
        AttemptOutcome outcome,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var attempt = await db.PaymentAttempts.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == attemptId, cancellationToken)
                ?? throw new LedgerRuleException("not_found", "Payment attempt was not found in this tenant.");
            if (!AcceptanceMachine.CanMoveAttempt(attempt.Outcome, outcome))
                throw new LedgerRuleException("forbidden_transition", "That attempt outcome is not allowed.");
            attempt.Outcome = outcome;
            attempt.ResolvedAt = UtcTime.Require(at);
            attempt.Version++;
            var option = await LoadOption(tenantId, attempt.PaymentOptionId, cancellationToken);
            if (outcome == AttemptOutcome.unknown && option.Status == OptionStatus.issuing)
                option.Status = OptionStatus.issuance_unknown;
            await db.SaveChangesAsync(cancellationToken);
            var intent = await LoadIntent(tenantId, option.PaymentIntentId, cancellationToken);
            await ApplyProjection(intent, "attempt", at, cancellationToken);
        }, cancellationToken);

    public async Task Issue(
        Guid tenantId,
        Guid paymentIntentId,
        Guid paymentOptionId,
        Guid attemptId,
        string instructions,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var intent = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            var option = await LoadOption(tenantId, paymentOptionId, cancellationToken);
            var attempt = await db.PaymentAttempts.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == attemptId, cancellationToken)
                ?? throw new LedgerRuleException("not_found", "Payment attempt was not found in this tenant.");
            if (option.PaymentIntentId != intent.Id || attempt.PaymentOptionId != option.Id)
                throw new LedgerRuleException("tenant_mismatch", "The attempt does not belong to this option.");
            if (intent.AcceptanceStatus != AcceptanceStatus.created)
                throw new LedgerRuleException("forbidden_transition", "Instructions can only be issued from created.");
            if (option.RouteDecisionId is null)
                throw new LedgerRuleException("forbidden_transition", "Instructions require a recorded routing decision.");
            if (attempt.Outcome != AttemptOutcome.committed)
                throw new LedgerRuleException("forbidden_transition", "Instructions require a committed attempt.");
            var loaded = await LoadFacts(intent, cancellationToken);
            if (loaded.Facts.Compliance != CompliancePosture.clear)
                throw new LedgerRuleException("forbidden_transition", "Instructions require a clear compliance decision.");
            if (loaded.Facts.AttemptBlocks)
                throw new LedgerRuleException("forbidden_transition", "An unknown or prepared attempt blocks issuance.");
            option.Status = OptionStatus.issued;
            option.Instructions = TextRules.Json(instructions);
            option.InstructionsIssuedAt = UtcTime.Require(at);
            db.Add(AcceptanceDecision.Create(tenantId, intent.Id, "instructions_issued", at));
            await db.SaveChangesAsync(cancellationToken);
            await ApplyProjection(intent, "instructions", at, cancellationToken);
        }, cancellationToken);

    public async Task Expire(Guid tenantId, Guid paymentIntentId, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var intent = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            if (intent.ExpiryRecorded)
                return;
            var now = UtcTime.Require(at);
            if (now < intent.ExpiresAt)
                throw new LedgerRuleException("forbidden_transition", "The expiry deadline has not passed.");
            var loaded = await LoadFacts(intent, cancellationToken);
            if (AcceptanceMachine.ThresholdMet(loaded.Facts)
                || intent.AcceptanceStatus is AcceptanceStatus.confirming or AcceptanceStatus.succeeded or AcceptanceStatus.cancelled)
                throw new LedgerRuleException("forbidden_transition", "This intent cannot expire.");
            db.Add(AcceptanceDecision.Create(tenantId, intent.Id, "expired", now));
            await db.SaveChangesAsync(cancellationToken);
            await ApplyProjection(intent, "expiry", now, cancellationToken);
        }, cancellationToken);

    public async Task Cancel(Guid tenantId, Guid paymentIntentId, bool authorized, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var intent = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            if (intent.CancellationRecorded)
                return;
            if (!authorized)
                throw new LedgerRuleException("forbidden_transition", "Cancellation is not authorized.");
            var loaded = await LoadFacts(intent, cancellationToken);
            if (intent.AcceptanceStatus is not (AcceptanceStatus.created or AcceptanceStatus.requires_payment)
                || !loaded.Facts.Recognized.IsZero
                || loaded.Facts.AttemptBlocks)
                throw new LedgerRuleException("forbidden_transition", "This intent cannot be cancelled.");
            var now = UtcTime.Require(at);
            db.Add(AcceptanceDecision.Create(tenantId, intent.Id, "cancelled", now));
            await db.SaveChangesAsync(cancellationToken);
            await ApplyProjection(intent, "cancellation", now, cancellationToken);
        }, cancellationToken);

    public async Task<RecognitionResult> Recognize(RecognitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await InTransaction(() => RecognizeOnce(request, cancellationToken), cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUnique(exception))
        {
            db.ChangeTracker.Clear();
            return await InTransaction(() => RecognizeOnce(request, cancellationToken), cancellationToken);
        }
    }

    public async Task<FinancialEvent> Compensate(Guid tenantId, Guid financialEventId, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var original = await db.FinancialEvents.AsNoTracking().SingleOrDefaultAsync(
                row => row.TenantId == tenantId && row.Id == financialEventId,
                cancellationToken) ?? throw new LedgerRuleException("not_found", "Financial event was not found in this tenant.");
            if (original.PaymentIntentId is not Guid intentId)
                throw new LedgerRuleException("forbidden_transition", "The financial event is not attached to an intent.");
            if (await db.FinancialEvents.AnyAsync(row => row.CompensatesEventId == original.Id, cancellationToken))
                throw new LedgerRuleException("duplicate_recognition", "This recognition already has a compensation.");
            var compensation = FinancialEvent.Compensation(original, at);
            db.Add(compensation);
            await db.SaveChangesAsync(cancellationToken);
            var intent = await LoadIntent(tenantId, intentId, cancellationToken);
            await ApplyProjection(intent, "compensation", at, cancellationToken);
            return compensation;
        }, cancellationToken);

    public async Task RecordLifecycle(
        Guid tenantId,
        Guid observationId,
        ObservationLifecycle lifecycle,
        string? blockHash,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var observation = await db.BlockchainObservations.AsNoTracking().SingleOrDefaultAsync(
                row => row.TenantId == tenantId && row.Id == observationId,
                cancellationToken) ?? throw new LedgerRuleException("not_found", "Observation was not found in this tenant.");
            db.Add(ObservationEvidence.Create(tenantId, observation.Id, lifecycle, blockHash, at));
            await db.SaveChangesAsync(cancellationToken);
            var intentIds = await db.FinancialEvents.AsNoTracking()
                .Where(row => row.TenantId == tenantId && row.ObservationId == observation.Id && row.PaymentIntentId != null)
                .Select(row => row.PaymentIntentId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
            foreach (var intentId in intentIds)
            {
                var intent = await LoadIntent(tenantId, intentId, cancellationToken);
                await ApplyProjection(intent, lifecycle.ToString(), at, cancellationToken);
            }
        }, cancellationToken);

    public async Task<SettlementRecord> RecordSettlement(
        Guid tenantId,
        Guid providerAccountId,
        string providerReference,
        string payload,
        AssetAmount amount,
        Guid? paymentIntentId,
        SettlementPosture posture,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
            var existing = await db.SettlementRecords.SingleOrDefaultAsync(
                row => row.TenantId == tenantId && row.ProviderAccountId == providerAccountId && row.ProviderReference == providerReference,
                cancellationToken);
            if (existing is not null)
            {
                if (!string.Equals(existing.PayloadSha256, digest, StringComparison.Ordinal))
                    throw new LedgerRuleException("statement_conflict", "This provider reference was already stored with different evidence.");
                return existing;
            }

            var record = SettlementRecord.Create(tenantId, providerAccountId, providerReference, digest, amount, paymentIntentId, at);
            db.Add(record);
            await db.SaveChangesAsync(cancellationToken);
            if (paymentIntentId is Guid intentId)
            {
                db.Add(ReconciliationRecord.Create(tenantId, record.Id, intentId, posture, null, at));
                var eventType = posture == SettlementPosture.settled ? "settlement.completed" : "settlement.failed";
                db.Add(WebhookEvent.Create(tenantId, intentId, eventType, Payload(eventType, intentId, posture.ToString()), at));
                await db.SaveChangesAsync(cancellationToken);
                var intent = await LoadIntent(tenantId, intentId, cancellationToken);
                await ApplyProjection(intent, "settlement", at, cancellationToken);
            }

            return record;
        }, cancellationToken);

    public async Task<WebhookDelivery> RecordDelivery(
        Guid tenantId,
        Guid webhookEventId,
        int attemptNumber,
        string outcome,
        DateTimeOffset at,
        CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            _ = await db.WebhookEvents.AsNoTracking().SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == webhookEventId, cancellationToken)
                ?? throw new LedgerRuleException("not_found", "Webhook event was not found in this tenant.");
            var delivery = WebhookDelivery.Create(tenantId, webhookEventId, attemptNumber, outcome, at);
            db.Add(delivery);
            await db.SaveChangesAsync(cancellationToken);
            return delivery;
        }, cancellationToken);

    public async Task<PaymentIntent> Rebuild(Guid tenantId, Guid paymentIntentId, DateTimeOffset at, CancellationToken cancellationToken) =>
        await InTransaction(async () =>
        {
            var intent = await LoadIntent(tenantId, paymentIntentId, cancellationToken);
            var financialIds = await db.FinancialEvents.AsNoTracking()
                .Where(row => row.TenantId == tenantId && row.PaymentIntentId == paymentIntentId)
                .OrderBy(row => row.Id)
                .Select(row => new { row.Id, row.AmountMinor, row.AmountText, row.Kind })
                .ToListAsync(cancellationToken);
            var decisionCount = await db.AcceptanceDecisions.CountAsync(row => row.TenantId == tenantId && row.PaymentIntentId == paymentIntentId, cancellationToken);
            var loaded = await LoadFacts(intent, cancellationToken);
            var target = AcceptanceMachine.Target(loaded.Facts);
            var flags = AcceptanceMachine.Flags(loaded.Facts, loaded.Late);
            Assign(intent, loaded.Facts, flags);
            var path = AcceptanceMachine.Path(intent.AcceptanceStatus, target);
            var now = UtcTime.Require(at);
            if (path.Count == 0)
            {
                intent.UpdatedAt = now;
                intent.Version++;
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                foreach (var status in path)
                {
                    intent.AcceptanceStatus = status;
                    intent.UpdatedAt = now;
                    intent.Version++;
                    await db.SaveChangesAsync(cancellationToken);
                }
            }

            var after = await db.FinancialEvents.AsNoTracking()
                .Where(row => row.TenantId == tenantId && row.PaymentIntentId == paymentIntentId)
                .OrderBy(row => row.Id)
                .Select(row => new { row.Id, row.AmountMinor, row.AmountText, row.Kind })
                .ToListAsync(cancellationToken);
            var afterDecisions = await db.AcceptanceDecisions.CountAsync(row => row.TenantId == tenantId && row.PaymentIntentId == paymentIntentId, cancellationToken);
            if (afterDecisions != decisionCount || after.Count != financialIds.Count || !after.SequenceEqual(financialIds))
                throw new LedgerRuleException("evidence_conflict", "Rebuild changed financial history.");
            return intent;
        }, cancellationToken);

    private async Task<RecognitionResult> RecognizeOnce(RecognitionRequest request, CancellationToken cancellationToken)
    {
        var at = UtcTime.Require(request.ObservedAt);
        var intent = await LoadIntent(request.TenantId, request.PaymentIntentId, cancellationToken);
        var option = await LoadOption(request.TenantId, request.PaymentOptionId, cancellationToken);
        if (option.PaymentIntentId != intent.Id)
            throw new LedgerRuleException("tenant_mismatch", "Payment option was not found on this intent.");
        if (request.SourceKind is not ("chain" or "provider"))
            throw new LedgerRuleException("validation", "Observation source must be chain or provider.");
        if (request.SourceKind == "chain" && request.SourceKey != request.Key.SourceKey)
            throw new LedgerRuleException("validation", "Chain notifications use the chain observation identity.");
        var sourceKey = request.SourceKind == "chain" ? request.Key.SourceKey : TextRules.Required(request.SourceKey, "validation", 300);

        var observation = await db.BlockchainObservations.SingleOrDefaultAsync(
            row => row.ChainId == request.Key.ChainId && row.TransactionHash == request.Key.TransactionHash && row.LogIndex == request.Key.LogIndex,
            cancellationToken);
        if (observation is null)
        {
            observation = BlockchainObservation.Create(
                request.TenantId,
                request.Key,
                request.BlockNumber,
                request.BlockHash,
                request.TokenContract,
                request.Destination,
                request.Amount,
                request.AssetCode,
                request.RawProvenance,
                at);
            db.Add(observation);
            db.Add(ObservationEvidence.Create(request.TenantId, observation.Id, ObservationLifecycle.seen, request.BlockHash, at));
        }
        else if (observation.TenantId != request.TenantId)
        {
            throw new LedgerRuleException("tenant_mismatch", "The chain observation is outside this tenant.");
        }

        var existingSource = await db.EffectSources.AsNoTracking().SingleOrDefaultAsync(
            row => row.SourceKind == request.SourceKind && row.SourceKey == sourceKey,
            cancellationToken);
        if (existingSource is not null)
            return new RecognitionResult(existingSource.FinancialEventId, false, true);

        var primary = await db.FinancialEvents.AsNoTracking().SingleOrDefaultAsync(
            row => row.ObservationId == observation.Id && (row.Kind == FinancialEventKind.recognition || row.Kind == FinancialEventKind.unallocated),
            cancellationToken);
        if (primary is not null)
        {
            db.Add(EffectSource.Create(request.TenantId, primary.Id, request.SourceKind, sourceKey, at));
            if (primary.Kind == FinancialEventKind.recognition && (primary.AmountMinor != request.Amount.MinorUnits || primary.AmountScale != request.Amount.Scale))
                db.Add(FinancialEvent.SourceConflict(primary, SignedAmount.Credit(request.Amount), at));
            await db.SaveChangesAsync(cancellationToken);
            return new RecognitionResult(primary.Id, false, true);
        }

        var asset = TextRules.Token(request.AssetCode, "validation");
        var contract = TextRules.Contract(request.TokenContract);
        var destination = TextRules.Contract(request.Destination);
        var matches = intent.InstructionsIssued
            && option.Status == OptionStatus.issued
            && option.AssetCode == asset
            && option.ChainId == request.Key.ChainId
            && option.TokenContract == contract
            && option.Destination == destination
            && intent.ObligationAsset == asset
            && intent.ObligationScale == request.Amount.Scale;
        if (!matches)
        {
            var classification = Classify(option, intent, request.Key, asset, contract, destination);
            var unallocated = FinancialEvent.Unallocated(
                intent.TenantId,
                intent.MerchantId,
                intent.Id,
                option.Id,
                observation.Id,
                request.Amount,
                asset,
                request.Key.ChainId,
                contract,
                classification,
                at);
            db.Add(unallocated);
            db.Add(EffectSource.Create(request.TenantId, unallocated.Id, request.SourceKind, sourceKey, at));
            await db.SaveChangesAsync(cancellationToken);
            await ApplyProjection(intent, "unallocated", at, cancellationToken);
            return new RecognitionResult(unallocated.Id, false, false);
        }

        var recognition = FinancialEvent.Recognition(
            intent.TenantId,
            intent.MerchantId,
            intent.Id,
            option.Id,
            observation.Id,
            request.Amount,
            asset,
            request.Key.ChainId,
            contract,
            intent.TolerancePolicyVersionId,
            at);
        db.Add(recognition);
        db.Add(EffectSource.Create(request.TenantId, recognition.Id, request.SourceKind, sourceKey, at));
        await db.SaveChangesAsync(cancellationToken);
        await ApplyProjection(intent, "recognition", at, cancellationToken);
        return new RecognitionResult(recognition.Id, true, false);
    }

    private async Task ApplyProjection(PaymentIntent intent, string reason, DateTimeOffset at, CancellationToken cancellationToken)
    {
        var loaded = await LoadFacts(intent, cancellationToken);
        var target = AcceptanceMachine.Target(loaded.Facts);
        var flags = AcceptanceMachine.Flags(loaded.Facts, loaded.Late);
        var path = AcceptanceMachine.Path(intent.AcceptanceStatus, target);
        Assign(intent, loaded.Facts, flags);
        var now = UtcTime.Require(at);
        if (path.Count == 0)
        {
            intent.UpdatedAt = now;
            intent.Version++;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (var status in path)
        {
            var from = intent.AcceptanceStatus;
            intent.AcceptanceStatus = status;
            intent.UpdatedAt = now;
            intent.Version++;
            db.Add(StatusEdge.Create(intent.TenantId, intent.Id, from, status, reason, now));
            var webhook = AcceptanceMachine.WebhookType(status);
            if (webhook is not null)
                db.Add(WebhookEvent.Create(intent.TenantId, intent.Id, webhook, Payload(webhook, intent.Id, status), now));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<LoadedFacts> LoadFacts(PaymentIntent intent, CancellationToken cancellationToken)
    {
        var decisions = await db.AcceptanceDecisions.AsNoTracking()
            .Where(row => row.TenantId == intent.TenantId && row.PaymentIntentId == intent.Id)
            .ToListAsync(cancellationToken);
        var events = await db.FinancialEvents.AsNoTracking()
            .Where(row => row.TenantId == intent.TenantId && row.PaymentIntentId == intent.Id)
            .ToListAsync(cancellationToken);
        var optionIds = await db.PaymentOptions.AsNoTracking()
            .Where(row => row.TenantId == intent.TenantId && row.PaymentIntentId == intent.Id)
            .Select(row => row.Id)
            .ToListAsync(cancellationToken);
        var attempts = await db.PaymentAttempts.AsNoTracking()
            .Where(row => row.TenantId == intent.TenantId && optionIds.Contains(row.PaymentOptionId))
            .ToListAsync(cancellationToken);
        var compliance = await db.ComplianceDecisions.AsNoTracking()
            .Where(row => row.TenantId == intent.TenantId && row.PaymentIntentId == intent.Id)
            .OrderByDescending(row => row.RecordedAt)
            .ThenByDescending(row => row.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var observationIds = events.Where(row => row.ObservationId is not null).Select(row => row.ObservationId!.Value).Distinct().ToList();
        var evidence = observationIds.Count == 0
            ? []
            : await db.ObservationEvidence.AsNoTracking()
                .Where(row => row.TenantId == intent.TenantId && observationIds.Contains(row.ObservationId))
                .ToListAsync(cancellationToken);
        var reconciliation = await db.ReconciliationRecords.AsNoTracking()
            .Where(row => row.TenantId == intent.TenantId && row.PaymentIntentId == intent.Id)
            .OrderByDescending(row => row.RecordedAt)
            .ThenByDescending(row => row.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var recognized = AssetAmount.Zero(intent.ObligationScale);
        string? asset = null;
        long? chain = null;
        string? contract = null;
        foreach (var financial in events.Where(row => row.AffectsRecognizedValue).OrderBy(row => row.RecordedAt).ThenBy(row => row.Id))
        {
            if (financial.AmountScale != intent.ObligationScale)
                throw new LedgerRuleException("scale_mismatch", "Recognized value uses a different asset scale.");
            var absolute = new SignedAmount(financial.AmountMinor, financial.AmountScale).Absolute();
            recognized = financial.AmountMinor.Sign < 0 ? recognized.Subtract(absolute) : recognized.Add(absolute);
            asset ??= financial.AssetCode;
            chain ??= financial.ChainId;
            contract ??= financial.TokenContract;
        }

        var compensated = events.Where(row => row.Kind == FinancialEventKind.compensation).Select(row => row.CompensatesEventId).ToHashSet();
        var contributing = events
            .Where(row => row.Kind == FinancialEventKind.recognition && row.AffectsRecognizedValue && row.ObservationId is not null && !compensated.Contains(row.Id))
            .Select(row => row.ObservationId!.Value)
            .Distinct()
            .ToArray();
        var finality = contributing.Length > 0 && contributing.All(id =>
        {
            var latest = evidence.Where(row => row.ObservationId == id).OrderByDescending(row => row.RecordedAt).ThenByDescending(row => row.Id).FirstOrDefault();
            return latest?.Lifecycle == ObservationLifecycle.finalized;
        });
        var sticky = new List<ExceptionFlag>();
        foreach (var financial in events.Where(row => row.Kind == FinancialEventKind.unallocated))
        {
            if (financial.Classification is not null && Enum.TryParse<ExceptionFlag>(financial.Classification, out var flag))
                sticky.Add(flag);
        }

        if (evidence.Any(row => row.Lifecycle == ObservationLifecycle.orphaned))
            sticky.Add(ExceptionFlag.reorganization_review);
        var terminalAt = decisions.Where(row => row.Kind is "expired" or "cancelled").Select(row => (DateTimeOffset?)row.RecordedAt).Min();
        var late = terminalAt is not null && events.Any(row => row.RecordedAt >= terminalAt);
        return new LoadedFacts(new AcceptanceFacts
        {
            InstructionsIssued = decisions.Any(row => row.Kind == "instructions_issued"),
            ExpiryRecorded = decisions.Any(row => row.Kind == "expired"),
            CancellationRecorded = decisions.Any(row => row.Kind == "cancelled"),
            AnyRecognition = events.Any(row => row.Kind == FinancialEventKind.recognition && row.AffectsRecognizedValue),
            Obligation = intent.Obligation,
            Tolerance = intent.Tolerance,
            Recognized = recognized,
            FinalityMet = finality,
            Compliance = compliance?.Posture ?? CompliancePosture.not_evaluated,
            AttemptBlocks = attempts.Any(row => row.Outcome is AttemptOutcome.prepared or AttemptOutcome.unknown),
            ReorganizationReview = evidence.Any(row => row.Lifecycle == ObservationLifecycle.orphaned),
            Settlement = reconciliation?.Posture ?? SettlementPosture.pending,
            StickyFlags = FlagSet.From(sticky),
            RecognizedAsset = asset,
            RecognizedChainId = chain,
            RecognizedContract = contract,
        }, late);
    }

    private static void Assign(PaymentIntent intent, AcceptanceFacts facts, FlagSet flags)
    {
        intent.CompliancePosture = facts.Compliance;
        intent.SettlementPosture = facts.Settlement;
        intent.ExceptionFlags = flags.Encoded;
        intent.InstructionsIssued = facts.InstructionsIssued;
        intent.ExpiryRecorded = facts.ExpiryRecorded;
        intent.CancellationRecorded = facts.CancellationRecorded;
        intent.AnyRecognition = facts.AnyRecognition;
        intent.FinalityMet = facts.FinalityMet;
        intent.AttemptBlocks = facts.AttemptBlocks;
        intent.ReorganizationReview = facts.ReorganizationReview;
        intent.RecognizedMinor = facts.Recognized.MinorUnits;
        intent.RecognizedScale = facts.Recognized.Scale;
        intent.RecognizedText = facts.Recognized.CanonicalText;
        intent.RecognizedAsset = facts.RecognizedAsset;
        intent.RecognizedChainId = facts.RecognizedChainId;
        intent.RecognizedContract = facts.RecognizedContract;
    }

    private static string Classify(
        PaymentOption option,
        PaymentIntent intent,
        ChainObservationKey key,
        string asset,
        string contract,
        string destination)
    {
        if (option.ChainId != key.ChainId)
            return ExceptionFlag.wrong_chain.ToString();
        if (option.TokenContract != contract || option.AssetCode != asset || intent.ObligationAsset != asset)
            return ExceptionFlag.wrong_token.ToString();
        if (option.Destination != destination)
            return ExceptionFlag.unallocated.ToString();
        return ExceptionFlag.unallocated.ToString();
    }

    private async Task<T> InTransaction<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is not null)
            return await action();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private Task InTransaction(Func<Task> action, CancellationToken cancellationToken) =>
        InTransaction(async () =>
        {
            await action();
            return true;
        }, cancellationToken);

    private async Task<Tenant> LoadTenant(Guid tenantId, CancellationToken cancellationToken) =>
        await db.Tenants.SingleOrDefaultAsync(row => row.Id == tenantId, cancellationToken)
        ?? throw new LedgerRuleException("not_found", "Tenant was not found.");

    private async Task<Merchant> LoadMerchant(Guid tenantId, Guid merchantId, CancellationToken cancellationToken) =>
        await db.Merchants.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == merchantId, cancellationToken)
        ?? throw new LedgerRuleException("not_found", "Merchant was not found in this tenant.");

    private async Task<PaymentIntent> LoadIntent(Guid tenantId, Guid intentId, CancellationToken cancellationToken) =>
        await db.PaymentIntents.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == intentId, cancellationToken)
        ?? throw new LedgerRuleException("not_found", "Payment intent was not found in this tenant.");

    private async Task<PaymentOption> LoadOption(Guid tenantId, Guid optionId, CancellationToken cancellationToken) =>
        await db.PaymentOptions.SingleOrDefaultAsync(row => row.TenantId == tenantId && row.Id == optionId, cancellationToken)
        ?? throw new LedgerRuleException("not_found", "Payment option was not found in this tenant.");

    private static string Payload(string type, Guid intentId, AcceptanceStatus status) =>
        "{\"event_type\":\"" + type + "\",\"payment_intent_id\":\"" + intentId + "\",\"acceptance_status\":\"" + status + "\"}";

    private static string Payload(string type, Guid intentId, string settlement) =>
        "{\"event_type\":\"" + type + "\",\"payment_intent_id\":\"" + intentId + "\",\"settlement_posture\":\"" + settlement + "\"}";

    private static bool IsUnique(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
                return true;
        }

        return false;
    }

    private sealed record LoadedFacts(AcceptanceFacts Facts, bool Late);
}
