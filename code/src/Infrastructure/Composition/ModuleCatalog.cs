using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Compliance.Domain;
using StableRail.Modules.Finance.Domain;
using StableRail.Modules.Identity.Domain;
using StableRail.Modules.Monitoring.Domain;
using StableRail.Modules.Payments.Domain;
using StableRail.Modules.Providers.Domain;
using StableRail.Modules.Webhooks.Domain;

namespace StableRail.Infrastructure.Composition;

public static class ModuleCatalog
{
    public static readonly DateTimeOffset RecordedAt = new(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);

    public static readonly IReadOnlyList<ModuleOwnership> All =
    [
        ComplianceModule.Ownership,
        FinanceModule.Ownership,
        IdentityModule.Ownership,
        MonitoringModule.Ownership,
        PaymentsModule.Ownership,
        ProvidersModule.Ownership,
        WebhooksModule.Ownership,
    ];
}
