# Requirement and acceptance traceability

Source: PRD v1.0, 26 October 2026, compared with `prototype/src/domain.ts`, `prototype/src/domain.test.ts`, `prototype/src/App.tsx` and `prototype/tests/journeys.spec.ts`.

OPS, API, NFR and COM identifiers are assigned by this baseline so unnumbered P0 items in PRD §§6.10, 7, 11 and 13 have owners. They are not printed IDs in the PRD. Slice 1 produced this matrix and did not run product tests. Slice 3 records database evidence on the journal rows below. Every other row remains pending.

Prototype unit tests and browser journeys are sandbox evidence only. They do not close a production acceptance test.

## Tenant and merchant

### TEN-001

- **Priority:** P0
- **Requirement:** Independent tenant organizations, each with subordinate merchants.
- **Prototype evidence:** Atlas PSP / Northstar Commerce and Harbor PSP / Harbor Markets in `seed` and the tenant switcher.
- **Production gap:** The two tenants are fixtures in browser state. There is no organization record, membership, or server-side hierarchy.
- **Slice:** 4; console in 15
- **Verification evidence:** Pending. Tenant and merchant persistence tests in slice 4.

### TEN-002

- **Priority:** P0
- **Requirement:** Database and application isolation, with role-based authorization.
- **Prototype evidence:** `access` denies a harbor read of an atlas intent. The browser journey switches tenants and expects isolation.
- **Production gap:** Isolation is a filter on an in-memory array. There are no roles, database policies, worker filters, or export limits.
- **Slice:** 4; workers in 5; exports in 13
- **Verification evidence:** Pending. AT-011 is the acceptance scenario.

### TEN-003

- **Priority:** P0
- **Requirement:** Environment-scoped API credentials that can be revoked.
- **Prototype evidence:** Administration creates a masked `sk_test_atlas_…` string and can mark it revoked.
- **Production gap:** The value is not a secret, is not hashed, is not scoped to an environment, and is not presented once.
- **Slice:** 4
- **Verification evidence:** Pending. Credential lifecycle tests in slice 4.

### TEN-004

- **Priority:** P0
- **Requirement:** Per-merchant assets, networks, providers and operating limits, with audited changes.
- **Prototype evidence:** Merchant description list shows a fixed asset, network, limit and policy version.
- **Production gap:** Values are display fixtures. Provider limits are global seed data, not versioned merchant policy.
- **Slice:** 4; enforced in 8 and 9
- **Verification evidence:** Pending. Version-history tests in slice 4 and limit tests in slice 8.

### TEN-005

- **Priority:** P1
- **Requirement:** Tenant fees and pricing-plan configuration.
- **Prototype evidence:** None. Illustrative provider fees are not tenant prices.
- **Production gap:** Out of MVP. Do not add a pricing admin screen to close COM-001.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

### TEN-006

- **Priority:** P2
- **Requirement:** SSO and identity federation.
- **Prototype evidence:** None.
- **Production gap:** Remains P2. Privileged MFA is still required through NFR-001.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

## Payment intent

### PAY-001

- **Priority:** P0
- **Requirement:** Create, retrieve, list, search and cancel eligible intents.
- **Prototype evidence:** `createIntent`, `access`, `cancelIntent`, Payments and Create screens, create journey.
- **Production gap:** No HTTP API, cursor pagination, or server authority. Cancellation is local and only from `requires_payment`.
- **Slice:** 6; UI in 14
- **Verification evidence:** Pending. API contract tests in slice 6.

### PAY-002

- **Priority:** P0
- **Requirement:** Requested amount, currency, expiry, reference and metadata.
- **Prototype evidence:** Create form collects reference, decimal amount, network, expiry minutes and idempotency key.
- **Production gap:** No metadata field. Amount is simultaneously treated as USD and as six-decimal USDC. Expiry does not run on a clock.
- **Slice:** 6
- **Verification evidence:** Pending. Validation tests, including the fixed-token versus fiat split, in slice 6.

### PAY-003

- **Priority:** P0
- **Requirement:** Return supported assets, chains, quote and deposit instructions.
- **Prototype evidence:** Detail view shows the USDC contract, chain ID, shared deposit address and expiry.
- **Production gap:** One synchronous option. The address and contracts are literals. There is no quote object and no instruction-pending state.
- **Slice:** 6; issuance through 7 and 9
- **Verification evidence:** Pending. Contract tests that historical instructions remain readable after later config changes.

