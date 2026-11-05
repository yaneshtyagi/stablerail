# ADR-007 Chain-specific finality

- **Status:** Accepted engineering baseline, 5 November 2026
- **PRD:** ADR-007; MON-004 and AT-008
- **Scope:** MVP monitoring. Confirmation counts are not selected here.

## Context

Finality differs by chain and by the merchant’s risk policy. The prototype advances Ethereum to 12 confirmations and Polygon to 64 when the operator clicks a scenario. Those numbers are sandbox controls. Copying them into production configuration would present an unapproved safety floor as a fact.

## Decision

A versioned finality policy is stored per chain. Success uses canonical chain evidence (block number, block hash and confirmation depth or the policy’s finalized marker) evaluated by the monitor. UI actions and provider status strings do not satisfy finality.

The applied policy version is snapshotted on the payment. A later policy edit does not rewrite that snapshot.

Reorganizations and replaced blocks invalidate affected observations with compensating financial events. Provisional recognition is withdrawn. If settlement evidence already exists, the settlement projection becomes a mismatch or exception without deleting the original statement. Restoration of success requires new chain evidence under the snapshotted policy plus an explicit reassessment record. There is no force-finalize command.

Until an approved policy exists, the production finality evaluator is disabled: observations are stored and payments do not transition to `succeeded` because of confirmations. Development and Test may load a profile marked `dev-only` whose confirmation depths are obviously synthetic and are rejected when the environment is Production.

## Consequences

- Slice 11 implements the evaluator against the policy port.
- Tests inject depths inside the dev profile. They do not freeze 12 and 64 as the product rule.

## Production gate

Approved safety floors, RPC sources and chain identity are external inputs. This ADR does not approve Ethereum or Polygon mainnet parameters, including the prototype’s chain IDs `1` and `137`.

## Prototype conflict

`simulate(..., "finalize")` writes confirmation counts onto the existing transfer row and flips status in place. Production appends evidence and rebuilds the projection.
