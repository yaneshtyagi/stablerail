# StableRail product implementation prompts

Use the shared instruction below in every coding-agent session, then run the numbered prompts in order. Each prompt is an implementation slice intended to end in working code, meaningful tests and reviewable documentation. Keep subsequent prompts informed by the decisions and contracts created in earlier slices.

These prompts target the PRD's production MVP/P0, not just its narrower §22 proof of concept. They preserve the prototype's operations experience while replacing its browser-local simulation with server authority. P1/P2 work is listed separately at the end.

## Shared instruction — prepend to every prompt

```text
Implement the requested StableRail slice in this repository.

Read applicable AGENTS.md instructions and these sources before changing code:
- docs/Stablecoin_Payment_Orchestration_Gateway_PRD.pdf (v1.0, 26 October 2026)
- docs/PROTOTYPE.md
- docs/API_CONTRACT.md
- prototype/src/domain.ts and prototype/src/domain.test.ts
- relevant prototype/src/App.tsx screens and prototype/tests/journeys.spec.ts
- the implementation decisions and contracts already present under code/ and docs/.

Treat the PRD as the product scope and the prototype as behavioral/UI evidence. Document conflicts rather than silently inheriting prototype shortcuts. Implement the product under code/; retain prototype/ as an executable reference. Reuse the established React/TypeScript design system and visual language.

Follow the PRD topology: ASP.NET Core on a supported .NET LTS, PostgreSQL as authority, Redis for cache/throttling, transactional outbox plus a durable queue, separately runnable .NET workers, React/TypeScript/Vite UI, OpenTelemetry, and AWS/Terraform deployment. Inspect installed tooling and verify supported versions against official documentation before selecting versions. Avoid premature microservices.

Preserve these invariants:
- No customer custody or private-key handling.
- Server-derived tenant/merchant authority; isolation in APIs, database, workers and exports.
- Decimal-string API money and exact integer/decimal arithmetic with explicit asset precision; no floating-point financial calculations.
- Separate payment acceptance, chain observation/finality, compliance, exceptions, provider outcome and settlement states.
- Durable idempotency and event deduplication; no duplicate financial effects after retries or crashes.
- Immutable financial/administrative evidence; corrections append compensating events.
- Mandatory routing/compliance gates always precede ranking.
- An ambiguous provider timeout never permits blind fallback or duplicate execution.
- Previously issued deposit instructions and historical routing evidence remain inspectable.

Mocks and deterministic scenario controls belong only in explicit development/test environments. Never present them as live integrations. Do not copy illustrative deposit addresses, contract lists, fixed USD parity, chain confirmation counts or credentials into production configuration as verified facts.

Complete the slice, run appropriate checks, and update the PRD requirement/acceptance traceability matrix. Report changed behavior, verification results and remaining dependencies. If provider credentials, contracts or approved policy values are missing, finish the integration seam and tests, document the exact missing inputs, and keep that capability disabled. Do not invent approvals or claim a production gate passed.
```

## 1. Establish scope, decisions and acceptance traceability

```text
Create the implementation baseline before writing product features. Compare PRD §§5–11, 16, 20–23 with the actual prototype domain code, screens and tests. Produce:
1. A requirement matrix covering every P0 requirement and AT-001–AT-014, with prototype evidence, production gap, planned implementation slice and verification evidence fields.
2. A state-machine specification including created, requires_payment, detected, confirming, succeeded, expired and cancelled, with independent exception, compliance, provider-outcome and settlement dimensions.
3. ADRs reflecting the PRD's ADR-001–ADR-012 decisions.
4. A dependency register for launch jurisdiction, provider relationships, two overlapping providers, approved token/network combinations, payment denomination/quote semantics, custody responsibility, finality, tolerance, expiry, retention and operational approvals.
5. An ordered implementation checklist matching these prompts.

Identify the gap between the prototype's USDC-only POC and the MVP's two assets/two networks. Preserve SSO as P2 while providing privileged MFA in MVP. Record the PRD's P0 commercial revenue model versus P1 pricing configuration ambiguity; do not silently add a billing engine or omit commercial readiness. Use clearly labeled development defaults for unresolved engineering settings, and disable production behavior requiring external approval.

Acceptance: each P0 and critical acceptance test has an implementation owner/slice; allowed and forbidden transitions are explicit; unresolved external decisions have concrete inputs and production gate effects.
```

