# ADR-009 Deterministic routing first

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-009; RTE-001–RTE-005. RTE-006–RTE-008 are outside MVP.
- **Scope:** Route selection before instructions are issued.

## Context

The PRD eliminates ineligible routes before any ranking. A later illustrative score uses cost, time, failure and liquidity. The prototype already applies hard gates and then the lowest configured priority. Orbit’s lower illustrative fee does not win. That priority behavior is the MVP rule. The score is not.

## Decision

Mandatory gates run first and cannot be overridden by price, latency or a score:

- asset and exact chain or network support
- jurisdiction and compliance eligibility
- merchant and provider limits, including velocity once slice 8 exists
- liquidity signal when the adapter declares it required for the operation
- provider availability, fresh health and emergency stop

Among routes that pass, sort by ascending merchant priority, then ascending provider priority, then ascending stable provider ID. Fees are stored and displayed. They are not a sort key in the MVP.

The decision persists inputs, every candidate’s exclusion reasons, the selected route, policy version and capability version. A quote response labels amounts as estimates. Nothing in the MVP is an executable FX or gas quote.

Gates are revalidated immediately before `createPaymentInstructions`. After an option’s instructions are issued, routing must not change that option’s provider or destination. A new option may be added only under the state-machine rules, and never to redirect a transfer already aimed at an issued instruction.

## Consequences

- Slice 9 implements this order. Optimization scores are not implemented.
- Emergency stop affects future eligibility only. It does not rewrite stored decisions or stop monitoring.

## Production gate

Priorities and limits in production come from versioned tenant configuration. Fixture priorities are Development and Test data.

## Prototype conflict

`routes` sorts by `priority` and excludes stopped, unhealthy, over-limit and screening-held providers. Production keeps that order and adds jurisdiction, exact asset identity, liquidity and health freshness, which the prototype does not model.
