-- ============================================
-- STEP 2: DISCOVER ACTUAL TABLES IN EACH SCHEMA
-- ============================================

-- Check tables in each schema that might contain our data
SELECT 
    table_schema,
    table_name,
    table_type
FROM information_schema.tables 
WHERE table_schema IN ('amesa_analytics', 'amesa_auth', 'amesa_content', 'amesa_lottery', 'amesa_lottery_results', 'amesa_notification', 'amesa_payment', 'public')
    AND (
        table_name ILIKE '%language%' OR 
        table_name ILIKE '%translation%' OR 
        table_name ILIKE '%user%' OR 
        table_name ILIKE '%house%' OR
        table_name ILIKE '%content%' OR
        table_name = 'Languages' OR
        table_name = 'translations' OR
        table_name = 'Users' OR
        table_name = 'Houses' OR
        table_name = 'HouseImages'
    )
ORDER BY table_schema, table_name;

-- Also check what tables exist in amesa_content specifically
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'amesa_content'
ORDER BY table_name;

-- And check what tables exist in amesa_auth specifically  
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'amesa_auth'
ORDER BY table_name;

-- And check what tables exist in public schema
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public'
    AND table_type = 'BASE TABLE'
ORDER BY table_name;
