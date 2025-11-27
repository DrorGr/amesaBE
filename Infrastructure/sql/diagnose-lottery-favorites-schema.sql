-- Diagnostic script to check if tables and schemas exist
-- Run this BEFORE running the migration to verify prerequisites

-- Check if schemas exist
SELECT 
    schema_name,
    'Schema exists' as status
FROM information_schema.schemata
WHERE schema_name IN ('amesa_auth', 'amesa_lottery', 'amesa_payment')
ORDER BY schema_name;

-- Check if tables exist in amesa_lottery schema
SELECT 
    table_schema,
    table_name,
    'Table exists' as status
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
AND table_name IN ('lottery_tickets', 'houses')
ORDER BY table_name;

-- Check if tables exist in amesa_auth schema
SELECT 
    table_schema,
    table_name,
    'Table exists' as status
FROM information_schema.tables
WHERE table_schema = 'amesa_auth'
AND table_name IN ('users', 'user_preferences')
ORDER BY table_name;

-- Check column names in lottery_tickets table
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery'
AND table_name = 'lottery_tickets'
ORDER BY ordinal_position;

-- Check column names in houses table
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery'
AND table_name = 'houses'
ORDER BY ordinal_position;

-- Check column names in user_preferences table
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth'
AND table_name = 'user_preferences'
ORDER BY ordinal_position;

-- Check column names in users table
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_auth'
AND table_name = 'users'
ORDER BY ordinal_position;

-- Check existing indexes on lottery_tickets
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery'
AND tablename = 'lottery_tickets'
ORDER BY indexname;

-- Check existing indexes on houses
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_lottery'
AND tablename = 'houses'
ORDER BY indexname;

-- Check existing indexes on user_preferences
SELECT 
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE schemaname = 'amesa_auth'
AND tablename = 'user_preferences'
ORDER BY indexname;












