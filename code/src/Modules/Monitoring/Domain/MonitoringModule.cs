using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Monitoring.Domain;

public static class MonitoringModule
{
    public static readonly ModuleOwnership Ownership = new(
        "monitoring",
        "monitoring",
        "Chain observations, confirmation tracking and reorganization evidence.",
        ["MON-001", "MON-002", "MON-003", "MON-004", "MON-005", "MON-006"]);
}
