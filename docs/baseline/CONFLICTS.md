# Prototype conflicts

The prototype is evidence of the operations journeys and of several safety rules. Where it takes a shortcut, production follows the PRD and the baseline decisions below. “Do not inherit” means the shortcut must not be copied into `code/` configuration, domain logic, or the production UI.

Behaviors that production does inherit are listed at the end.

## Do not inherit

| Prototype behavior | Evidence | Production rule |
| --- | --- | --- |
| Instructions are issued inside `createIntent`, so the intent never exists as `created` | `domain.ts` sets `requires_payment` immediately | Persist `created` first. Issuance is a durable attempt and may be pending or unknown |
| One network and one implicit option per intent | `CreateInput.network` | One obligation, many options and many matching transfers |
| USDC only, and every amount uses six decimal places | `units` / `decimal`, `TOKEN` | Two assets on two networks, only approved pairs. Scale comes from the asset registry for that contract and chain |
| Hardcoded USDC contracts, chain IDs `1` and `137`, and one shared `DEPOSIT` address | `TOKEN`, `CHAIN`, `DEPOSIT` | Production allowlists and destinations start empty. These literals are not approved facts and must not be copied into production configuration |
| Finality is 12 Ethereum confirmations or 64 Polygon confirmations | `simulate` finalize branch | Chain-specific versioned policy. Those counts are not the safety floor. Production success stays disabled until a policy is approved |
| USD and USDC are the same number, including the “USD 100,000” limit check | `createIntent` and the API example in `docs/API_CONTRACT.md` | Fixed-token amounts are parsed at asset scale. Fiat amounts require a quote. No silent parity |
| Partial scenario credits `amount / 2` with integer division | `simulate` partial branch | Underpayment is an observed transfer amount, not a halved obligation. Remainder must not disappear |
| Wrong-token journal text still says USDC | `simulate` detail string | Store and display the observed contract and amount. Do not relabel it |
| Duplicate scenario no-ops when any transfer already exists | `scenario === "duplicate"` | Deduplicate the exact `(chain_id, transaction_hash, log_index)` even when other transfers exist, including across overlapping ingestion sources |
| Confirmations, received amount, flags and settlement are mutated in place | `simulate`, statement import | Append observations and compensating events. Rebuild projections |
| Client sends `tenant` and it is trusted | `CreateInput.tenant` | Tenant and merchant authority come only from the authenticated credential |
| Idempotency is a process-local map keyed by tenant and key | `state.idempotency` | Durable idempotency with canonical payload comparison, conflict, in-flight recovery and no financial double effect |
| Providers are the fictional Bridgeway and Orbit Payments records, with fees `0.80` and `0.45` | `seed` | Adapter names in production come from contracts. Fixtures stay in Development and Test and are labeled uncertified |
| Provider health, screening, webhooks and statements are in-browser simulations | `PROTOTYPE.md` architecture section | Real seams fail closed when unconfigured. Simulator controls render only in a labeled development environment |
| Dashboard numbers come from seeded local intents | `seed` | Metrics come from server evidence. Fixture volume must not be shown as production activity |
| Statement CSV is exactly three unquoted columns, and unknown intents reject the whole file | `importStatement` | Provider-specific formats plus a retained unmatched inbox. The three-column file remains a development format only |
| “Record matched statement” marks settlement without an external file | journeys and finance UI | Settlement changes only from imported evidence. No simulated confirmation in production |
| Webhook delivery is a fabricated HTTP 503 and replay fabricates HTTP 200 | `simulate`, `replayHook` | Signed delivery, real attempts, backoff and replay that does not change financial value |
| Credentials are masked display strings created in the browser | Administration screen | Generate a secret, show it once, store a verifier, scope it to an environment, and support rotation and revocation |
| Audit and tenant isolation depend on the selected demo tenant in the browser | `access` | Isolation in the database, API, workers, cache keys and exports. Denied access is audited without leaking the foreign record |
| Expiry and webhook retry do not advance with the clock | `PROTOTYPE.md` | Durable scheduled expiry and retry. The sandbox clock is not the product clock |
| Compliance is a boolean plus `CASE-1042` | `simulate` hold | Versioned decisions with source, freshness and case reference. Fail closed. No unrestricted release |
| Policy id `pol_2026_10_v3` is a constant | `createIntent` | Real version rows. The constant is not an approved policy |
| Local storage key `stablerail.sandbox.v3` survives reload | `App.tsx` `load` | Not a durability mechanism. Product UI does not treat local storage as financial authority |
| SSO is absent, and so is MFA | prototype scope | SSO stays P2. Privileged MFA is required in the MVP |
| Second tenant is a demo switcher | Harbor PSP seed | Tenants are server-isolated organizations, not a UI toggle |

## Asset and network gap

PRD §22’s proof of concept, which the prototype implements, is one token on two chains. PRD §5’s MVP is two assets and two networks, with the working default USDC and USDT on two EVM chains, subject to local eligibility. Two assets times two networks is not permission to enable every combination.

The prototype therefore does not prove USDT, does not prove per-asset precision, and does not prove an approved allowlist. Slice 10 monitors only combinations present in the enabled registry. The production registry ships empty. Development fixtures, if slice 7 or 10 need them for tests, are marked uncertified and are refused at production startup. Public contract addresses that appear in the prototype are not copied forward as verified configuration.

## Commercial scope that is easy to get wrong

PRD §13 makes platform subscription and a usage fee P0 revenue sources. PRD §6.1 TEN-005 makes pricing-plan configuration P1. The Starter and Growth prices in §13 are labeled hypothetical.

The MVP does not add a billing or invoicing engine and does not store those prices. Slice 16 does add append-only usage records so a commercial system can rate completed transactions later. The engineering default for that seam, which is not an accounting approval, is one usage record the first time acceptance reaches `succeeded`, and a compensating usage record if that success is withdrawn. Settlement is not treated as the billable event by this default. Finance can replace the definition before launch; until then, production must not present usage records as invoices or revenue.

## Inherit

These prototype rules are the behavioral target, reimplemented on the server:

- Decimal strings at the boundary and exact integer arithmetic. No binary floating point for money.
- Hard gates run before priority. A lower fee does not override a failed gate. Equal eligibility uses the lower priority number.
- An ambiguous provider timeout blocks cancellation, finality and a second execution. Reconciliation returns to the original attempt.
- A duplicate observation does not increase recognized value.
- A late transfer on an expired or cancelled intent stays attached and does not auto-succeed.
- A wrong contract is stored and is not credited.
- A reorganization keeps prior evidence, withdraws provisional finality, and blocks restoration until independent chain evidence exists. Settled projections become mismatches.
- Historical route evidence and policy version survive later provider edits.
- Compliance hold, exception flags, unknown outcome and settlement are separate from acceptance status. A hold blocks success.
- Webhook replay keeps the logical event ID, appends an attempt, and does not change financial value.
- Cross-tenant read and mutation are denied and audited.
- Statement references are idempotent. The same reference with different financial data conflicts. Corrections use a new reference.
- CSV exports neutralize formula prefixes.
- There is no one-click force-finalize or release-hold control.
- Cancellation does not claim to reverse a broadcast transfer, and monitoring continues.
