-- ============================================
-- DEBUG SCHEMA ISSUE - FIND WHERE TABLES ACTUALLY ARE
-- ============================================

-- 1. Show current search path
SHOW search_path;

-- 2. Find ALL tables that contain our target names across ALL schemas
SELECT 
    schemaname,
    tablename,
    'Found in schema: ' || schemaname AS location
FROM pg_tables 
WHERE tablename IN ('languages', 'translations', 'users', 'user_addresses', 'user_phones', 'houses', 'house_images')
ORDER BY schemaname, tablename;

-- 3. Check if tables exist in public schema (default)
SELECT 
    'PUBLIC SCHEMA TABLES:' AS info,
    table_name
FROM information_schema.tables 
WHERE table_schema = 'public' 
    AND table_name IN ('languages', 'translations', 'users', 'user_addresses', 'user_phones', 'houses', 'house_images')
ORDER BY table_name;

-- 4. Try to find tables with LIKE pattern in case of naming differences
SELECT 
    table_schema,
    table_name,
    'Pattern match for: ' || table_name AS match_info
FROM information_schema.tables 
WHERE (
    table_name LIKE '%language%' OR 
    table_name LIKE '%translation%' OR 
    table_name LIKE '%user%' OR 
    table_name LIKE '%house%'
)
ORDER BY table_schema, table_name;

-- 5. Check what schema we're currently in
SELECT current_schema();

-- 6. List all schemas
SELECT schema_name FROM information_schema.schemata WHERE schema_name NOT LIKE 'pg_%' AND schema_name != 'information_schema' ORDER BY schema_name;
