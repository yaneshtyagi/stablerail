# ADR-006 At-least-once event processing

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-006; §§6.6 and 9
- **Scope:** Outbox consumers, provider callbacks, chain observations and merchant webhooks.

## Context

A durable queue and webhook delivery retry messages. Exactly-once delivery is not available across PostgreSQL, the broker and an external HTTP endpoint.

## Decision

Processing is at-least-once. Consumers persist an inbox record keyed by the stable event identity in the same transaction as the resulting business write. A repeated delivery is a no-op for financial value and still returns success once the original effect is durable.

Identities:

- Outbox and merchant webhook logical events use a stable event ID. A replay creates a new delivery attempt and keeps that event ID.
- Chain observations use network (chain ID), transaction hash and log index.
- Provider notifications correlate to a payment attempt or observation. They do not create a second financial recognition for the same observation.

Merchant consumers are required to be idempotent. The integration guide includes an executable receiver example in slice 19. The platform does not claim exactly-once webhook delivery.

## Consequences

- Retries, crashes and operator replays are safe only because effects are deduplicated.
- “Retryable” in the API error envelope means the same idempotent request may be repeated. It does not mean a second provider execution is safe.

## Production gate

AT-005, AT-009 and AT-013 are the acceptance evidence. They are not yet run against the product.
