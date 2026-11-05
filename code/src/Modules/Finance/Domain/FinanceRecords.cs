using System.Numerics;
using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Finance.Domain;

public sealed class FinancialEvent
{
    private FinancialEvent()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? MerchantId { get; set; }
    public Guid? PaymentIntentId { get; set; }
    public Guid? PaymentOptionId { get; set; }
    public Guid? ObservationId { get; set; }
    public FinancialEventKind Kind { get; set; }
    public bool AffectsRecognizedValue { get; set; }
    public BigInteger AmountMinor { get; set; }
    public int AmountScale { get; set; }
    public string AmountText { get; set; } = "";
    public string? AssetCode { get; set; }
    public long? ChainId { get; set; }
    public string? TokenContract { get; set; }
    public Guid? CompensatesEventId { get; set; }
    public string? Classification { get; set; }
    public Guid? PolicyVersionId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public static FinancialEvent Recognition(
        Guid tenantId,
        Guid merchantId,
        Guid paymentIntentId,
        Guid paymentOptionId,
        Guid observationId,
        AssetAmount amount,
        string assetCode,
        long chainId,
        string tokenContract,
        Guid? policyVersionId,
        DateTimeOffset recordedAt)
    {
        if (amount.IsZero)
            throw new LedgerRuleException("precision", "Recognition requires a positive amount.");
        return Create(
            tenantId,
            merchantId,
            paymentIntentId,
            paymentOptionId,
            observationId,
            FinancialEventKind.recognition,
            true,
            SignedAmount.Credit(amount),
            assetCode,
            chainId,
            tokenContract,
            null,
            null,
            policyVersionId,
            recordedAt);
    }

    public static FinancialEvent Compensation(
        FinancialEvent original,
        DateTimeOffset recordedAt)
    {
        if (original.Kind != FinancialEventKind.recognition || !original.AffectsRecognizedValue)
            throw new LedgerRuleException("forbidden_transition", "Only a value recognition can be compensated.");
        var signed = new SignedAmount(original.AmountMinor, original.AmountScale).Negate();
        return Create(
            original.TenantId,
            original.MerchantId,
            original.PaymentIntentId,
            original.PaymentOptionId,
            original.ObservationId,
            FinancialEventKind.compensation,
            true,
            signed,
            original.AssetCode,
            original.ChainId,
            original.TokenContract,
            original.Id,
            "compensation",
            original.PolicyVersionId,
            recordedAt);
    }

    public static FinancialEvent Unallocated(
        Guid tenantId,
        Guid merchantId,
        Guid paymentIntentId,
        Guid paymentOptionId,
        Guid observationId,
        AssetAmount amount,
        string assetCode,
        long chainId,
        string tokenContract,
        string classification,
        DateTimeOffset recordedAt)
    {
        if (amount.IsZero)
            throw new LedgerRuleException("precision", "An unallocated observation still records a positive amount.");
        return Create(
            tenantId,
            merchantId,
            paymentIntentId,
            paymentOptionId,
            observationId,
            FinancialEventKind.unallocated,
            false,
            SignedAmount.Credit(amount),
            assetCode,
            chainId,
            tokenContract,
            null,
            classification,
            null,
            recordedAt);
    }

    public static FinancialEvent SourceConflict(
        FinancialEvent original,
        SignedAmount reported,
        DateTimeOffset recordedAt) =>
        Create(
            original.TenantId,
            original.MerchantId,
            original.PaymentIntentId,
            original.PaymentOptionId,
            original.ObservationId,
            FinancialEventKind.source_conflict,
            false,
            reported,
            original.AssetCode,
            original.ChainId,
            original.TokenContract,
            null,
            "source_conflict",
            null,
            recordedAt);

    private static FinancialEvent Create(
        Guid tenantId,
        Guid? merchantId,
        Guid? paymentIntentId,
        Guid? paymentOptionId,
        Guid? observationId,
        FinancialEventKind kind,
        bool affectsRecognizedValue,
        SignedAmount amount,
        string? assetCode,
        long? chainId,
        string? tokenContract,
        Guid? compensatesEventId,
        string? classification,
        Guid? policyVersionId,
        DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            PaymentIntentId = paymentIntentId,
            PaymentOptionId = paymentOptionId,
            ObservationId = observationId,
            Kind = kind,
            AffectsRecognizedValue = affectsRecognizedValue,
            AmountMinor = amount.MinorUnits,
            AmountScale = amount.Scale,
            AmountText = amount.Text,
            AssetCode = assetCode is null ? null : TextRules.Token(assetCode, "validation"),
            ChainId = chainId,
            TokenContract = tokenContract is null ? null : TextRules.Contract(tokenContract),
            CompensatesEventId = compensatesEventId,
            Classification = classification,
            PolicyVersionId = policyVersionId,
            RecordedAt = UtcTime.Require(recordedAt),
        };
}

public sealed class EffectSource
{
    private EffectSource()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FinancialEventId { get; set; }
    public string SourceKind { get; set; } = "";
    public string SourceKey { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }

    public static EffectSource Create(
        Guid tenantId,
        Guid financialEventId,
        string sourceKind,
        string sourceKey,
        DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FinancialEventId = financialEventId,
            SourceKind = sourceKind,
            SourceKey = TextRules.Required(sourceKey, "validation", 300),
            RecordedAt = UtcTime.Require(recordedAt),
        };
}

public sealed class SettlementRecord
{
    private SettlementRecord()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProviderAccountId { get; set; }
    public string ProviderReference { get; set; } = "";
    public string PayloadSha256 { get; set; } = "";
    public BigInteger AmountMinor { get; set; }
    public int AmountScale { get; set; }
    public string AmountText { get; set; } = "";
    public Guid? PaymentIntentId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public static SettlementRecord Create(
        Guid tenantId,
        Guid providerAccountId,
        string providerReference,
        string payloadSha256,
        AssetAmount amount,
        Guid? paymentIntentId,
        DateTimeOffset recordedAt)
    {
        var hash = TextRules.Required(payloadSha256, "validation", 64);
        if (hash.Length != 64 || hash.Any(character => !char.IsAsciiHexDigit(character)))
            throw new LedgerRuleException("validation", "Settlement payload identity must be a SHA-256 hex digest.");
        return new SettlementRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProviderAccountId = providerAccountId,
            ProviderReference = TextRules.Required(providerReference, "validation", 200),
            PayloadSha256 = hash.ToLowerInvariant(),
            AmountMinor = amount.MinorUnits,
            AmountScale = amount.Scale,
            AmountText = amount.OriginalText,
            PaymentIntentId = paymentIntentId,
            RecordedAt = UtcTime.Require(recordedAt),
        };
    }
}

public sealed class ReconciliationRecord
{
    private ReconciliationRecord()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SettlementRecordId { get; set; }
    public Guid? PaymentIntentId { get; set; }
    public SettlementPosture Posture { get; set; }
    public Guid? FinancialEventId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public static ReconciliationRecord Create(
        Guid tenantId,
        Guid settlementRecordId,
        Guid? paymentIntentId,
        SettlementPosture posture,
        Guid? financialEventId,
        DateTimeOffset recordedAt)
    {
        if (posture == SettlementPosture.pending)
            throw new LedgerRuleException("validation", "A reconciliation record records an external result.");
        return new ReconciliationRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SettlementRecordId = settlementRecordId,
            PaymentIntentId = paymentIntentId,
            Posture = posture,
            FinancialEventId = financialEventId,
            RecordedAt = UtcTime.Require(recordedAt),
        };
    }
}