### PAY-004

- **Priority:** P0
- **Requirement:** Mandatory idempotency for creation and other financial mutations.
- **Prototype evidence:** Domain tests replay a normalized amount and reject a conflicting amount for the same key.
- **Production gap:** The store is process-local, the tenant is client-supplied, and concurrent requests are not constrained.
- **Slice:** 5; applied by 6 and 13
- **Verification evidence:** Pending. AT-001 and AT-002.

### PAY-005

- **Priority:** P0
- **Requirement:** Track partial, excess, late, wrong-token and duplicate transfers.
- **Prototype evidence:** Domain tests for partial-plus-hold, late transfer, wrong contract, duplicate replay, and overpayment flags in `simulate`.
- **Production gap:** Scenarios mutate one intent. Duplicate handling is not keyed by log identity. Partial credit uses integer division.
- **Slice:** 10; journal in 3
- **Verification evidence:** Slice 3 retains an observed minor-unit amount and rejects a second recognition for one chain observation. Allocation workflows and AT-005, AT-006, and AT-007 remain slice 10.

### PAY-006

- **Priority:** P0
- **Requirement:** Merchant-configured confirmation, tolerance and expiry policies.
- **Prototype evidence:** Expiry minutes are a form field. Confirmation depth is hardcoded per network. Tolerance is absent.
- **Production gap:** No versioned merchant policy. Hardcoded depths must not ship. Tolerance is unspecified.
- **Slice:** 6; finality in 11; policy versions in 4
- **Verification evidence:** Pending. Expiry and zero-tolerance tests in slices 6 and 11.

### PAY-007

- **Priority:** P1
- **Requirement:** Payment-option reservation and hosted checkout.
- **Prototype evidence:** None.
- **Production gap:** Multiple options are in MVP. Reservation locks and hosted checkout are not.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

## Providers

### PRV-001

- **Priority:** P0
- **Requirement:** Capability-based common provider adapter.
- **Prototype evidence:** `API_CONTRACT.md` names the five adapter operations. The app uses in-memory provider rows instead.
- **Production gap:** No adapter interface, capability version, or exact token identity on a provider.
- **Slice:** 7
- **Verification evidence:** Pending. Adapter contract tests in slice 7.

### PRV-002

- **Priority:** P0
- **Requirement:** Provider health, availability and rate limits.
- **Prototype evidence:** Providers screen toggles `healthy` and `enabled`. Routing uses those booleans.
- **Production gap:** Health is not probed, has no freshness, and is not rate-limited. Emergency stop is local.
- **Slice:** 7; routing use in 9
- **Verification evidence:** Pending. Health-outage and throttle tests in slice 7.

### PRV-003

- **Priority:** P0
- **Requirement:** Secure credentials, normalized errors and correlation references.
- **Prototype evidence:** Errors are thrown strings such as `no_eligible_route`. No provider credential store.
- **Production gap:** No secret loading, error envelope, or correlation ID on attempts.
- **Slice:** 7; API envelope in 6; attempt IDs in 5
- **Verification evidence:** Pending. Normalized error and secret-redaction tests in slice 7.

### PRV-004

- **Priority:** P0
- **Requirement:** Circuit breakers, timeouts and safe failover.
- **Prototype evidence:** Timeout scenario sets `unknownOutcome` and blocks a second execution. Primary outage selects Orbit before issuance.
- **Production gap:** No breaker, timeout budget, or durable attempt. Failover is a boolean branch in the browser.
- **Slice:** 7; fallback rules in 9
- **Verification evidence:** Pending. AT-003 and AT-004.

### PRV-005

- **Priority:** P1
- **Requirement:** Adapter contract and version verification, and capability-change handling.
- **Prototype evidence:** None beyond a copied route snapshot.
- **Production gap:** MVP still versions capabilities on each decision (ADR-012). A separate certification workflow is P1.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

## Routing

### RTE-001

- **Priority:** P0
- **Requirement:** Discover eligible routes and enforce hard constraints before ranking.
- **Prototype evidence:** `routes` drops stopped, unhealthy, over-limit and screening-held providers. Browser routing journey.
- **Production gap:** Missing jurisdiction, exact asset, liquidity and health-freshness gates.
- **Slice:** 9
- **Verification evidence:** Pending. Gate-order tests in slice 9.

