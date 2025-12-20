-- Comprehensive Database Seeding Script
-- Seeds all schemas with relevant data

-- ============================================
-- amesa_content schema: Languages
-- ============================================
SET search_path TO amesa_content;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'en');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'he');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'ar');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'es');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'fr');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'pl');

SELECT '✅ Languages seeded: ' || COUNT(*)::text FROM "Languages";

-- ============================================
-- Note: For complex data (Users, Houses, etc.)
-- Use the .NET DatabaseSeeder which handles:
-- - Password hashing (SHA256)
-- - Complex relationships
-- - Comprehensive translations
-- - House images
-- - Lottery tickets and draws
-- ============================================

