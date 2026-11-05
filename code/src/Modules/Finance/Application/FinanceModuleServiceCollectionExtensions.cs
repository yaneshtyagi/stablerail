using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Finance.Domain;

namespace StableRail.Modules.Finance.Application;

public static class FinanceModuleServiceCollectionExtensions
{
    public static IServiceCollection AddFinanceModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(FinanceModule.Ownership.Name));
        return services;
    }
}
