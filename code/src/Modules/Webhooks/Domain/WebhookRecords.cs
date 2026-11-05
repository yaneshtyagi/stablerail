using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Webhooks.Domain;

public sealed class WebhookEvent
{
    private WebhookEvent()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? PaymentIntentId { get; set; }
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }

    public static WebhookEvent Create(
        Guid tenantId,
        Guid? paymentIntentId,
        string eventType,
        string payload,
        DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PaymentIntentId = paymentIntentId,
            EventType = TextRules.Required(eventType, "validation", 100),
            Payload = TextRules.Json(payload),
            RecordedAt = UtcTime.Require(recordedAt),
        };
}

public sealed class WebhookDelivery
{
    private WebhookDelivery()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WebhookEventId { get; set; }
    public int AttemptNumber { get; set; }
    public string Outcome { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }

    public static WebhookDelivery Create(
        Guid tenantId,
        Guid webhookEventId,
        int attemptNumber,
        string outcome,
        DateTimeOffset recordedAt)
    {
        if (attemptNumber < 1)
            throw new LedgerRuleException("validation", "Delivery attempt numbers start at 1.");
        return new WebhookDelivery
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            WebhookEventId = webhookEventId,
            AttemptNumber = attemptNumber,
            Outcome = TextRules.Required(outcome, "validation", 64),
            RecordedAt = UtcTime.Require(recordedAt),
        };
    }
}
