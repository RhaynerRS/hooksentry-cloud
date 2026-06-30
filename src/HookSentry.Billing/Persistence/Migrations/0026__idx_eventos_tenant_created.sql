CREATE INDEX IF NOT EXISTS ix_eventos_tenant_created
    ON eventos (tenant_id, created_at);
