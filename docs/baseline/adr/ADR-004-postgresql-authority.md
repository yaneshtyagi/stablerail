# ADR-004 PostgreSQL authority

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-004; §§8 and 9
- **Scope:** All environments.

## Context

Financial correctness requires one authority for intents, observations, journal entries, idempotency and audit. The prototype stores that state in `localStorage` under `stablerail.sandbox.v3`, which the operator can edit.

## Decision

PostgreSQL is the only authority for tenant, merchant, credential verifiers, payment intent, payment option, payment attempt, routing decision, provider account, blockchain observation, financial event, settlement, reconciliation, compliance decision, webhook event, webhook delivery and audit event.

Redis may cache reads and throttling counters. A Redis miss or flush must not change financial history. Cache keys include the tenant identifier. The browser is not an authority; the operations UI holds view state only.

Money columns are exact integers in the asset’s minor units plus an explicit scale, or `NUMERIC` constrained to that scale. Floating-point columns are forbidden for amounts, rates used in financial allocation, and limits. API values are decimal strings.

Database roles used by the application may insert immutable evidence and may not update or delete it. Corrections go through append-only compensating rows (ADR-008). Tenant predicates are mandatory in queries, jobs and exports.

## Consequences

- Projections can be rebuilt from retained evidence.
- Slice 3 owns the migrations and the role grants.
- Tests that need a reset use a disposable database, not row deletes from the application role.

## Production gate

Backup, point-in-time recovery and retention durations are deployment and legal inputs (slices 17 and 20). This ADR does not claim a restore drill.
