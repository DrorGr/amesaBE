-- ============================================================================
-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;

-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;

-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;

-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;

-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;

-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;

-- Database Verification Queries for Issues
-- ============================================================================
-- This script verifies all tables related to the 500 errors we've been fixing:
-- 1. Houses Favorites endpoints (/api/v1/houses/favorites)
-- 2. Promotions Available endpoint (/api/v1/promotions/available)
-- 3. Tickets Active/Analytics endpoints (/api/v1/tickets/active, /api/v1/tickets/analytics)
-- ============================================================================

-- ============================================================================
-- 1. VERIFY HOUSES FAVORITES TABLES
-- ============================================================================

-- Check if houses table exists and has required columns
SELECT 
    'HOUSES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'houses'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as columns
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
GROUP BY table_schema, table_name;

-- Verify critical columns for houses table
SELECT 
    'HOUSES COLUMNS' as check_type,
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'houses'
  AND column_name IN ('id', 'title', 'status', 'deleted_at', 'created_at', 'price', 'location', 'lottery_end_date')
ORDER BY ordinal_position;

-- Check house_images table
SELECT 
    'HOUSE_IMAGES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'house_images'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'house_images';

-- Verify user_preferences table (stores favorite house IDs in JSONB)
SELECT 
    'USER_PREFERENCES TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_auth' 
            AND table_name = 'user_preferences'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth' 
  AND table_name = 'user_preferences'
  AND column_name IN ('id', 'user_id', 'preferences_json', 'version')
ORDER BY ordinal_position;

-- Check user_watchlist table (alternative favorites mechanism)
SELECT 
    'USER_WATCHLIST TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'user_watchlist'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'user_watchlist';

-- ============================================================================
-- 2. VERIFY PROMOTIONS TABLES
-- ============================================================================

-- Check promotions table
SELECT 
    'PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'promotions'
  AND column_name IN ('id', 'code', 'title', 'is_active', 'start_date', 'end_date', 'usage_limit', 'usage_count', 'applicable_houses', 'created_at')
ORDER BY ordinal_position;

-- Check user_promotions table (tracks which promotions users have used)
SELECT 
    'USER_PROMOTIONS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_admin' 
            AND table_name = 'user_promotions'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_admin' 
  AND table_name = 'user_promotions'
  AND column_name IN ('id', 'user_id', 'promotion_id', 'used_at', 'transaction_id')
ORDER BY ordinal_position;

-- ============================================================================
-- 3. VERIFY TICKETS TABLES
-- ============================================================================

-- Check lottery_tickets table
SELECT 
    'LOTTERY_TICKETS TABLE CHECK' as check_type,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_schema = 'amesa_lottery' 
            AND table_name = 'lottery_tickets'
        ) THEN 'EXISTS' 
        ELSE 'MISSING' 
    END as table_status,
    column_name,
    data_type,
    is_nullable,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery' 
  AND table_name = 'lottery_tickets'
  AND column_name IN ('id', 'ticket_number', 'house_id', 'user_id', 'status', 'purchase_price', 'purchase_date', 'created_at')
ORDER BY ordinal_position;

-- ============================================================================
-- 4. DATA INTEGRITY CHECKS
-- ============================================================================

-- Check for houses with null status (should not exist based on our fixes)
SELECT 
    'HOUSES NULL STATUS CHECK' as check_type,
    COUNT(*) as null_status_count,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_houses
FROM amesa_lottery.houses;

-- Check for tickets with null status (should not exist based on our fixes)
SELECT 
    'TICKETS NULL STATUS CHECK' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status_tickets,
    COUNT(*) FILTER (WHERE status IS NOT NULL) as valid_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Check for houses with invalid status values
SELECT 
    'HOUSES STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.houses
GROUP BY status
ORDER BY count DESC;

-- Check for tickets with invalid status values
SELECT 
    'TICKETS STATUS VALUES CHECK' as check_type,
    status,
    COUNT(*) as count
FROM amesa_lottery.lottery_tickets
GROUP BY status
ORDER BY count DESC;

-- ============================================================================
-- 5. INDEX VERIFICATION
-- ============================================================================

-- Check indexes on houses table
SELECT 
    'HOUSES INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'houses'
ORDER BY indexname;

-- Check indexes on lottery_tickets table
SELECT 
    'TICKETS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery' 
  AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check indexes on promotions table
SELECT 
    'PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'promotions'
