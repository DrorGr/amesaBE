-- Seed amesa_content schema
-- Languages and Translations

SET search_path TO amesa_content;

-- Insert Languages (if not exists)
INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'en',
    'English',
    'English',
    '🇺🇸',
    true,
    true,
    1,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'en')
ON CONFLICT DO NOTHING;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'he',
    'Hebrew',
    'עברית',
    '🇮🇱',
    true,
    false,
    2,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'he')
ON CONFLICT DO NOTHING;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'ar',
    'Arabic',
    'العربية',
    '🇸🇦',
    true,
    false,
    3,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'ar')
ON CONFLICT DO NOTHING;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'es',
    'Spanish',
    'Español',
    '🇪🇸',
    true,
    false,
    4,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'es')
ON CONFLICT DO NOTHING;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'fr',
    'French',
    'Français',
    '🇫🇷',
    true,
    false,
    5,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'fr')
ON CONFLICT DO NOTHING;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'pl',
    'Polish',
    'Polski',
    '🇵🇱',
    true,
    false,
    6,
    NOW(),
    NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'pl')
ON CONFLICT DO NOTHING;

-- Note: Translations require the ComprehensiveTranslations class data
-- These should be seeded via .NET seeder or additional SQL scripts

SELECT 'Languages seeded successfully' AS result;

