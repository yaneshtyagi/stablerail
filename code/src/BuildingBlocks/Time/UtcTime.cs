using StableRail.BuildingBlocks.Ledger;

namespace StableRail.BuildingBlocks.Time;

public static class UtcTime
{
    public static DateTimeOffset Require(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
            throw new LedgerRuleException("timestamp", "Timestamps must be UTC.");
        return value;
    }
}
