-- Add Missing Translation Keys
-- This script adds the specific keys that the frontend is requesting but are missing from our database

INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
    -- Hero Section Keys
    (gen_random_uuid(), 'en', 'hero.title', 'Win Your Dream Home', 'Hero section main title', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'hero.title', 'זכה בבית החלומות שלך', 'Hero section main title', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'hero.title', 'اربح منزل أحلامك', 'Hero section main title', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'hero.title', 'Gana la casa de tus sueños', 'Hero section main title', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'hero.title', 'Gagnez la maison de vos rêves', 'Hero section main title', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'hero.title', 'Wygraj dom swoich marzeń', 'Hero section main title', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'hero.browseLotteries', 'Browse Lotteries', 'Hero section browse button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'hero.browseLotteries', 'עיין בהגרלות', 'Hero section browse button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'hero.browseLotteries', 'تصفح اليانصيب', 'Hero section browse button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'hero.browseLotteries', 'Explorar loterías', 'Hero section browse button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'hero.browseLotteries', 'Parcourir les loteries', 'Hero section browse button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'hero.browseLotteries', 'Przeglądaj loterie', 'Hero section browse button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'hero.howItWorks', 'How It Works', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'hero.howItWorks', 'איך זה עובד', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'hero.howItWorks', 'كيف يعمل', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'hero.howItWorks', 'Cómo funciona', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'hero.howItWorks', 'Comment ça marche', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'hero.howItWorks', 'Jak to działa', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    -- House Status Keys
    (gen_random_uuid(), 'en', 'house.active', 'Active', 'House status - active', 'House', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'house.active', 'פעיל', 'House status - active', 'House', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'house.active', 'نشط', 'House status - active', 'House', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'house.active', 'Activo', 'House status - active', 'House', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'house.active', 'Actif', 'House status - active', 'House', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'house.active', 'Aktywny', 'House status - active', 'House', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    -- User Preferences Keys
    (gen_random_uuid(), 'en', 'preferences.title', 'User Preferences', 'Preferences page title', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.title', 'העדפות משתמש', 'Preferences page title', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.title', 'تفضيلات المستخدم', 'Preferences page title', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.title', 'Preferencias de usuario', 'Preferences page title', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.title', 'Préférences utilisateur', 'Preferences page title', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.title', 'Preferencje użytkownika', 'Preferences page title', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.subtitle', 'Customize your experience', 'Preferences page subtitle', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.subtitle', 'התאם את החוויה שלך', 'Preferences page subtitle', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.subtitle', 'خصص تجربتك', 'Preferences page subtitle', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.subtitle', 'Personaliza tu experiencia', 'Preferences page subtitle', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.subtitle', 'Personnalisez votre expérience', 'Preferences page subtitle', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.subtitle', 'Dostosuj swoje doświadczenie', 'Preferences page subtitle', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.theme', 'Theme', 'Theme preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.theme', 'ערכת נושא', 'Theme preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.theme', 'المظهر', 'Theme preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.theme', 'Tema', 'Theme preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.theme', 'Thème', 'Theme preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.theme', 'Motyw', 'Theme preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.theme.light', 'Light', 'Light theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.theme.light', 'בהיר', 'Light theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.theme.light', 'فاتح', 'Light theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.theme.light', 'Claro', 'Light theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.theme.light', 'Clair', 'Light theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.theme.light', 'Jasny', 'Light theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.theme.dark', 'Dark', 'Dark theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.theme.dark', 'כהה', 'Dark theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.theme.dark', 'داكن', 'Dark theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.theme.dark', 'Oscuro', 'Dark theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.theme.dark', 'Sombre', 'Dark theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.theme.dark', 'Ciemny', 'Dark theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.theme.auto', 'Auto', 'Auto theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.theme.auto', 'אוטומטי', 'Auto theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.theme.auto', 'تلقائي', 'Auto theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.theme.auto', 'Automático', 'Auto theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.theme.auto', 'Automatique', 'Auto theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.theme.auto', 'Automatyczny', 'Auto theme option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.fontSize', 'Font Size', 'Font size preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.fontSize', 'גודל גופן', 'Font size preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.fontSize', 'حجم الخط', 'Font size preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.fontSize', 'Tamaño de fuente', 'Font size preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.fontSize', 'Taille de police', 'Font size preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.fontSize', 'Rozmiar czcionki', 'Font size preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.fontSize.small', 'Small', 'Small font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.fontSize.small', 'קטן', 'Small font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.fontSize.small', 'صغير', 'Small font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.fontSize.small', 'Pequeño', 'Small font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.fontSize.small', 'Petit', 'Small font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.fontSize.small', 'Mały', 'Small font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.fontSize.medium', 'Medium', 'Medium font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.fontSize.medium', 'בינוני', 'Medium font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.fontSize.medium', 'متوسط', 'Medium font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.fontSize.medium', 'Mediano', 'Medium font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.fontSize.medium', 'Moyen', 'Medium font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.fontSize.medium', 'Średni', 'Medium font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.fontSize.large', 'Large', 'Large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.fontSize.large', 'גדול', 'Large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.fontSize.large', 'كبير', 'Large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.fontSize.large', 'Grande', 'Large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.fontSize.large', 'Grand', 'Large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.fontSize.large', 'Duży', 'Large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.fontSize.extraLarge', 'Extra Large', 'Extra large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.fontSize.extraLarge', 'גדול מאוד', 'Extra large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.fontSize.extraLarge', 'كبير جداً', 'Extra large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.fontSize.extraLarge', 'Extra grande', 'Extra large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.fontSize.extraLarge', 'Très grand', 'Extra large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.fontSize.extraLarge', 'Bardzo duży', 'Extra large font size option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.uiDensity', 'UI Density', 'UI density preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.uiDensity', 'צפיפות ממשק', 'UI density preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.uiDensity', 'كثافة الواجهة', 'UI density preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.uiDensity', 'Densidad de interfaz', 'UI density preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.uiDensity', 'Densité de l''interface', 'UI density preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.uiDensity', 'Gęstość interfejsu', 'UI density preference label', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.uiDensity.compact', 'Compact', 'Compact UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.uiDensity.compact', 'קומפקטי', 'Compact UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.uiDensity.compact', 'مضغوط', 'Compact UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.uiDensity.compact', 'Compacto', 'Compact UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.uiDensity.compact', 'Compact', 'Compact UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.uiDensity.compact', 'Kompaktowy', 'Compact UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.uiDensity.comfortable', 'Comfortable', 'Comfortable UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.uiDensity.comfortable', 'נוח', 'Comfortable UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.uiDensity.comfortable', 'مريح', 'Comfortable UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.uiDensity.comfortable', 'Cómodo', 'Comfortable UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.uiDensity.comfortable', 'Confortable', 'Comfortable UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.uiDensity.comfortable', 'Wygodny', 'Comfortable UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),

    (gen_random_uuid(), 'en', 'preferences.uiDensity.spacious', 'Spacious', 'Spacious UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'he', 'preferences.uiDensity.spacious', 'מרווח', 'Spacious UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'ar', 'preferences.uiDensity.spacious', 'واسع', 'Spacious UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'es', 'preferences.uiDensity.spacious', 'Espacioso', 'Spacious UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'fr', 'preferences.uiDensity.spacious', 'Spacieux', 'Spacious UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys'),
    (gen_random_uuid(), 'pl', 'preferences.uiDensity.spacious', 'Przestronny', 'Spacious UI density option', 'Preferences', true, NOW(), NOW(), 'missing-keys', 'missing-keys')

ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Verify the new translations were added
SELECT 
    "LanguageCode",
    COUNT(*) as translation_count
FROM amesa_content.translations 
GROUP BY "LanguageCode"
ORDER BY "LanguageCode";

-- Show the new keys we just added
SELECT 
    "LanguageCode",
    "Key",
    "Value"
FROM amesa_content.translations 
WHERE "CreatedBy" = 'missing-keys'
ORDER BY "LanguageCode", "Key";

