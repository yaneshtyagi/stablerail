using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Compliance.Application;
using StableRail.Modules.Finance.Application;
using StableRail.Modules.Identity.Application;
using StableRail.Modules.Monitoring.Application;
using StableRail.Modules.Payments.Application;
using StableRail.Modules.Providers.Application;
using StableRail.Modules.Webhooks.Application;

namespace StableRail.Infrastructure.Composition;

public static class ModuleComposition
{
    public static IServiceCollection AddStableRailModules(this IServiceCollection services)
    {
        services.AddComplianceModule();
        services.AddFinanceModule();
        services.AddIdentityModule();
        services.AddMonitoringModule();
        services.AddPaymentsModule();
        services.AddProvidersModule();
        services.AddWebhooksModule();
        return services;
    }
}
