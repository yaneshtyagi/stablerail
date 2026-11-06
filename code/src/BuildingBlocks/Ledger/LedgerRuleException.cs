namespace StableRail.BuildingBlocks.Ledger;

public sealed class LedgerRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
