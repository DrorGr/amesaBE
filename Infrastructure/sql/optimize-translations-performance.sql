-- Performance Optimization for Translations Endpoint
-- Adds index for faster queries on LanguageCode + IsActive

-- Index for GetTranslations query: WHERE LanguageCode = ? AND IsActive = true ORDER BY Key
CREATE INDEX IF NOT EXISTS idx_translations_languagecode_isactive_key 
ON amesa_content.translations("LanguageCode", "IsActive", "Key");

-- Analyze table to update statistics
ANALYZE amesa_content.translations;

-- Verify index was created
SELECT 
    indexname, 
    indexdef 
FROM pg_indexes 
WHERE tablename = 'translations' 
    AND schemaname = 'amesa_content'
    AND indexname LIKE '%languagecode%'
ORDER BY indexname;












