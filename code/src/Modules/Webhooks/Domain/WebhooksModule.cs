using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Webhooks.Domain;

public static class WebhooksModule
{
    public static readonly ModuleOwnership Ownership = new(
        "webhooks",
        "webhooks",
        "Endpoints, signed delivery attempts and replays.",
        ["WHK-001", "WHK-002", "WHK-003", "WHK-004", "NFR-003"]);
}
