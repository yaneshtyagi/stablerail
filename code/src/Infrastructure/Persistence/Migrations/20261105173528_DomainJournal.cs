using System;
using Microsoft.EntityFrameworkCore.Migrations;
using StableRail.Infrastructure.Persistence;

#nullable disable

namespace StableRail.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DomainJournal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payments");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "monitoring");

            migrationBuilder.EnsureSchema(
                name: "compliance");

            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.EnsureSchema(
                name: "providers");

            migrationBuilder.EnsureSchema(
                name: "webhooks");

            migrationBuilder.CreateTable(
                name: "providers",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenants",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    object_type = table.Column<string>(type: "text", nullable: false),
                    object_id = table.Column<Guid>(type: "uuid", nullable: true),
                    result = table.Column<string>(type: "text", nullable: false),
                    details = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "blockchain_observations",
                schema: "monitoring",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chain_id = table.Column<long>(type: "bigint", nullable: false),
                    transaction_hash = table.Column<string>(type: "text", nullable: false),
                    log_index = table.Column<int>(type: "integer", nullable: false),
                    block_number = table.Column<long>(type: "bigint", nullable: true),
                    block_hash = table.Column<string>(type: "text", nullable: true),
                    token_contract = table.Column<string>(type: "text", nullable: false),
                    destination = table.Column<string>(type: "text", nullable: false),
                    amount_minor = table.Column<string>(type: "text", nullable: false),
                    amount_scale = table.Column<int>(type: "integer", nullable: false),
                    amount_text = table.Column<string>(type: "text", nullable: false),
                    asset_code = table.Column<string>(type: "text", nullable: false),
                    raw_provenance = table.Column<string>(type: "text", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_blockchain_observations", x => x.id);
                    table.UniqueConstraint("AK_blockchain_observations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_observation_log", "log_index >= 0 AND chain_id > 0");
                    table.CheckConstraint("ck_observation_minor", "amount_minor ~ '^(0|[1-9][0-9]{0,37})$'");
                    table.CheckConstraint("ck_observation_scale", "amount_scale BETWEEN 0 AND 18");
                    table.ForeignKey(
                        name: "FK_blockchain_observations_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "merchants",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchants", x => x.id);
                    table.UniqueConstraint("AK_merchants_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_merchants_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provider_accounts",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    environment_name = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_accounts", x => x.id);
                    table.UniqueConstraint("AK_provider_accounts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_provider_accounts_providers_provider_id",
                        column: x => x.provider_id,
                        principalSchema: "providers",
                        principalTable: "providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_provider_accounts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "routing_policy_versions",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_policy_versions", x => x.id);
                    table.UniqueConstraint("AK_routing_policy_versions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_routing_policy_versions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "observation_evidence",
                schema: "monitoring",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lifecycle = table.Column<string>(type: "text", nullable: false),
                    block_hash = table.Column<string>(type: "text", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observation_evidence", x => x.id);
                    table.ForeignKey(
                        name: "FK_observation_evidence_blockchain_observations_tenant_id_obse~",
                        columns: x => new { x.tenant_id, x.observation_id },
                        principalSchema: "monitoring",
                        principalTable: "blockchain_observations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credentials",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    environment_name = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: false),
                    verifier_hash = table.Column<string>(type: "text", nullable: false),
                    scopes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    rotated_from_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credentials", x => x.id);
                    table.ForeignKey(
                        name: "FK_credentials_merchants_tenant_id_merchant_id",
                        columns: x => new { x.tenant_id, x.merchant_id },
                        principalSchema: "identity",
                        principalTable: "merchants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credentials_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "identity",
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "merchant_policy_versions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    document = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merchant_policy_versions", x => x.id);
                    table.UniqueConstraint("AK_merchant_policy_versions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_merchant_policy_versions_merchants_tenant_id_merchant_id",
                        columns: x => new { x.tenant_id, x.merchant_id },
                        principalSchema: "identity",
                        principalTable: "merchants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_intents",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_reference = table.Column<string>(type: "text", nullable: false),
                    acceptance_status = table.Column<string>(type: "text", nullable: false),
                    compliance_posture = table.Column<string>(type: "text", nullable: false),
                    settlement_posture = table.Column<string>(type: "text", nullable: false),
                    exception_flags = table.Column<string>(type: "text", nullable: false),
                    instructions_issued = table.Column<bool>(type: "boolean", nullable: false),
                    expiry_recorded = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_recorded = table.Column<bool>(type: "boolean", nullable: false),
                    any_recognition = table.Column<bool>(type: "boolean", nullable: false),
                    finality_met = table.Column<bool>(type: "boolean", nullable: false),
                    attempt_blocks = table.Column<bool>(type: "boolean", nullable: false),
                    reorganization_review = table.Column<bool>(type: "boolean", nullable: false),
                    obligation_minor = table.Column<string>(type: "text", nullable: false),
                    obligation_scale = table.Column<int>(type: "integer", nullable: false),
                    obligation_text = table.Column<string>(type: "text", nullable: false),
                    obligation_asset = table.Column<string>(type: "text", nullable: false),
                    quoted_fiat_minor = table.Column<string>(type: "text", nullable: true),
                    quoted_fiat_scale = table.Column<int>(type: "integer", nullable: true),
                    quoted_fiat_text = table.Column<string>(type: "text", nullable: true),
                    quoted_fiat_currency = table.Column<string>(type: "text", nullable: true),
                    quote_reference = table.Column<string>(type: "text", nullable: true),
                    quote_policy_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recognized_minor = table.Column<string>(type: "text", nullable: false),
                    recognized_scale = table.Column<int>(type: "integer", nullable: false),
                    recognized_text = table.Column<string>(type: "text", nullable: false),
                    recognized_asset = table.Column<string>(type: "text", nullable: true),
                    recognized_chain_id = table.Column<long>(type: "bigint", nullable: true),
                    recognized_contract = table.Column<string>(type: "text", nullable: true),
                    tolerance_minor = table.Column<string>(type: "text", nullable: false),
                    tolerance_scale = table.Column<int>(type: "integer", nullable: false),
                    tolerance_policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_intents", x => x.id);
                    table.UniqueConstraint("AK_payment_intents_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_intent_minor", "obligation_minor ~ '^(0|[1-9][0-9]{0,37})$' AND recognized_minor ~ '^(0|[1-9][0-9]{0,37})$' AND tolerance_minor ~ '^(0|[1-9][0-9]{0,37})$' AND (quoted_fiat_minor IS NULL OR quoted_fiat_minor ~ '^(0|[1-9][0-9]{0,37})$')");
                    table.CheckConstraint("ck_intent_quote", "(quoted_fiat_minor IS NULL AND quoted_fiat_scale IS NULL AND quoted_fiat_text IS NULL AND quoted_fiat_currency IS NULL AND quote_reference IS NULL) OR (quoted_fiat_minor IS NOT NULL AND quoted_fiat_scale IS NOT NULL AND quoted_fiat_text IS NOT NULL AND quoted_fiat_currency IS NOT NULL AND quote_reference IS NOT NULL)");
                    table.CheckConstraint("ck_intent_scales", "obligation_scale BETWEEN 0 AND 18 AND recognized_scale BETWEEN 0 AND 18 AND tolerance_scale BETWEEN 0 AND 18 AND (quoted_fiat_scale IS NULL OR quoted_fiat_scale BETWEEN 0 AND 18)");
                    table.ForeignKey(
                        name: "FK_payment_intents_merchant_policy_versions_tenant_id_toleranc~",
                        columns: x => new { x.tenant_id, x.tolerance_policy_version_id },
                        principalSchema: "identity",
                        principalTable: "merchant_policy_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_intents_merchants_tenant_id_merchant_id",
                        columns: x => new { x.tenant_id, x.merchant_id },
                        principalSchema: "identity",
                        principalTable: "merchants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "acceptance_decisions",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_acceptance_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_acceptance_decisions_payment_intents_tenant_id_payment_inte~",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compliance_decisions",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posture = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fresh_until = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    case_reference = table.Column<string>(type: "text", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_decisions", x => x.id);
                    table.ForeignKey(
                        name: "FK_compliance_decisions_merchant_policy_versions_tenant_id_pol~",
                        columns: x => new { x.tenant_id, x.policy_version_id },
                        principalSchema: "identity",
                        principalTable: "merchant_policy_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compliance_decisions_merchants_tenant_id_merchant_id",
                        columns: x => new { x.tenant_id, x.merchant_id },
                        principalSchema: "identity",
                        principalTable: "merchants",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_compliance_decisions_payment_intents_tenant_id_payment_inte~",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "routing_decisions",
                schema: "providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inputs = table.Column<string>(type: "text", nullable: false),
                    candidates = table.Column<string>(type: "text", nullable: false),
                    selected_provider_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routing_decisions", x => x.id);
                    table.UniqueConstraint("AK_routing_decisions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_routing_decisions_payment_intents_tenant_id_payment_intent_~",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_routing_decisions_provider_accounts_tenant_id_selected_prov~",
                        columns: x => new { x.tenant_id, x.selected_provider_account_id },
                        principalSchema: "providers",
                        principalTable: "provider_accounts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_routing_decisions_routing_policy_versions_tenant_id_policy_~",
                        columns: x => new { x.tenant_id, x.policy_version_id },
                        principalSchema: "providers",
                        principalTable: "routing_policy_versions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "settlement_records",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_reference = table.Column<string>(type: "text", nullable: false),
                    payload_sha256 = table.Column<string>(type: "text", nullable: false),
                    amount_minor = table.Column<string>(type: "text", nullable: false),
                    amount_scale = table.Column<int>(type: "integer", nullable: false),
                    amount_text = table.Column<string>(type: "text", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement_records", x => x.id);
                    table.UniqueConstraint("AK_settlement_records_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_settlement_minor", "amount_minor ~ '^(0|[1-9][0-9]{0,37})$'");
                    table.CheckConstraint("ck_settlement_scale", "amount_scale BETWEEN 0 AND 18");
                    table.ForeignKey(
                        name: "FK_settlement_records_payment_intents_tenant_id_payment_intent~",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_settlement_records_provider_accounts_tenant_id_provider_acc~",
                        columns: x => new { x.tenant_id, x.provider_account_id },
                        principalSchema: "providers",
                        principalTable: "provider_accounts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "status_edges",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "text", nullable: false),
                    to_status = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_status_edges", x => x.id);
                    table.ForeignKey(
                        name: "FK_status_edges_payment_intents_tenant_id_payment_intent_id",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_events",
                schema: "webhooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_events", x => x.id);
                    table.UniqueConstraint("AK_webhook_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_webhook_events_payment_intents_tenant_id_payment_intent_id",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_options",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_code = table.Column<string>(type: "text", nullable: false),
                    chain_id = table.Column<long>(type: "bigint", nullable: false),
                    token_contract = table.Column<string>(type: "text", nullable: false),
                    destination = table.Column<string>(type: "text", nullable: false),
                    provider_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    instructions = table.Column<string>(type: "text", nullable: true),
                    instructions_issued_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    route_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    amount_minor = table.Column<string>(type: "text", nullable: false),
                    amount_scale = table.Column<int>(type: "integer", nullable: false),
                    amount_text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_options", x => x.id);
                    table.UniqueConstraint("AK_payment_options_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_option_minor", "amount_minor ~ '^(0|[1-9][0-9]{0,37})$'");
                    table.CheckConstraint("ck_option_scale", "amount_scale BETWEEN 0 AND 18");
                    table.ForeignKey(
                        name: "FK_payment_options_payment_intents_tenant_id_payment_intent_id",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_options_provider_accounts_tenant_id_provider_accoun~",
                        columns: x => new { x.tenant_id, x.provider_account_id },
                        principalSchema: "providers",
                        principalTable: "provider_accounts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_options_routing_decisions_tenant_id_route_decision_~",
                        columns: x => new { x.tenant_id, x.route_decision_id },
                        principalSchema: "providers",
                        principalTable: "routing_decisions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_records",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settlement_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posture = table.Column<string>(type: "text", nullable: false),
                    financial_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reconciliation_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_reconciliation_records_payment_intents_tenant_id_payment_in~",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reconciliation_records_settlement_records_tenant_id_settlem~",
                        columns: x => new { x.tenant_id, x.settlement_record_id },
                        principalSchema: "finance",
                        principalTable: "settlement_records",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_deliveries",
                schema: "webhooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "FK_webhook_deliveries_webhook_events_tenant_id_webhook_event_id",
                        columns: x => new { x.tenant_id, x.webhook_event_id },
                        principalSchema: "webhooks",
                        principalTable: "webhook_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "financial_events",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    merchant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_intent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    observation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    affects_recognized_value = table.Column<bool>(type: "boolean", nullable: false),
                    amount_minor = table.Column<string>(type: "text", nullable: false),
                    amount_scale = table.Column<int>(type: "integer", nullable: false),
                    amount_text = table.Column<string>(type: "text", nullable: false),
                    asset_code = table.Column<string>(type: "text", nullable: true),
                    chain_id = table.Column<long>(type: "bigint", nullable: true),
                    token_contract = table.Column<string>(type: "text", nullable: true),
                    compensates_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    classification = table.Column<string>(type: "text", nullable: true),
                    policy_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_events", x => x.id);
                    table.UniqueConstraint("AK_financial_events_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_event_effect", "(kind = 'recognition' AND affects_recognized_value AND amount_minor::numeric > 0) OR (kind = 'compensation' AND affects_recognized_value AND amount_minor::numeric < 0) OR (kind = 'unallocated' AND NOT affects_recognized_value AND amount_minor::numeric > 0) OR (kind = 'source_conflict' AND NOT affects_recognized_value)");
                    table.CheckConstraint("ck_event_minor", "amount_minor ~ '^-?(0|[1-9][0-9]{0,37})$'");
                    table.CheckConstraint("ck_event_scale", "amount_scale BETWEEN 0 AND 18");
                    table.ForeignKey(
                        name: "FK_financial_events_blockchain_observations_tenant_id_observat~",
                        columns: x => new { x.tenant_id, x.observation_id },
                        principalSchema: "monitoring",
                        principalTable: "blockchain_observations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_events_payment_intents_tenant_id_payment_intent_id",
                        columns: x => new { x.tenant_id, x.payment_intent_id },
                        principalSchema: "payments",
                        principalTable: "payment_intents",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_financial_events_payment_options_tenant_id_payment_option_id",
                        columns: x => new { x.tenant_id, x.payment_option_id },
                        principalSchema: "payments",
                        principalTable: "payment_options",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_attempts",
                schema: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "text", nullable: false),
                    correlation_reference = table.Column<string>(type: "text", nullable: false),
                    prepared_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_attempts", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_attempts_payment_options_tenant_id_payment_option_id",
                        columns: x => new { x.tenant_id, x.payment_option_id },
                        principalSchema: "payments",
                        principalTable: "payment_options",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "effect_sources",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    financial_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_kind = table.Column<string>(type: "text", nullable: false),
                    source_key = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_effect_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_effect_sources_financial_events_tenant_id_financial_event_id",
                        columns: x => new { x.tenant_id, x.financial_event_id },
                        principalSchema: "finance",
                        principalTable: "financial_events",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_acceptance_decisions_tenant_id_payment_intent_id_kind",
                schema: "payments",
                table: "acceptance_decisions",
                columns: new[] { "tenant_id", "payment_intent_id", "kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_tenant_id",
                schema: "identity",
                table: "audit_events",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_blockchain_observations_chain_id_transaction_hash_log_index",
                schema: "monitoring",
                table: "blockchain_observations",
                columns: new[] { "chain_id", "transaction_hash", "log_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_compliance_decisions_tenant_id_merchant_id",
                schema: "compliance",
                table: "compliance_decisions",
                columns: new[] { "tenant_id", "merchant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_decisions_tenant_id_payment_intent_id",
                schema: "compliance",
                table: "compliance_decisions",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compliance_decisions_tenant_id_policy_version_id",
                schema: "compliance",
                table: "compliance_decisions",
                columns: new[] { "tenant_id", "policy_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_credentials_tenant_id_merchant_id",
                schema: "identity",
                table: "credentials",
                columns: new[] { "tenant_id", "merchant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_effect_sources_source_kind_source_key",
                schema: "finance",
                table: "effect_sources",
                columns: new[] { "source_kind", "source_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_effect_sources_tenant_id_financial_event_id",
                schema: "finance",
                table: "effect_sources",
                columns: new[] { "tenant_id", "financial_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_events_compensates_event_id",
                schema: "finance",
                table: "financial_events",
                column: "compensates_event_id",
                unique: true,
                filter: "kind = 'compensation'");

            migrationBuilder.CreateIndex(
                name: "IX_financial_events_observation_id",
                schema: "finance",
                table: "financial_events",
                column: "observation_id",
                unique: true,
                filter: "kind IN ('recognition', 'unallocated') AND observation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_financial_events_tenant_id_observation_id",
                schema: "finance",
                table: "financial_events",
                columns: new[] { "tenant_id", "observation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_events_tenant_id_payment_intent_id",
                schema: "finance",
                table: "financial_events",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_financial_events_tenant_id_payment_option_id",
                schema: "finance",
                table: "financial_events",
                columns: new[] { "tenant_id", "payment_option_id" });

            migrationBuilder.CreateIndex(
                name: "IX_merchant_policy_versions_tenant_id_merchant_id",
                schema: "identity",
                table: "merchant_policy_versions",
                columns: new[] { "tenant_id", "merchant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_observation_evidence_tenant_id_observation_id",
                schema: "monitoring",
                table: "observation_evidence",
                columns: new[] { "tenant_id", "observation_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_attempts_tenant_id_payment_option_id",
                schema: "payments",
                table: "payment_attempts",
                columns: new[] { "tenant_id", "payment_option_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_tenant_id_merchant_id",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "tenant_id", "merchant_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_intents_tenant_id_tolerance_policy_version_id",
                schema: "payments",
                table: "payment_intents",
                columns: new[] { "tenant_id", "tolerance_policy_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_options_tenant_id_payment_intent_id",
                schema: "payments",
                table: "payment_options",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_options_tenant_id_provider_account_id",
                schema: "payments",
                table: "payment_options",
                columns: new[] { "tenant_id", "provider_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_options_tenant_id_route_decision_id",
                schema: "payments",
                table: "payment_options",
                columns: new[] { "tenant_id", "route_decision_id" });

            migrationBuilder.CreateIndex(
                name: "IX_provider_accounts_provider_id",
                schema: "providers",
                table: "provider_accounts",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_records_tenant_id_payment_intent_id",
                schema: "finance",
                table: "reconciliation_records",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_reconciliation_records_tenant_id_settlement_record_id",
                schema: "finance",
                table: "reconciliation_records",
                columns: new[] { "tenant_id", "settlement_record_id" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_decisions_tenant_id_payment_intent_id",
                schema: "providers",
                table: "routing_decisions",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_decisions_tenant_id_policy_version_id",
                schema: "providers",
                table: "routing_decisions",
                columns: new[] { "tenant_id", "policy_version_id" });

            migrationBuilder.CreateIndex(
                name: "IX_routing_decisions_tenant_id_selected_provider_account_id",
                schema: "providers",
                table: "routing_decisions",
                columns: new[] { "tenant_id", "selected_provider_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_records_provider_account_id_provider_reference",
                schema: "finance",
                table: "settlement_records",
                columns: new[] { "provider_account_id", "provider_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_settlement_records_tenant_id_payment_intent_id",
                schema: "finance",
                table: "settlement_records",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_records_tenant_id_provider_account_id",
                schema: "finance",
                table: "settlement_records",
                columns: new[] { "tenant_id", "provider_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_status_edges_tenant_id_payment_intent_id",
                schema: "payments",
                table: "status_edges",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_tenant_id_webhook_event_id",
                schema: "webhooks",
                table: "webhook_deliveries",
                columns: new[] { "tenant_id", "webhook_event_id" });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_deliveries_webhook_event_id_attempt_number",
                schema: "webhooks",
                table: "webhook_deliveries",
                columns: new[] { "webhook_event_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_events_tenant_id_payment_intent_id",
                schema: "webhooks",
                table: "webhook_events",
                columns: new[] { "tenant_id", "payment_intent_id" });

            migrationBuilder.Sql(JournalSecurity.ApplySql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "acceptance_decisions",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "compliance_decisions",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "credentials",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "effect_sources",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "observation_evidence",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "payment_attempts",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "reconciliation_records",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "status_edges",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "webhook_deliveries",
                schema: "webhooks");

            migrationBuilder.DropTable(
                name: "financial_events",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "settlement_records",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "webhook_events",
                schema: "webhooks");

            migrationBuilder.DropTable(
                name: "blockchain_observations",
                schema: "monitoring");

            migrationBuilder.DropTable(
                name: "payment_options",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "routing_decisions",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "payment_intents",
                schema: "payments");

            migrationBuilder.DropTable(
                name: "provider_accounts",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "routing_policy_versions",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "merchant_policy_versions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "providers",
                schema: "providers");

            migrationBuilder.DropTable(
                name: "merchants",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "tenants",
                schema: "identity");

            migrationBuilder.Sql(
                """
                DROP FUNCTION IF EXISTS platform.reject_evidence_mutation();
                DROP FUNCTION IF EXISTS platform.reject_row_delete();
                DROP FUNCTION IF EXISTS payments.reject_forbidden_acceptance_transition();
                DROP FUNCTION IF EXISTS payments.reject_payment_option_mutation();
                DROP FUNCTION IF EXISTS payments.reject_attempt_mutation();
                DROP FUNCTION IF EXISTS identity.reject_credential_rewrite();
                """);
        }
    }
}
