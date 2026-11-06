using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Identity.Domain;

public sealed class Tenant
{
    private Tenant()
    {
    }

    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static Tenant Create(string name, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = TextRules.Required(name, "validation", 200),
            CreatedAt = UtcTime.Require(createdAt),
        };
}

public sealed class Merchant
{
    private Merchant()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static Merchant Create(Guid tenantId, string name, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = TextRules.Required(name, "validation", 200),
            CreatedAt = UtcTime.Require(createdAt),
        };
}

public sealed class Credential
{
    private Credential()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? MerchantId { get; set; }
    public string EnvironmentName { get; set; } = "";
    public string Label { get; set; } = "";
    public string VerifierHash { get; set; } = "";
    public string Scopes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public Guid? RotatedFromId { get; set; }

    public static Credential Create(
        Guid tenantId,
        Guid? merchantId,
        string environmentName,
        string label,
        string verifierHash,
        string scopes,
        DateTimeOffset createdAt,
        Guid? rotatedFromId)
    {
        var hash = TextRules.Required(verifierHash, "validation", 128);
        if (hash.Length < 32 || hash.Any(character => !char.IsAsciiHexDigit(character)) || hash.Contains("sk_", StringComparison.OrdinalIgnoreCase))
            throw new LedgerRuleException("validation", "Credential storage accepts a hex verifier, not a secret.");
        return new Credential
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            EnvironmentName = TextRules.Required(environmentName, "validation", 32),
            Label = TextRules.Required(label, "validation", 200),
            VerifierHash = hash.ToLowerInvariant(),
            Scopes = TextRules.Required(scopes, "validation", 400),
            CreatedAt = UtcTime.Require(createdAt),
            RotatedFromId = rotatedFromId,
        };
    }
}

public sealed class MerchantPolicyVersion
{
    private MerchantPolicyVersion()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MerchantId { get; set; }
    public string Kind { get; set; } = "";
    public string Document { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }

    public static MerchantPolicyVersion Create(
        Guid tenantId,
        Guid merchantId,
        string kind,
        string document,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            MerchantId = merchantId,
            Kind = TextRules.Required(kind, "validation", 64),
            Document = TextRules.Json(document),
            CreatedAt = UtcTime.Require(createdAt),
        };
}

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ActorId { get; set; } = "";
    public string Action { get; set; } = "";
    public string ObjectType { get; set; } = "";
    public Guid? ObjectId { get; set; }
    public string Result { get; set; } = "";
    public string Details { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }

    public static AuditEvent Create(
        Guid tenantId,
        string actorId,
        string action,
        string objectType,
        Guid? objectId,
        string result,
        string details,
        DateTimeOffset recordedAt)
    {
        TextRules.RejectSensitive(details);
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorId = TextRules.Required(actorId, "validation", 200),
            Action = TextRules.Required(action, "validation", 100),
            ObjectType = TextRules.Required(objectType, "validation", 100),
            ObjectId = objectId,
            Result = TextRules.Required(result, "validation", 64),
            Details = TextRules.Json(details),
            RecordedAt = UtcTime.Require(recordedAt),
        };
    }
}
