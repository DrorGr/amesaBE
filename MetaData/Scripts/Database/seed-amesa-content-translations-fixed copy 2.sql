-- Seed translations into amesa_content.translations schema
-- This script seeds the translations that the content service needs
-- WITH ERROR HANDLING to see what fails

SET search_path TO amesa_content;

-- First, verify Languages table structure
DO $$
BEGIN
    RAISE NOTICE 'Checking Languages table...';
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'amesa_content' AND table_name = 'languages') THEN
        RAISE EXCEPTION 'Languages table does not exist!';
    END IF;
    RAISE NOTICE 'Languages table exists.';
END $$;

-- Ensure Languages exist first
-- Languages table uses Code as primary key (no Id column)
-- Check exact column names from migration
DO $$
DECLARE
    lang_count INTEGER;
BEGIN
    RAISE NOTICE 'Inserting Languages...';
    
    INSERT INTO amesa_content.languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
    VALUES 
        ('en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()),
        ('pl', 'Polish', 'Polski', '🇵🇱', true, false, 2, NOW(), NOW())
    ON CONFLICT ("Code") DO NOTHING;
    
    SELECT COUNT(*) INTO lang_count FROM amesa_content.languages;
    RAISE NOTICE 'Languages count after insert: %', lang_count;
END $$;

-- Verify Languages were inserted
SELECT 'Languages check' as step, COUNT(*) as count FROM amesa_content.languages;

-- Now insert Translations
-- Check exact column names from migration
DO $$
DECLARE
    trans_count INTEGER;
    error_msg TEXT;
BEGIN
    RAISE NOTICE 'Inserting Translations...';
    
    BEGIN
        -- Navigation
        INSERT INTO amesa_content.translations (id, "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy")
        VALUES 
            (gen_random_uuid(), 'en', 'nav.lotteries', 'Lotteries', NULL, 'Navigation', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'nav.promotions', 'Promotions', NULL, 'Navigation', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'nav.howItWorks', 'How It Works', NULL, 'Navigation', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'nav.winners', 'Winners', NULL, 'Navigation', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'nav.signIn', 'Sign In', NULL, 'Navigation', true, NOW(), NOW(), 'System')
        ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
        
        RAISE NOTICE 'Navigation translations inserted.';
    EXCEPTION WHEN OTHERS THEN
        error_msg := SQLERRM;
        RAISE NOTICE 'Error inserting Navigation: %', error_msg;
    END;
    
    BEGIN
        -- Hero Section
        INSERT INTO amesa_content.translations (id, "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy")
        VALUES 
            (gen_random_uuid(), 'en', 'hero.title', 'Win Your Dream Home', NULL, 'Hero', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'hero.subtitle', 'Enter exclusive house lotteries and get the chance to win amazing properties at a fraction of their market value.', NULL, 'Hero', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'hero.browseLotteries', 'Browse Lotteries', NULL, 'Hero', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'hero.howItWorks', 'How It Works', NULL, 'Hero', true, NOW(), NOW(), 'System')
        ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
        
        RAISE NOTICE 'Hero translations inserted.';
    EXCEPTION WHEN OTHERS THEN
        error_msg := SQLERRM;
        RAISE NOTICE 'Error inserting Hero: %', error_msg;
    END;
    
    BEGIN
        -- How It Works
        INSERT INTO amesa_content.translations (id, "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy")
        VALUES 
            (gen_random_uuid(), 'en', 'howItWorks.heroTitle', 'How Amesa Lottery Works', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.heroSubtitle', 'Your path to winning a dream home is simple and transparent.', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.simpleProcess', 'Simple Process', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.introduction', 'Participating in our house lotteries is straightforward and secure. Follow these simple steps to get started on your journey to homeownership.', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.step1Title', 'Choose Your Lottery', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.step1Desc', 'Browse our exclusive selection of luxury homes. Each property is a separate lottery with a limited number of tickets.', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.step2Title', 'Buy Tickets', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.step2Desc', 'Purchase tickets for your chosen lottery. The more tickets you buy, the higher your chances of winning. All transactions are secure and transparent.', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.step3Title', 'Win & Own', NULL, 'HowItWorks', true, NOW(), NOW(), 'System'),
            (gen_random_uuid(), 'en', 'howItWorks.step3Desc', 'If you win, you become the proud owner of your dream property with all legal fees covered.', NULL, 'HowItWorks', true, NOW(), NOW(), 'System')
        ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
        
        RAISE NOTICE 'HowItWorks translations inserted.';
    EXCEPTION WHEN OTHERS THEN
        error_msg := SQLERRM;
        RAISE NOTICE 'Error inserting HowItWorks: %', error_msg;
    END;
    
    SELECT COUNT(*) INTO trans_count FROM amesa_content.translations WHERE "LanguageCode" = 'en';
    RAISE NOTICE 'Total English translations after insert: %', trans_count;
END $$;

-- Final verification
SELECT 'Translations seeded successfully into amesa_content.translations' AS result;
SELECT COUNT(*) AS translation_count FROM amesa_content.translations WHERE "LanguageCode" = 'en';

-- Verify the data
SELECT "Key", "Value" FROM amesa_content.translations WHERE "LanguageCode" = 'en' LIMIT 10;

