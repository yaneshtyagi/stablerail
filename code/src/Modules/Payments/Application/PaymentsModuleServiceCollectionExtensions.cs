using Microsoft.Extensions.DependencyInjection;
using StableRail.BuildingBlocks.Modules;
using StableRail.Modules.Payments.Domain;

namespace StableRail.Modules.Payments.Application;

public static class PaymentsModuleServiceCollectionExtensions
{
    public static IServiceCollection AddPaymentsModule(this IServiceCollection services)
    {
        services.AddSingleton<IModule>(new ModuleRegistration(PaymentsModule.Ownership.Name));
        return services;
    }
}
