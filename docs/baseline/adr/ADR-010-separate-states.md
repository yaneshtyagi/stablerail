# ADR-010 Separate payment, chain and settlement states

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-010; §§6.2, 6.5 and 6.8
- **Scope:** Domain model for MVP inbound payments.

## Context

The PRD forbids collapsing underpayment, overpayment, holds and settlement into one payment status. The prototype already stores `status`, `hold`, `flags`, `unknownOutcome` and `settlement` separately, and the detail screen shows them separately. That separation is kept and made explicit.

## Decision

These dimensions change independently:

- payment acceptance (`created`, `requires_payment`, `detected`, `confirming`, `succeeded`, `expired`, `cancelled`)
- exception flags
- compliance posture
- provider-attempt outcome
- settlement posture

`succeeded` means the acceptance gates passed. It does not mean funds settled to the beneficiary and it does not clear an exception flag such as overpayment. Settlement evidence comes from an external statement or provider settlement feed. The platform does not invent an internal settlement confirmation.

Chain observation state (seen, included, finalized, orphaned) is evidence on the observation. It is not a substitute for the acceptance status.

The allowed and forbidden transitions are specified in [STATE_MACHINE.md](../STATE_MACHINE.md).

## Consequences

- APIs and the operations UI render each dimension.
- Tests that only assert a single status string are incomplete for holds, unknown outcomes and settlement.

## Production gate

Success and settlement remain separate in every environment. A production console must not offer a control that marks settlement complete without external evidence.

## Prototype conflict

The prototype never uses `created`; issuance is synchronous inside `createIntent`. Production adds `created` for instruction-pending work. See the state machine.
