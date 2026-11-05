namespace StableRail.Infrastructure.Persistence;

public static class JournalSecurity
{
    public const string ApplicationRole = "stablerail_app";

    public static readonly string[] ImmutableTables =
    [
        "identity.audit_events",
        "identity.merchant_policy_versions",
        "providers.routing_policy_versions",
        "providers.routing_decisions",
        "payments.acceptance_decisions",
        "payments.status_edges",
        "monitoring.blockchain_observations",
        "monitoring.observation_evidence",
        "finance.financial_events",
        "finance.effect_sources",
        "finance.settlement_records",
        "finance.reconciliation_records",
        "compliance.compliance_decisions",
        "webhooks.webhook_events",
        "webhooks.webhook_deliveries",
    ];

    public const string ApplySql =
        """
        DO $role$
        BEGIN
          IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'stablerail_app') THEN
            CREATE ROLE stablerail_app NOLOGIN;
          END IF;
        END
        $role$;

        DO $connect$
        BEGIN
          EXECUTE format('GRANT CONNECT ON DATABASE %I TO stablerail_app', current_database());
        END
        $connect$;

        GRANT USAGE ON SCHEMA platform, identity, payments, providers, monitoring, finance, compliance, webhooks TO stablerail_app;
        GRANT SELECT ON platform.module_ownership TO stablerail_app;

        GRANT SELECT, INSERT, UPDATE ON
          identity.tenants,
          identity.merchants,
          identity.credentials,
          providers.providers,
          providers.provider_accounts,
          payments.payment_intents,
          payments.payment_options,
          payments.payment_attempts
        TO stablerail_app;

        GRANT SELECT, INSERT ON
          identity.audit_events,
          identity.merchant_policy_versions,
          providers.routing_policy_versions,
          providers.routing_decisions,
          payments.acceptance_decisions,
          payments.status_edges,
          monitoring.blockchain_observations,
          monitoring.observation_evidence,
          finance.financial_events,
          finance.effect_sources,
          finance.settlement_records,
          finance.reconciliation_records,
          compliance.compliance_decisions,
          webhooks.webhook_events,
          webhooks.webhook_deliveries
        TO stablerail_app;

        CREATE OR REPLACE FUNCTION platform.reject_evidence_mutation()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $body$
        BEGIN
          RAISE EXCEPTION 'immutable evidence cannot be updated or deleted'
            USING ERRCODE = '42501';
        END
        $body$;

        CREATE OR REPLACE FUNCTION payments.reject_forbidden_acceptance_transition()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $body$
        BEGIN
          IF NEW.id IS DISTINCT FROM OLD.id
            OR NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
            OR NEW.merchant_id IS DISTINCT FROM OLD.merchant_id
            OR NEW.merchant_reference IS DISTINCT FROM OLD.merchant_reference
            OR NEW.obligation_minor IS DISTINCT FROM OLD.obligation_minor
            OR NEW.obligation_scale IS DISTINCT FROM OLD.obligation_scale
            OR NEW.obligation_text IS DISTINCT FROM OLD.obligation_text
            OR NEW.obligation_asset IS DISTINCT FROM OLD.obligation_asset
            OR NEW.quoted_fiat_minor IS DISTINCT FROM OLD.quoted_fiat_minor
            OR NEW.quoted_fiat_scale IS DISTINCT FROM OLD.quoted_fiat_scale
            OR NEW.quoted_fiat_text IS DISTINCT FROM OLD.quoted_fiat_text
            OR NEW.quoted_fiat_currency IS DISTINCT FROM OLD.quoted_fiat_currency
            OR NEW.quote_reference IS DISTINCT FROM OLD.quote_reference
            OR NEW.quote_policy_version_id IS DISTINCT FROM OLD.quote_policy_version_id
            OR NEW.tolerance_minor IS DISTINCT FROM OLD.tolerance_minor
            OR NEW.tolerance_scale IS DISTINCT FROM OLD.tolerance_scale
            OR NEW.tolerance_policy_version_id IS DISTINCT FROM OLD.tolerance_policy_version_id
            OR NEW.expires_at IS DISTINCT FROM OLD.expires_at
            OR NEW.created_at IS DISTINCT FROM OLD.created_at
          THEN
            RAISE EXCEPTION 'payment commercial terms cannot be rewritten' USING ERRCODE = '42501';
          END IF;
          IF OLD.acceptance_status IS NOT DISTINCT FROM NEW.acceptance_status THEN
            RETURN NEW;
          END IF;
          IF (OLD.acceptance_status, NEW.acceptance_status) IN (
            ('created', 'requires_payment'),
            ('created', 'expired'),
            ('created', 'cancelled'),
            ('requires_payment', 'detected'),
            ('requires_payment', 'confirming'),
            ('requires_payment', 'succeeded'),
            ('requires_payment', 'expired'),
            ('requires_payment', 'cancelled'),
            ('detected', 'confirming'),
            ('detected', 'succeeded'),
            ('detected', 'expired'),
            ('confirming', 'succeeded'),
            ('confirming', 'detected'),
            ('succeeded', 'confirming')
          ) THEN
            RETURN NEW;
          END IF;
          RAISE EXCEPTION 'forbidden acceptance transition from % to %', OLD.acceptance_status, NEW.acceptance_status
            USING ERRCODE = '42501';
        END
        $body$;

        CREATE OR REPLACE FUNCTION payments.reject_payment_option_mutation()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $body$
        BEGIN
          IF TG_OP = 'DELETE' THEN
            RAISE EXCEPTION 'payment options cannot be deleted' USING ERRCODE = '42501';
          END IF;
          IF OLD.status IS DISTINCT FROM NEW.status AND (OLD.status, NEW.status) NOT IN (
            ('draft', 'issuing'),
            ('draft', 'issued'),
            ('draft', 'issuance_unknown'),
            ('issuing', 'issued'),
            ('issuing', 'issuance_unknown'),
            ('issuance_unknown', 'issued')
          ) THEN
            RAISE EXCEPTION 'forbidden option status transition' USING ERRCODE = '42501';
          END IF;
          IF OLD.status = 'issued' AND (
            NEW.provider_account_id IS DISTINCT FROM OLD.provider_account_id
            OR NEW.token_contract IS DISTINCT FROM OLD.token_contract
            OR NEW.chain_id IS DISTINCT FROM OLD.chain_id
            OR NEW.destination IS DISTINCT FROM OLD.destination
            OR NEW.asset_code IS DISTINCT FROM OLD.asset_code
            OR NEW.amount_minor IS DISTINCT FROM OLD.amount_minor
            OR NEW.amount_scale IS DISTINCT FROM OLD.amount_scale
            OR NEW.amount_text IS DISTINCT FROM OLD.amount_text
            OR NEW.route_decision_id IS DISTINCT FROM OLD.route_decision_id
            OR NEW.instructions IS DISTINCT FROM OLD.instructions
            OR NEW.instructions_issued_at IS DISTINCT FROM OLD.instructions_issued_at
            OR NEW.payment_intent_id IS DISTINCT FROM OLD.payment_intent_id
            OR NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
            OR NEW.id IS DISTINCT FROM OLD.id
          ) THEN
            RAISE EXCEPTION 'issued payment instructions cannot be replaced' USING ERRCODE = '42501';
          END IF;
          RETURN NEW;
        END
        $body$;

        CREATE OR REPLACE FUNCTION payments.reject_attempt_mutation()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $body$
        BEGIN
          IF TG_OP = 'DELETE' THEN
            RAISE EXCEPTION 'payment attempts cannot be deleted' USING ERRCODE = '42501';
          END IF;
          IF NEW.id IS DISTINCT FROM OLD.id
            OR NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
            OR NEW.payment_option_id IS DISTINCT FROM OLD.payment_option_id
            OR NEW.correlation_reference IS DISTINCT FROM OLD.correlation_reference
            OR NEW.prepared_at IS DISTINCT FROM OLD.prepared_at
          THEN
            RAISE EXCEPTION 'payment attempt identity cannot be rewritten' USING ERRCODE = '42501';
          END IF;
          IF OLD.outcome IS DISTINCT FROM NEW.outcome AND (OLD.outcome, NEW.outcome) NOT IN (
            ('prepared', 'failed_before_commit'),
            ('prepared', 'known_not_committed'),
            ('prepared', 'committed'),
            ('prepared', 'unknown'),
            ('unknown', 'committed'),
            ('unknown', 'known_not_committed')
          ) THEN
            RAISE EXCEPTION 'forbidden attempt outcome' USING ERRCODE = '42501';
          END IF;
          RETURN NEW;
        END
        $body$;

        CREATE OR REPLACE FUNCTION identity.reject_credential_rewrite()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $body$
        BEGIN
          IF NEW.id IS DISTINCT FROM OLD.id
            OR NEW.tenant_id IS DISTINCT FROM OLD.tenant_id
            OR NEW.merchant_id IS DISTINCT FROM OLD.merchant_id
            OR NEW.environment_name IS DISTINCT FROM OLD.environment_name
            OR NEW.verifier_hash IS DISTINCT FROM OLD.verifier_hash
            OR NEW.created_at IS DISTINCT FROM OLD.created_at
            OR NEW.rotated_from_id IS DISTINCT FROM OLD.rotated_from_id
          THEN
            RAISE EXCEPTION 'credential verifiers cannot be rewritten' USING ERRCODE = '42501';
          END IF;
          RETURN NEW;
        END
        $body$;

        CREATE OR REPLACE FUNCTION platform.reject_row_delete()
        RETURNS trigger
        LANGUAGE plpgsql
        AS $body$
        BEGIN
          RAISE EXCEPTION 'business rows cannot be deleted' USING ERRCODE = '42501';
        END
        $body$;

        DO $triggers$
        DECLARE
          qualified text;
        BEGIN
          FOREACH qualified IN ARRAY ARRAY[
            'identity.audit_events',
            'identity.merchant_policy_versions',
            'providers.routing_policy_versions',
            'providers.routing_decisions',
            'payments.acceptance_decisions',
            'payments.status_edges',
            'monitoring.blockchain_observations',
            'monitoring.observation_evidence',
            'finance.financial_events',
            'finance.effect_sources',
            'finance.settlement_records',
            'finance.reconciliation_records',
            'compliance.compliance_decisions',
            'webhooks.webhook_events',
            'webhooks.webhook_deliveries'
          ]
          LOOP
            EXECUTE format('DROP TRIGGER IF EXISTS reject_evidence_mutation ON %s', qualified);
            EXECUTE format(
              'CREATE TRIGGER reject_evidence_mutation BEFORE UPDATE OR DELETE ON %s FOR EACH ROW EXECUTE FUNCTION platform.reject_evidence_mutation()',
              qualified);
          END LOOP;
        END
        $triggers$;

        DROP TRIGGER IF EXISTS reject_forbidden_acceptance_transition ON payments.payment_intents;
        CREATE TRIGGER reject_forbidden_acceptance_transition
          BEFORE UPDATE ON payments.payment_intents
          FOR EACH ROW EXECUTE FUNCTION payments.reject_forbidden_acceptance_transition();
        DROP TRIGGER IF EXISTS reject_intent_delete ON payments.payment_intents;
        CREATE TRIGGER reject_intent_delete
          BEFORE DELETE ON payments.payment_intents
          FOR EACH ROW EXECUTE FUNCTION platform.reject_row_delete();

        DROP TRIGGER IF EXISTS reject_payment_option_mutation ON payments.payment_options;
        CREATE TRIGGER reject_payment_option_mutation
          BEFORE UPDATE OR DELETE ON payments.payment_options
          FOR EACH ROW EXECUTE FUNCTION payments.reject_payment_option_mutation();

        DROP TRIGGER IF EXISTS reject_attempt_mutation ON payments.payment_attempts;
        CREATE TRIGGER reject_attempt_mutation
          BEFORE UPDATE OR DELETE ON payments.payment_attempts
          FOR EACH ROW EXECUTE FUNCTION payments.reject_attempt_mutation();

        DROP TRIGGER IF EXISTS reject_credential_rewrite ON identity.credentials;
        CREATE TRIGGER reject_credential_rewrite
          BEFORE UPDATE ON identity.credentials
          FOR EACH ROW EXECUTE FUNCTION identity.reject_credential_rewrite();
        DROP TRIGGER IF EXISTS reject_credential_delete ON identity.credentials;
        CREATE TRIGGER reject_credential_delete
          BEFORE DELETE ON identity.credentials
          FOR EACH ROW EXECUTE FUNCTION platform.reject_row_delete();

        DROP TRIGGER IF EXISTS reject_tenant_delete ON identity.tenants;
        CREATE TRIGGER reject_tenant_delete BEFORE DELETE ON identity.tenants
          FOR EACH ROW EXECUTE FUNCTION platform.reject_row_delete();
        DROP TRIGGER IF EXISTS reject_merchant_delete ON identity.merchants;
        CREATE TRIGGER reject_merchant_delete BEFORE DELETE ON identity.merchants
          FOR EACH ROW EXECUTE FUNCTION platform.reject_row_delete();
        DROP TRIGGER IF EXISTS reject_provider_delete ON providers.providers;
        CREATE TRIGGER reject_provider_delete BEFORE DELETE ON providers.providers
          FOR EACH ROW EXECUTE FUNCTION platform.reject_row_delete();
        DROP TRIGGER IF EXISTS reject_provider_account_delete ON providers.provider_accounts;
        CREATE TRIGGER reject_provider_account_delete BEFORE DELETE ON providers.provider_accounts
          FOR EACH ROW EXECUTE FUNCTION platform.reject_row_delete();

        ALTER TABLE payments.payment_intents
          ADD COLUMN IF NOT EXISTS obligation_units numeric(38,0) GENERATED ALWAYS AS (obligation_minor::numeric(38,0)) STORED,
          ADD COLUMN IF NOT EXISTS recognized_units numeric(38,0) GENERATED ALWAYS AS (recognized_minor::numeric(38,0)) STORED,
          ADD COLUMN IF NOT EXISTS tolerance_units numeric(38,0) GENERATED ALWAYS AS (tolerance_minor::numeric(38,0)) STORED,
          ADD COLUMN IF NOT EXISTS quoted_fiat_units numeric(38,0) GENERATED ALWAYS AS (
            CASE WHEN quoted_fiat_minor IS NULL THEN NULL ELSE quoted_fiat_minor::numeric(38,0) END
          ) STORED;
        ALTER TABLE payments.payment_options
          ADD COLUMN IF NOT EXISTS amount_units numeric(38,0) GENERATED ALWAYS AS (amount_minor::numeric(38,0)) STORED;
        ALTER TABLE monitoring.blockchain_observations
          ADD COLUMN IF NOT EXISTS amount_units numeric(38,0) GENERATED ALWAYS AS (amount_minor::numeric(38,0)) STORED;
        ALTER TABLE finance.financial_events
          ADD COLUMN IF NOT EXISTS amount_units numeric(38,0) GENERATED ALWAYS AS (amount_minor::numeric(38,0)) STORED;
        ALTER TABLE finance.settlement_records
          ADD COLUMN IF NOT EXISTS amount_units numeric(38,0) GENERATED ALWAYS AS (amount_minor::numeric(38,0)) STORED;
        """;
}
