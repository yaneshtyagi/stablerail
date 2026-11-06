using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Providers.Domain;

public sealed class Provider
{
    private Provider()
    {
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static Provider Create(string name, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = TextRules.Required(name, "validation", 200),
            CreatedAt = UtcTime.Require(createdAt),
        };
}

public sealed class ProviderAccount
{
    private ProviderAccount()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProviderId { get; set; }
    public string EnvironmentName { get; set; } = "";
    public string Label { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static ProviderAccount Create(
        Guid tenantId,
        Guid providerId,
        string environmentName,
        string label,
        string status,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProviderId = providerId,
            EnvironmentName = TextRules.Required(environmentName, "validation", 32),
            Label = TextRules.Required(label, "validation", 200),
            Status = TextRules.Required(status, "validation", 64),
            CreatedAt = UtcTime.Require(createdAt),
        };
}

public sealed class RoutingPolicyVersion
{
    private RoutingPolicyVersion()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Document { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static RoutingPolicyVersion Create(Guid tenantId, string document, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Document = TextRules.Json(document),
            CreatedAt = UtcTime.Require(createdAt),
        };
}

public sealed class RoutingDecision
{
    private RoutingDecision()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MerchantId { get; set; }
    public Guid PaymentIntentId { get; set; }
    public Guid PolicyVersionId { get; set; }
    public string Inputs { get; set; } = "";
    public string Candidates { get; set; } = "";
    public Guid? SelectedProviderAccountId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public static RoutingDecision Create(
        Guid tenantId,
        Guid merchantId,
        Guid paymentIntentId,
        Guid policyVersionId,
        string inputs,
        string candidates,
        Guid? selectedProviderAccountId,
        DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            PaymentIntentId = paymentIntentId,
            PolicyVersionId = policyVersionId,
            Inputs = TextRules.Json(inputs),
            Candidates = TextRules.Json(candidates),
            SelectedProviderAccountId = selectedProviderAccountId,
            RecordedAt = UtcTime.Require(recordedAt),
        };
}
