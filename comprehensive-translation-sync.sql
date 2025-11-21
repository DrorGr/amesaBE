-- Comprehensive Translation Sync Script
-- This script extracts all existing English keys and creates translations for them in all 6 languages

-- First, let's see what English keys we have
SELECT 
    "Key",
    "Value" as "EnglishValue",
    "Category"
FROM amesa_content.translations 
WHERE "LanguageCode" = 'en'
ORDER BY "Category", "Key";

-- Create a temporary table with all English keys and their translations in all languages
WITH english_keys AS (
    SELECT DISTINCT "Key", "Category", "Description"
    FROM amesa_content.translations 
    WHERE "LanguageCode" = 'en'
),
language_codes AS (
    SELECT unnest(ARRAY['en', 'he', 'ar', 'es', 'fr', 'pl']) as lang_code
),
all_combinations AS (
    SELECT 
        ek."Key",
        ek."Category",
        ek."Description",
        lc.lang_code
    FROM english_keys ek
    CROSS JOIN language_codes lc
)
-- Insert comprehensive translations for all existing English keys
INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
SELECT 
    gen_random_uuid(),
    ac.lang_code,
    ac."Key",
    CASE 
        -- Navigation translations
        WHEN ac."Key" = 'nav.home' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Home'
                WHEN 'he' THEN 'בית'
                WHEN 'ar' THEN 'الرئيسية'
                WHEN 'es' THEN 'Inicio'
                WHEN 'fr' THEN 'Accueil'
                WHEN 'pl' THEN 'Strona główna'
            END
        WHEN ac."Key" = 'nav.about' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'About'
                WHEN 'he' THEN 'אודות'
                WHEN 'ar' THEN 'حول'
                WHEN 'es' THEN 'Acerca de'
                WHEN 'fr' THEN 'À propos'
                WHEN 'pl' THEN 'O nas'
            END
        WHEN ac."Key" = 'nav.contact' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Contact'
                WHEN 'he' THEN 'צור קשר'
                WHEN 'ar' THEN 'اتصل بنا'
                WHEN 'es' THEN 'Contacto'
                WHEN 'fr' THEN 'Contact'
                WHEN 'pl' THEN 'Kontakt'
            END
        WHEN ac."Key" = 'nav.login' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Login'
                WHEN 'he' THEN 'התחברות'
                WHEN 'ar' THEN 'تسجيل الدخول'
                WHEN 'es' THEN 'Iniciar sesión'
                WHEN 'fr' THEN 'Se connecter'
                WHEN 'pl' THEN 'Zaloguj się'
            END
        WHEN ac."Key" = 'nav.register' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Register'
                WHEN 'he' THEN 'הרשמה'
                WHEN 'ar' THEN 'التسجيل'
                WHEN 'es' THEN 'Registrarse'
                WHEN 'fr' THEN 'S''inscrire'
                WHEN 'pl' THEN 'Zarejestruj się'
            END
        WHEN ac."Key" = 'nav.logout' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Logout'
                WHEN 'he' THEN 'התנתקות'
                WHEN 'ar' THEN 'تسجيل الخروج'
                WHEN 'es' THEN 'Cerrar sesión'
                WHEN 'fr' THEN 'Se déconnecter'
                WHEN 'pl' THEN 'Wyloguj się'
            END
        WHEN ac."Key" = 'nav.profile' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Profile'
                WHEN 'he' THEN 'פרופיל'
                WHEN 'ar' THEN 'الملف الشخصي'
                WHEN 'es' THEN 'Perfil'
                WHEN 'fr' THEN 'Profil'
                WHEN 'pl' THEN 'Profil'
            END
        WHEN ac."Key" = 'nav.settings' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Settings'
                WHEN 'he' THEN 'הגדרות'
                WHEN 'ar' THEN 'الإعدادات'
                WHEN 'es' THEN 'Configuración'
                WHEN 'fr' THEN 'Paramètres'
                WHEN 'pl' THEN 'Ustawienia'
            END
        WHEN ac."Key" = 'nav.dashboard' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Dashboard'
                WHEN 'he' THEN 'לוח בקרה'
                WHEN 'ar' THEN 'لوحة التحكم'
                WHEN 'es' THEN 'Panel de control'
                WHEN 'fr' THEN 'Tableau de bord'
                WHEN 'pl' THEN 'Panel kontrolny'
            END
        WHEN ac."Key" = 'nav.lottery' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Lottery'
                WHEN 'he' THEN 'הגרלה'
                WHEN 'ar' THEN 'اليانصيب'
                WHEN 'es' THEN 'Lotería'
                WHEN 'fr' THEN 'Loterie'
                WHEN 'pl' THEN 'Loteria'
            END
        
        -- Common UI elements
        WHEN ac."Key" = 'common.save' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Save'
                WHEN 'he' THEN 'שמור'
                WHEN 'ar' THEN 'حفظ'
                WHEN 'es' THEN 'Guardar'
                WHEN 'fr' THEN 'Enregistrer'
                WHEN 'pl' THEN 'Zapisz'
            END
        WHEN ac."Key" = 'common.cancel' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Cancel'
                WHEN 'he' THEN 'בטל'
                WHEN 'ar' THEN 'إلغاء'
                WHEN 'es' THEN 'Cancelar'
                WHEN 'fr' THEN 'Annuler'
                WHEN 'pl' THEN 'Anuluj'
            END
        WHEN ac."Key" = 'common.delete' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Delete'
                WHEN 'he' THEN 'מחק'
                WHEN 'ar' THEN 'حذف'
                WHEN 'es' THEN 'Eliminar'
                WHEN 'fr' THEN 'Supprimer'
                WHEN 'pl' THEN 'Usuń'
            END
        WHEN ac."Key" = 'common.edit' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Edit'
                WHEN 'he' THEN 'ערוך'
                WHEN 'ar' THEN 'تحرير'
                WHEN 'es' THEN 'Editar'
                WHEN 'fr' THEN 'Modifier'
                WHEN 'pl' THEN 'Edytuj'
            END
        WHEN ac."Key" = 'common.submit' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Submit'
                WHEN 'he' THEN 'שלח'
                WHEN 'ar' THEN 'إرسال'
                WHEN 'es' THEN 'Enviar'
                WHEN 'fr' THEN 'Soumettre'
                WHEN 'pl' THEN 'Wyślij'
            END
        WHEN ac."Key" = 'common.loading' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Loading...'
                WHEN 'he' THEN 'טוען...'
                WHEN 'ar' THEN 'جاري التحميل...'
                WHEN 'es' THEN 'Cargando...'
                WHEN 'fr' THEN 'Chargement...'
                WHEN 'pl' THEN 'Ładowanie...'
            END
        WHEN ac."Key" = 'common.error' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Error'
                WHEN 'he' THEN 'שגיאה'
                WHEN 'ar' THEN 'خطأ'
                WHEN 'es' THEN 'Error'
                WHEN 'fr' THEN 'Erreur'
                WHEN 'pl' THEN 'Błąd'
            END
        WHEN ac."Key" = 'common.success' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Success'
                WHEN 'he' THEN 'הצלחה'
                WHEN 'ar' THEN 'نجح'
                WHEN 'es' THEN 'Éxito'
                WHEN 'fr' THEN 'Succès'
                WHEN 'pl' THEN 'Sukces'
            END
        WHEN ac."Key" = 'common.warning' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Warning'
                WHEN 'he' THEN 'אזהרה'
                WHEN 'ar' THEN 'تحذير'
                WHEN 'es' THEN 'Advertencia'
                WHEN 'fr' THEN 'Avertissement'
                WHEN 'pl' THEN 'Ostrzeżenie'
            END
        WHEN ac."Key" = 'common.info' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Information'
                WHEN 'he' THEN 'מידע'
                WHEN 'ar' THEN 'معلومات'
                WHEN 'es' THEN 'Información'
                WHEN 'fr' THEN 'Information'
                WHEN 'pl' THEN 'Informacja'
            END
        WHEN ac."Key" = 'common.yes' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Yes'
                WHEN 'he' THEN 'כן'
                WHEN 'ar' THEN 'نعم'
                WHEN 'es' THEN 'Sí'
                WHEN 'fr' THEN 'Oui'
                WHEN 'pl' THEN 'Tak'
            END
        WHEN ac."Key" = 'common.no' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'No'
                WHEN 'he' THEN 'לא'
                WHEN 'ar' THEN 'لا'
                WHEN 'es' THEN 'No'
                WHEN 'fr' THEN 'Non'
                WHEN 'pl' THEN 'Nie'
            END
        WHEN ac."Key" = 'common.ok' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'OK'
                WHEN 'he' THEN 'אישור'
                WHEN 'ar' THEN 'موافق'
                WHEN 'es' THEN 'Aceptar'
                WHEN 'fr' THEN 'OK'
                WHEN 'pl' THEN 'OK'
            END
        WHEN ac."Key" = 'common.close' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Close'
                WHEN 'he' THEN 'סגור'
                WHEN 'ar' THEN 'إغلاق'
                WHEN 'es' THEN 'Cerrar'
                WHEN 'fr' THEN 'Fermer'
                WHEN 'pl' THEN 'Zamknij'
            END
        
        -- Form fields
        WHEN ac."Key" = 'form.email' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Email'
                WHEN 'he' THEN 'אימייל'
                WHEN 'ar' THEN 'البريد الإلكتروني'
                WHEN 'es' THEN 'Correo electrónico'
                WHEN 'fr' THEN 'Email'
                WHEN 'pl' THEN 'Email'
            END
        WHEN ac."Key" = 'form.password' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Password'
                WHEN 'he' THEN 'סיסמה'
                WHEN 'ar' THEN 'كلمة المرور'
                WHEN 'es' THEN 'Contraseña'
                WHEN 'fr' THEN 'Mot de passe'
                WHEN 'pl' THEN 'Hasło'
            END
        WHEN ac."Key" = 'form.confirmPassword' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Confirm Password'
                WHEN 'he' THEN 'אישור סיסמה'
                WHEN 'ar' THEN 'تأكيد كلمة المرور'
                WHEN 'es' THEN 'Confirmar contraseña'
                WHEN 'fr' THEN 'Confirmer le mot de passe'
                WHEN 'pl' THEN 'Potwierdź hasło'
            END
        WHEN ac."Key" = 'form.firstName' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'First Name'
                WHEN 'he' THEN 'שם פרטי'
                WHEN 'ar' THEN 'الاسم الأول'
                WHEN 'es' THEN 'Nombre'
                WHEN 'fr' THEN 'Prénom'
                WHEN 'pl' THEN 'Imię'
            END
        WHEN ac."Key" = 'form.lastName' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Last Name'
                WHEN 'he' THEN 'שם משפחה'
                WHEN 'ar' THEN 'اسم العائلة'
                WHEN 'es' THEN 'Apellido'
                WHEN 'fr' THEN 'Nom de famille'
                WHEN 'pl' THEN 'Nazwisko'
            END
        WHEN ac."Key" = 'form.phone' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Phone Number'
                WHEN 'he' THEN 'טלפון'
                WHEN 'ar' THEN 'رقم الهاتف'
                WHEN 'es' THEN 'Número de teléfono'
                WHEN 'fr' THEN 'Numéro de téléphone'
                WHEN 'pl' THEN 'Telefon'
            END
        WHEN ac."Key" = 'form.address' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Address'
                WHEN 'he' THEN 'כתובת'
                WHEN 'ar' THEN 'العنوان'
                WHEN 'es' THEN 'Dirección'
                WHEN 'fr' THEN 'Adresse'
                WHEN 'pl' THEN 'Adres'
            END
        WHEN ac."Key" = 'form.city' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'City'
                WHEN 'he' THEN 'עיר'
                WHEN 'ar' THEN 'المدينة'
                WHEN 'es' THEN 'Ciudad'
                WHEN 'fr' THEN 'Ville'
                WHEN 'pl' THEN 'Miasto'
            END
        WHEN ac."Key" = 'form.country' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Country'
                WHEN 'he' THEN 'מדינה'
                WHEN 'ar' THEN 'البلد'
                WHEN 'es' THEN 'País'
                WHEN 'fr' THEN 'Pays'
                WHEN 'pl' THEN 'Kraj'
            END
        WHEN ac."Key" = 'form.required' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'This field is required'
                WHEN 'he' THEN 'שדה זה נדרש'
                WHEN 'ar' THEN 'هذا الحقل مطلوب'
                WHEN 'es' THEN 'Este campo es obligatorio'
                WHEN 'fr' THEN 'Ce champ est requis'
                WHEN 'pl' THEN 'To pole jest wymagane'
            END
        
        -- Lottery specific
        WHEN ac."Key" = 'lottery.title' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Win Your Dream Home'
                WHEN 'he' THEN 'זכה בבית החלומות שלך'
                WHEN 'ar' THEN 'اربح منزل أحلامك'
                WHEN 'es' THEN 'Gana la casa de tus sueños'
                WHEN 'fr' THEN 'Gagnez la maison de vos rêves'
                WHEN 'pl' THEN 'Wygraj dom swoich marzeń'
            END
        WHEN ac."Key" = 'lottery.subtitle' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Enter our lottery for a chance to win amazing properties'
                WHEN 'he' THEN 'הכנס להגרלה שלנו לזכות בנכסים מדהימים'
                WHEN 'ar' THEN 'ادخل في يانصيبنا للفوز بعقارات مذهلة'
                WHEN 'es' THEN 'Participa en nuestra lotería para ganar propiedades increíbles'
                WHEN 'fr' THEN 'Participez à notre loterie pour gagner des propriétés incroyables'
                WHEN 'pl' THEN 'Weź udział w naszej loterii, aby wygrać niesamowite nieruchomości'
            END
        WHEN ac."Key" = 'lottery.buyTicket' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Buy Ticket'
                WHEN 'he' THEN 'קנה כרטיס'
                WHEN 'ar' THEN 'شراء تذكرة'
                WHEN 'es' THEN 'Comprar boleto'
                WHEN 'fr' THEN 'Acheter un billet'
                WHEN 'pl' THEN 'Kup bilet'
            END
        WHEN ac."Key" = 'lottery.ticketPrice' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Ticket Price'
                WHEN 'he' THEN 'מחיר כרטיס'
                WHEN 'ar' THEN 'سعر التذكرة'
                WHEN 'es' THEN 'Precio del boleto'
                WHEN 'fr' THEN 'Prix du billet'
                WHEN 'pl' THEN 'Cena biletu'
            END
        WHEN ac."Key" = 'lottery.drawDate' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Draw Date'
                WHEN 'he' THEN 'תאריך הגרלה'
                WHEN 'ar' THEN 'تاريخ السحب'
                WHEN 'es' THEN 'Fecha del sorteo'
                WHEN 'fr' THEN 'Date du tirage'
                WHEN 'pl' THEN 'Data losowania'
            END
        WHEN ac."Key" = 'lottery.ticketsLeft' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Tickets Remaining'
                WHEN 'he' THEN 'כרטיסים נותרו'
                WHEN 'ar' THEN 'التذاكر المتبقية'
                WHEN 'es' THEN 'Boletos restantes'
                WHEN 'fr' THEN 'Billets restants'
                WHEN 'pl' THEN 'Pozostałe bilety'
            END
        WHEN ac."Key" = 'lottery.winner' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Winner'
                WHEN 'he' THEN 'זוכה'
                WHEN 'ar' THEN 'الفائز'
                WHEN 'es' THEN 'Ganador'
                WHEN 'fr' THEN 'Gagnant'
                WHEN 'pl' THEN 'Zwycięzca'
            END
        WHEN ac."Key" = 'lottery.results' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'Results'
                WHEN 'he' THEN 'תוצאות'
                WHEN 'ar' THEN 'النتائج'
                WHEN 'es' THEN 'Resultados'
                WHEN 'fr' THEN 'Résultats'
                WHEN 'pl' THEN 'Wyniki'
            END
        WHEN ac."Key" = 'lottery.myTickets' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'My Tickets'
                WHEN 'he' THEN 'הכרטיסים שלי'
                WHEN 'ar' THEN 'تذاكري'
                WHEN 'es' THEN 'Mis boletos'
                WHEN 'fr' THEN 'Mes billets'
                WHEN 'pl' THEN 'Moje bilety'
            END
        WHEN ac."Key" = 'lottery.history' THEN 
            CASE ac.lang_code 
                WHEN 'en' THEN 'History'
                WHEN 'he' THEN 'היסטוריה'
                WHEN 'ar' THEN 'التاريخ'
                WHEN 'es' THEN 'Historial'
                WHEN 'fr' THEN 'Historique'
                WHEN 'pl' THEN 'Historia'
            END
        
        -- For any other keys not explicitly mapped, use a generic approach
        ELSE 
            CASE ac.lang_code 
                WHEN 'en' THEN (SELECT "Value" FROM amesa_content.translations WHERE "LanguageCode" = 'en' AND "Key" = ac."Key" LIMIT 1)
                WHEN 'he' THEN '[HE] ' || ac."Key"
                WHEN 'ar' THEN '[AR] ' || ac."Key"
                WHEN 'es' THEN '[ES] ' || ac."Key"
                WHEN 'fr' THEN '[FR] ' || ac."Key"
                WHEN 'pl' THEN '[PL] ' || ac."Key"
            END
    END as "Value",
    ac."Description",
    ac."Category",
    true as "IsActive",
    NOW() as "CreatedAt",
    NOW() as "UpdatedAt",
    'comprehensive-sync' as "CreatedBy",
    'comprehensive-sync' as "UpdatedBy"
FROM all_combinations ac
WHERE NOT EXISTS (
    SELECT 1 FROM amesa_content.translations t 
    WHERE t."LanguageCode" = ac.lang_code 
    AND t."Key" = ac."Key"
)
ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Verify the results
SELECT 
    "LanguageCode",
    COUNT(*) as translation_count
FROM amesa_content.translations 
GROUP BY "LanguageCode"
ORDER BY "LanguageCode";

-- Show sample of newly created translations
SELECT 
    "LanguageCode",
    "Key",
    "Value",
    "Category"
FROM amesa_content.translations 
WHERE "CreatedBy" = 'comprehensive-sync'
ORDER BY "LanguageCode", "Key"
LIMIT 30;
