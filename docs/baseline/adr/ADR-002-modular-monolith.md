# ADR-002 Modular monolith

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-002; §9
- **Scope:** MVP topology. Service boundaries may be revisited only after a measured operational need.

## Context

The PRD recommends one control plane with separately scalable workers, and warns against premature microservices. The prototype is a single browser application with no server.

## Decision

Ship one modular .NET solution for the control plane and separate worker processes built from the same solution. Modules own identity, payments, providers and routing, monitoring, compliance, finance, and webhooks. Modules communicate through in-process application interfaces and the transactional outbox. They do not start as network services.

Verified targets on 5 November 2026:

- **API and workers:** ASP.NET Core on .NET 10 LTS. [Microsoft’s support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), last updated 25 October 2026, lists .NET 10 as LTS in active support through 14 November 2028 (latest listed runtime patch 10.0.14). .NET 8 LTS and .NET 9 STS end support on 10 November 2026. The machine has SDK `10.0.301`; slice 2 pins a current `net10.0` patch rather than treating that SDK banner as the runtime patch.
- **System of record:** PostgreSQL 18. The [PostgreSQL versioning policy](https://www.postgresql.org/support/versioning/) lists 18 as supported through 14 November 2030, with 18.6 the current minor at the time of that page read. Slice 2 pins the current minor image. Local `psql` 16.15 is a supported client and is not the server pin. PostgreSQL 14 reaches end of life on 12 November 2026 and must not be used.
- **UI toolchain:** Node.js 26 LTS (Lithium, LTS from 28 October 2026 through April 2029 per the Node.js 26.13.0 release note). Installed Node.js 25 is end of life and must not build `code/`. The prototype remains on its documented Node.js 22 line.
- **UI stack:** React, TypeScript and Vite, reusing `@yaneshtyagi/design-system` at the prototype’s 1.1.0 line unless a later slice records an upgrade.
- **Cache:** Redis for cache and throttling only. The major version is pinned in slice 2 against current Redis support. Redis is not a financial store.
- **Telemetry:** OpenTelemetry for traces, metrics and logs across API and workers.

## Consequences

- One database and one migration stream for the control plane.
- Workers scale independently without a separate public API.
- Cross-module calls stay in process. A network hop requires a new ADR.

## Production gate

Version pins are engineering selections, not a capacity, availability or restore approval. Slice 17 encodes deployment. Slice 18 measures the PRD performance targets.
