using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;

namespace StableRail.Modules.Payments.Domain;

public sealed record AcceptanceFacts
{
    public required bool InstructionsIssued { get; init; }
    public required bool ExpiryRecorded { get; init; }
    public required bool CancellationRecorded { get; init; }
    public required bool AnyRecognition { get; init; }
    public required AssetAmount Obligation { get; init; }
    public required AssetAmount Tolerance { get; init; }
    public required AssetAmount Recognized { get; init; }
    public required bool FinalityMet { get; init; }
    public required CompliancePosture Compliance { get; init; }
    public required bool AttemptBlocks { get; init; }
    public required bool ReorganizationReview { get; init; }
    public required SettlementPosture Settlement { get; init; }
    public required FlagSet StickyFlags { get; init; }
    public string? RecognizedAsset { get; init; }
    public long? RecognizedChainId { get; init; }
    public string? RecognizedContract { get; init; }
}

public static class AcceptanceMachine
{
    private static readonly IReadOnlyDictionary<AcceptanceStatus, AcceptanceStatus[]> Edges =
        new Dictionary<AcceptanceStatus, AcceptanceStatus[]>
        {
            [AcceptanceStatus.created] = [AcceptanceStatus.requires_payment, AcceptanceStatus.expired, AcceptanceStatus.cancelled],
            [AcceptanceStatus.requires_payment] =
            [
                AcceptanceStatus.detected,
                AcceptanceStatus.confirming,
                AcceptanceStatus.succeeded,
                AcceptanceStatus.expired,
                AcceptanceStatus.cancelled,
            ],
            [AcceptanceStatus.detected] = [AcceptanceStatus.confirming, AcceptanceStatus.succeeded, AcceptanceStatus.expired],
            [AcceptanceStatus.confirming] = [AcceptanceStatus.succeeded, AcceptanceStatus.detected],
            [AcceptanceStatus.succeeded] = [AcceptanceStatus.confirming],
            [AcceptanceStatus.expired] = [],
            [AcceptanceStatus.cancelled] = [],
        };

    public static AssetAmount Threshold(AssetAmount obligation, AssetAmount tolerance)
    {
        if (tolerance.CompareTo(obligation) > 0)
            throw new LedgerRuleException("precision", "Tolerance cannot exceed the obligation.");
        return obligation.Subtract(tolerance);
    }

    public static bool ThresholdMet(AcceptanceFacts facts) =>
        facts.Recognized.CompareTo(Threshold(facts.Obligation, facts.Tolerance)) >= 0;

    public static bool CanSucceed(AcceptanceFacts facts) =>
        ThresholdMet(facts)
        && facts.FinalityMet
        && facts.Compliance == CompliancePosture.clear
        && !facts.AttemptBlocks
        && !facts.ReorganizationReview;

    public static AcceptanceStatus Target(AcceptanceFacts facts)
    {
        if (!facts.InstructionsIssued && facts.AnyRecognition)
            throw new LedgerRuleException("evidence_conflict", "Recognition exists without issued instructions.");
        if (facts.CancellationRecorded)
            return AcceptanceStatus.cancelled;
        if (facts.ExpiryRecorded)
            return AcceptanceStatus.expired;
        if (!facts.InstructionsIssued)
            return AcceptanceStatus.created;
        if (!facts.AnyRecognition)
            return AcceptanceStatus.requires_payment;
        if (ThresholdMet(facts) && CanSucceed(facts))
            return AcceptanceStatus.succeeded;
        if (ThresholdMet(facts))
            return AcceptanceStatus.confirming;
        return AcceptanceStatus.detected;
    }

    public static FlagSet Flags(AcceptanceFacts facts, bool lateTransfer)
    {
        var flags = facts.StickyFlags.ExceptDerived();
        if (lateTransfer || facts.StickyFlags.Contains(ExceptionFlag.late_transfer))
            flags = flags.Add(ExceptionFlag.late_transfer);
        if (facts.ReorganizationReview)
            flags = flags.Add(ExceptionFlag.reorganization_review);
        var threshold = Threshold(facts.Obligation, facts.Tolerance);
        if (!facts.Recognized.IsZero && facts.Recognized.CompareTo(threshold) < 0)
            flags = flags.Add(ExceptionFlag.underpayment);
        if (facts.Recognized.CompareTo(facts.Obligation) > 0)
            flags = flags.Add(ExceptionFlag.overpayment);
        return flags;
    }

    public static bool IsDirect(AcceptanceStatus from, AcceptanceStatus to) =>
        from != to && Edges[from].Contains(to);

    public static IReadOnlyList<AcceptanceStatus> Path(AcceptanceStatus from, AcceptanceStatus to)
    {
        if (from == to)
            return [];

        var previous = new Dictionary<AcceptanceStatus, AcceptanceStatus>();
        var seen = new HashSet<AcceptanceStatus> { from };
        var queue = new Queue<AcceptanceStatus>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var next in Edges[current])
            {
                if (!seen.Add(next))
                    continue;
                previous[next] = current;
                if (next == to)
                    return Reconstruct(previous, from, to);
                queue.Enqueue(next);
            }
        }

        throw new LedgerRuleException("forbidden_transition", $"No allowed path from {from} to {to}.");
    }

    public static string? WebhookType(AcceptanceStatus status) => status switch
    {
        AcceptanceStatus.created => "payment_intent.created",
        AcceptanceStatus.detected => "payment_detected",
        AcceptanceStatus.confirming => "confirming",
        AcceptanceStatus.succeeded => "succeeded",
        AcceptanceStatus.expired => "expired",
        AcceptanceStatus.cancelled => "cancelled",
        _ => null,
    };

    public static bool CanMoveAttempt(AttemptOutcome from, AttemptOutcome to) => (from, to) switch
    {
        (AttemptOutcome.not_attempted, AttemptOutcome.prepared) => true,
        (AttemptOutcome.prepared, AttemptOutcome.failed_before_commit) => true,
        (AttemptOutcome.prepared, AttemptOutcome.known_not_committed) => true,
        (AttemptOutcome.prepared, AttemptOutcome.committed) => true,
        (AttemptOutcome.prepared, AttemptOutcome.unknown) => true,
        (AttemptOutcome.unknown, AttemptOutcome.committed) => true,
        (AttemptOutcome.unknown, AttemptOutcome.known_not_committed) => true,
        _ => false,
    };

    private static IReadOnlyList<AcceptanceStatus> Reconstruct(
        Dictionary<AcceptanceStatus, AcceptanceStatus> previous,
        AcceptanceStatus from,
        AcceptanceStatus to)
    {
        var reversed = new List<AcceptanceStatus>();
        var cursor = to;
        while (cursor != from)
        {
            reversed.Add(cursor);
            cursor = previous[cursor];
        }

        reversed.Reverse();
        return reversed;
    }
}
