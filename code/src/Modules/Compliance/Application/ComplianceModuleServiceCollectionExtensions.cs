using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Compliance.Domain;

namespace StableRail.Modules.Compliance.Application;

public static class ComplianceModuleServiceCollectionExtensions
{
    public static IServiceCollection AddComplianceModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(ComplianceModule.Ownership.Name));
        return services;
    }
}
