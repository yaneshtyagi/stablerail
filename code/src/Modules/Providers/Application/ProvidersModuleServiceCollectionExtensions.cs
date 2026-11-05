using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Providers.Domain;

namespace StableRail.Modules.Providers.Application;

public static class ProvidersModuleServiceCollectionExtensions
{
    public static IServiceCollection AddProvidersModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(ProvidersModule.Ownership.Name));
        return services;
    }
}
