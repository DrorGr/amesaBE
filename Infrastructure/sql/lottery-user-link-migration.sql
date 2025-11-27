-- =====================================================
-- LOTTERY-USER LINK SYSTEM MIGRATION
-- =====================================================
-- Date: 2025-01-25
-- Purpose: Add participant caps and watchlist functionality
-- Database: amesa_prod
-- Schema: amesa_lottery
-- =====================================================
-- IMPORTANT: This is a MANUAL migration script.
-- Run this script manually on the production database.
-- DO NOT execute from application code.
-- =====================================================

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- 1. ADD max_participants COLUMN TO houses TABLE
-- =====================================================

ALTER TABLE amesa_lottery.houses 
ADD COLUMN IF NOT EXISTS max_participants INTEGER;

COMMENT ON COLUMN amesa_lottery.houses.max_participants IS 
'Maximum number of unique users allowed to participate. NULL = unlimited participants. Must be > 0 if set.';

-- =====================================================
-- 2. CREATE INDEXES FOR PERFORMANCE
-- =====================================================

CREATE INDEX IF NOT EXISTS idx_houses_status_participants 
ON amesa_lottery.houses("Status", max_participants) 
WHERE max_participants IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_lottery_tickets_house_user 
ON amesa_lottery.lottery_tickets("HouseId", "UserId") 
WHERE "Status" = 'Active';

CREATE INDEX IF NOT EXISTS idx_lottery_tickets_house_status 
ON amesa_lottery.lottery_tickets("HouseId", "Status") 
WHERE "Status" = 'Active';

-- =====================================================
-- 3. CREATE lottery_participants VIEW
-- =====================================================

DROP VIEW IF EXISTS amesa_lottery.lottery_participants;

CREATE VIEW amesa_lottery.lottery_participants AS
SELECT
    "HouseId" as house_id,
    COUNT(DISTINCT "UserId") as unique_participants,
    COUNT(*) as total_tickets,
    MAX("PurchaseDate") as last_entry_date
FROM amesa_lottery.lottery_tickets
WHERE "Status" = 'Active'
GROUP BY "HouseId";

COMMENT ON VIEW amesa_lottery.lottery_participants IS 
'Aggregated view of lottery participants per house. Counts only active tickets.';

-- =====================================================
-- 4. CREATE user_watchlist TABLE
-- =====================================================

CREATE TABLE IF NOT EXISTS amesa_lottery.user_watchlist (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    house_id UUID NOT NULL REFERENCES amesa_lottery.houses("Id") ON DELETE CASCADE,
    notification_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_user_watchlist_user_house UNIQUE (user_id, house_id)
);

COMMENT ON TABLE amesa_lottery.user_watchlist IS 
'User watchlist for tracking lotteries. Separate from favorites (stored in user_preferences JSONB).';

COMMENT ON COLUMN amesa_lottery.user_watchlist.user_id IS 
'References amesa_auth.users.Id (cross-schema, validated in application layer)';

COMMENT ON COLUMN amesa_lottery.user_watchlist.house_id IS 
'References amesa_lottery.houses.Id';

COMMENT ON COLUMN amesa_lottery.user_watchlist.notification_enabled IS 
'Whether user wants to receive notifications for this watchlist item';

-- =====================================================
-- 5. CREATE INDEXES FOR user_watchlist
-- =====================================================

CREATE INDEX IF NOT EXISTS idx_user_watchlist_user_id 
ON amesa_lottery.user_watchlist(user_id);

CREATE INDEX IF NOT EXISTS idx_user_watchlist_house_id 
ON amesa_lottery.user_watchlist(house_id);

-- =====================================================
-- Date: 2025-01-25
-- Purpose: Add participant caps and watchlist functionality
-- Database: amesa_prod
-- Schema: amesa_lottery
-- =====================================================
-- IMPORTANT: This is a MANUAL migration script.
-- Run this script manually on the production database.
-- DO NOT execute from application code.
-- =====================================================

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- =====================================================
-- 1. ADD max_participants COLUMN TO houses TABLE
-- =====================================================

ALTER TABLE amesa_lottery.houses 
ADD COLUMN IF NOT EXISTS max_participants INTEGER;

COMMENT ON COLUMN amesa_lottery.houses.max_participants IS 
'Maximum number of unique users allowed to participate. NULL = unlimited participants. Must be > 0 if set.';

-- =====================================================
-- 2. CREATE INDEXES FOR PERFORMANCE
-- =====================================================

CREATE INDEX IF NOT EXISTS idx_houses_status_participants 
ON amesa_lottery.houses("Status", max_participants) 
WHERE max_participants IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_lottery_tickets_house_user 
ON amesa_lottery.lottery_tickets("HouseId", "UserId") 
WHERE "Status" = 'Active';

CREATE INDEX IF NOT EXISTS idx_lottery_tickets_house_status 
ON amesa_lottery.lottery_tickets("HouseId", "Status") 
WHERE "Status" = 'Active';

-- =====================================================
-- 3. CREATE lottery_participants VIEW
-- =====================================================

DROP VIEW IF EXISTS amesa_lottery.lottery_participants;

CREATE VIEW amesa_lottery.lottery_participants AS
SELECT
    "HouseId" as house_id,
    COUNT(DISTINCT "UserId") as unique_participants,
    COUNT(*) as total_tickets,
    MAX("PurchaseDate") as last_entry_date
FROM amesa_lottery.lottery_tickets
WHERE "Status" = 'Active'
GROUP BY "HouseId";

COMMENT ON VIEW amesa_lottery.lottery_participants IS 
'Aggregated view of lottery participants per house. Counts only active tickets.';

-- =====================================================
-- 4. CREATE user_watchlist TABLE
-- =====================================================

CREATE TABLE IF NOT EXISTS amesa_lottery.user_watchlist (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL,
    house_id UUID NOT NULL REFERENCES amesa_lottery.houses("Id") ON DELETE CASCADE,
    notification_enabled BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_user_watchlist_user_house UNIQUE (user_id, house_id)
);

COMMENT ON TABLE amesa_lottery.user_watchlist IS 
'User watchlist for tracking lotteries. Separate from favorites (stored in user_preferences JSONB).';

COMMENT ON COLUMN amesa_lottery.user_watchlist.user_id IS 
'References amesa_auth.users.Id (cross-schema, validated in application layer)';

COMMENT ON COLUMN amesa_lottery.user_watchlist.house_id IS 
'References amesa_lottery.houses.Id';

COMMENT ON COLUMN amesa_lottery.user_watchlist.notification_enabled IS 
'Whether user wants to receive notifications for this watchlist item';

-- =====================================================
-- 5. CREATE INDEXES FOR user_watchlist
-- =====================================================

CREATE INDEX IF NOT EXISTS idx_user_watchlist_user_id 
ON amesa_lottery.user_watchlist(user_id);

CREATE INDEX IF NOT EXISTS idx_user_watchlist_house_id 
ON amesa_lottery.user_watchlist(house_id);
