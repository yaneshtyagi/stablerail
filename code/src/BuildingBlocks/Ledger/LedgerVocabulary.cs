namespace StableRail.BuildingBlocks.Ledger;

public enum AcceptanceStatus
{
    created,
    requires_payment,
    detected,
    confirming,
    succeeded,
    expired,
    cancelled,
}

public enum ExceptionFlag
{
    underpayment,
    overpayment,
    late_transfer,
    wrong_token,
    wrong_chain,
    unallocated,
    reorganization_review,
}

public enum CompliancePosture
{
    not_evaluated,
    clear,
    held,
    blocked,
    unavailable,
}

public enum AttemptOutcome
{
    not_attempted,
    prepared,
    failed_before_commit,
    known_not_committed,
    committed,
    unknown,
}

public enum SettlementPosture
{
    pending,
    settled,
    mismatch,
    exception,
}

public enum OptionStatus
{
    draft,
    issuing,
    issued,
    issuance_unknown,
}

public enum ObservationLifecycle
{
    seen,
    included,
    finalized,
    orphaned,
}

public enum FinancialEventKind
{
    recognition,
    compensation,
    unallocated,
    source_conflict,
}
