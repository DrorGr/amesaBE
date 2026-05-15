-- Member account type labels (replaces dynamic translate('member.' + accountType))
SET search_path TO amesa_content;

INSERT INTO amesa_content.translations (
    "Id", "LanguageCode", "Key", "Value", "Description", "Category",
    "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"
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
        WHEN 'de' THEN s.de
        WHEN 'ru' THEN s.ru
        ELSE s.en
    END,
    s.description,
    'Member',
    true,
    NOW(),
    NOW(),
    'seed-member-account-type',
    'seed-member-account-type'
FROM amesa_content.languages AS l
CROSS JOIN (
    VALUES
        ('member.accountType.basic',    'Basic',    'Básico',      'Basique',    'Podstawowe', 'Basis',    'Базовый',    'Basic account tier'),
        ('member.accountType.gold',     'Gold',     'Oro',         'Or',         'Złoto',      'Gold',     'Золото',     'Gold account tier'),
        ('member.accountType.premium',  'Premium',  'Premium',     'Premium',    'Premium',    'Premium',  'Премиум',    'Premium account tier')
) AS s(key, en, es, fr, pl, de, ru, description)
WHERE l."IsActive" = true
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;
