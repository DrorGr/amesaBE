# PowerShell script to generate complete translation SQL for all 507 keys

# Read all translation keys
$keys = Get-Content "BE/all-translation-keys.txt"

# Define language mappings with common translations
$languages = @{
    'en' = @{
        'name' = 'English'
        'common' = @{
            'save' = 'Save'
            'cancel' = 'Cancel'
            'delete' = 'Delete'
            'edit' = 'Edit'
            'close' = 'Close'
            'yes' = 'Yes'
            'no' = 'No'
            'ok' = 'OK'
            'error' = 'Error'
            'success' = 'Success'
            'loading' = 'Loading...'
            'home' = 'Home'
            'about' = 'About'
            'contact' = 'Contact'
            'login' = 'Login'
            'logout' = 'Logout'
            'register' = 'Register'
            'profile' = 'Profile'
            'settings' = 'Settings'
            'help' = 'Help'
            'support' = 'Support'
        }
    }
    'he' = @{
        'name' = 'Hebrew'
        'common' = @{
            'save' = 'שמור'
            'cancel' = 'בטל'
            'delete' = 'מחק'
            'edit' = 'ערוך'
            'close' = 'סגור'
            'yes' = 'כן'
            'no' = 'לא'
            'ok' = 'אישור'
            'error' = 'שגיאה'
            'success' = 'הצלחה'
            'loading' = 'טוען...'
            'home' = 'בית'
            'about' = 'אודות'
            'contact' = 'צור קשר'
            'login' = 'התחברות'
            'logout' = 'התנתקות'
            'register' = 'הרשמה'
            'profile' = 'פרופיל'
            'settings' = 'הגדרות'
            'help' = 'עזרה'
            'support' = 'תמיכה'
        }
    }
    'ar' = @{
        'name' = 'Arabic'
        'common' = @{
            'save' = 'حفظ'
            'cancel' = 'إلغاء'
            'delete' = 'حذف'
            'edit' = 'تحرير'
            'close' = 'إغلاق'
            'yes' = 'نعم'
            'no' = 'لا'
            'ok' = 'موافق'
            'error' = 'خطأ'
            'success' = 'نجح'
            'loading' = 'جاري التحميل...'
            'home' = 'الرئيسية'
            'about' = 'حول'
            'contact' = 'اتصل بنا'
            'login' = 'تسجيل الدخول'
            'logout' = 'تسجيل الخروج'
            'register' = 'التسجيل'
            'profile' = 'الملف الشخصي'
            'settings' = 'الإعدادات'
            'help' = 'المساعدة'
            'support' = 'الدعم'
        }
    }
    'es' = @{
        'name' = 'Spanish'
        'common' = @{
            'save' = 'Guardar'
            'cancel' = 'Cancelar'
            'delete' = 'Eliminar'
            'edit' = 'Editar'
            'close' = 'Cerrar'
            'yes' = 'Sí'
            'no' = 'No'
            'ok' = 'Aceptar'
            'error' = 'Error'
            'success' = 'Éxito'
            'loading' = 'Cargando...'
            'home' = 'Inicio'
            'about' = 'Acerca de'
            'contact' = 'Contacto'
            'login' = 'Iniciar sesión'
            'logout' = 'Cerrar sesión'
            'register' = 'Registrarse'
            'profile' = 'Perfil'
            'settings' = 'Configuración'
            'help' = 'Ayuda'
            'support' = 'Soporte'
        }
    }
    'fr' = @{
        'name' = 'French'
        'common' = @{
            'save' = 'Enregistrer'
            'cancel' = 'Annuler'
            'delete' = 'Supprimer'
            'edit' = 'Modifier'
            'close' = 'Fermer'
            'yes' = 'Oui'
            'no' = 'Non'
            'ok' = 'OK'
            'error' = 'Erreur'
            'success' = 'Succès'
            'loading' = 'Chargement...'
            'home' = 'Accueil'
            'about' = 'À propos'
            'contact' = 'Contact'
            'login' = 'Se connecter'
            'logout' = 'Se déconnecter'
            'register' = 'S''inscrire'
            'profile' = 'Profil'
            'settings' = 'Paramètres'
            'help' = 'Aide'
            'support' = 'Support'
        }
    }
    'pl' = @{
        'name' = 'Polish'
        'common' = @{
            'save' = 'Zapisz'
            'cancel' = 'Anuluj'
            'delete' = 'Usuń'
            'edit' = 'Edytuj'
            'close' = 'Zamknij'
            'yes' = 'Tak'
            'no' = 'Nie'
            'ok' = 'OK'
            'error' = 'Błąd'
            'success' = 'Sukces'
            'loading' = 'Ładowanie...'
            'home' = 'Strona główna'
            'about' = 'O nas'
            'contact' = 'Kontakt'
            'login' = 'Zaloguj się'
            'logout' = 'Wyloguj się'
            'register' = 'Zarejestruj się'
            'profile' = 'Profil'
            'settings' = 'Ustawienia'
            'help' = 'Pomoc'
            'support' = 'Wsparcie'
        }
    }
}