### RTE-002

- **Priority:** P0
- **Requirement:** Deterministic merchant and provider priority, including explicit exclusion.
- **Prototype evidence:** Lowest `priority` wins. Orbit’s lower fee does not override Bridgeway.
- **Production gap:** No merchant-level priority or stable tie-break beyond array order.
- **Slice:** 9
- **Verification evidence:** Pending. Tie-break tests in slice 9.

### RTE-003

- **Priority:** P0
- **Requirement:** Persist policy version, inputs, selected route and rationale.
- **Prototype evidence:** `routeEvidence` and `policy` are copied onto the intent. AT-014 unit test edits providers afterwards.
- **Production gap:** The copy lives in editable browser storage. Policy id is a constant.
- **Slice:** 9; storage in 3
- **Verification evidence:** Slice 3 stores each routing decision as an immutable row with its policy version and input snapshot. The historical-edit scenario AT-014 remains slice 9.

### RTE-004

- **Priority:** P0
- **Requirement:** Revalidate eligibility before irreversible execution.
- **Prototype evidence:** Routes are evaluated once inside `createIntent`. There is no later execution step.
- **Production gap:** No second gate immediately before the provider call, and no stale-health case.
- **Slice:** 9
- **Verification evidence:** Pending. Revalidation tests, including a kill switch between quote and issuance.

### RTE-005

- **Priority:** P0
- **Requirement:** Fallback only before commitment or after the original outcome is reconciled.
- **Prototype evidence:** Timeout then `reconcile_timeout` keeps Bridgeway and a single transfer. Cancel is rejected while unknown.
- **Production gap:** Reconciliation is a scenario flag, not provider evidence. No crash window around the call.
- **Slice:** 9; attempt protocol in 5
- **Verification evidence:** Pending. AT-004.

### RTE-006

- **Priority:** P1
- **Requirement:** Compare fees, gas, spread, slippage and liquidity quotes.
- **Prototype evidence:** A single illustrative fee string is displayed.
- **Production gap:** Not an MVP sort key. Estimates may be stored and shown, and must be labeled as estimates.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

### RTE-007

- **Priority:** P1
- **Requirement:** Optimize by time, cost, reliability and liquidity risk.
- **Prototype evidence:** None.
- **Production gap:** The illustrative score in PRD §6.4 is not implemented.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

### RTE-008

- **Priority:** P2
- **Requirement:** Policy simulation, performance feedback and predictive scores.
- **Prototype evidence:** None.
- **Production gap:** Remains P2.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

## Monitoring

### MON-001

- **Priority:** P0
- **Requirement:** Monitor relevant transactions on supported chains.
- **Prototype evidence:** Operator runs detect, confirm and finalize scenarios.
- **Production gap:** No chain connection, checkpoint, or worker.
- **Slice:** 10
- **Verification evidence:** Pending. Ingestion tests against a fake chain source in slice 10.

### MON-002

- **Priority:** P0
- **Requirement:** Validate chain, contract, destination, transfer event and amount before allocation.
- **Prototype evidence:** Wrong-contract scenario stores `credited: false`. Destination is not actually checked; every payment shares one address.
- **Production gap:** No destination or event-topic validation. Contract literals are not an allowlist.
- **Slice:** 10
- **Verification evidence:** Pending. AT-007.

### MON-003

- **Priority:** P0
- **Requirement:** Deduplicate chain and provider events.
- **Prototype evidence:** Duplicate scenario leaves one transfer and `250.000000` received, including after JSON reload.
- **Production gap:** Dedup is “if any transfer exists”, not a unique chain observation, and it does not correlate a provider callback.
- **Slice:** 10; inbox in 5
- **Verification evidence:** Pending. AT-005.

### MON-004

- **Priority:** P0
- **Requirement:** Apply chain-specific finality and reorganization policies.
- **Prototype evidence:** Finalize writes 12 or 64 confirmations. Reorg test withdraws finality and blocks restoration.
- **Production gap:** Depths are constants. Reorg mutates the transfer instead of appending a compensating event.
- **Slice:** 11
- **Verification evidence:** Pending. AT-008.

### MON-005

