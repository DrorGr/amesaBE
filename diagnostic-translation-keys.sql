-- Diagnostic Query: Check what translation keys we actually have
-- This will help identify missing keys that the frontend is requesting

-- 1. Show all English keys we currently have
SELECT 
    "Key",
    "Value",
    "Category"
FROM amesa_content.translations 
WHERE "LanguageCode" = 'en'
ORDER BY "Category", "Key";

-- 2. Count translations by category
SELECT 
    "Category",
    COUNT(*) as key_count
FROM amesa_content.translations 
WHERE "LanguageCode" = 'en'
GROUP BY "Category"
ORDER BY "Category";

-- 3. Check if we have the specific keys that appear to be missing from the frontend
SELECT 
    'hero.title' as missing_key,
    CASE WHEN EXISTS(SELECT 1 FROM amesa_content.translations WHERE "Key" = 'hero.title' AND "LanguageCode" = 'en') 
         THEN 'EXISTS' ELSE 'MISSING' END as status
UNION ALL
SELECT 
    'hero.subtitle' as missing_key,
    CASE WHEN EXISTS(SELECT 1 FROM amesa_content.translations WHERE "Key" = 'hero.subtitle' AND "LanguageCode" = 'en') 
         THEN 'EXISTS' ELSE 'MISSING' END as status
UNION ALL
SELECT 
    'hero.browseLotteries' as missing_key,
    CASE WHEN EXISTS(SELECT 1 FROM amesa_content.translations WHERE "Key" = 'hero.browseLotteries' AND "LanguageCode" = 'en') 
         THEN 'EXISTS' ELSE 'MISSING' END as status
UNION ALL
SELECT 
    'hero.howItWorks' as missing_key,
    CASE WHEN EXISTS(SELECT 1 FROM amesa_content.translations WHERE "Key" = 'hero.howItWorks' AND "LanguageCode" = 'en') 
         THEN 'EXISTS' ELSE 'MISSING' END as status
UNION ALL
SELECT 
    'house.active' as missing_key,
    CASE WHEN EXISTS(SELECT 1 FROM amesa_content.translations WHERE "Key" = 'house.active' AND "LanguageCode" = 'en') 
         THEN 'EXISTS' ELSE 'MISSING' END as status;

-- 4. Show sample of what we DO have for Polish
SELECT 
    "Key",
    "Value"
FROM amesa_content.translations 
WHERE "LanguageCode" = 'pl'
ORDER BY "Key"
LIMIT 20;





















