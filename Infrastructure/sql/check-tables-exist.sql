-- Quick check to see what tables actually exist in the schemas

-- Check all tables in amesa_lottery schema
SELECT 
    'amesa_lottery' as schema_name,
    table_name,
    'Table exists' as status
FROM information_schema.tables
WHERE table_schema = 'amesa_lottery'
ORDER BY table_name;

-- Check all tables in amesa_auth schema  
SELECT 
    'amesa_auth' as schema_name,
    table_name,
    'Table exists' as status
FROM information_schema.tables
WHERE table_schema = 'amesa_auth'
ORDER BY table_name;

-- Specifically check for the tables we need
SELECT 
    table_schema,
    table_name,
    CASE 
        WHEN table_name = 'lottery_tickets' THEN '✓ Found'
        WHEN table_name = 'houses' THEN '✓ Found'
        WHEN table_name = 'users' THEN '✓ Found'
        WHEN table_name = 'user_preferences' THEN '✓ Found'
        ELSE 'Other table'
    END as relevance
FROM information_schema.tables
WHERE table_schema IN ('amesa_lottery', 'amesa_auth')
AND table_name IN ('lottery_tickets', 'houses', 'users', 'user_preferences')
ORDER BY table_schema, table_name;


