CREATE TABLE usage_snapshots (
    tenant_id         UUID        NOT NULL,
    period_year_month VARCHAR(7)  NOT NULL,
    event_count       INT         NOT NULL DEFAULT 0,
    updated_at        TIMESTAMPTZ NOT NULL,

    CONSTRAINT pk_usage_snapshots        PRIMARY KEY (tenant_id, period_year_month),
    CONSTRAINT fk_usage_snapshots_tenant FOREIGN KEY (tenant_id) REFERENCES tenants (id)
);
