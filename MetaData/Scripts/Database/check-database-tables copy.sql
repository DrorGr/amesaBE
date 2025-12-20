-- Check database tables and schemas
-- This helps understand the database structure

-- 1. Count tables by schema (excluding system schemas)
SELECT 
    table_schema,
    COUNT(*) as table_count
FROM information_schema.tables
WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
GROUP BY table_schema
ORDER BY table_count DESC;

-- 2. List all application schemas
SELECT DISTINCT table_schema
FROM information_schema.tables
WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
ORDER BY table_schema;

-- 3. Count tables in each application schema
SELECT 
    table_schema,
    COUNT(*) as table_count,
    STRING_AGG(table_name, ', ' ORDER BY table_name) as table_names
FROM information_schema.tables
WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast', 'pg_temp_1', 'pg_toast_temp_1')
GROUP BY table_schema
ORDER BY table_schema;

-- 4. Check if translations table exists in different schemas
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_name LIKE '%translation%' OR table_name LIKE '%Translation%'
ORDER BY table_schema, table_name;

-- 5. Check if languages table exists in different schemas
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables
WHERE table_name LIKE '%language%' OR table_name LIKE '%Language%'
ORDER BY table_schema, table_name;

