# ADR-003 Capability-based provider adapters

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-003; §6.3 PRV-001–PRV-004
- **Scope:** MVP inbound collection. Payout and refund operations stay out of the MVP adapter surface.

## Context

Routing and instruction issuance need a common provider contract. The prototype’s “Bridgeway” and “Orbit Payments” records are named fixtures with illustrative fees, limits and health. They are not contracted vendors.

## Decision

Every execution provider implements:

- `getCapabilities`
- `checkHealth`
- `createPaymentInstructions`
- `getTransactionStatus`
- `estimateFees`

Capabilities identify the exact chain ID, token contract, decimals, jurisdiction, limits, settlement mechanism, account and environment. Token symbols are labels. Health, timeouts, rate limits, circuit breakers and normalized errors are part of the adapter, not the UI.

The MVP loads two adapters whose configured inbound capabilities overlap on at least one approved asset and network pair. Deterministic fakes implement the same contract for tests. Fakes are registered only when the host environment is Development or Test. Production startup rejects fake adapters and missing live credentials by disabling live issuance, not by substituting demo data.

`initiatePayout` and `initiateRefund` are not MVP operations.

## Consequences

- Slice 7 can finish the seam and contract tests without credentials.
- Live use stays off until two named providers, contracts and sandbox evidence exist.
- A capability change is a new version. Historical decisions keep the old version (ADR-012).

## Production gate

Provider names, credentials, overlapping coverage and certification are external inputs. Prototype names must not appear in production configuration as vendors. No adapter is certified by this ADR.

## Prototype conflict

Fees such as `0.80` and `0.45`, the USD 100,000 and 50,000 limits, and latency strings are scenario data. They are not provider tariffs.
