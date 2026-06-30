-- Remove billing-only plans; keep only free
DELETE FROM plans WHERE name != 'free';

-- Add retention_days (7 days default for existing free plan row)
ALTER TABLE plans ADD COLUMN IF NOT EXISTS retention_days INT NOT NULL DEFAULT 7;

-- Correct free plan limits to match spec (maxUsers=-1, maxDestinations=-1, maxEventsPerMonth=1000)
UPDATE plans
SET max_users = -1, max_destinations = -1, max_events_per_month = 1000
WHERE name = 'free';

-- Drop billing-specific columns
ALTER TABLE plans
    DROP COLUMN IF EXISTS stripe_price_id,
    DROP COLUMN IF EXISTS price_monthly,
    DROP COLUMN IF EXISTS features,
    DROP COLUMN IF EXISTS is_active;
