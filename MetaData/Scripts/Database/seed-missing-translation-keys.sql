-- Auto-generated: seed missing translation keys + backfill all languages from English
-- Generated: 2026-05-15 12:04:14 UTC
SET search_path TO amesa_content;

-- Activate German and Russian; deactivate Hebrew and Arabic
INSERT INTO amesa_content.languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
VALUES
    ('de', 'German', 'Deutsch', '🇩🇪', true, false, 5, NOW(), NOW()),
    ('ru', 'Russian', 'Русский', '🇷🇺', true, false, 6, NOW(), NOW())
ON CONFLICT ("Code") DO UPDATE SET
    "Name" = EXCLUDED."Name",
    "NativeName" = EXCLUDED."NativeName",
    "IsActive" = true,
    "UpdatedAt" = NOW();

UPDATE amesa_content.languages
SET "IsActive" = false, "UpdatedAt" = NOW()
WHERE "Code" IN ('he', 'ar');

-- No new English keys to insert


-- Backfill es from English
INSERT INTO amesa_content.translations (
    "Id", "LanguageCode", "Key", "Value", "Description", "Category",
    "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    'es',
    e."Key",
    e."Value",
    e."Description",
    e."Category",
    true,
    NOW(),
    NOW(),
    'seed-missing-keys',
    'seed-missing-keys'
FROM amesa_content.translations e
WHERE e."LanguageCode" = 'en'
  AND e."IsActive" = true
  AND NOT EXISTS (
      SELECT 1
      FROM amesa_content.translations t
      WHERE t."LanguageCode" = 'es'
        AND t."Key" = e."Key"
  )
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Backfill fr from English
INSERT INTO amesa_content.translations (
    "Id", "LanguageCode", "Key", "Value", "Description", "Category",
    "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    'fr',
    e."Key",
    e."Value",
    e."Description",
    e."Category",
    true,
    NOW(),
    NOW(),
    'seed-missing-keys',
    'seed-missing-keys'
FROM amesa_content.translations e
WHERE e."LanguageCode" = 'en'
  AND e."IsActive" = true
  AND NOT EXISTS (
      SELECT 1
      FROM amesa_content.translations t
      WHERE t."LanguageCode" = 'fr'
        AND t."Key" = e."Key"
  )
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Backfill pl from English
INSERT INTO amesa_content.translations (
    "Id", "LanguageCode", "Key", "Value", "Description", "Category",
    "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    'pl',
    e."Key",
    e."Value",
    e."Description",
    e."Category",
    true,
    NOW(),
    NOW(),
    'seed-missing-keys',
    'seed-missing-keys'
FROM amesa_content.translations e
WHERE e."LanguageCode" = 'en'
  AND e."IsActive" = true
  AND NOT EXISTS (
      SELECT 1
      FROM amesa_content.translations t
      WHERE t."LanguageCode" = 'pl'
        AND t."Key" = e."Key"
  )
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Backfill de from English
INSERT INTO amesa_content.translations (
    "Id", "LanguageCode", "Key", "Value", "Description", "Category",
    "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    'de',
    e."Key",
    e."Value",
    e."Description",
    e."Category",
    true,
    NOW(),
    NOW(),
    'seed-missing-keys',
    'seed-missing-keys'
FROM amesa_content.translations e
WHERE e."LanguageCode" = 'en'
  AND e."IsActive" = true
  AND NOT EXISTS (
      SELECT 1
      FROM amesa_content.translations t
      WHERE t."LanguageCode" = 'de'
        AND t."Key" = e."Key"
  )
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Backfill ru from English
INSERT INTO amesa_content.translations (
    "Id", "LanguageCode", "Key", "Value", "Description", "Category",
    "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    'ru',
    e."Key",
    e."Value",
    e."Description",
    e."Category",
    true,
    NOW(),
    NOW(),
    'seed-missing-keys',
    'seed-missing-keys'
FROM amesa_content.translations e
WHERE e."LanguageCode" = 'en'
  AND e."IsActive" = true
  AND NOT EXISTS (
      SELECT 1
      FROM amesa_content.translations t
      WHERE t."LanguageCode" = 'ru'
        AND t."Key" = e."Key"
  )
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