# Function to get translation for a key
function Get-Translation {
    param($key, $langCode)
    
    # Extract the last part of the key for common word matching
    $lastPart = $key.Split('.')[-1]
    
    # Check if we have a common translation
    if ($languages[$langCode]['common'].ContainsKey($lastPart)) {
        return $languages[$langCode]['common'][$lastPart]
    }
    
    # For specific keys, provide targeted translations
    switch -Regex ($key) {
        '^hero\.title$' {
            switch ($langCode) {
                'en' { return 'Win Your Dream Home' }
                'he' { return 'זכה בבית החלומות שלך' }
                'ar' { return 'اربح منزل أحلامك' }
                'es' { return 'Gana la casa de tus sueños' }
                'fr' { return 'Gagnez la maison de vos rêves' }
                'pl' { return 'Wygraj dom swoich marzeń' }
            }
        }
        '^hero\.browseLotteries$' {
            switch ($langCode) {
                'en' { return 'Browse Lotteries' }
                'he' { return 'עיין בהגרלות' }
                'ar' { return 'تصفح اليانصيب' }
                'es' { return 'Explorar loterías' }
                'fr' { return 'Parcourir les loteries' }
                'pl' { return 'Przeglądaj loterie' }
            }
        }
        '^hero\.howItWorks$' {
            switch ($langCode) {
                'en' { return 'How It Works' }
                'he' { return 'איך זה עובד' }
                'ar' { return 'كيف يعمل' }
                'es' { return 'Cómo funciona' }
                'fr' { return 'Comment ça marche' }
                'pl' { return 'Jak to działa' }
            }
        }
        '^house\.active$' {
            switch ($langCode) {
                'en' { return 'Active' }
                'he' { return 'פעיל' }
                'ar' { return 'نشط' }
                'es' { return 'Activo' }
                'fr' { return 'Actif' }
                'pl' { return 'Aktywny' }
            }
        }
        default {
            # For unmapped keys, create a placeholder with language prefix
            return "[$($langCode.ToUpper())] $key"
        }
    }
}

# Generate SQL
$sql = @"
-- Complete Translation Sync Script - ALL 507 KEYS
-- Generated automatically from frontend scan
-- Ensures all 6 languages have identical translation keys

-- Show current state
SELECT 
    'BEFORE SYNC' as status,
    "LanguageCode",
    COUNT(*) as translation_count
FROM amesa_content.translations 
GROUP BY "LanguageCode"
ORDER BY "LanguageCode";

-- Insert all translations
INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
"@

$valueLines = @()

foreach ($key in $keys) {
    if ($key.Trim() -eq "") { continue }
    
    # Determine category from key prefix
    $category = "General"
    if ($key.StartsWith("nav.")) { $category = "Navigation" }
    elseif ($key.StartsWith("hero.")) { $category = "Hero" }
    elseif ($key.StartsWith("footer.")) { $category = "Footer" }
    elseif ($key.StartsWith("house.")) { $category = "House" }
    elseif ($key.StartsWith("auth.")) { $category = "Authentication" }
    elseif ($key.StartsWith("preferences.")) { $category = "Preferences" }
    elseif ($key.StartsWith("about.")) { $category = "About" }
    elseif ($key.StartsWith("help.")) { $category = "Help" }
    elseif ($key.StartsWith("faq.")) { $category = "FAQ" }
    elseif ($key.StartsWith("sponsor.")) { $category = "Sponsor" }
    elseif ($key.StartsWith("member.")) { $category = "Member" }
    elseif ($key.StartsWith("register.")) { $category = "Register" }
    elseif ($key.StartsWith("lotteryResults.")) { $category = "LotteryResults" }
    elseif ($key.StartsWith("promotions.")) { $category = "Promotions" }
    elseif ($key.StartsWith("responsible.")) { $category = "Responsible" }
    elseif ($key.StartsWith("partners.")) { $category = "Partners" }
    elseif ($key.StartsWith("accessibility.")) { $category = "Accessibility" }
    elseif ($key.StartsWith("chatbot.")) { $category = "Chatbot" }
    elseif ($key.StartsWith("carousel.")) { $category = "Carousel" }
    elseif ($key.StartsWith("houses.")) { $category = "Houses" }
    elseif ($key.StartsWith("howItWorks.")) { $category = "HowItWorks" }
    
    foreach ($langCode in $languages.Keys) {
        $translation = Get-Translation -key $key -langCode $langCode
        $escapedTranslation = $translation -replace "'", "''"
        $escapedKey = $key -replace "'", "''"
        
        $valueLines += "    (gen_random_uuid(), '$langCode', '$escapedKey', '$escapedTranslation', 'Auto-generated translation', '$category', true, NOW(), NOW(), 'auto-sync', 'auto-sync')"
    }
}

$sql += ($valueLines -join ",`n")
$sql += @"

ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Show final results
SELECT 
    'AFTER SYNC' as status,
    "LanguageCode",
    COUNT(*) as translation_count
FROM amesa_content.translations 
GROUP BY "LanguageCode"
ORDER BY "LanguageCode";

-- Verify all languages have the same count
SELECT 
    CASE 
        WHEN COUNT(DISTINCT translation_count) = 1 THEN 'SUCCESS: All languages have equal translations (' || MAX(translation_count) || ' each)'
        ELSE 'WARNING: Languages have different translation counts'
    END as sync_status
FROM (
    SELECT "LanguageCode", COUNT(*) as translation_count
    FROM amesa_content.translations 
    GROUP BY "LanguageCode"
) counts;

-- Show breakdown by category
SELECT 
    "Category",
    COUNT(*) / 6 as keys_per_category,
    COUNT(*) as total_translations
FROM amesa_content.translations 
WHERE "CreatedBy" = 'auto-sync'
GROUP BY "Category"
ORDER BY keys_per_category DESC;
"@

# Save to file
$sql | Out-File -FilePath "BE/complete-507-translations.sql" -Encoding UTF8

Write-Host "Generated complete SQL script with all 507 keys for 6 languages"
Write-Host "Total translations to be created: $($keys.Count * 6)"
Write-Host "Saved to: BE/complete-507-translations.sql"
