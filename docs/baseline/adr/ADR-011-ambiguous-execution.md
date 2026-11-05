# ADR-011 Never blindly replay an ambiguous irreversible operation

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-011; RTE-005 and AT-004
- **Scope:** Any provider call that may commit instructions or a later P1 transfer.

## Context

A timeout does not prove that the provider failed. The prototype sets `unknownOutcome`, blocks cancellation and finality, and reconciles back to the original provider. That safety rule is kept. The prototype can enforce it only because nothing is actually sent.

## Decision

Before an external provider call, the platform commits a payment attempt in `prepared` state with a new correlation and provider idempotency reference. The call then uses that same reference.

Outcomes:

- **Committed:** the provider response is a definitive acknowledgement of these instructions or this execution. Persist the instructions or execution evidence. Fallback to another provider is forbidden for this attempt.
- **Known not committed:** the provider response is an authoritative rejection that no commitment was created. Fallback is allowed only after gates are revalidated, and only by creating a new attempt. The rejected attempt remains.
- **Unknown:** timeout, connection loss, or an ambiguous server error. Mark the attempt `unknown`. Do not cancel the intent, do not start another provider attempt, and do not treat the payment as failed or final. Reconciliation calls `getTransactionStatus` with the original correlation reference.

A crash after the attempt commit and before a definitive response is `unknown`, including when the process dies before the request leaves the host. Recovery must not insert a second attempt for the same operation.

Issued deposit instructions remain readable after expiry, cancellation, provider stop and later configuration changes.

## Consequences

- Slice 5 provides the attempt record and crash tests.
- Slice 7 classifies adapter errors into the three outcomes. An unclassified error is `unknown`.
- Slice 9’s fallback uses only `known_not_committed` or a pre-attempt failure.

## Production gate

AT-004 and the crash cases in AT-013 are required before live providers are enabled. They have not been run.

## Prototype conflict

`reconcile_timeout` instantly clears the flag because the scenario knows the answer. Production reconciliation depends on provider evidence, which stays unavailable until credentials exist. The dev fake may resolve unknown attempts only in Development and Test, and the UI must label that control as simulated.
