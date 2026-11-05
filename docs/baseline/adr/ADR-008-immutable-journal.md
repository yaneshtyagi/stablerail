# ADR-008 Immutable financial event journal

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-008; §8 and SET-004
- **Scope:** Financial events, routing decisions, compliance decisions, observations and administrative audit.

## Context

Investigations and audits must show what the platform believed at the time. The prototype appends journal titles in application logic, then mutates transfer confirmations, received amounts, flags and settlement on the same object. Browser storage can be edited.

## Decision

Financial events, blockchain observations, routing decisions, compliance decisions and audit events are append-only. The application database role has insert and select on those tables and does not have update or delete.

A correction inserts a compensating financial event that references the original event. It does not change the original amount, the original observation payload or a historical routing decision. Projections (intent status, recognized amount, settlement posture) are rebuildable from the journal and may be stored as a concurrency-controlled cache.

Recognized value for one observation is posted once. Duplicate provider or chain notifications correlate to that posting. Withdrawing recognition posts the opposite amount; it does not delete the posting.

Administrative audit entries record the actor, tenant, action, object reference and result, including access denials. They omit secrets and foreign-tenant payloads.

## Consequences

- Slice 3 tests forbidden updates, duplicate recognition and reconstruction.
- Statement corrections and reorgs add evidence. They do not overwrite the first observation.

## Production gate

Retention length is a legal input. The engineering behavior until that input arrives is to keep evidence and to disable deletion jobs.
