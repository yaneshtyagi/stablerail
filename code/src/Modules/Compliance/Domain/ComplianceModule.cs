using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Compliance.Domain;

public static class ComplianceModule
{
    public static readonly ModuleOwnership Ownership = new(
        "compliance",
        "compliance",
        "Screening decisions, holds and case references.",
        ["CMP-001", "CMP-002", "CMP-003", "CMP-004"]);
}
