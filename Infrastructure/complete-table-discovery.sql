-- ============================================
-- COMPLETE TABLE DISCOVERY - GET ALL EXACT DETAILS
-- ============================================

-- 1. Show ALL tables in each schema with exact names
SELECT 
    'SCHEMA: ' || table_schema AS info,
    table_name AS exact_table_name,
    table_type
FROM information_schema.tables 
WHERE table_schema IN ('amesa_analytics', 'amesa_auth', 'amesa_content', 'amesa_lottery', 'amesa_lottery_results', 'amesa_notification', 'amesa_payment', 'public')
ORDER BY table_schema, table_name;

-- 2. Get EXACT column details for languages table (wherever it exists)
SELECT 
    'LANGUAGES TABLE COLUMNS:' AS info,
    table_schema,
    table_name,
    column_name AS exact_column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name ILIKE '%language%'
ORDER BY table_schema, table_name, ordinal_position;

-- 3. Get EXACT column details for translations table (wherever it exists)
SELECT 
    'TRANSLATIONS TABLE COLUMNS:' AS info,
    table_schema,
    table_name,
    column_name AS exact_column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name ILIKE '%translation%'
ORDER BY table_schema, table_name, ordinal_position;

-- 4. Get EXACT column details for users table (wherever it exists)
SELECT 
    'USERS TABLE COLUMNS:' AS info,
    table_schema,
    table_name,
    column_name AS exact_column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name ILIKE '%user%'
ORDER BY table_schema, table_name, ordinal_position;

-- 5. Get EXACT column details for houses table (wherever it exists)
SELECT 
    'HOUSES TABLE COLUMNS:' AS info,
    table_schema,
    table_name,
    column_name AS exact_column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_name ILIKE '%house%'
ORDER BY table_schema, table_name, ordinal_position;

-- 6. Show current search path
SHOW search_path;

-- 7. Show current schema
SELECT current_schema();

-- 8. List ALL schemas
SELECT schema_name FROM information_schema.schemata ORDER BY schema_name;
