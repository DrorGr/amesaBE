-- Idempotent seed: short-form lottery/house status labels (status.*)
-- Schema: amesa_content.translations (see AmesaBackend.Content Models/Translation.cs, ContentDbContext)
-- API: GET /api/v1/translations/{languageCode} (AmesaBackend.Content Controllers/TranslationsController)
--
-- Rationale: legacy SQL seeders used houses.status.{active,upcoming,ended}; some UIs expect status.*.
-- Inserts one row per (active language from amesa_content.languages, key). Unknown language codes fall back to English text.
--
-- Run (example Aurora PostgreSQL / RDS):
--   psql "host=... port=5432 dbname=... user=... sslmode=require" -f MetaData/Scripts/Database/seed-status-translation-keys.sql
-- From repo root with AWS CLI + psql (reads connection string from SSM; RDS Data API is off on prod):
--   MetaData\Scripts\Database\run-seed-status-keys.ps1 -Apply
-- Or use Query Editor against the content database.
--
-- After run: flush Redis keys translations_* if you cache translations (Content service).

INSERT INTO amesa_content.translations (
    "Id",
    "LanguageCode",
    "Key",
    "Value",
    "Description",
    "Category",
    "IsActive",
    "CreatedAt",
    "UpdatedAt",
    "CreatedBy",
    "UpdatedBy"
)
SELECT
    gen_random_uuid(),
    l."Code",
    s.key,
    CASE l."Code"
        WHEN 'en' THEN s.en
        WHEN 'es' THEN s.es
        WHEN 'fr' THEN s.fr
        WHEN 'pl' THEN s.pl
        WHEN 'he' THEN s.he
        WHEN 'ar' THEN s.ar
        ELSE s.en
    END,
    s.description,
    'Status',
    true,
    NOW(),
    NOW(),
    'seed-status-keys',
    'seed-status-keys'
FROM amesa_content.languages AS l
CROSS JOIN (
    VALUES
        ('status.active',    'Active',    'Activo',     'Actif',     'Aktywny',    'פעיל',      'نشط',       'Display label: active lottery/house'),
        ('status.upcoming',  'Upcoming',  'Próximo',    'À venir',   'Nadchodzący', 'בקרוב',     'قادم',      'Display label: upcoming lottery/house'),
        ('status.ended',     'Ended',     'Finalizado', 'Terminé',   'Zakończony',  'הסתיים',    'انتهى',     'Display label: ended lottery/house')
) AS s(key, en, es, fr, pl, he, ar, description)
WHERE l."IsActive" = true
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
