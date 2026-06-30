CREATE TABLE tenant_cloud_states (
    id           UUID         NOT NULL,
    tenant_id    UUID         NOT NULL,
    is_blocked   BOOLEAN      NOT NULL DEFAULT FALSE,
    blocked_at   TIMESTAMPTZ  NULL,
    block_reason VARCHAR(100) NULL,

    CONSTRAINT pk_tenant_cloud_states         PRIMARY KEY (id),
    CONSTRAINT uq_tenant_cloud_states_tenant  UNIQUE (tenant_id),
    CONSTRAINT fk_tenant_cloud_states_tenant  FOREIGN KEY (tenant_id) REFERENCES tenants (id)
);
