-- =====================================================
-- Lottery Favorites & Entry Management - Database Migration
-- =====================================================
-- 
-- Purpose: Add indexes and views to support lottery favorites
--          and entry management features
--
-- Created: 2025-01-XX
-- Agent: BE Agent (Agent 1)
-- Task: BE-1.1 - Database Schema Extensions
--
-- Database Schemas:
--   - amesa_auth: users, user_preferences (uses lowercase column names for user_id)
--   - amesa_lottery: houses, lottery_tickets (uses PascalCase column names)
--
-- Note: user_preferences uses lowercase (user_id), other tables use PascalCase ("UserId")
-- =====================================================
-- PREREQUISITES
-- =====================================================
-- 
-- This migration assumes:
-- 1. Tables exist in correct schemas:
--    - amesa_lottery.lottery_tickets (with "UserId", "Status", "HouseId", "PurchasePrice", "PurchaseDate", "IsWinner")
--    - amesa_lottery.houses (with "Status", "Location", "Id")
--    - amesa_auth.user_preferences (with user_id lowercase, preferences_json)
--    - amesa_auth.users (with "Id")
-- 2. PostgreSQL version 12+ (for JSONB GIN index support)
--
-- =====================================================
-- INDEXES FOR PERFORMANCE OPTIMIZATION
-- =====================================================

-- Composite index for user's lottery tickets filtered by status
-- Used for: GET /api/v1/tickets/active endpoint
-- Improves: Queries filtering tickets by user_id and status
CREATE INDEX IF NOT EXISTS idx_lottery_tickets_user_status 
ON amesa_lottery.lottery_tickets("UserId", "Status");

-- Composite index for houses filtered by status and location
-- Used for: GET /api/v1/houses/recommendations endpoint
-- Improves: Recommendation queries filtering by status and location
CREATE INDEX IF NOT EXISTS idx_houses_status_location 
ON amesa_lottery.houses("Status", "Location");

-- GIN index for JSONB lottery preferences in user_preferences
-- Used for: Querying lottery preferences stored in JSONB
-- Improves: JSONB queries on preferences_json->'lotteryPreferences'
-- Note: Column name is preferences_json (not preferences)
CREATE INDEX IF NOT EXISTS idx_user_preferences_lottery_gin 
ON amesa_auth.user_preferences USING GIN ((preferences_json->'lotteryPreferences'));

-- =====================================================
-- VIEW: amesa_auth.user_lottery_dashboard
-- =====================================================
-- 
-- Purpose: Aggregated view for user lottery dashboard data
-- Provides: Favorites count, active entries, stats, recent activity
--

