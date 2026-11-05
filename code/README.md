# StableRail product

Modular control plane for slice 2. The sandbox in `../prototype` stays independent and is not imported as production authority.

Decisions are in [`docs/baseline`](../docs/baseline/README.md). This slice provides the solution, local dependencies, migrations, health, logging, request IDs, OpenTelemetry and the operations shell. It does not implement payment acceptance, provider calls, or financial tables. Those remain later slices. No production gate is passed.

## Runtime pins

Checked on 5 November 2026:

| Dependency | Pin |
| --- | --- |
| API and worker | ASP.NET Core, `net10.0`, SDK 10.0.301 or a later 10.0 SDK |
| PostgreSQL | `postgres:18.6` |
| Redis | `redis:8.10.3`, cache and throttling only |
| Queue | Amazon SQS standard queue. Local emulator ElasticMQ `softwaremill/elasticmq-native:1.7.1` |
| Operations UI | Node.js 26.13.0 LTS, React, TypeScript, Vite, `@yaneshtyagi/design-system` 1.1.0 |

The durable queue is Amazon SQS. The local emulator is ElasticMQ. RabbitMQ is not used. Queue names are `stablerail-control-plane` and `stablerail-control-plane-dlq` in `us-east-1`. The local account id is `000000000000`. Visibility timeout is 30 seconds, long poll is 20 seconds, and the dead-letter redrive count is 5. Slice 5 owns publishing, retries and dead-letter behavior. The outbox remains the authority.

Redis is not a financial store. PostgreSQL is the system of record. The browser is not an authority.

## Module ownership

| Module | Schema | Owns |
| --- | --- | --- |
| identity | `identity` | Tenants, merchants, accounts, sessions, MFA and environment-scoped credentials |
| payments | `payments` | Intents, options, instructions, idempotency and acceptance |
| providers | `providers` | Provider adapters, health, routing policy and route decisions |
| monitoring | `monitoring` | Chain observations, confirmations and reorganization evidence |
| compliance | `compliance` | Screening decisions, holds and case references |
| finance | `finance` | Settlement evidence, reconciliation and statement imports |
| webhooks | `webhooks` | Endpoints, signed delivery attempts and replays |

Modules talk through in-process application interfaces. They are not network services. Domain projects reference only the building blocks. Application projects reference their own domain. Infrastructure wires them into the API and worker.

## Fresh checkout

From `code/`:

```sh
./scripts/dependencies.sh
./scripts/migrate.sh
dotnet run --project src/Api
dotnet run --project src/Worker
```

In another shell, with Node.js 26:

```sh
cd operations
pnpm install --frozen-lockfile
pnpm dev
```

Open http://localhost:5174. The API listens on http://localhost:5080 and the worker on http://localhost:5081.

| Port | Process |
| --- | --- |
| 54329 | PostgreSQL 18.6 |
| 63799 | Redis 8.10.3 |
| 9324 | ElasticMQ SQS API |
| 9325 | ElasticMQ UI |
| 5080 | API |
| 5081 | Worker health |
| 5174 | Operations UI |

`./scripts/test.sh` starts dependencies, applies migrations, and runs the backend and frontend checks. `./scripts/build.sh` builds the solution and the UI.

Example configuration without secrets:

- `config/appsettings.json` fails closed
- `config/appsettings.Development.json` is the local development file
- `config/local.env.example` repeats the local development values
- `config/production.env.example` shows the production keys with empty required values

The local database password `stablerail` and queue credentials `local` / `local` are emulator defaults. They are not production secrets, and production startup rejects them.

## Checks

```sh
./scripts/test-backend.sh
./scripts/test-frontend.sh
```

Backend tests need the dependencies running. Frontend checks require Node.js 26 and refuse any other major version. The installed Node.js 25 line must not build this app. The prototype keeps its own Node.js 22 toolchain.

GitHub Actions workflow `product.yml` runs the same dependency, migration, backend and frontend commands. The prototype workflow is unchanged.

## Production startup

Only `Development` and `Test` may use the local dependency endpoints, simulation, fake provider adapters, the `dev-only` finality profile, trace diagnostics, or the console trace exporter. Development authentication exists only when the host environment is `Development`. Every other environment, including `Production`, rejects that mode and rejects missing PostgreSQL, Redis, queue name, dead-letter name, queue region, or OTLP endpoint.

```sh
ASPNETCORE_ENVIRONMENT=Production DOTNET_ENVIRONMENT=Production \
  dotnet run --project src/Api --no-launch-profile
```

The process exits before serving traffic. The same guard runs in the worker. Do not fill `config/production.env.example` with live credentials; production queue access uses the AWS credential chain, not keys in application settings.

## HTTP surface

- `GET /health/live` is process liveness
- `GET /health/ready` checks PostgreSQL, Redis and the queue
- `GET /v1/system` describes the host mode and records that payment volume is unavailable
- `GET /v1/modules` reads persisted module ownership
- `GET /v1/diagnostics/correlation` is enabled only in development and propagates request, intent, route, provider, chain and reconciliation correlation on one trace

Logs are JSON to stdout and include the request id, trace id and service name. Responses echo `X-Request-Id` and `X-Trace-Id`. OpenTelemetry traces, metrics and logs are exported to `StableRail:Telemetry:OtlpEndpoint` when that endpoint is set.

`/v1` requires an operator credential unless authentication mode is `Development` or `Disabled`. `Required` returns 401 until slice 4 issues credentials. Health endpoints stay unauthenticated.
