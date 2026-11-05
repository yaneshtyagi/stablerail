# ADR-005 Transactional outbox and durable queue

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-005; §9
- **Scope:** MVP asynchronous work. The broker product is an engineering default, not a throughput certification.

## Context

The PRD allows RabbitMQ or a managed durable queue, provided a transactional outbox is the source of events. Workers must survive process crashes. The prototype has neither.

## Decision

Business state and the outbox row commit in the same PostgreSQL transaction. A publisher relays committed outbox rows to one durable queue. Consumers are .NET worker services. Acknowledgement happens after the consumer’s own durable inbox write commits.

**Development default:** Amazon SQS is the queue product, with a local SQS-compatible emulator for Development and Test. The outbox payload, message identity and inbox deduplication do not depend on SQS features that cannot be reproduced locally. RabbitMQ remains acceptable only if slice 2 records a concrete tooling reason and keeps the same outbox contract. Do not run two brokers.

The queue is not the financial authority. A lost or duplicated message is recovered from the outbox and neutralized by inbox deduplication.

## Consequences

- Slice 2 records the emulator and the AWS queue name in infrastructure stubs.
- Slice 5 implements publishers, leases, retries and dead letters.
- Slice 17 provisions the production queue. Source code contains no queue credentials.

## Production gate

SQS is not approved as a measured capacity design. Slice 18’s load and outage tests are the evidence. Switching brokers later must preserve outbox identity and must not be done by dual-writing financial effects.
