using StableRail.BuildingBlocks.Ledger;
using StableRail.BuildingBlocks.Money;
using StableRail.Modules.Payments.Domain;

namespace StableRail.Infrastructure.Tests;

public sealed class AcceptanceMachineTests
{
    private static readonly AssetAmount Obligation = AssetAmount.Parse("250", 6);
    private static readonly AssetAmount Zero = AssetAmount.Zero(6);

    [Fact]
    public void Full_final_clear_funding_succeeds_and_partial_funding_does_not()
    {
        var partial = Facts(recognized: AssetAmount.Parse("100", 6), anyRecognition: true, instructions: true, finality: true);
        var full = partial with { Recognized = Obligation };

        Assert.Equal(AcceptanceStatus.detected, AcceptanceMachine.Target(partial));
        Assert.Equal(AcceptanceStatus.succeeded, AcceptanceMachine.Target(full));
        Assert.Contains(ExceptionFlag.underpayment, AcceptanceMachine.Flags(partial, false).Items);
        Assert.Contains(ExceptionFlag.overpayment, AcceptanceMachine.Flags(full with { Recognized = AssetAmount.Parse("300", 6) }, false).Items);
    }

    [Fact]
    public void Threshold_without_finality_or_clear_compliance_stays_confirming()
    {
        var open = Facts(Obligation, anyRecognition: true, instructions: true, finality: false);
        var held = open with { FinalityMet = true, Compliance = CompliancePosture.held };

        Assert.Equal(AcceptanceStatus.confirming, AcceptanceMachine.Target(open));
        Assert.Equal(AcceptanceStatus.confirming, AcceptanceMachine.Target(held));
        Assert.False(AcceptanceMachine.CanSucceed(held));
    }

    [Fact]
    public void Terminal_decisions_and_missing_instructions_win_over_funding()
    {
        var funded = Facts(Obligation, anyRecognition: true, instructions: true, finality: true);

        Assert.Equal(AcceptanceStatus.expired, AcceptanceMachine.Target(funded with { ExpiryRecorded = true }));
        Assert.Equal(AcceptanceStatus.cancelled, AcceptanceMachine.Target(funded with { CancellationRecorded = true }));
        Assert.Equal(AcceptanceStatus.created, AcceptanceMachine.Target(Facts(Zero, anyRecognition: false, instructions: false, finality: false)));
        Assert.Throws<LedgerRuleException>(() => AcceptanceMachine.Target(funded with { InstructionsIssued = false }));
    }

    [Fact]
    public void Allowed_paths_stop_at_the_documented_edges()
    {
        Assert.Equal([AcceptanceStatus.succeeded], AcceptanceMachine.Path(AcceptanceStatus.requires_payment, AcceptanceStatus.succeeded));
        Assert.Equal(
            [AcceptanceStatus.confirming, AcceptanceStatus.detected],
            AcceptanceMachine.Path(AcceptanceStatus.succeeded, AcceptanceStatus.detected));
        Assert.Throws<LedgerRuleException>(() => AcceptanceMachine.Path(AcceptanceStatus.succeeded, AcceptanceStatus.requires_payment));
        Assert.Throws<LedgerRuleException>(() => AcceptanceMachine.Path(AcceptanceStatus.expired, AcceptanceStatus.succeeded));
        Assert.False(AcceptanceMachine.IsDirect(AcceptanceStatus.created, AcceptanceStatus.succeeded));
        Assert.False(AcceptanceMachine.IsDirect(AcceptanceStatus.succeeded, AcceptanceStatus.detected));
    }

    [Theory]
    [InlineData(AttemptOutcome.prepared, AttemptOutcome.unknown, true)]
    [InlineData(AttemptOutcome.unknown, AttemptOutcome.committed, true)]
    [InlineData(AttemptOutcome.unknown, AttemptOutcome.prepared, false)]
    [InlineData(AttemptOutcome.committed, AttemptOutcome.unknown, false)]
    public void Attempt_outcomes_move_only_through_the_reconciliation_edges(AttemptOutcome from, AttemptOutcome to, bool allowed) =>
        Assert.Equal(allowed, AcceptanceMachine.CanMoveAttempt(from, to));

    [Fact]
    public void Reorganization_and_an_unknown_attempt_block_success()
    {
        var ready = Facts(Obligation, anyRecognition: true, instructions: true, finality: true);

        Assert.Equal(AcceptanceStatus.confirming, AcceptanceMachine.Target(ready with { ReorganizationReview = true }));
        Assert.Equal(AcceptanceStatus.confirming, AcceptanceMachine.Target(ready with { AttemptBlocks = true }));
    }

    private static AcceptanceFacts Facts(AssetAmount recognized, bool anyRecognition, bool instructions, bool finality) => new()
    {
        InstructionsIssued = instructions,
        ExpiryRecorded = false,
        CancellationRecorded = false,
        AnyRecognition = anyRecognition,
        Obligation = Obligation,
        Tolerance = Zero,
        Recognized = recognized,
        FinalityMet = finality,
        Compliance = CompliancePosture.clear,
        AttemptBlocks = false,
        ReorganizationReview = false,
        Settlement = SettlementPosture.pending,
        StickyFlags = FlagSet.Empty,
        RecognizedAsset = "SYN",
        RecognizedChainId = 990001,
        RecognizedContract = "syntheticcontract",
    };
}
