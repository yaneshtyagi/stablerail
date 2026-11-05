using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Monitoring.Domain;

namespace StableRail.Modules.Monitoring.Application;

public static class MonitoringModuleServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoringModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(MonitoringModule.Ownership.Name));
        return services;
    }
}