## 2. Scaffold the product and development environment

```text
Create the product under code/ as a modular .NET solution with API, domain/application modules, infrastructure, workers and tests, plus a React/TypeScript/Vite operations app. Define module ownership for identity, payments, providers/routing, monitoring, compliance, finance and webhooks. Choose and record one durable queue implementation.

Provide containerized local PostgreSQL, Redis and queue dependencies, example configuration without secrets, migrations, health/readiness endpoints, structured logging, request IDs, OpenTelemetry plumbing and reproducible start/build/test commands. Add CI for backend/frontend checks and migration validation. Keep the prototype runnable independently.

Acceptance: a fresh checkout can start dependencies, apply migrations, run API/worker/UI and pass CI checks using documented commands. Production startup rejects missing critical configuration and development authentication/simulation modes.
```

## 3. Implement the durable domain and financial journal

```text
Implement the PRD §8 entities and database migrations: tenant, merchant, credential, payment intent/options, payment attempt, routing decision, provider/account, blockchain observation, financial event, settlement/reconciliation, compliance decision, webhook event/delivery and audit event. Do not build P1 refund/payout workflows yet.

Implement the agreed state machine, exact asset-aware monetary types, UTC timestamps, concurrency control, unique chain-observation identity, tenant ownership and append-only financial corrections. Persist original precision, quoted fiat equivalent, confirmed token amount, policy versions and historical evidence separately. Design recognition so overlapping chain/provider notifications correlate to one financial effect. Define database roles/permissions that prevent routine application updates/deletes of immutable evidence.

Acceptance: database integration tests demonstrate forbidden transitions, precision boundaries, competing updates, duplicate observation/recognition prevention and compensating corrections. Projections can be reconstructed from retained evidence without changing financial history.
```

## 4. Implement identity, tenant isolation and merchant administration

```text
Implement independent PSP tenants with subordinate merchants, authenticated operator sessions, least-privilege roles and privileged MFA. Add environment-scoped revocable API credentials: generate securely, show secrets once, store only a secure verifier, enforce scopes and support rotation/revocation. Use a documented identity-provider integration; federation/enterprise SSO remains optional P2.

Implement merchant asset/network/provider allowlists and operating limits with versioned audited changes. Derive authority from authenticated context, never a client-supplied tenant ID. Enforce isolation in repository/database access, background jobs, cache keys, object references, aggregate queries and exports. Add durable audit evidence for denied access and privileged changes without leaking secrets or foreign-tenant data.

Acceptance: AT-011 covers API reads/writes, nested merchant access, worker processing, caches and exports. Revoked and wrong-environment keys fail. Test role restrictions and MFA requirements for privileged actions.
```

## 5. Implement transactional idempotency, outbox and worker recovery

```text
Implement a reusable transactional idempotency mechanism for financial mutations, scoped by environment, authenticated principal/tenant and operation as appropriate. Define canonical payload comparison, durable response replay, conflict handling, in-flight recovery and retention. Protect concurrent requests with database constraints, not process-local locks alone.

Implement transactional outbox publishing, durable consumers, inbox/deduplication records, bounded retries, dead-letter handling and worker leases/recovery. Ensure committing business state and recording an event is atomic. Specify the protocol for the unavoidable boundary between a database commit and an external provider operation; use durable attempts, provider idempotency/correlation and outcome reconciliation.

Acceptance: AT-001/002 pass with concurrent requests and restarts. Fault injection before/after commit, publish, external acknowledgement and consumer acknowledgement causes neither lost committed events nor duplicate financial effects. Add foundations for AT-013.
```

## 6. Implement the API contract and payment-intent core