- **Priority:** P0
- **Requirement:** Track late, partial, overpaid and unallocated transfers.
- **Prototype evidence:** Flags for underpayment, overpayment, late transfer and wrong token. Expired seed intent ORD-8417 carries a late transfer.
- **Production gap:** Flags are strings on a mutable object. Unallocated amount can still be described as USDC.
- **Slice:** 10
- **Verification evidence:** Pending. Allocation tests in slice 10.

### MON-006

- **Priority:** P0 for RPC failover and bounded catch-up. Unbounded historical backfill is P1.
- **Requirement:** RPC and provider failover, plus historical reprocessing.
- **Prototype evidence:** None. A provider health toggle is not an RPC failover.
- **Production gap:** No endpoints, checkpoints, or reprocessing bounds.
- **Slice:** 10
- **Verification evidence:** Pending. Failover and checkpoint-replay tests in slice 10. Unbounded backfill stays unscheduled.

## Webhooks

### WHK-001

- **Priority:** P0
- **Requirement:** Register merchant endpoints and sign event envelopes.
- **Prototype evidence:** Webhooks screen saves an HTTPS URL per tenant. No signature is computed.
- **Production gap:** No signing, timestamp, or secret storage.
- **Slice:** 12
- **Verification evidence:** Pending. Signature tests in slice 12.

### WHK-002

- **Priority:** P0
- **Requirement:** At-least-once delivery, stable event IDs and timestamp checks.
- **Prototype evidence:** Replay test keeps the hook ID and increments attempts.
- **Production gap:** Delivery is not attempted. Consumers are not given a verification example.
- **Slice:** 12
- **Verification evidence:** Pending. AT-009.

### WHK-003

- **Priority:** P0
- **Requirement:** Retry with backoff, dead-letter handling and manual replay.
- **Prototype evidence:** Failed attempt text says “next retry in 30 seconds” while the sandbox clock is paused. Replay is a confirmed UI action.
- **Production gap:** No scheduler, jitter, dead-letter, or authorized replay worker.
- **Slice:** 12
- **Verification evidence:** Pending. Retry and replay tests in slice 12.

### WHK-004

- **Priority:** P0 for delivery history and secret rotation. Event filtering is P1.
- **Requirement:** Delivery history, secret rotation and filtering.
- **Prototype evidence:** Attempt history is a list of journal rows. No secret rotation or event filter.
- **Production gap:** History is not durable. Rotation overlap is unspecified.
- **Slice:** 12
- **Verification evidence:** Pending. Rotation-overlap tests in slice 12. Filtering stays unscheduled.

## Settlement

### SET-001

- **Priority:** P0
- **Requirement:** Keep payment acceptance, funds receipt and settlement distinct.
- **Prototype evidence:** Domain test expects `detected`, hold, underpayment and `settlement: pending` together. Detail screen shows separate badges.
- **Production gap:** The distinction is local state. A simulated “record statement” action can still mark settlement.
- **Slice:** 13; model in 3; UI in 15
- **Verification evidence:** Slice 3 stores settlement posture separately from acceptance and keeps a mismatch there while acceptance stays succeeded. Statement matching remains slice 13.

### SET-002

- **Priority:** P0
- **Requirement:** Import and reconcile external provider settlement statements.
- **Prototype evidence:** `importStatement` and the finance browser journey. Header `intent_id,amount_usdc,provider_reference`.
- **Production gap:** One sandbox CSV. Unknown intents reject the file. No provider-specific adapter or staged import.
- **Slice:** 13
- **Verification evidence:** Pending. Import tests in slice 13.

### SET-003

- **Priority:** P0
- **Requirement:** Identify missing, duplicate, excess and mismatched entries.
- **Prototype evidence:** Mismatch test keeps the original mismatch event after a second reference settles. Conflicting reuse throws `statement_conflict`.
- **Production gap:** Missing and unknown rows are not retained. Excess is only an overpayment flag on the intent.
- **Slice:** 13
- **Verification evidence:** Pending. AT-012.

### SET-004

- **Priority:** P0
- **Requirement:** Audit financial events and corrections without overwriting history.
- **Prototype evidence:** Correction test asserts the mismatch title is still present. Reorg keeps prior event objects by appending.
- **Production gap:** Settlement and confirmation fields on the same intent are still overwritten. Database permissions do not exist.
- **Slice:** 3; statement corrections in 13
- **Verification evidence:** Slice 3 database tests append a compensating financial event, leave the original amount in place, and rebuild the projection from retained rows. Statement-import corrections remain slice 13.

