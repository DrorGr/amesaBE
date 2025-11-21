-- Copy translations from public schema (or wherever they are) to amesa_content.translations
-- This script helps recover translations that were seeded in the wrong schema

-- First, check if translations exist in public schema
DO $$
DECLARE
    translation_count_public INTEGER;
    translation_count_content INTEGER;
BEGIN
    -- Check public schema
    SELECT COUNT(*) INTO translation_count_public
    FROM information_schema.tables 
    WHERE table_schema = 'public' AND table_name = 'translations';
    
    -- Check amesa_content schema
    SELECT COUNT(*) INTO translation_count_content
    FROM information_schema.tables 
    WHERE table_schema = 'amesa_content' AND table_name = 'translations';
    
    RAISE NOTICE 'Public schema has translations table: %', (translation_count_public > 0);
    RAISE NOTICE 'amesa_content schema has translations table: %', (translation_count_content > 0);
    
    -- If translations exist in public schema, copy them
    IF translation_count_public > 0 THEN
        RAISE NOTICE 'Copying translations from public.translations to amesa_content.translations...';
        
        -- Ensure languages exist in amesa_content
        INSERT INTO amesa_content.languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
        SELECT "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt"
        FROM public.languages
        ON CONFLICT ("Code") DO NOTHING;
        
        -- Copy translations
        INSERT INTO amesa_content.translations (id, "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy")
        SELECT id, "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy"
        FROM public.translations
        ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
        
        RAISE NOTICE 'Translations copied successfully!';
    ELSE
        RAISE NOTICE 'No translations found in public schema. You may need to re-seed.';
    END IF;
END $$;

-- Show count of translations in amesa_content
SELECT COUNT(*) AS translation_count FROM amesa_content.translations;
SELECT "Key", "Value" FROM amesa_content.translations WHERE "LanguageCode" = 'en' LIMIT 10;

