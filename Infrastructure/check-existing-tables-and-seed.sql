-- ============================================
-- CHECK EXISTING TABLES AND SEED ACCORDINGLY
-- First discover the actual table structure
-- ============================================

-- Check what schemas exist
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
ORDER BY schema_name;

-- Check tables in each potential schema
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables 
WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
    AND (
        table_name ILIKE '%language%' OR 
        table_name ILIKE '%translation%' OR 
        table_name ILIKE '%user%' OR 
        table_name ILIKE '%house%' OR
        table_name ILIKE '%content%'
    )
ORDER BY table_schema, table_name;

-- Check column structure for potential translation tables
SELECT 
    table_schema,
    table_name,
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns 
WHERE table_schema NOT IN ('information_schema', 'pg_catalog', 'pg_toast')
    AND (
        table_name ILIKE '%language%' OR 
        table_name ILIKE '%translation%'
    )
ORDER BY table_schema, table_name, ordinal_position;
