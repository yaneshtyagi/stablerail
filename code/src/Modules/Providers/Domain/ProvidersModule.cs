using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Providers.Domain;

public static class ProvidersModule
{
    public static readonly ModuleOwnership Ownership = new(
        "providers",
        "providers",
        "Provider adapters, health, routing policy and route decisions.",
        ["PRV-001", "PRV-002", "PRV-003", "PRV-004", "RTE-001", "RTE-002", "RTE-003", "RTE-004", "RTE-005"]);
}