### SET-005

- **Priority:** P0
- **Requirement:** Export merchant and period settlement CSV reports.
- **Prototype evidence:** `exportCsv` quotes fields and prefixes formula triggers. Exports follow the selected tenant.
- **Production gap:** Export is client-side over browser state, with no period query or server authorization.
- **Slice:** 13
- **Verification evidence:** Pending. Export isolation and formula tests in slice 13.

### SET-006

- **Priority:** P1
- **Requirement:** FX details, settlement batching and accounting exports.
- **Prototype evidence:** None.
- **Production gap:** Not in MVP. Do not invent FX rows to fill this gap.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

## Compliance

### CMP-001

- **Priority:** P0
- **Requirement:** Jurisdiction, tenant, asset and provider eligibility policies.
- **Prototype evidence:** Screening hold excludes every route in `routes(seed(), "250", true)`.
- **Production gap:** No jurisdiction or asset policy. The hold is a boolean argument.
- **Slice:** 8; route use in 9
- **Verification evidence:** Pending. AT-010.

### CMP-002

- **Priority:** P0
- **Requirement:** Applicable merchant KYB and wallet or sanctions screening.
- **Prototype evidence:** Administration shows a fixed Approved KYB badge. Hold text cites unavailable screening.
- **Production gap:** No screening adapter, freshness, or merchant KYB record.
- **Slice:** 8
- **Verification evidence:** Pending. Fail-closed outage tests in slice 8.

### CMP-003

- **Priority:** P0
- **Requirement:** Amount and velocity rules, holds and case references.
- **Prototype evidence:** Provider `limit` is checked per quote. Hold uses the fixed case id CASE-1042. No velocity window.
- **Production gap:** Limits are not atomic under concurrency. The case id is a fixture. There is no resolution role.
- **Slice:** 8
- **Verification evidence:** Pending. Concurrent limit and case-role tests in slice 8.

### CMP-004

- **Priority:** P0
- **Requirement:** Audit decisions with source and policy version.
- **Prototype evidence:** Journal line “CASE-1042 · Mandatory screening unavailable”.
- **Production gap:** No source, timestamp, freshness, or policy version on a compliance record.
- **Slice:** 8; audit storage in 3
- **Verification evidence:** Pending. Decision-record tests in slice 8.

### CMP-005

- **Priority:** P1
- **Requirement:** Transaction risk scoring and Travel Rule workflows where applicable.
- **Prototype evidence:** None.
- **Production gap:** Remains P1. Mandatory screening still fails closed without it.
- **Slice:** deferred
- **Verification evidence:** Not scheduled in slices 1–20.

## Operations console

These rows track PRD §6.10. The prototype already renders the corresponding screens as a sandbox.

### OPS-001

- **Priority:** P0
- **Requirement:** Dashboard with volumes, status mix, outstanding exceptions and provider health.
- **Prototype evidence:** Overview KPIs, chart and alerts from seeded local intents. Screenshot `docs/screenshots/overview.png`.
- **Production gap:** Figures are fixture state, not server metrics.
- **Slice:** 14; metric definitions in 16
- **Verification evidence:** Pending. Dashboard reconciliation tests in slice 16.

### OPS-002

- **Priority:** P0
- **Requirement:** Search and filter intents and inspect the transaction timeline.
- **Prototype evidence:** Payments list, state filter, sort, and detail timeline.
- **Production gap:** No server pagination or authoritative timeline.
- **Slice:** 14
- **Verification evidence:** Pending. Browser journey against the API in slice 14.

### OPS-003

- **Priority:** P0
- **Requirement:** Provider status, configuration and routing audit evidence.
- **Prototype evidence:** Routing and Providers screens, including emergency-stop confirmation.
- **Production gap:** Controls edit local fixtures and are not role-gated or audited on a server.
- **Slice:** 15
- **Verification evidence:** Pending. Role and kill-switch browser tests in slice 15.

### OPS-004

- **Priority:** P0
- **Requirement:** Reconciliation exceptions and settlement reports.
- **Prototype evidence:** Reconciliation and Settlements screens and the statement-import journey.
- **Production gap:** No unmatched inbox and no server report.
- **Slice:** 15; data in 13
- **Verification evidence:** Pending. Import feedback must reflect the durable job.

