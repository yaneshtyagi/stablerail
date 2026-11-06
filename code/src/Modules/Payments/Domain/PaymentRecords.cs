using System.Numerics;
using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Payments.Domain;

public sealed class PaymentIntent
{
    private PaymentIntent()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MerchantId { get; set; }
    public string MerchantReference { get; set; } = "";
    public AcceptanceStatus AcceptanceStatus { get; set; }
    public CompliancePosture CompliancePosture { get; set; }
    public SettlementPosture SettlementPosture { get; set; }
    public string ExceptionFlags { get; set; } = "";
    public bool InstructionsIssued { get; set; }
    public bool ExpiryRecorded { get; set; }
    public bool CancellationRecorded { get; set; }
    public bool AnyRecognition { get; set; }
    public bool FinalityMet { get; set; }
    public bool AttemptBlocks { get; set; }
    public bool ReorganizationReview { get; set; }
    public BigInteger ObligationMinor { get; set; }
    public int ObligationScale { get; set; }
    public string ObligationText { get; set; } = "";
    public string ObligationAsset { get; set; } = "";
    public BigInteger? QuotedFiatMinor { get; set; }
    public int? QuotedFiatScale { get; set; }
    public string? QuotedFiatText { get; set; }
    public string? QuotedFiatCurrency { get; set; }
    public string? QuoteReference { get; set; }
    public Guid? QuotePolicyVersionId { get; set; }
    public BigInteger RecognizedMinor { get; set; }
    public int RecognizedScale { get; set; }
    public string RecognizedText { get; set; } = "";
    public string? RecognizedAsset { get; set; }
    public long? RecognizedChainId { get; set; }
    public string? RecognizedContract { get; set; }
    public BigInteger ToleranceMinor { get; set; }
    public int ToleranceScale { get; set; }
    public Guid TolerancePolicyVersionId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int Version { get; set; }

    public AssetAmount Obligation => AssetAmount.FromMinor(ObligationMinor, ObligationScale);
    public AssetAmount Tolerance => AssetAmount.FromMinor(ToleranceMinor, ToleranceScale);
    public AssetAmount Recognized => AssetAmount.FromMinor(RecognizedMinor, RecognizedScale);

    public static PaymentIntent Create(
        Guid tenantId,
        Guid merchantId,
        string merchantReference,
        AssetAmount obligation,
        string obligationAsset,
        AssetAmount tolerance,
        Guid tolerancePolicyVersionId,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        FiatQuote? quotedFiat)
    {
        var at = UtcTime.Require(createdAt);
        var expiry = UtcTime.Require(expiresAt);
        if (expiry <= at)
            throw new LedgerRuleException("validation", "Expiry must be after creation.");
        if (obligation.IsZero)
            throw new LedgerRuleException("precision", "The obligation must be greater than zero.");
        _ = AcceptanceMachine.Threshold(obligation, tolerance);
        var asset = TextRules.Token(obligationAsset, "validation");
        var quote = quotedFiat?.Validate();
        return new PaymentIntent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            MerchantReference = TextRules.Required(merchantReference, "validation", 200),
            AcceptanceStatus = AcceptanceStatus.created,
            CompliancePosture = CompliancePosture.not_evaluated,
            SettlementPosture = SettlementPosture.pending,
            ExceptionFlags = "",
            ObligationMinor = obligation.MinorUnits,
            ObligationScale = obligation.Scale,
            ObligationText = obligation.OriginalText,
            ObligationAsset = asset,
            QuotedFiatMinor = quote?.Amount.MinorUnits,
            QuotedFiatScale = quote?.Amount.Scale,
            QuotedFiatText = quote?.Amount.OriginalText,
            QuotedFiatCurrency = quote?.Currency,
            QuoteReference = quote?.Reference,
            QuotePolicyVersionId = quote?.PolicyVersionId,
            RecognizedMinor = BigInteger.Zero,
            RecognizedScale = obligation.Scale,
            RecognizedText = AssetAmount.Zero(obligation.Scale).CanonicalText,
            ToleranceMinor = tolerance.MinorUnits,
            ToleranceScale = tolerance.Scale,
            TolerancePolicyVersionId = tolerancePolicyVersionId,
            ExpiresAt = expiry,
            CreatedAt = at,
            UpdatedAt = at,
            Version = 1,
        };
    }
}

