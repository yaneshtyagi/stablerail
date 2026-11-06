using System.Numerics;
using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;
using StableRail.BuildingBlocks.Time;

namespace StableRail.Modules.Monitoring.Domain;

public readonly record struct ChainObservationKey(long ChainId, string TransactionHash, int LogIndex)
{
    public static ChainObservationKey Create(long chainId, string transactionHash, int logIndex)
    {
        if (chainId <= 0)
            throw new LedgerRuleException("validation", "Chain id must be positive.");
        if (logIndex < 0)
            throw new LedgerRuleException("validation", "Log index cannot be negative.");
        var hash = TextRules.Required(transactionHash, "validation", 128).ToLowerInvariant();
        if (hash.Length < 8 || hash.Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new LedgerRuleException("validation", "Transaction hash must be 8 to 128 letters or digits.");
        return new ChainObservationKey(chainId, hash, logIndex);
    }

    public string SourceKey => $"chain:{ChainId}:{TransactionHash}:{LogIndex}";
}

public sealed class BlockchainObservation
{
    private BlockchainObservation()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public long ChainId { get; set; }
    public string TransactionHash { get; set; } = "";
    public int LogIndex { get; set; }
    public long? BlockNumber { get; set; }
    public string? BlockHash { get; set; }
    public string TokenContract { get; set; } = "";
    public string Destination { get; set; } = "";
    public BigInteger AmountMinor { get; set; }
    public int AmountScale { get; set; }
    public string AmountText { get; set; } = "";
    public string AssetCode { get; set; } = "";
    public string RawProvenance { get; set; } = "";
    public DateTimeOffset ObservedAt { get; set; }

    public static BlockchainObservation Create(
        Guid tenantId,
        ChainObservationKey key,
        long? blockNumber,
        string? blockHash,
        string tokenContract,
        string destination,
        AssetAmount amount,
        string assetCode,
        string rawProvenance,
        DateTimeOffset observedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ChainId = key.ChainId,
            TransactionHash = key.TransactionHash,
            LogIndex = key.LogIndex,
            BlockNumber = blockNumber,
            BlockHash = blockHash is null ? null : TextRules.Contract(blockHash),
            TokenContract = TextRules.Contract(tokenContract),
            Destination = TextRules.Contract(destination),
            AmountMinor = amount.MinorUnits,
            AmountScale = amount.Scale,
            AmountText = amount.OriginalText,
            AssetCode = TextRules.Token(assetCode, "validation"),
            RawProvenance = TextRules.Json(rawProvenance),
            ObservedAt = UtcTime.Require(observedAt),
        };
}

public sealed class ObservationEvidence
{
    private ObservationEvidence()
    {
    }

    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ObservationId { get; set; }
    public ObservationLifecycle Lifecycle { get; set; }
    public string? BlockHash { get; set; }
    public DateTimeOffset RecordedAt { get; set; }

    public static ObservationEvidence Create(
        Guid tenantId,
        Guid observationId,
        ObservationLifecycle lifecycle,
        string? blockHash,
        DateTimeOffset recordedAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ObservationId = observationId,
            Lifecycle = lifecycle,
            BlockHash = blockHash is null ? null : TextRules.Contract(blockHash),
            RecordedAt = UtcTime.Require(recordedAt),
        };
}
