using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Compliance.Domain;

public sealed class ComplianceDecision
{
    private ComplianceDecision()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MerchantId { get; set; }
    public Guid? PaymentIntentId { get; set; }
    public CompliancePosture Posture { get; set; }
    public string Source { get; set; } = "";
    public DateTimeOffset DecidedAt { get; set; }
    public Guid PolicyVersionId { get; set; }
    public DateTimeOffset FreshUntil { get; set; }
    public string Outcome { get; set; } = "";
    public string? CaseReference { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public static ComplianceDecision Create(
        Guid tenantId,
        Guid merchantId,
        Guid? paymentIntentId,
        CompliancePosture posture,
        string source,
        DateTimeOffset decidedAt,
        Guid policyVersionId,
        DateTimeOffset freshUntil,
        string outcome,
        string? caseReference,
        DateTimeOffset recordedAt)
    {
        if (posture == CompliancePosture.not_evaluated)
            throw new LedgerRuleException("validation", "A compliance decision has an evaluated posture.");
        if (posture == CompliancePosture.held && string.IsNullOrWhiteSpace(caseReference))
            throw new LedgerRuleException("validation", "A hold requires a case reference.");
        var at = UtcTime.Require(recordedAt);
        var decided = UtcTime.Require(decidedAt);
        var fresh = UtcTime.Require(freshUntil);
        return new ComplianceDecision
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            PaymentIntentId = paymentIntentId,
            Posture = posture,
            Source = TextRules.Required(source, "validation", 200),
            DecidedAt = decided,
            PolicyVersionId = policyVersionId,
            FreshUntil = fresh,
            Outcome = TextRules.Required(outcome, "validation", 200),
            CaseReference = string.IsNullOrWhiteSpace(caseReference) ? null : TextRules.Required(caseReference, "validation", 200),
            RecordedAt = at,
        };
    }
}