public sealed record FiatQuote(string Currency, AssetAmount Amount, string Reference, Guid? PolicyVersionId)
{
    public FiatQuote Validate()
    {
        var currency = TextRules.Required(Currency, "validation", 8);
        if (currency.Length != 3 || currency.Any(character => !char.IsAsciiLetter(character)))
            throw new LedgerRuleException("validation", "Quoted fiat currency must be three letters.");
        if (Amount.IsZero)
            throw new LedgerRuleException("precision", "A quoted fiat amount must be greater than zero.");
        return this with
        {
            Currency = currency.ToUpperInvariant(),
            Reference = TextRules.Required(Reference, "validation", 200),
        };
    }
}

public sealed class PaymentOption
{
    private PaymentOption()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string AssetCode { get; set; } = "";
    public long ChainId { get; set; }
    public string TokenContract { get; set; } = "";
    public string Destination { get; set; } = "";
    public Guid ProviderAccountId { get; set; }
    public OptionStatus Status { get; set; }
    public string? Instructions { get; set; }
    public DateTimeOffset? InstructionsIssuedAt { get; set; }
    public Guid? RouteDecisionId { get; set; }
    public BigInteger AmountMinor { get; set; }
    public int AmountScale { get; set; }
    public string AmountText { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static PaymentOption Create(
        Guid tenantId,
        Guid paymentIntentId,
        string assetCode,
        long chainId,
        string tokenContract,
        string destination,
        Guid providerAccountId,
        AssetAmount amount,
        DateTimeOffset createdAt)
    {
        if (chainId <= 0)
            throw new LedgerRuleException("validation", "Chain id must be positive.");
        if (amount.IsZero)
            throw new LedgerRuleException("precision", "A payment option amount must be greater than zero.");
        return new PaymentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentIntentId = paymentIntentId,
            AssetCode = TextRules.Token(assetCode, "validation"),
            ChainId = chainId,
            TokenContract = TextRules.Contract(tokenContract),
            Destination = TextRules.Contract(destination),
            ProviderAccountId = providerAccountId,
            Status = OptionStatus.draft,
            AmountMinor = amount.MinorUnits,
            AmountScale = amount.Scale,
            AmountText = amount.OriginalText,
            CreatedAt = UtcTime.Require(createdAt),
        };
    }
}

public sealed class PaymentAttempt
{
    private PaymentAttempt()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PaymentOptionId { get; set; }
    public AttemptOutcome Outcome { get; set; }
    public string CorrelationReference { get; set; } = "";
    public DateTimeOffset PreparedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public int Version { get; set; }

    public static PaymentAttempt Prepare(Guid tenantId, Guid paymentOptionId, string correlationReference, DateTimeOffset preparedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentOptionId = paymentOptionId,
            Outcome = AttemptOutcome.prepared,
            CorrelationReference = TextRules.Required(correlationReference, "validation", 200),
            PreparedAt = UtcTime.Require(preparedAt),
            Version = 1,
        };
}

public sealed class AcceptanceDecision
{
    private AcceptanceDecision()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public string Kind { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }

    public static AcceptanceDecision Create(Guid tenantId, Guid paymentIntentId, string kind, DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentIntentId = paymentIntentId,
            Kind = kind,
            RecordedAt = UtcTime.Require(recordedAt),
        };
}

public sealed class StatusEdge
{
    private StatusEdge()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public AcceptanceStatus FromStatus { get; set; }
    public AcceptanceStatus ToStatus { get; set; }
    public string Reason { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }

    public static StatusEdge Create(
        Guid tenantId,
        Guid paymentIntentId,
        AcceptanceStatus from,
        AcceptanceStatus to,
        string reason,
        DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentIntentId = paymentIntentId,
            FromStatus = from,
            ToStatus = to,
            Reason = reason,
            RecordedAt = UtcTime.Require(recordedAt),
        };
}
