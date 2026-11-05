# Architecture decision records

These records adopt the PRD §9 decisions ADR-001–ADR-012 for the MVP. Each one is an engineering baseline. None of them is a legal, provider, pricing or operational approval.

| ADR | Decision |
| --- | --- |
| [ADR-001](ADR-001-non-custodial.md) | Non-custodial MVP |
| [ADR-002](ADR-002-modular-monolith.md) | Modular monolith and verified runtime targets |
| [ADR-003](ADR-003-provider-adapters.md) | Capability-based provider adapters |
| [ADR-004](ADR-004-postgresql-authority.md) | PostgreSQL is the system of record |
| [ADR-005](ADR-005-outbox-queue.md) | Transactional outbox and one durable queue |
| [ADR-006](ADR-006-at-least-once.md) | At-least-once processing |
| [ADR-007](ADR-007-chain-finality.md) | Chain-specific finality |
| [ADR-008](ADR-008-immutable-journal.md) | Immutable financial journal |
| [ADR-009](ADR-009-deterministic-routing.md) | Deterministic routing before optimization |
| [ADR-010](ADR-010-separate-states.md) | Separate payment, chain, compliance and settlement states |
| [ADR-011](ADR-011-ambiguous-execution.md) | No blind replay of ambiguous irreversible operations |
| [ADR-012](ADR-012-versioned-policy.md) | Version and audit policy configuration |
