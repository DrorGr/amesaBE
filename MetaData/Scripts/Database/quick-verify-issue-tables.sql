-- ============================================================================
-- Quick Verification Query for Issue Tables
-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- Run this first for a quick overview, then run verify-issue-tables.sql for details
-- ============================================================================

-- First, discover actual column names (to handle case sensitivity)
SELECT 
    'COLUMN NAME DISCOVERY' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name ILIKE '%status%' OR column_name ILIKE '%deleted%' OR column_name = 'Id' OR column_name = 'id')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND (column_name ILIKE '%status%' OR column_name ILIKE '%user%' OR column_name ILIKE '%house%' OR column_name = 'Id' OR column_name = 'id'))
ORDER BY table_schema, table_name, column_name;

-- Quick table existence check
SELECT 
    'TABLE EXISTENCE CHECK' as check_type,
    schemaname,
    tablename,
    'EXISTS' as status
FROM pg_tables
WHERE (schemaname = 'amesa_lottery' AND tablename IN ('houses', 'house_images', 'lottery_tickets', 'user_watchlist'))
   OR (schemaname = 'amesa_auth' AND tablename = 'user_preferences')
   OR (schemaname = 'amesa_admin' AND tablename IN ('promotions', 'user_promotions'))
ORDER BY schemaname, tablename;

-- Critical columns check
-- Note: Checking both snake_case and PascalCase column names
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'DeletedAt', 'deleted_at'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND column_name IN ('Id', 'id', 'Status', 'status', 'UserId', 'user_id', 'HouseId', 'house_id'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PreferencesJson', 'preferences_json'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND column_name IN ('Id', 'id', 'Code', 'code', 'IsActive', 'is_active', 'StartDate', 'start_date', 'EndDate', 'end_date'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND column_name IN ('Id', 'id', 'UserId', 'user_id', 'PromotionId', 'promotion_id'))
ORDER BY table_schema, table_name, column_name;

-- Data integrity quick check
-- Note: Using quoted identifiers for case-sensitive column names
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Code" IS NULL OR "Code" = '') as null_code_count,
    COUNT(*) FILTER (WHERE "IsActive" = true) as active_count
FROM amesa_admin.promotions
UNION ALL
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "PreferencesJson" IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE "PreferencesJson"::jsonb ? 'lotteryPreferences' 
        AND "PreferencesJson"::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;

