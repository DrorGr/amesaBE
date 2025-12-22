-- ============================================================================
-- Quick Verification Query for Issue Tables (Fixed for Case Sensitivity)
-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;


-- ============================================================================
-- Quick check to verify all tables related to the 500 errors are correct
-- This version handles case-sensitive column names in PostgreSQL
-- ============================================================================

-- Step 1: Discover actual column names for critical tables
SELECT 
    'COLUMN DISCOVERY' as check_type,
    table_schema,
    table_name,
    string_agg(column_name, ', ' ORDER BY ordinal_position) as all_columns
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses')
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets')
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences')
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions')
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions')
GROUP BY table_schema, table_name
ORDER BY table_schema, table_name;

-- Step 2: Quick table existence check
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

-- Step 3: Critical columns check (case-insensitive search)
SELECT 
    'CRITICAL COLUMNS CHECK' as check_type,
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE (table_schema = 'amesa_lottery' AND table_name = 'houses' 
       AND LOWER(column_name) IN ('id', 'status', 'deletedat'))
   OR (table_schema = 'amesa_lottery' AND table_name = 'lottery_tickets' 
       AND LOWER(column_name) IN ('id', 'status', 'userid', 'houseid'))
   OR (table_schema = 'amesa_auth' AND table_name = 'user_preferences' 
       AND LOWER(column_name) IN ('id', 'userid', 'preferencesjson'))
   OR (table_schema = 'amesa_admin' AND table_name = 'promotions' 
       AND LOWER(column_name) IN ('id', 'code', 'isactive', 'startdate', 'enddate'))
   OR (table_schema = 'amesa_admin' AND table_name = 'user_promotions' 
       AND LOWER(column_name) IN ('id', 'userid', 'promotionid'))
ORDER BY table_schema, table_name, column_name;

-- Step 4: Data integrity check
-- Note: amesa_lottery uses PascalCase, amesa_admin and amesa_auth use snake_case

-- For houses table (PascalCase: Status, DeletedAt)
SELECT 
    'DATA INTEGRITY' as check_type,
    'houses' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "DeletedAt" IS NULL) as active_count
FROM amesa_lottery.houses;

-- For lottery_tickets table (PascalCase: Status)
SELECT 
    'DATA INTEGRITY' as check_type,
    'lottery_tickets' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE "Status" IS NULL) as null_status_count,
    COUNT(*) FILTER (WHERE "Status" = 'Active') as active_count
FROM amesa_lottery.lottery_tickets;

-- For promotions table (snake_case: code, is_active)
SELECT 
    'DATA INTEGRITY' as check_type,
    'promotions' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE code IS NULL OR code = '') as null_code_count,
    COUNT(*) FILTER (WHERE is_active = true) as active_count
FROM amesa_admin.promotions;

-- For user_preferences table (snake_case: preferences_json)
SELECT 
    'DATA INTEGRITY' as check_type,
    'user_preferences' as table_name,
    COUNT(*) as total_rows,
    COUNT(*) FILTER (WHERE preferences_json IS NULL) as null_json_count,
    COUNT(*) FILTER (
        WHERE preferences_json::jsonb ? 'lotteryPreferences' 
        AND preferences_json::jsonb->'lotteryPreferences' ? 'favoriteHouseIds'
    ) as has_favorites_count
FROM amesa_auth.user_preferences;