```text
Publish and implement an OpenAPI 3.1 /v1 contract for create/list/search/retrieve/cancel intents, payment options, route quotes and transaction details. Include PRD metadata, amount/currency, references, expiry, accepted assets/networks, cursor pagination, request correlation and the complete normalized error envelope. Add finance/webhook/admin contract sections for subsequent slices.

Support one business obligation with multiple payment options and matching transfers. Implement created and instruction-pending behavior rather than assuming instructions are synchronous. Connect issuance through the provider/routing interfaces as those slices land. Support idempotent eligible cancellation, durable scheduled expiry and configurable merchant tolerance/expiry policies. Preserve monitoring after expiry/cancellation. Distinguish fixed-token obligations from time-limited fiat-conversion quotes; never imply executable USD parity or silently aggregate incompatible assets.

Acceptance: API contract and database tests cover validation, idempotency, metadata, filtering/pagination, authorized retrieval and cancellation, scheduled expiry, partial funding and concurrent expiry/funding. Cancellation cannot erase evidence or declare an ambiguous provider attempt safe.
```

## 7. Implement provider adapters and two overlapping integrations

```text
Implement the provider adapter contract: getCapabilities, checkHealth, createPaymentInstructions, getTransactionStatus and estimateFees. Model exact chain/token identity, limits, jurisdictions, settlement mechanisms, liquidity signals, account/environment and correlation references. Implement secure credential loading, timeouts, normalized errors, rate limits, circuit breakers and health freshness.

Add two configurable provider adapters with overlapping approved inbound capabilities, plus deterministic adapter fakes for tests. Use official provider documentation and sandbox credentials where supplied. If provider selection/credentials are unresolved, complete adapters against explicit contracts/test fixtures, label them uncertified and keep live use disabled; do not treat prototype provider names as real contracted vendors.

Acceptance: adapter contract tests cover valid/invalid instructions, timeout ambiguity, malformed responses, throttling, capability changes and health outages. Produce a capability/coverage matrix with sandbox evidence and explicit certification gaps. No custody/private-key material enters the platform.
```

## 8. Implement compliance decisions and financial limits

```text
Implement versioned eligibility policies for jurisdiction, merchant KYB/KYC status, asset, network, provider, wallet/sanctions screening, transaction amount and velocity. Connect a replaceable screening adapter and retain source, timestamp, policy version, freshness, outcome and case reference. Enforce limits atomically under concurrency.

Fail closed on mandatory missing, expired or unavailable checks. Represent holds independently of payment status, block further unsafe execution/acceptance, and preserve observed transfers. Implement case investigation and any authorized resolution under the documented role/approval policy; do not provide an unrestricted release-hold action. Make approved launch markets/provider responsibilities configuration prerequisites.

Acceptance: tests cover outages, stale decisions, simultaneous limit consumption, prohibited corridors, role restrictions and hold behavior before/after detection. No cheaper route bypasses a mandatory control. Missing legal/provider approval blocks production enablement without preventing development tests.
```

## 9. Implement deterministic routing and safe provider fallback

```text
Implement RTE-001–005. Eliminate routes failing asset/chain, jurisdiction/compliance, limits, liquidity, availability, health or emergency-stop requirements, then apply deterministic merchant/provider priority with an explicit tie-break rule. Persist inputs, all candidate exclusions, selected route and policy/capability versions. Route quotes must distinguish estimates from executable terms.

Revalidate required gates before the relevant external action. Once deposit instructions are issued, do not silently redirect an existing transfer by changing providers. Track durable attempts and known-not-committed, committed and unknown outcomes. Permit fallback only before a commitment or after evidence proves safe; reconcile unknown outcomes with the original provider and correlation/idempotency reference.

Acceptance: AT-003, AT-004, AT-010 and AT-014 pass, including crashes around provider acknowledgement, stale health and concurrent kill switches. Historical decisions stay unchanged after configuration edits. No optimization score or timeout launches duplicate execution.
```

## 10. Implement chain ingestion and verified transfer allocation

