using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Identity.Domain;

public static class IdentityModule
{
    public static readonly ModuleOwnership Ownership = new(
        "identity",
        "identity",
        "Tenants, merchants, operator accounts, sessions, MFA and environment-scoped credentials.",
        ["TEN-001", "TEN-002", "TEN-003", "TEN-004", "NFR-001"]);
}
