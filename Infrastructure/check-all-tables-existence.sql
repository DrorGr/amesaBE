-- ============================================
-- COMPREHENSIVE TABLE EXISTENCE CHECK
-- ============================================
-- This script checks what tables actually exist in the database
-- and provides the exact structure we need to work with

-- Check if uuid-ossp extension exists
SELECT 
    'Extension Check' as check_type,
    extname as name,
    'EXISTS' as status
FROM pg_extension 
WHERE extname = 'uuid-ossp'
UNION ALL
SELECT 
    'Extension Check' as check_type,
    'uuid-ossp' as name,
    'MISSING - NEED TO CREATE' as status
WHERE NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'uuid-ossp');

-- Check all schemas
SELECT 
    'Schema Check' as check_type,
    schema_name as name,
    'EXISTS' as status
FROM information_schema.schemata 
WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
ORDER BY schema_name;

-- Check ALL tables in ALL schemas
SELECT 
    'Table Check' as check_type,
    table_schema || '.' || table_name as name,
    table_type as status
FROM information_schema.tables 
WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
ORDER BY table_schema, table_name;

-- Check if our expected tables exist ANYWHERE
SELECT 
    'Expected Table Check' as check_type,
    expected_table as name,
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM information_schema.tables 
            WHERE table_name = expected_table 
            AND table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
        ) THEN 'EXISTS in schema: ' || (
            SELECT table_schema FROM information_schema.tables 
            WHERE table_name = expected_table 
            AND table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
            LIMIT 1
        )
        ELSE 'MISSING - NEED TO CREATE'
    END as status
FROM (
    VALUES 
        ('languages'),
        ('translations'), 
        ('users'),
        ('user_addresses'),
        ('user_phones'),
        ('houses'),
        ('house_images')
) AS expected(expected_table);

-- If no tables exist, we need to create them first!
SELECT 
    'CONCLUSION' as check_type,
    'DATABASE STATUS' as name,
    CASE 
        WHEN (SELECT COUNT(*) FROM information_schema.tables 
              WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast')) = 0 
        THEN 'EMPTY DATABASE - NEED TO CREATE ALL TABLES FIRST'
        ELSE 'TABLES EXIST - CHECK ABOVE FOR LOCATIONS'
    END as status;