### OPS-005

- **Priority:** P0
- **Requirement:** Webhook endpoints, attempts, retries and replay.
- **Prototype evidence:** Webhook journey replays a stable ID and shows history.
- **Production gap:** Replay does not call an endpoint.
- **Slice:** 15; delivery in 12
- **Verification evidence:** Pending. Replay UI test against a durable attempt.

### OPS-006

- **Priority:** P0
- **Requirement:** Merchants, users, credentials, policy changes and audit.
- **Prototype evidence:** Administration and Audit screens. Credential revoke asks for confirmation.
- **Production gap:** No users, MFA, or real policy history. Audit rows are browser state.
- **Slice:** 15; server enforcement in 4
- **Verification evidence:** Pending. Privileged-action tests in slices 4 and 15.

## API, non-functional and commercial

### API-001

- **Priority:** P0
- **Requirement:** HTTPS JSON API, OpenAPI 3.1, `/v1`, cursor pagination, UTC timestamps, decimal-string money, correlation IDs and the PRD error envelope.
- **Prototype evidence:** `docs/API_CONTRACT.md` maps future routes to local functions and shows the error fields. No server exists.
- **Production gap:** The whole contract is unimplemented. The sample create body uses USD without a quote.
- **Slice:** 6
- **Verification evidence:** Pending. Published OpenAPI and contract tests in slice 6.

### NFR-001

- **Priority:** P0
- **Requirement:** Scoped credentials, least privilege and privileged MFA.
- **Prototype evidence:** None for MFA or roles. Credentials are illustrative.
- **Production gap:** Authentication is absent. SSO is not the MVP substitute.
- **Slice:** 4
- **Verification evidence:** Pending. MFA and role tests in slice 4.

### NFR-002

- **Priority:** P0
- **Requirement:** TLS, encryption at rest, secrets management and no secrets in source.
- **Prototype evidence:** Prototype states that no signing secret or provider credential is present.
- **Production gap:** No deployment, KMS, or TLS termination.
- **Slice:** 17; redaction in 18
- **Verification evidence:** Pending. Secret scans and infrastructure review in slices 17 and 18.

### NFR-003

- **Priority:** P0
- **Requirement:** Webhook signature verification, replay prevention and secret rotation.
- **Prototype evidence:** Copy on the webhooks screen describes a contract. No cryptography is implemented.
- **Production gap:** Same gap as WHK-001 and WHK-004.
- **Slice:** 12
- **Verification evidence:** Pending. Timestamp-window tests in slice 12.

### NFR-004

- **Priority:** P0
- **Requirement:** Immutable operational and financial audit.
- **Prototype evidence:** Audit timeline appends titles. Prototype docs say browser storage is user-editable.
- **Production gap:** No append-only database permissions.
- **Slice:** 3; denied-access audit in 4
- **Verification evidence:** Slice 3 grants and triggers reject routine updates and deletes of financial events, observations, routing decisions, compliance decisions, audit events, and webhook history. Denied-access audit remains slice 4.

### NFR-005

- **Priority:** P0
- **Requirement:** Approval separation and emergency provider kill switches.
- **Prototype evidence:** Emergency stop uses a confirmation dialog and affects later `routes` results. Historical `routeEvidence` is left in place.
- **Production gap:** No second-person approval and no server-wide stop.
- **Slice:** 9; UI in 15
- **Verification evidence:** Pending. Concurrent kill-switch tests in slice 9.

### NFR-006

- **Priority:** P0
- **Requirement:** 99.9% API availability as a planning target.
- **Prototype evidence:** None.
- **Production gap:** No deployed API and no measurement.
- **Slice:** 18
- **Verification evidence:** Pending. A short test must not be reported as this target having been met.

### NFR-007

- **Priority:** P0
- **Requirement:** Intent creation p95 under 500 ms, excluding external quotes.
- **Prototype evidence:** None.
- **Production gap:** No measurement.
- **Slice:** 18
- **Verification evidence:** Pending. Load-test report in slice 18.

### NFR-008

