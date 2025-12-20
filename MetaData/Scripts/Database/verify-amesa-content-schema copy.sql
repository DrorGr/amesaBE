-- Verify amesa_content schema structure and check translations
-- This helps understand what tables exist and their current state

-- 1. List all tables in amesa_content schema
SELECT 
    table_name,
    table_type
FROM information_schema.tables
WHERE table_schema = 'amesa_content'
ORDER BY table_name;

-- 2. Check if translations table exists and has data
SELECT 
    'translations' as table_name,
    COUNT(*) as row_count
FROM amesa_content.translations;

-- 3. Check if languages table exists and has data
SELECT 
    'languages' as table_name,
    COUNT(*) as row_count
FROM amesa_content.languages;

-- 4. Show sample translations (if any exist)
SELECT 
    "LanguageCode",
    "Key",
    "Value",
    "Category"
FROM amesa_content.translations
WHERE "LanguageCode" = 'en'
LIMIT 10;

-- 5. Show all languages
SELECT 
    "Code",
    "Name",
    "NativeName",
    "IsActive",
    "IsDefault"
FROM amesa_content.languages
ORDER BY "DisplayOrder";

