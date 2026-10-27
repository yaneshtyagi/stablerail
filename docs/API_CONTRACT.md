# Sandbox service boundary and future API contract

The prototype calls local TypeScript operations, not HTTP endpoints. This contract maps the browser model to the PRD's future `/v1` API. Mock provider names, deposits and fees are demonstration data.

| Future endpoint                              | Local equivalent                           | Key contract                                                                                                              |
| -------------------------------------------- | ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| `POST /v1/payment-intents`                   | `createIntent(state, input)`               | Required tenant, merchant reference, decimal amount, network, expiry and idempotency key; returns `{ state, id, replay }` |
| `GET /v1/payment-intents`                    | Tenant-scoped list + filter/sort           | Search reference/ID/provider/chain; state dimension filter; CSV reflects filtered results                                 |
| `GET /v1/payment-intents/{id}`               | `access(state, id, tenant)`                | Reject foreign or missing record; exact token, chain, amount and event evidence                                           |
| `POST /v1/payment-intents/{id}/cancel`       | `cancelIntent`                             | Only unfunded requires_payment with known provider outcome; explicit UI confirmation                                      |
| `GET /v1/payment-intents/{id}/transactions`  | `Intent.transfers`                         | Network + transaction hash + log index identify observation; contract and token base precision retained                   |
| `GET /v1/payment-options`                    | `TOKEN`, `CHAIN`, fixed supported networks | Native USDC, six decimals, Ethereum 1 and Polygon 137                                                                     |
| `POST /v1/route-quotes`                      | `routes`                                   | Hard gates first, priority next; returns all candidates with exclusion reasons and fee estimates                          |
| `GET /v1/settlements`                        | Independent `Intent.settlement`            | pending / settled / mismatch, independent of payment acceptance                                                           |
| `GET /v1/reconciliation-exceptions`          | Separate flags + hold + mismatch view      | Original financial events remain present                                                                                  |
| Provider statement import (adapter-specific) | `importStatement`                          | Atomic validation, amount comparison, provider-reference deduplication and evidence append                                |
| `POST /v1/webhook-endpoints`                 | Tenant `State.endpoints`                   | Valid HTTPS URL; no real request from browser                                                                             |
| `POST /v1/webhook-events/{id}/replay`        | `replayHook`                               | Same event ID, append attempt history, no payment mutation                                                                |

## Core typed records

See exported interfaces in `src/domain.ts`: `Intent`, `Transfer`, `Provider`, `Route`, `HookEvent`, `Credential`, `Journal`, `CreateInput` and `State`.

- Amounts are decimal **strings**, never binary floats; supported USDC precision is six places.
- Datetimes are UTC ISO 8601 in state; the console displays labeled Asia/Kolkata times.
- Intent state: `requires_payment`, `detected`, `confirming`, `succeeded`, `expired`, `cancelled`. Creation is immediately followed by instruction issuance in the mock service.
- Compliance hold, exception flags, unknown provider outcome and settlement are independent dimensions.
- Route snapshot retains policy version, candidate fee/priority/gate decisions; parent intent retains tenant, amount, network and token identifier.
- Idempotency scope is tenant + key. Normalized amount, reference, merchant, network and expiry form the comparison payload.
- Webhook attempt history retains timestamp, result and stable event ID. At-least-once consumers must be idempotent.

Illustrative production create request:

```http
POST /v1/payment-intents
Authorization: Bearer <server-issued environment-scoped credential>
Idempotency-Key: order-98431
Content-Type: application/json
```

```json
{
  "merchant_reference": "ORD-98431",
  "amount": "250.00",
  "currency": "USD",
  "accepted_assets": ["USDC"],
  "accepted_networks": ["polygon"],
  "expires_in_seconds": 900
}
```

The server must derive tenant and merchant authority from authenticated context, never trust a client-supplied tenant claim. Production IDs, cursor pagination, real provider correlation IDs and transactional durability must be added behind this boundary.

## Errors

The mock throws descriptive errors surfaced in persistent form alerts or action toasts. It distinguishes `idempotency_conflict`, `no_eligible_route`, `authorization_denied`, `statement_conflict`, input validation and financial-state blocks. It does not pretend to expose server HTTP errors.

The future server should normalize these into the PRD envelope:

```json
{
  "error": {
    "code": "idempotency_conflict",
    "message": "This key was used with a different payload.",
    "category": "financial_state",
    "retryable": false,
    "request_id": "req_example",
    "details": { "field": "Idempotency-Key" }
  }
}
```

Map validation to 422, scope denial to 403 (or non-disclosing 404), conflicting key/reference to 409, and no eligible route to a non-retryable routing error until inputs or eligibility change. A provider timeout must report an unknown outcome, not permission to submit another transfer. Retrying must mean repeating the same idempotent operation safely.

## Provider/worker replacement seam

Replace deterministic fixtures with adapters implementing `getCapabilities`, `checkHealth`, `createPaymentInstructions`, `getTransactionStatus` and `estimateFees`. Preserve exact token contracts, chain IDs, jurisdiction, amount/velocity limits, liquidity availability and capability versions. Revalidate hard gates before any irreversible operation.

Chain ingestion must atomically deduplicate `(network, transaction_hash, log_index)`, validate destination and exact token contract, track reorgs, and append financial effects with compensating corrections. Persist the intent/outbox transactionally before emitting signed webhooks. Actual HTTP signing, retries, dead-letter handling and worker-crash tests are deliberately outside this frontend prototype.
