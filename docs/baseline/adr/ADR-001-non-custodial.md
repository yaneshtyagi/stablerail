# ADR-001 Non-custodial MVP

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-001; §§1, 5 and 20
- **Scope:** MVP. Refunds and payouts remain P1 and stay non-custodial when they are built.

## Context

StableRail orchestrates stablecoin acceptance. Custody, exchange and fiat settlement stay with authorized parties. Holding customer assets or private keys would change the licensing and control model. The prototype never signs transactions, but it also invents one shared deposit address, which must not become a platform-held destination.

## Decision

The MVP does not generate, store, import or use private keys, seed phrases, signer credentials or customer-asset custody. It does not broadcast token transfers. Destination addresses and payment instructions come from a provider adapter or from a merchant beneficiary account that the merchant or its provider controls. The schema has no key-material columns. Logs and traces must not contain provider secrets or signing material.

P1 refunds and payouts, when scoped later, are new provider-executed transfers. They are not on-chain reversals and they are not sent by a platform wallet.

## Consequences

- Instruction issuance is an adapter call, not a local wallet operation.
- A missing beneficiary or provider account disables instruction issuance. The platform does not substitute an address of its own.
- Security review treats any signing dependency as a scope violation.

## Production gate

Custody responsibility must be written down for the launch market and the contracted providers before live instructions are enabled. See the dependency register. This ADR does not supply that approval.

## Prototype conflict

`DEPOSIT` in `prototype/src/domain.ts` is one illustrative address for every payment. Production configuration must not contain it, and tests must not present it as a payable production destination.
