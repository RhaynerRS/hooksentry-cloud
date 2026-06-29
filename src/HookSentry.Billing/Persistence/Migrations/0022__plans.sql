CREATE TABLE plans (
    id                   UUID         NOT NULL,
    name                 VARCHAR(100) NOT NULL,
    stripe_price_id      VARCHAR(255) NULL,
    price_monthly        NUMERIC(10,2) NOT NULL,
    max_users            INT          NOT NULL,
    max_destinations     INT          NOT NULL,
    max_events_per_month INT          NOT NULL,
    features             BIGINT       NOT NULL DEFAULT 0,
    is_active            BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ  NOT NULL,
    updated_at           TIMESTAMPTZ  NOT NULL,

    CONSTRAINT pk_plans      PRIMARY KEY (id),
    CONSTRAINT uq_plans_name UNIQUE (name)
);
