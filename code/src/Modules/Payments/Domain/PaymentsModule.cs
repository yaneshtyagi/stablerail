using StableRail.BuildingBlocks.Modules;

namespace StableRail.Modules.Payments.Domain;

public static class PaymentsModule
{
    public static readonly ModuleOwnership Ownership = new(
        "payments",
        "payments",
        "Payment intents, options, instructions, idempotency and the acceptance lifecycle.",
        ["PAY-001", "PAY-002", "PAY-003", "PAY-004", "PAY-005", "PAY-006"]);
}