```text
Implement EVM monitoring adapters for the approved two chains with configurable RPC endpoints, durable checkpoints, reconnect/catch-up, bounded historical reprocessing and RPC failover. Validate chain ID, exact allowlisted token contract, transfer event, destination and amount before allocation. Preserve block number/hash, transaction hash, log index, token precision and raw provenance.

Atomically deduplicate chain observations and correlate provider notifications. Allocate valid transfers to payment options without double-counting across assets/options. Record partial, excess, late, wrong-token, wrong-chain and unallocated observations with investigation evidence. Never credit by symbol alone or promise recovery of unsupported transfers.

Acceptance: AT-005/006/007 pass with duplicate/out-of-order callbacks, overlapping ingestion sources, checkpoint replay and restarts. Late transfers remain attached/traceable after expiry/cancellation. Wrong contracts never increase accepted payment value. Unmatched observations are retained for reconciliation.
```

## 11. Implement finality, reorganization and acceptance policies

```text
Implement chain-specific, versioned finality policies and merchant confirmation/tolerance rules with approved safety floors. Calculate finality from canonical chain evidence, not UI actions or unverified provider claims. Gate success on sufficient valid funding, finality, compliance and resolved execution state. Snapshot the applied policy.

Detect orphaned/replaced observations and reorganizations before and after apparent finality. Preserve prior evidence, invalidate affected provisional recognition using compensating events, and place affected payments/settlements into controlled reassessment. Define independently verified recovery and escalation; do not expose a one-click force-finalize control. Continue monitoring late transfers under the configured acceptance policy.

Acceptance: AT-008 covers reorgs before/after success and after settlement, RPC disagreement, finality-policy changes and worker restarts. Settlement discrepancies become traceable exceptions. Approval/chain evidence requirements prevent premature restoration of success.
```

## 12. Implement signed, durable merchant webhooks

```text
Implement endpoint registration, event envelopes and delivery for the MVP payment/settlement events. Persist a stable event ID and append-only delivery history. Define and document cryptographic signing, signed timestamps, consumer verification/replay protection, secret storage and rotation overlap. Deliver from the transactional outbox through durable workers with backoff, jitter, bounded timeouts, dead-letter handling and authorized manual replay.

Implement endpoint validation and delivery egress protections against private/link-local/metadata destinations, unsafe redirects and DNS rebinding. Do not log signing secrets. Replay preserves the logical event ID while creating a new delivery attempt; it cannot mutate financial value. Document at-least-once consumption with an executable idempotent receiver example.

Acceptance: AT-009 passes across process crashes, endpoint failures, duplicates, key rotation and manual replay. Tests verify signatures/timestamp windows and egress restrictions. Retry/replay operations preserve tenant scope and stable payload semantics.
```

## 13. Implement settlement ingestion and reconciliation

```text
Implement SET-001–005 with provider-specific statement adapters, durable imports, validation/staging and explicit partial-failure/atomicity rules. Replace the prototype's three-column-only assumption with documented provider formats while retaining a controlled development CSV format. Store source evidence, normalized rows, provider references and import provenance.

Match external settlement evidence against payments, transfers and financial events without equating payment success with settlement. Identify missing, duplicate, excess, mismatched and unknown/unallocated entries. Add an unmatched-statement inbox rather than rejecting all unknown rows without retaining evidence. Make repeated identical imports idempotent, reject conflicting reference reuse and append corrections with new evidence.

Implement tenant/merchant/time-period reports and safe CSV exports with formula-injection protection. Separate settlement approval from fabricated internal confirmation.

Acceptance: AT-012 covers mismatch and corrective evidence; tests include repeated/concurrent imports, foreign-tenant references, precision differences, unknown intents, duplicates and reorg corrections. Original statements and observations remain inspectable after resolution.
```

## 14. Implement the operations console shell and payment journeys

