# Implementation checklist

Slices run in this order. Later slices follow `docs/baseline` and must not reopen a closed decision without a new ADR. P1 and P2 items in [REQUIREMENTS.md](REQUIREMENTS.md) are not part of these slices.

| Slice | Status | Work | Acceptance |
| --- | --- | --- | --- |
| 1 | Done in this baseline | Scope, state machine, ADRs, dependency register, this checklist, traceability matrix | Every P0 row and AT-001–AT-014 has a slice. Transitions are explicit. External inputs name a production effect |
| 2 | Not started | `code/` solution, workers, React app, PostgreSQL, Redis, queue, migrations, health, OpenTelemetry, CI | Fresh checkout starts dependencies and passes checks. Production startup rejects missing critical configuration and development authentication |
| 3 | Not started | §8 entities, money types, state machine, append-only journal, database roles | Integration tests cover forbidden transitions, precision, concurrency, duplicate recognition and compensating corrections |
| 4 | Not started | Tenants, merchants, sessions, MFA, credentials, allowlists, limits, isolation, audit | AT-011 across API, workers, cache and exports. Revoked and wrong-environment keys fail. Privileged actions require MFA |
| 5 | Not started | Idempotency, outbox, inbox, retries, dead letters, attempt protocol | AT-001 and AT-002 under concurrency and restart. Fault injection does not lose committed events or double-post money. AT-013 foundations |
| 6 | Not started | OpenAPI 3.1 `/v1`, intents, options, quotes, expiry, cancellation | Contract tests for validation, idempotency, pagination, expiry and partial funding. Cancellation cannot erase evidence or clear an unknown attempt |
| 7 | Not started | Adapter contract, two overlapping adapters, fakes, health and timeouts | Contract tests for ambiguity, malformed responses and outages. Capability matrix states certification gaps. Live calls stay disabled without credentials |
| 8 | Not started | Eligibility policy, screening port, limits, holds | Outage, staleness, concurrent limits, prohibited corridors and role checks. Missing approval keeps production screening fail-closed |
| 9 | Not started | RTE-001–RTE-005, persisted rationale, safe fallback | AT-003, AT-004, AT-010 and AT-014, including crashes, stale health and kill switches |
| 10 | Not started | Chain ingestion, verification, allocation, deduplication | AT-005, AT-006 and AT-007 with duplicates, reordering, replay and restarts. Wrong contracts do not increase accepted value |
| 11 | Not started | Finality policies, reorgs, success gates | AT-008 before and after success and after settlement. No force-finalize. Production policy absent means success from finality stays off |
| 12 | Not started | Signed webhooks, egress controls, replay | AT-009 across crashes, rotation and replay. Signatures and blocked destinations are tested |
| 13 | Not started | Statement import, matching, unmatched inbox, exports | AT-012 including conflicts, foreign tenants, precision, duplicates and reorg corrections |
| 14 | Not started | Operations shell, Overview, Payments, Create, Detail | Browser tests for the payment journeys, both themes, narrow screens and keyboard behavior. No fixture metric shown as live data |
| 15 | Not started | Routing, Providers, Reconciliation, Settlements, Webhooks, Administration, Audit | Role-specific browser tests. Imports and replays reflect durable jobs |
| 16 | Not started | §14 metrics and usage-record seam | Aggregates reconcile to records and respect isolation. No billing engine and no hypothetical prices |
| 17 | Not started | Terraform and deployment pipeline for AWS | Infrastructure validates. Secrets stay out of source and images. Restore settings are targets, not claimed drill results |
| 18 | Not started | Threat model, alerts, load and failure tests | Measured results published. Unmet targets stay open. AT-013 runs on real durable infrastructure |
| 19 | Not started | Full AT-001–AT-014 on integrated components, sandbox certification, runbooks | Matrix links each P0 and AT to evidence or an explicit remaining dependency. Evidence is labeled simulated, sandbox, testnet or mainnet |
| 20 | Not started | Pilot release candidate and go/no-go packet | A reviewer can decide from evidence. Missing gates keep production activation off |

## Explicitly later

Do not fold these into slices 1–20: refunds and payouts, fiat off-ramp and executable quotes, cost-optimized routing, SDKs and hosted checkout, pricing configuration, enterprise SSO, predictive ranking, multi-leg or cross-chain orchestration, treasury optimization, and white-label packaging.

## Slice 1 verification

`python3 docs/baseline/check_traceability.py` checks that every P0 identifier and AT-001–AT-014 has the required fields and an MVP slice, that ADR-001–ADR-012 exist, and that the state machine names every acceptance status.
