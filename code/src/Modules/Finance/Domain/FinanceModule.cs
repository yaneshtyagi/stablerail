using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Finance.Domain;

public static class FinanceModule
{
    public static readonly ModuleOwnership Ownership = new(
        "finance",
        "finance",
        "Settlement evidence, reconciliation and statement imports.",
        ["SET-001", "SET-002", "SET-003", "SET-004", "SET-005"]);
}