```text
Build the authenticated operations UI under code/ using the prototype's design system, layouts, logos, light/dark themes and responsive shell. Split the large prototype App.tsx into feature modules. Add a typed API client, authenticated session handling, real loading/error/empty states and server-backed cursor pagination/filtering. Remove localStorage as the source of financial authority.

Implement Overview, Payments, Create Payment and Payment Detail against the real API. Show exact asset/contract, chain ID, instructions/quote expiry, received amounts, independent state dimensions, route evidence, transfers and full event timeline. Support idempotent retries, eligible cancellation confirmation and reliable browser navigation. Render multiple options accurately. Keep simulator controls restricted to a clearly labeled development environment.

Acceptance: browser tests cover create-to-success, concurrent/retried create, errors with preserved inputs, expiry/cancellation plus late transfer, denied access, both themes, narrow screens and keyboard/focus behavior. No mocked success or fixture-only metric appears as production data.
```

## 15. Implement operational, finance and administration screens

```text
Implement server-backed Routing, Providers, Reconciliation, Settlements, Webhooks, Administration and Audit screens using prototype journeys as the baseline. Add merchant/user management, policy version history, role-aware actions, credential create/rotate/revoke, provider health/emergency controls, case references, statement import/unmatched inbox, exports and delivery attempts/replay.

Apply required approval separation and explicit confirmations for sensitive changes. Enforce permissions server-side as well as in the UI. Clearly distinguish tenant-wide/provider-wide control scope. Emergency stops affect future eligible activity and cannot erase monitoring or rewrite historical decisions. Correlate related intent, route, provider, chain, settlement and webhook evidence with navigable links.

Acceptance: role-specific browser tests exercise all core operator workflows, denied actions, audit evidence and tenant switching where authorized. Import and replay feedback reflects actual durable jobs. No direct status override or simulated statement confirmation is available in production.
```

## 16. Implement operational metrics and product reporting

```text
Implement PRD §14 dashboards and reports from server-authoritative data: volume/status mix, detect/finality/settlement latency, outstanding/aging exceptions, eligible alternatives/fallbacks, provider health/timeouts/concentration, reconciliation match rates and compliance holds. Support applicable tenant, merchant, provider, asset, chain, jurisdiction, period and state filters.

Document metric definitions and denominators, including late/reorg corrections, event-time versus processing-time and the source of any USD equivalent. Distinguish estimated provider fees from actual costs. Add auditable completed-transaction usage records as a commercial integration seam; implement subscription/invoicing only if the baseline resolved it into scope. Do not hardcode the PRD's hypothetical prices or invent revenue/margin data.

Acceptance: aggregate values reconcile to underlying records, respect isolation and remain correct after duplicates/corrections. Dashboard queries have bounded cost and representative-data performance checks. Financial projections and operational metrics have clearly documented provenance.
```

## 17. Implement AWS infrastructure and secure deployment

```text
Create Terraform and container deployment configuration for isolated development/staging/production environments on AWS. Provision API/UI/worker runtime, PostgreSQL with point-in-time recovery, Redis, durable queue, TLS, Secrets Manager/KMS, network boundaries, least-privilege service roles, monitoring and artifact storage as needed. Document the chosen deployment topology and costs as estimates.

Implement CI/CD with short-lived deployment identity, migration safety, secret injection, environment validation, readiness checks and rollback procedures that account for schema/event compatibility. Separate provider/testnet credentials from production. Encode required approvals/configuration checks for live capability enablement. Prepare a reviewable deployment plan; do not claim unexecuted cloud changes or restore drills succeeded.

Acceptance: infrastructure validates, local/container smoke tests pass, staging deployment is reproducible when credentials are supplied, and secrets are absent from source/images/logs. Backup, retention and recovery settings map to the PRD targets and approved deployment policies.
```

## 18. Harden security, resilience and capacity

