using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Identity.Domain;

namespace StableRail.Modules.Identity.Application;

public static class IdentityModuleServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(IdentityModule.Ownership.Name));
        return services;
    }
}
