-- Puzyrik.Entitlements — Phase 1 schema (reference copy).
-- Applied automatically at startup by DbInitializer; kept here for psql/pgAdmin use.

CREATE TABLE IF NOT EXISTS entitlements (
    purchase_token TEXT        PRIMARY KEY,   -- Google Play purchaseToken; idempotency anchor
    user_id        TEXT        NOT NULL,
    product_id     TEXT        NOT NULL,
    order_id       TEXT,                       -- nullable: promo-code purchases have no orderId
    granted_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
    revoked_at     TIMESTAMPTZ                 -- set on refund/void (Phase 2)
);

CREATE INDEX IF NOT EXISTS ix_entitlements_user ON entitlements (user_id);
