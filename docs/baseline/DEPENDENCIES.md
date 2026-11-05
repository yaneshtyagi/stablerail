# Dependency register

Unresolved inputs stay unresolved. Each row names the concrete artifact that would close it, the development default used until then, and the production behavior while it is missing. Development defaults are engineering choices. They are not approvals, and production startup must reject the ones marked as rejected in Production.

No row in this register is satisfied.

| Input | Concrete missing artifact | Development default | Production gate effect |
| --- | --- | --- | --- |
| Launch jurisdiction | Written activity-specific legal opinion naming the market and the permitted SPOG activities | No market code is enabled | Live routing, screening bypass and deployment of real payment traffic stay disabled. PRD §21 legal gate is not passed |
| Provider responsibility and licensing | Signed matrix allocating custody, KYB, sanctions, Travel Rule, monitoring and licensing between SPOG, the merchant and each provider | No responsibility matrix loaded | Production provider accounts cannot be activated |
| Two overlapping providers | Executed agreements with two named providers, sandbox credentials, and evidence that their inbound asset and network coverage overlap | Two uncertified adapter fakes in Development and Test only. Prototype names Bridgeway and Orbit Payments are not used as vendors | Live `createPaymentInstructions` stays disabled. Fallback cannot be claimed |
| Approved token and network pairs | Allowlist rows: chain ID, contract address, decimals, asset code, issuer and permitted jurisdictions, from the provider and compliance owners | Empty registry. Tests may use synthetic chain IDs and synthetic contracts that cannot be mistaken for mainnet | Monitoring and routing have nothing to enable. Prototype USDC addresses and chain IDs `1` and `137` are not loaded |
| Payment denomination and quotes | Decision of whether launch merchants send a fixed token amount or a fiat amount, plus a provider that can issue an executable quote if fiat is required | Fixed-token requests are the only enabled shape. Fiat bodies return `quote_required`. No USD parity constant exists | Fiat-denominated acceptance and executable quotes stay off. P1 quote work is not pulled into the MVP |
| Custody and beneficiary | Written confirmation of who owns destination wallets, and a provider or merchant source for those addresses | No platform address and no key store. Instruction issuance returns a controlled error when the adapter has no destination | The illustrative prototype deposit address is not configured. ADR-001 stays in force |
| Finality | Approved per-chain finality policy, including the safety floor and the evidence that satisfies it | A `dev-only` profile may set synthetic depths for tests. Production configuration rejects that profile | Payments do not become `succeeded` from confirmation counts. AT-008 can still be tested with the dev profile |
| Tolerance | Approved merchant tolerance, as an exact minor-unit amount per asset | Zero. Recognized amount must cover the obligation | A non-zero tolerance is not invented to make partial payments succeed |
| Expiry | Approved default and maximum expiry for the launch merchant | 900 seconds, with a validated range of 60 seconds to 24 hours, labeled as an unapproved engineering default | Expiry jobs may run in non-production. Production merchant records need an explicit policy version before intents are accepted |
| Retention and privacy | Jurisdiction retention schedule, deletion rights and log-redaction rules | Financial, routing, compliance and audit evidence is retained. Deletion jobs are not shipped | No evidence is purged to meet an assumed schedule |
| Operational RACI | Named approvers for emergency stop, credential administration, policy publication, case resolution, webhook replay and settlement correction | Role set below, labeled unapproved. MFA is required for those actions in every environment that has real operator login | Privileged production actions stay disabled until the RACI is loaded and MFA is enforced. SSO is not the substitute; SSO is P2 |
| Commercial rates and billable definition | Approved subscription, usage fee, caps and the accounting definition of a billable transaction | No prices stored. Usage-record seam uses first transition to `succeeded`, with a compensating record if success is withdrawn. This is not revenue recognition | The product must not emit invoices or display the PRD’s hypothetical $199, $999, 0.15% or 0.08% figures as prices |
| Screening provider | Contract, API credentials, freshness SLA and fail-closed behavior confirmed by compliance | Replaceable fake adapter in Development and Test. Mandatory checks in any other environment return `unavailable` | Routing and success fail closed. A cheaper route cannot skip the check |
| RPC and chain trust | Approved endpoint URLs, chain ID authentication and failover endpoints | No production RPC URLs. Tests inject a fake chain source | Ingestion of real networks is disabled |
| Webhook signing secrets | Algorithm confirmation and a secret-management procedure | Slice 12 specifies the algorithm and tests it with generated test secrets. Production secrets are not in source | Endpoints can be stored; delivery to real merchants waits for secret provisioning |
| Identity host | Confirmation that operator login may be local ASP.NET Core Identity rather than a customer IdP | Local accounts with TOTP MFA. Development authentication bypass exists only in Development and is rejected at production startup | Enterprise federation is not offered. TEN-006 remains P2 |
| Durable queue product | Slice 2 confirmation of the local emulator and the AWS queue settings | Amazon SQS, with a local SQS-compatible emulator. Outbox remains the authority | Not a capacity or disaster-recovery approval. Slice 18 measures outages |
| Restore objectives | Executed backup, restore and region-failure drill against RPO 15 minutes and RTO 4 hours | Settings are documented in slice 17 as targets, not as measured results | PRD §21 restore gate stays open |

## Development role set

This set is the least-privilege starting point for slice 4. It is not an approved operating model.

| Role | May do | May not do |
| --- | --- | --- |
| Auditor | Read tenant-scoped intents, routes, cases, settlements, webhooks and audit | Mutate financial or administrative state |
| Developer | Create and read intents for scoped merchants, manage that merchant’s test webhook endpoints | Change limits, stop providers, resolve cases, or read another tenant |
| Payments operator | Read and cancel eligible intents, inspect routes and provider health | Publish policy, import settlement corrections, or release a hold |
| Finance operator | Import statements, inspect exceptions, request exports | Approve their own corrective import when the RACI requires separation |
| Compliance officer | Read decisions, record case notes, resolve a case only under the slice-8 approval rule | Disable a mandatory check or force success |
| Tenant administrator | Manage merchants, users and credentials inside the tenant, with MFA | Reach another tenant or turn on an uncertified provider |
| Platform administrator | Provider emergency stop and restore, with MFA, affecting future eligibility only | Edit historical decisions, skip monitoring, or act without an audited tenant context |

Authority is always the authenticated principal’s environment, tenant and scopes. A client-supplied tenant ID is not authority. If the body contains a tenant ID that disagrees with the credential, the request is rejected.

## Engineering defaults already selected

These are selected so later slices do not invent them again. External gates above still dominate production enablement.

| Setting | Selection | Source |
| --- | --- | --- |
| Runtime | .NET 10 LTS, `net10.0` | Microsoft support policy, read 5 November 2026. Support through 14 November 2028 |
| Database | PostgreSQL 18 | PostgreSQL versioning policy. Support through 14 November 2030 |
| UI toolchain | Node.js 26 LTS | Node.js 26 release line. Installed Node.js 25 is end of life and is not the product toolchain |
| Cache | Redis, non-authoritative | PRD §9. Major pin in slice 2 |
| Queue | SQS plus outbox | ADR-005 |
| Routing tie-break | Merchant priority, then provider priority, then provider ID, all ascending | ADR-009 |
| Money | Decimal strings on the API; integer minor units and an explicit scale in the database | ADR-004 and the state machine |
| Idempotency retention | Same lifetime as the financial evidence | State machine |
| Billing engine | Not in the MVP | TEN-005 is P1. Usage seam only |
| SSO | Not in the MVP | TEN-006 is P2. MFA is in the MVP |