- **Priority:** P0
- **Requirement:** Planning RPO of 15 minutes and RTO of 4 hours.
- **Prototype evidence:** JSON reload in the duplicate test is not a restore drill.
- **Production gap:** No backups or drills.
- **Slice:** 17; drill in 18
- **Verification evidence:** Pending. Drill evidence only if infrastructure access exists. Otherwise the gate stays open.

### NFR-009

- **Priority:** P0
- **Requirement:** Scale planning point of 100 sustained API requests per second, 500 burst, and 100,000 intents per day.
- **Prototype evidence:** None.
- **Production gap:** No harness.
- **Slice:** 18
- **Verification evidence:** Pending. Documented mix and results in slice 18.

### NFR-010

- **Priority:** P0
- **Requirement:** Correlate intent, route, provider, chain and reconciliation events.
- **Prototype evidence:** Detail timeline and cross-links in the sandbox UI.
- **Production gap:** Alert delivery is still slice 18. Slice 2 records the trace context only.
- **Slice:** 2; alerts in 18
- **Verification evidence:** `Correlation_spans_share_the_incoming_trace_and_request` correlates request, intent, route, provider, chain and reconciliation on one trace. Alert delivery remains slice 18.

### NFR-011

- **Priority:** P0
- **Requirement:** Point-in-time recovery, worker replay and verified restore drills.
- **Prototype evidence:** None beyond the local duplicate reload.
- **Production gap:** No PITR configuration and no worker.
- **Slice:** 17; replay tests in 5 and 18
- **Verification evidence:** Pending. Restore remains an open gate until a drill is recorded.

### COM-001

- **Priority:** P0 commercial model. Pricing configuration itself is TEN-005 and stays P1.
- **Requirement:** Platform subscription is a P0 revenue source. The product must be able to support a subscription commercially without embedding hypothetical prices.
- **Prototype evidence:** None.
- **Production gap:** No customer contract and no price. Building an invoicing engine would contradict TEN-005.
- **Slice:** 16; commercial packet in 20
- **Verification evidence:** Pending. Slice 16 documents the seam and stores no price. Slice 20 records the external commercial status.

### COM-002

- **Priority:** P0 commercial model. Rating and invoicing stay outside the MVP.
- **Requirement:** Usage fee per completed billable transaction, with auditable usage records.
- **Prototype evidence:** None.
- **Production gap:** “Completed” is undefined commercially. Success is not settlement.
- **Slice:** 16
- **Verification evidence:** Pending. Usage-record tests for a first success and a compensating withdrawal. Records must not be displayed as invoices.

## Acceptance tests

### AT-001

- **Priority:** P0
- **Requirement:** The same idempotency key and payload return the same logical intent.
- **Prototype evidence:** Domain test “returns the same logical intent for the same key and normalized payload”.
- **Production gap:** Browser-local map. No concurrent requests, durable store, or authenticated scope.
- **Slice:** 5; API in 6
- **Verification evidence:** Pending. Concurrent replay test on PostgreSQL.

### AT-002

- **Priority:** P0
- **Requirement:** A conflicting payload with a reused key returns an explicit conflict.
- **Prototype evidence:** Domain test expects `idempotency_conflict`.
- **Production gap:** Conflict is a thrown string, not the PRD error envelope, and it is not durable.
- **Slice:** 5; envelope in 6
- **Verification evidence:** Pending. Conflict test that asserts no second intent.

### AT-003

- **Priority:** P0
- **Requirement:** A pre-execution outage selects an eligible alternative.
- **Prototype evidence:** Domain test sets Bridgeway unhealthy and expects Orbit. Browser routing journey.
- **Production gap:** Simulated health, before any real instruction call.
- **Slice:** 9
- **Verification evidence:** Pending. Outage test with two uncertified adapters.

### AT-004

- **Priority:** P0
- **Requirement:** An ambiguous execution timeout cannot trigger a duplicate transfer.
- **Prototype evidence:** Domain test blocks cancel and finalize, then reconciles to Bridgeway with one transfer.
- **Production gap:** No external call and no crash around acknowledgement.
- **Slice:** 9; crash window in 5
- **Verification evidence:** Pending. Timeout and crash tests that leave a single attempt.

### AT-005

- **Priority:** P0
- **Requirement:** A duplicate on-chain event never double-credits an intent.
- **Prototype evidence:** Domain test reloads JSON and runs the duplicate scenario. Received stays `250.000000` with one transfer.
- **Production gap:** Not an ingestion worker and not a unique log key.
- **Slice:** 10
- **Verification evidence:** Pending. Overlapping-source and restart tests.

