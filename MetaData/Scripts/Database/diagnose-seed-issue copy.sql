-- Diagnose why translations aren't being inserted
-- Run this to see what's wrong

SET search_path TO amesa_content;

-- 1. Check if Languages exist
SELECT 'Languages check' as step, COUNT(*) as count FROM amesa_content.languages;
SELECT "Code", "Name" FROM amesa_content.languages;

-- 2. Check current translations count
SELECT 'Current translations' as step, COUNT(*) as count FROM amesa_content.translations;

-- 3. Check table structure - verify column names
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_content' 
  AND table_name = 'translations'
ORDER BY ordinal_position;

-- 4. Check table structure for languages
SELECT 
    column_name,
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_schema = 'amesa_content' 
  AND table_name = 'languages'
ORDER BY ordinal_position;

-- 5. Try a test insert to see the error
DO $$
BEGIN
    -- First ensure language exists
    INSERT INTO amesa_content.languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
    VALUES ('en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW())
    ON CONFLICT ("Code") DO NOTHING;
    
    -- Try inserting one translation
    INSERT INTO amesa_content.translations (id, "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy")
    VALUES (gen_random_uuid(), 'en', 'test.key', 'Test Value', NULL, 'Test', true, NOW(), NOW(), 'System')
    ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
    
    RAISE NOTICE 'Test insert completed. Check if test.key exists.';
END $$;

-- 6. Verify test insert
SELECT 'Test translation' as step, COUNT(*) as count 
FROM amesa_content.translations 
WHERE "Key" = 'test.key';

-- 7. Show all translations if any exist
SELECT "Key", "Value", "LanguageCode" 
FROM amesa_content.translations 
LIMIT 10;

