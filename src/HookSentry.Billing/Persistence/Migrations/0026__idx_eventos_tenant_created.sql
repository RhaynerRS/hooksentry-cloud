CREATE INDEX IF NOT EXISTS ix_eventos_tenant_created
    ON eventos (tenant_id, accepted_at);