```text
Complete a threat model for authentication, tenant boundaries, credential theft, provider callbacks, webhook egress, RPC trust, statement ingestion, privileged actions and financial replay. Implement the resulting MVP controls, dependency/container scanning and sensitive-data redaction. Verify provider webhook authenticity according to each provider's official protocol.

Add end-to-end traces and actionable alerts for queue/outbox age, ingestion lag, stuck unknown outcomes, screening outages, finality/reorg incidents, webhook failures and settlement anomalies. Run representative load/endurance tests against the PRD planning targets: 100 sustained API RPS, 500 burst RPS, 100k intents/day and intent creation p95 under 500 ms excluding external quotes. Define the test mix/duration, dependency latency and capacity assumptions; do not imply measured 99.9% availability from a short load test.

Exercise queue outages, worker termination, duplicate/out-of-order messages, RPC failure, provider timeouts and database recovery. Execute backup/restore and disaster-recovery drills where infrastructure access permits; measure against RPO <=15 minutes and RTO <=4 hours.

Acceptance: publish measured results and fixes, AT-013 passes with real durable infrastructure, and any unmet target/gate is explicit with reproducible evidence.
```

## 19. Certify provider/testnet and complete MVP acceptance

```text
Run the complete AT-001–AT-014 suite against the integrated API, PostgreSQL, queue, workers and UI. Add real provider sandbox/testnet certification for two overlapping routes, approved asset/network coverage, verified instruction provenance, compliance integration, finality/reorg handling, signed webhooks and statement reconciliation. Test USDC and USDT only on the exact approved combinations; two assets and two networks does not justify unsupported cross-product combinations.

Create a merchant integration guide, OpenAPI examples, webhook receiver example, operational runbooks and troubleshooting procedures for unknown outcomes, late/wrong-token funding, reorgs, screening outages, provider stops, reconciliation and queue recovery. Document escalation ownership and evidence retention.

Acceptance: the traceability matrix links every P0 and AT to passing evidence or an explicit unresolved dependency. Simulated, sandbox, testnet and controlled-mainnet evidence are labeled separately. Controlled mainnet validation requires the applicable legal/provider approvals and an authorized bounded plan; do not infer authorization from this implementation prompt.
```

## 20. Prepare the pilot release and go/no-go packet

```text
Prepare a versioned pilot release candidate and a PRD §21 go/no-go packet. Include deployed versions/configuration, supported capabilities, test/certification evidence, tenant/RBAC/MFA controls, financial limits, compliance policies, provider overlap/fallback, finality/reorg recovery, webhook durability, reconciliation, restore results, monitoring, incident contacts and a rollback/kill-switch plan.

Check external evidence for launch-market legal opinion, provider responsibility/licensing matrix, contracted providers, merchant certification and pilot support ownership. Record commercial onboarding/subscription/usage arrangements without pretending engineering can approve them. Separate completed engineering work from external approvals and remaining blockers.

Acceptance: an authorized reviewer can decide whether to launch from concrete evidence. Missing gates prevent production activation. Do not publish/deploy a live financial service merely because a demo works or these prompts are complete.
```

## Later releases — do not fold these into MVP

After the pilot, create separate implementation slices for:

- **Refunds and payouts (P1):** new transfers, cumulative refund limits, independently verified destinations, screening, funding checks, approvals, unknown-outcome recovery and reconciliation. Never automatically refund to the on-chain sender.
- **Fiat off-ramp and executable quotes (P1):** provider-authorized conversion, quote expiry, liquidity, slippage and settlement evidence. Resolve quote requirements earlier if fiat-denominated acceptance depends on them.
- **Routing optimization (P1):** actual total costs, liquidity, reliability and scorecards, preserving mandatory gates.
- **Developer/commercial experience (P1):** SDKs, hosted checkout, payment-option reservations, pricing configuration and enhanced reporting.
- **Enterprise/treasury (P2):** SSO/federation, predictive ranking, multi-leg/cross-chain orchestration, treasury optimization and white-label experiences.

## Sources

- `docs/Stablecoin_Payment_Orchestration_Gateway_PRD.pdf`: scope, requirement IDs, architecture, acceptance tests and launch gates.
- `docs/PROTOTYPE.md` and `docs/API_CONTRACT.md`: implemented journeys, intended replacement seams and explicit production boundaries.
- `prototype/src/domain.ts`, `prototype/src/domain.test.ts`, `prototype/src/App.tsx`, `prototype/tests/journeys.spec.ts`: actual state model, safety behavior, screens and executable checks inspected when preparing this sequence.
