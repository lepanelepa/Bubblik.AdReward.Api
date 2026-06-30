-- Puzyrik.AdReward — Phase 1 schema (reference copy).
-- Applied automatically at API startup by DbInitializer; kept here for psql/pgAdmin use.

CREATE TABLE IF NOT EXISTS reward_grants (
    transaction_id  TEXT        PRIMARY KEY,   -- AdMob SSV transaction_id; idempotency anchor
    user_id         TEXT        NOT NULL,
    ad_unit         TEXT        NOT NULL,
    reward_item     TEXT        NOT NULL,
    reward_amount   INTEGER     NOT NULL,
    granted_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Phase 2 will add:
--   ad_events  (high-throughput impression/click ingestion via System.Threading.Channels)
--   ad_config  (remote frequency capping / A/B buckets / reward amounts)
