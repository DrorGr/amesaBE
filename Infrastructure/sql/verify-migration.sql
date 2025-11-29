-- Verification queries for lottery favorites migration

-- 1. Check indexes
SELECT 
    schemaname,
    tablename,
    indexname
FROM pg_indexes 
WHERE schemaname IN ('amesa_lottery', 'amesa_auth')
AND (indexname LIKE 'idx_%lottery%' OR indexname LIKE 'idx_%houses%' OR indexname LIKE 'idx_%tickets%' OR indexname LIKE 'idx_%preferences%')
ORDER BY schemaname, indexname;

-- 2. Check view
SELECT 
    table_schema,
    table_name
FROM information_schema.views
WHERE table_schema = 'amesa_auth'
AND table_name = 'user_lottery_dashboard';

-- 3. Check translations
SELECT 
    "Category",
    COUNT(DISTINCT "Key") as key_count,
    COUNT(*) as total_translations
FROM amesa_content.translations
WHERE "Category" = 'Lottery'
GROUP BY "Category";

-- 4. Sample translation keys
SELECT 
    "LanguageCode",
    "Key",
    "Value"
FROM amesa_content.translations
WHERE "Category" = 'Lottery'
ORDER BY "Key", "LanguageCode"
LIMIT 20;














