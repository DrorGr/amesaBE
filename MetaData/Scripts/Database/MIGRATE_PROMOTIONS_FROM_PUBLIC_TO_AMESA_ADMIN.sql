-- ============================================================
-- Migrate Promotions Tables from public to amesa_admin Schema
-- ============================================================
-- Created: 2025-01-25
-- Purpose: Drop old promotions tables from public schema if they exist
-- 
-- INSTRUCTIONS:
-- 1. Verify that tables exist in amesa_admin schema (run verification queries)
-- 2. Backup your database before running this script
-- 3. Run this script to clean up old tables from public schema
-- 4. Verify tables are removed from public schema
-- ============================================================

-- ============================================================
-- STEP 1: VERIFY TABLES EXIST IN amesa_admin SCHEMA
-- ============================================================
-- Run these queries first to confirm tables are in amesa_admin:

-- SELECT table_schema, table_name 
-- FROM information_schema.tables 
-- WHERE table_schema = 'amesa_admin' 
--   AND table_name IN ('promotions', 'user_promotions');

-- If the above returns 2 rows, proceed with dropping public schema tables
-- ============================================================

-- ============================================================
-- STEP 2: DROP INDEXES FROM public SCHEMA (if they exist)
-- ============================================================

-- Drop indexes on user_promotions table in public schema
DROP INDEX IF EXISTS public.idx_user_promotions_user_used;
DROP INDEX IF EXISTS public.idx_user_promotions_transaction;
DROP INDEX IF EXISTS public.idx_user_promotions_promotion;
DROP INDEX IF EXISTS public.idx_user_promotions_user;

-- Drop indexes on promotions table in public schema
DROP INDEX IF EXISTS public.idx_promotions_type_active;
DROP INDEX IF EXISTS public.idx_promotions_active_dates;
DROP INDEX IF EXISTS public.idx_promotions_code;

-- ============================================================
-- STEP 3: DROP TABLES FROM public SCHEMA (if they exist)
-- ============================================================

-- Drop user_promotions table first (due to foreign key dependency)
DROP TABLE IF EXISTS public.user_promotions CASCADE;

-- Drop promotions table
DROP TABLE IF EXISTS public.promotions CASCADE;

-- ============================================================
-- STEP 4: VERIFICATION QUERIES
-- ============================================================
-- Run these queries after executing the script to verify cleanup:

-- Verify tables are removed from public schema
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'public' 
  AND table_name IN ('promotions', 'user_promotions')
ORDER BY table_name;
-- Expected: 0 rows (tables should not exist in public schema)

-- Verify tables still exist in amesa_admin schema
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_admin' 
  AND table_name IN ('promotions', 'user_promotions')
ORDER BY table_name;
-- Expected: 2 rows (tables should exist in amesa_admin schema)

-- Verify indexes are removed from public schema
SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE schemaname = 'public' 
  AND tablename IN ('promotions', 'user_promotions')
ORDER BY tablename, indexname;
-- Expected: 0 rows (indexes should not exist in public schema)

-- Verify indexes exist in amesa_admin schema
SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_indexes
WHERE schemaname = 'amesa_admin' 
  AND tablename IN ('promotions', 'user_promotions')
ORDER BY tablename, indexname;
-- Expected: 11 rows (7 custom indexes + 2 primary keys + 2 unique constraints)

-- ============================================================
-- NOTES
-- ============================================================
-- 1. This script uses IF EXISTS, so it's safe to run even if tables don't exist
-- 2. CASCADE will drop dependent objects (foreign keys, etc.)
-- 3. If you have data in public.promotions or public.user_promotions that you need,
--    migrate it to amesa_admin schema BEFORE running this script
-- 4. Always backup your database before running migration scripts
-- ============================================================