ORDER BY indexname;

-- Check indexes on user_promotions table
SELECT 
    'USER_PROMOTIONS INDEXES' as check_type,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename = 'user_promotions'
ORDER BY indexname;

-- ============================================================================
-- 6. FOREIGN KEY VERIFICATION
-- ============================================================================

-- Check foreign keys for houses
SELECT 
    'HOUSES FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'houses';

-- Check foreign keys for lottery_tickets
SELECT 
    'TICKETS FOREIGN KEYS' as check_type,
    tc.constraint_name,
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
  AND tc.table_schema = kcu.table_schema
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
  AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY'
  AND tc.table_schema = 'amesa_lottery'
  AND tc.table_name = 'lottery_tickets';

-- ============================================================================
-- 7. SAMPLE DATA CHECKS
-- ============================================================================

-- Sample houses data (check for null values in critical fields)
SELECT 
    'HOUSES SAMPLE DATA' as check_type,
    COUNT(*) as total_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NULL) as active_houses,
    COUNT(*) FILTER (WHERE deleted_at IS NOT NULL) as deleted_houses,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE title IS NULL OR title = '') as null_or_empty_title
FROM amesa_lottery.houses;

-- Sample tickets data
SELECT 
    'TICKETS SAMPLE DATA' as check_type,
    COUNT(*) as total_tickets,
    COUNT(*) FILTER (WHERE status IS NULL) as null_status,
    COUNT(*) FILTER (WHERE status = 'Active') as active_tickets,
    COUNT(*) FILTER (WHERE status != 'Active' AND status IS NOT NULL) as other_status_tickets
FROM amesa_lottery.lottery_tickets;

-- Sample promotions data
SELECT 
    'PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_promotions,
    COUNT(*) FILTER (WHERE is_active = true) as active_promotions,
    COUNT(*) FILTER (WHERE is_active = false) as inactive_promotions,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_or_empty_code
FROM amesa_admin.promotions;

-- Sample user_promotions data
SELECT 
    'USER_PROMOTIONS SAMPLE DATA' as check_type,
    COUNT(*) as total_user_promotions,
    COUNT(DISTINCT user_id) as unique_users,
    COUNT(DISTINCT promotion_id) as unique_promotions
FROM amesa_admin.user_promotions;

-- ============================================================================
-- 8. JSONB STRUCTURE CHECK FOR USER_PREFERENCES
-- ============================================================================

-- Check if user_preferences has valid JSONB structure for favorites
SELECT 
    'USER_PREFERENCES JSONB CHECK' as check_type,
    COUNT(*) as total_preferences,
    COUNT(*) FILTER (WHERE preferences_json IS NOT NULL) as has_json,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_structure
FROM amesa_auth.user_preferences;

-- Sample favorite house IDs from JSONB (first 5 users)
SELECT 
    'USER_PREFERENCES FAVORITES SAMPLE' as check_type,
    user_id,
    preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds' as favorite_house_ids,
    jsonb_array_length(preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds') as favorite_count
FROM amesa_auth.user_preferences
WHERE preferences_json::jsonb ? 'lotteryPreferences' 
  AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
LIMIT 5;

-- ============================================================================
-- 9. CROSS-SCHEMA RELATIONSHIPS CHECK
-- ============================================================================

-- Verify that favorite house IDs in user_preferences actually exist in houses table
SELECT 
    'FAVORITES INTEGRITY CHECK' as check_type,
    COUNT(DISTINCT up.user_id) as users_with_favorites,
    COUNT(DISTINCT house_id) as unique_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id AND h.deleted_at IS NULL
        )
    ) as valid_favorite_house_ids,
    COUNT(DISTINCT house_id) FILTER (
        WHERE NOT EXISTS (
            SELECT 1 FROM amesa_lottery.houses h 
            WHERE h.id = house_id
        )
    ) as invalid_favorite_house_ids
FROM amesa_auth.user_preferences up
CROSS JOIN LATERAL jsonb_array_elements_text(
    COALESCE(
        up.preferences_json::jsonb->'lotteryPreferences'->'favoriteHouseIds',
        '[]'::jsonb
    )
) AS house_id;

-- ============================================================================
-- 10. SUMMARY REPORT
-- ============================================================================

SELECT 
    'SUMMARY' as report_section,
    'All verification queries completed. Review results above.' as message,
    NOW() as verification_timestamp;




