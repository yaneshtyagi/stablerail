# ADR-012 Version and audit policy configuration

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-012; RTE-003, CMP-004 and TEN-004
- **Scope:** Routing, compliance, finality, merchant limits, credentials and other administrative policy.

## Context

Operators need to see why a historical payment chose a route after someone edits configuration. The prototype copies route evidence onto the intent and keeps `pol_2026_10_v3`. That snapshot behavior is right. The policy ID is a fixture, and browser audit rows are not durable.

## Decision

Policy documents are immutable versions. A change inserts a new version and an audit event. It does not alter the previous version. Routing decisions, compliance decisions, finality evaluations and issued instructions store the version identifiers they used.

Privileged changes require an authenticated operator, the least-privilege role defined in slice 4, and MFA. The MVP does not implement enterprise SSO (TEN-006, P2). Privileged actions include credential create, rotate and revoke, merchant allowlist and limit changes, emergency stop and restore, finality and compliance policy publication, manual webhook replay, and settlement-correction approval.

Denied cross-tenant access writes an audit event on the caller’s tenant without copying the foreign record.

## Consequences

- Slice 4 persists versions and the audit trail.
- Slice 9 and slice 11 snapshot the versions they apply.
- Tests edit current configuration and assert historical decisions are unchanged (AT-014).

## Production gate

Who may approve which action is an organizational input. The development role set in the dependency register is labeled unapproved. Production enablement of privileged writes waits for that RACI and for MFA, not for SSO.
