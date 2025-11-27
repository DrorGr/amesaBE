-- Query to check all existing tables and views in amesa_lottery schema
-- Run this before migration to verify what exists

-- =====================================================
-- 1. LIST ALL TABLES IN amesa_lottery SCHEMA
-- =====================================================
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
ORDER BY table_name;

-- =====================================================
-- 2. CHECK FOR WATCHLIST-RELATED TABLES
-- =====================================================
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
AND (
    table_name ILIKE '%watchlist%' 
    OR table_name ILIKE '%watch%'
    OR table_name ILIKE '%track%'
    OR table_name ILIKE '%bookmark%'
    OR table_name ILIKE '%follow%'
    OR table_name ILIKE '%monitor%'
)
ORDER BY table_name;

-- =====================================================
-- 3. CHECK FOR PARTICIPANT-RELATED VIEWS/TABLES
-- =====================================================
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
AND (
    table_name ILIKE '%participant%'
    OR table_name ILIKE '%stats%'
    OR table_name ILIKE '%count%'
)
ORDER BY table_name;

-- =====================================================
-- 4. CHECK ALL VIEWS IN amesa_lottery SCHEMA
-- =====================================================
SELECT 
    schemaname,
    viewname,
    definition
FROM pg_views
WHERE schemaname = 'amesa_lottery'
ORDER BY viewname;

-- =====================================================
-- 5. CHECK COLUMNS IN houses TABLE (for max_participants)
-- =====================================================
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery'
AND table_name = 'houses'
ORDER BY ordinal_position;

-- =====================================================
-- 6. CHECK ALL INDEXES IN amesa_lottery SCHEMA
-- =====================================================
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery'
ORDER BY tablename, indexname;

-- Run this before migration to verify what exists

-- =====================================================
-- 1. LIST ALL TABLES IN amesa_lottery SCHEMA
-- =====================================================
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
ORDER BY table_name;

-- =====================================================
-- 2. CHECK FOR WATCHLIST-RELATED TABLES
-- =====================================================
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
AND (
    table_name ILIKE '%watchlist%' 
    OR table_name ILIKE '%watch%'
    OR table_name ILIKE '%track%'
    OR table_name ILIKE '%bookmark%'
    OR table_name ILIKE '%follow%'
    OR table_name ILIKE '%monitor%'
)
ORDER BY table_name;

-- =====================================================
-- 3. CHECK FOR PARTICIPANT-RELATED VIEWS/TABLES
-- =====================================================
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
AND (
    table_name ILIKE '%participant%'
    OR table_name ILIKE '%stats%'
    OR table_name ILIKE '%count%'
)
ORDER BY table_name;

-- =====================================================
-- 4. CHECK ALL VIEWS IN amesa_lottery SCHEMA
-- =====================================================
SELECT 
    schemaname,
    viewname,
    definition
FROM pg_views
WHERE schemaname = 'amesa_lottery'
ORDER BY viewname;

-- =====================================================
-- 5. CHECK COLUMNS IN houses TABLE (for max_participants)
-- =====================================================
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery'
AND table_name = 'houses'
ORDER BY ordinal_position;

-- =====================================================
-- 6. CHECK ALL INDEXES IN amesa_lottery SCHEMA
-- =====================================================
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery'
ORDER BY tablename, indexname;



