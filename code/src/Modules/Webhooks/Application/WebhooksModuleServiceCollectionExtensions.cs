using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Webhooks.Domain;

namespace StableRail.Modules.Webhooks.Application;

public static class WebhooksModuleServiceCollectionExtensions
{
    public static IServiceCollection AddWebhooksModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(WebhooksModule.Ownership.Name));
        return services;
    }
}
