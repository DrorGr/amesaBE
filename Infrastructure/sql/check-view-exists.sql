-- Check if the user_lottery_dashboard view exists
SELECT 
    table_schema,
    table_name,
    view_definition
FROM information_schema.views
WHERE table_schema = 'amesa_auth'
AND table_name = 'user_lottery_dashboard';

-- If the view exists, test it with a sample query
-- (Replace with an actual user_id from your database)
-- SELECT * FROM amesa_auth.user_lottery_dashboard LIMIT 1;



