CREATE OR REPLACE VIEW amesa_auth.user_lottery_dashboard AS
SELECT 
    u."Id" AS user_id,
    
    -- Favorites Statistics
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_auth.user_preferences up
        WHERE up.user_id = u."Id"
        AND up.preferences_json->'lotteryPreferences'->'favoriteHouseIds' IS NOT NULL
        AND jsonb_array_length(up.preferences_json->'lotteryPreferences'->'favoriteHouseIds') > 0
    ) AS favorite_houses_count,
    
    -- Active Entries Statistics
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
        AND lt."Status" = 'active'
    ) AS active_entries_count,
    
    -- Total Entries Statistics
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
    ) AS total_entries_count,
    
    -- Total Wins
    (
        SELECT COUNT(*)::INTEGER
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
        AND lt."IsWinner" = true
    ) AS total_wins,
    
    -- Total Spending
    (
        SELECT COALESCE(SUM(lt."PurchasePrice"), 0)::DECIMAL(10,2)
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
    ) AS total_spending,
    
    -- Total Winnings
    (
        SELECT COALESCE(SUM(t."Amount"), 0)::DECIMAL(10,2)
        FROM amesa_payment.transactions t
        WHERE t."UserId" = u."Id"
        AND t."Type" = 'winning'
        AND t."Status" = 'completed'
    ) AS total_winnings,
    
    -- Win Rate Percentage
    CASE 
        WHEN (
            SELECT COUNT(*)::INTEGER
            FROM amesa_lottery.lottery_tickets lt
            WHERE lt."UserId" = u."Id"
        ) > 0 THEN
            ROUND(
                (
                    SELECT COUNT(*)::INTEGER
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                    AND lt."IsWinner" = true
                )::DECIMAL / 
                (
                    SELECT COUNT(*)::INTEGER
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                )::DECIMAL * 100, 
                2
            )
        ELSE 0
    END AS win_rate_percentage,
    
    -- Average Spending Per Entry
    CASE 
        WHEN (
            SELECT COUNT(*)::INTEGER
            FROM amesa_lottery.lottery_tickets lt
            WHERE lt."UserId" = u."Id"
        ) > 0 THEN
            ROUND(
                (
                    SELECT COALESCE(SUM(lt."PurchasePrice"), 0)
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                )::DECIMAL / 
                (
                    SELECT COUNT(*)::INTEGER
                    FROM amesa_lottery.lottery_tickets lt
                    WHERE lt."UserId" = u."Id"
                )::DECIMAL, 
                2
            )
        ELSE 0
    END AS average_spending_per_entry,
    
    -- Favorite House ID
    (
        SELECT h."Id"
        FROM amesa_lottery.lottery_tickets lt
        JOIN amesa_lottery.houses h ON lt."HouseId" = h."Id"
        WHERE lt."UserId" = u."Id"
        GROUP BY h."Id"
        ORDER BY COUNT(*) DESC
        LIMIT 1
    ) AS favorite_house_id,
    
    -- Most Active Month
    (
        SELECT TO_CHAR(lt."PurchaseDate", 'YYYY-MM')
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
        GROUP BY TO_CHAR(lt."PurchaseDate", 'YYYY-MM')
        ORDER BY COUNT(*) DESC
        LIMIT 1
    ) AS most_active_month,
    
    -- Last Entry Date
    (
        SELECT MAX(lt."PurchaseDate")
        FROM amesa_lottery.lottery_tickets lt
        WHERE lt."UserId" = u."Id"
    ) AS last_entry_date,
    
    -- Last Updated Timestamp
    CURRENT_TIMESTAMP AS last_updated

FROM amesa_auth.users u;

-- =====================================================
-- COMMENTS FOR DOCUMENTATION
-- =====================================================

COMMENT ON INDEX amesa_lottery.idx_lottery_tickets_user_status IS 
'Composite index for efficient queries filtering lottery tickets by user and status. Used by favorites and entry management features.';

COMMENT ON INDEX amesa_lottery.idx_houses_status_location IS 
'Composite index for efficient queries filtering houses by status and location. Used by recommendation engine.';

COMMENT ON INDEX amesa_auth.idx_user_preferences_lottery_gin IS 
'GIN index for efficient JSONB queries on lottery preferences. Enables fast lookups of favorite house IDs stored in user_preferences.preferences_json JSONB column.';

COMMENT ON VIEW amesa_auth.user_lottery_dashboard IS 
'Aggregated dashboard view providing comprehensive lottery statistics for each user. Includes favorites count, entry statistics, spending analytics, win rates, and activity metrics.';

-- =====================================================
-- VERIFICATION QUERIES (Run these after migration)
-- =====================================================

-- 1. Check indexes were created:
-- SELECT 
--     schemaname,
--     tablename,
--     indexname,
--     indexdef 
-- FROM pg_indexes 
-- WHERE schemaname IN ('amesa_lottery', 'amesa_auth')
-- AND indexname LIKE 'idx_%'
-- ORDER BY schemaname, tablename, indexname;

-- 2. Check view was created:
-- SELECT 
--     table_schema,
--     table_name,
--     view_definition
-- FROM information_schema.views
-- WHERE table_schema = 'amesa_auth'
-- AND table_name = 'user_lottery_dashboard';

-- 3. Test view with sample user:
-- SELECT * 
-- FROM amesa_auth.user_lottery_dashboard 
-- WHERE user_id = 'YOUR_USER_ID_HERE'::uuid;

-- =====================================================
-- ROLLBACK SCRIPT (if needed)
-- =====================================================
-- DROP VIEW IF EXISTS amesa_auth.user_lottery_dashboard;
-- DROP INDEX IF EXISTS amesa_auth.idx_user_preferences_lottery_gin;
-- DROP INDEX IF EXISTS amesa_lottery.idx_houses_status_location;
-- DROP INDEX IF EXISTS amesa_lottery.idx_lottery_tickets_user_status;
-- =====================================================
