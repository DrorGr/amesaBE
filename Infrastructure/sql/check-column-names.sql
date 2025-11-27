-- Check exact column names in the tables we need

-- Check lottery_tickets columns
SELECT 
    'lottery_tickets' as table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery'
AND table_name = 'lottery_tickets'
ORDER BY ordinal_position;

-- Check houses columns
SELECT 
    'houses' as table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'amesa_lottery'
AND table_name = 'houses'
ORDER BY ordinal_position;

-- Check user_preferences columns
SELECT 
    'user_preferences' as table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'amesa_auth'
AND table_name = 'user_preferences'
ORDER BY ordinal_position;

-- Check users columns (for the view)
SELECT 
    'users' as table_name,
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'amesa_auth'
AND table_name = 'users'
AND column_name IN ('Id', 'id', 'user_id', 'UserId')
ORDER BY ordinal_position;