### AT-006

- **Priority:** P0
- **Requirement:** An expired intent with a late transfer remains traceable.
- **Prototype evidence:** Domain test keeps `expired`, one transfer, and the late-transfer flag, and refuses finalize.
- **Production gap:** Clock is manual. Traceability is local state.
- **Slice:** 10; expiry scheduling in 6
- **Verification evidence:** Pending. Scheduled expiry plus a later observation.

### AT-007

- **Priority:** P0
- **Requirement:** A wrong contract or token transfer is not automatically credited.
- **Prototype evidence:** Domain test expects received `0.000000`, `credited: false`, and status `requires_payment`.
- **Production gap:** The wrong contract is a hardcoded zero address, and the journal still says USDC.
- **Slice:** 10
- **Verification evidence:** Pending. Wrong-contract ingestion test with recognized amount unchanged.

### AT-008

- **Priority:** P0
- **Requirement:** A chain reorganization triggers controlled reassessment.
- **Prototype evidence:** Domain test moves a succeeded intent back to `confirming`, keeps prior events, blocks finalize, and the reorg scenario flips `settled` to `mismatch`.
- **Production gap:** In-place mutation. No independent chain evidence and no post-settlement compensating journal.
- **Slice:** 11
- **Verification evidence:** Pending. Reorg cases before success, after success and after settlement.

### AT-009

- **Priority:** P0
- **Requirement:** A failed merchant webhook retries durably and can be replayed.
- **Prototype evidence:** Domain replay test and the webhooks browser journey. Delivery failure is fabricated.
- **Production gap:** No HTTP delivery, queue, or crash recovery.
- **Slice:** 12
- **Verification evidence:** Pending. Crash, duplicate and replay tests.

### AT-010

- **Priority:** P0
- **Requirement:** A prohibited route is rejected even when it is cheapest.
- **Prototype evidence:** Screening test expects every route ineligible. Orbit has the lower fee.
- **Production gap:** Fixture boolean, not a policy decision.
- **Slice:** 8; ordering in 9
- **Verification evidence:** Pending. Prohibited-corridor test with a cheaper candidate still excluded.

### AT-011

- **Priority:** P0
- **Requirement:** Cross-tenant attempts are denied and audited.
- **Prototype evidence:** Domain test denies harbor access and harbor `simulate`. Browser journey covers the switch. Statement import from the wrong tenant is denied.
- **Production gap:** Not applied to workers, cache, aggregates, or exports. Audit of the denial is incomplete for reads.
- **Slice:** 4
- **Verification evidence:** Pending. API, worker, cache and export cases.

### AT-012

- **Priority:** P0
- **Requirement:** A mismatched settlement yields a reconciliation exception.
- **Prototype evidence:** Domain mismatch test and the statement browser journey. Original mismatch event remains after correction.
- **Production gap:** Sandbox CSV only. No unmatched inbox.
- **Slice:** 13
- **Verification evidence:** Pending. Mismatch, duplicate, foreign-tenant and correction tests.

### AT-013

- **Priority:** P0
- **Requirement:** Worker crash recovery does not duplicate financial execution.
- **Prototype evidence:** JSON serialize and reload preserves the duplicate-event result. The prototype docs mark real crash testing as out of scope.
- **Production gap:** No worker, queue, or attempt protocol.
- **Slice:** 5; full infrastructure case in 18
- **Verification evidence:** Pending. Fault injection around commit, publish and provider acknowledgement.

### AT-014

- **Priority:** P0
- **Requirement:** A historical routing decision retains its policy and inputs.
- **Prototype evidence:** Domain test compares `routeEvidence` and `pol_2026_10_v3` after `enabled` is set false.
- **Production gap:** Snapshot is mutable browser state. The policy id is not a version row.
- **Slice:** 9
- **Verification evidence:** Pending. Decision-history test after a configuration change.

## Reading the matrix

Primary slice numbers are the implementation owners. Supporting slice numbers are listed after the semicolon when a requirement is enforced or demonstrated again. Deferred rows are P1 or P2 and have no MVP owner. External blockers that can stop a slice from enabling production behavior are in [DEPENDENCIES.md](DEPENDENCIES.md).
