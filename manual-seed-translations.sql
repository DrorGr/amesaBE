-- Manual Translation Seeding Script for Public Schema
-- This script adds all 6 languages and their translations to the public.translations table

-- First, ensure we have all 6 languages in public.languages
INSERT INTO amesa_content.languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
VALUES 
    ('en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()),
    ('he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()),
    ('ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()),
    ('es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()),
    ('fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()),
    ('pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW())
ON CONFLICT ("Code") DO NOTHING;

-- Now insert comprehensive translations for all languages
-- Core Navigation & UI
INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
    -- English translations
    (gen_random_uuid(), 'en', 'nav.home', 'Home', 'Navigation - Home link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.about', 'About', 'Navigation - About link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.contact', 'Contact', 'Navigation - Contact link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.login', 'Login', 'Navigation - Login link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.register', 'Register', 'Navigation - Register link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.logout', 'Logout', 'Navigation - Logout link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.profile', 'Profile', 'Navigation - Profile link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.settings', 'Settings', 'Navigation - Settings link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.dashboard', 'Dashboard', 'Navigation - Dashboard link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'nav.lottery', 'Lottery', 'Navigation - Lottery link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    -- Common UI Elements
    (gen_random_uuid(), 'en', 'common.save', 'Save', 'Common - Save button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.cancel', 'Cancel', 'Common - Cancel button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.delete', 'Delete', 'Common - Delete button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.edit', 'Edit', 'Common - Edit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.submit', 'Submit', 'Common - Submit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.loading', 'Loading...', 'Common - Loading message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.error', 'Error', 'Common - Error message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.success', 'Success', 'Common - Success message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.warning', 'Warning', 'Common - Warning message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'common.info', 'Information', 'Common - Info message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    -- Forms
    (gen_random_uuid(), 'en', 'form.email', 'Email', 'Form - Email field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.password', 'Password', 'Form - Password field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.confirmPassword', 'Confirm Password', 'Form - Confirm password field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.firstName', 'First Name', 'Form - First name field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.lastName', 'Last Name', 'Form - Last name field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.phone', 'Phone Number', 'Form - Phone field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.address', 'Address', 'Form - Address field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.city', 'City', 'Form - City field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.country', 'Country', 'Form - Country field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'form.required', 'This field is required', 'Form - Required validation', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    -- Lottery specific
    (gen_random_uuid(), 'en', 'lottery.title', 'Win Your Dream Home', 'Lottery - Main title', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.subtitle', 'Enter our lottery for a chance to win amazing properties', 'Lottery - Subtitle', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.buyTicket', 'Buy Ticket', 'Lottery - Buy ticket button', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.ticketPrice', 'Ticket Price', 'Lottery - Ticket price label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.drawDate', 'Draw Date', 'Lottery - Draw date label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.ticketsLeft', 'Tickets Remaining', 'Lottery - Tickets left label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.winner', 'Winner', 'Lottery - Winner label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.results', 'Results', 'Lottery - Results label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.myTickets', 'My Tickets', 'Lottery - My tickets label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'en', 'lottery.history', 'History', 'Lottery - History label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),

    -- Hebrew translations
    (gen_random_uuid(), 'he', 'nav.home', 'בית', 'Navigation - Home link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.about', 'אודות', 'Navigation - About link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.contact', 'צור קשר', 'Navigation - Contact link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.login', 'התחברות', 'Navigation - Login link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.register', 'הרשמה', 'Navigation - Register link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.logout', 'התנתקות', 'Navigation - Logout link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.profile', 'פרופיל', 'Navigation - Profile link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.settings', 'הגדרות', 'Navigation - Settings link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.dashboard', 'לוח בקרה', 'Navigation - Dashboard link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'nav.lottery', 'הגרלה', 'Navigation - Lottery link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'he', 'common.save', 'שמור', 'Common - Save button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.cancel', 'בטל', 'Common - Cancel button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.delete', 'מחק', 'Common - Delete button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.edit', 'ערוך', 'Common - Edit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.submit', 'שלח', 'Common - Submit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.loading', 'טוען...', 'Common - Loading message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.error', 'שגיאה', 'Common - Error message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.success', 'הצלחה', 'Common - Success message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.warning', 'אזהרה', 'Common - Warning message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'common.info', 'מידע', 'Common - Info message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'he', 'form.email', 'אימייל', 'Form - Email field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.password', 'סיסמה', 'Form - Password field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.confirmPassword', 'אישור סיסמה', 'Form - Confirm password field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.firstName', 'שם פרטי', 'Form - First name field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.lastName', 'שם משפחה', 'Form - Last name field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.phone', 'טלפון', 'Form - Phone field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.address', 'כתובת', 'Form - Address field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.city', 'עיר', 'Form - City field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.country', 'מדינה', 'Form - Country field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'form.required', 'שדה זה נדרש', 'Form - Required validation', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'he', 'lottery.title', 'זכה בבית החלומות שלך', 'Lottery - Main title', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.subtitle', 'הכנס להגרלה שלנו לזכות בנכסים מדהימים', 'Lottery - Subtitle', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.buyTicket', 'קנה כרטיס', 'Lottery - Buy ticket button', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.ticketPrice', 'מחיר כרטיס', 'Lottery - Ticket price label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.drawDate', 'תאריך הגרלה', 'Lottery - Draw date label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.ticketsLeft', 'כרטיסים נותרו', 'Lottery - Tickets left label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.winner', 'זוכה', 'Lottery - Winner label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.results', 'תוצאות', 'Lottery - Results label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.myTickets', 'הכרטיסים שלי', 'Lottery - My tickets label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'he', 'lottery.history', 'היסטוריה', 'Lottery - History label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),

    -- Arabic translations
    (gen_random_uuid(), 'ar', 'nav.home', 'الرئيسية', 'Navigation - Home link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.about', 'حول', 'Navigation - About link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.contact', 'اتصل بنا', 'Navigation - Contact link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.login', 'تسجيل الدخول', 'Navigation - Login link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.register', 'التسجيل', 'Navigation - Register link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.logout', 'تسجيل الخروج', 'Navigation - Logout link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.profile', 'الملف الشخصي', 'Navigation - Profile link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.settings', 'الإعدادات', 'Navigation - Settings link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.dashboard', 'لوحة التحكم', 'Navigation - Dashboard link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'nav.lottery', 'اليانصيب', 'Navigation - Lottery link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'ar', 'common.save', 'حفظ', 'Common - Save button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.cancel', 'إلغاء', 'Common - Cancel button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.delete', 'حذف', 'Common - Delete button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.edit', 'تحرير', 'Common - Edit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.submit', 'إرسال', 'Common - Submit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.loading', 'جاري التحميل...', 'Common - Loading message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.error', 'خطأ', 'Common - Error message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.success', 'نجح', 'Common - Success message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.warning', 'تحذير', 'Common - Warning message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'common.info', 'معلومات', 'Common - Info message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'ar', 'lottery.title', 'اربح منزل أحلامك', 'Lottery - Main title', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'lottery.subtitle', 'ادخل في يانصيبنا للفوز بعقارات مذهلة', 'Lottery - Subtitle', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'ar', 'lottery.buyTicket', 'شراء تذكرة', 'Lottery - Buy ticket button', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),

    -- Spanish translations
    (gen_random_uuid(), 'es', 'nav.home', 'Inicio', 'Navigation - Home link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.about', 'Acerca de', 'Navigation - About link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.contact', 'Contacto', 'Navigation - Contact link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.login', 'Iniciar sesión', 'Navigation - Login link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.register', 'Registrarse', 'Navigation - Register link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.logout', 'Cerrar sesión', 'Navigation - Logout link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.profile', 'Perfil', 'Navigation - Profile link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.settings', 'Configuración', 'Navigation - Settings link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.dashboard', 'Panel de control', 'Navigation - Dashboard link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'nav.lottery', 'Lotería', 'Navigation - Lottery link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'es', 'common.save', 'Guardar', 'Common - Save button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'common.cancel', 'Cancelar', 'Common - Cancel button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'common.delete', 'Eliminar', 'Common - Delete button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'common.edit', 'Editar', 'Common - Edit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'common.submit', 'Enviar', 'Common - Submit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'common.loading', 'Cargando...', 'Common - Loading message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'es', 'lottery.title', 'Gana la casa de tus sueños', 'Lottery - Main title', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'lottery.subtitle', 'Participa en nuestra lotería para ganar propiedades increíbles', 'Lottery - Subtitle', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'es', 'lottery.buyTicket', 'Comprar boleto', 'Lottery - Buy ticket button', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),

    -- French translations
    (gen_random_uuid(), 'fr', 'nav.home', 'Accueil', 'Navigation - Home link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.about', 'À propos', 'Navigation - About link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.contact', 'Contact', 'Navigation - Contact link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.login', 'Se connecter', 'Navigation - Login link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.register', 'S''inscrire', 'Navigation - Register link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.logout', 'Se déconnecter', 'Navigation - Logout link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.profile', 'Profil', 'Navigation - Profile link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.settings', 'Paramètres', 'Navigation - Settings link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.dashboard', 'Tableau de bord', 'Navigation - Dashboard link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'nav.lottery', 'Loterie', 'Navigation - Lottery link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'fr', 'common.save', 'Enregistrer', 'Common - Save button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'common.cancel', 'Annuler', 'Common - Cancel button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'common.delete', 'Supprimer', 'Common - Delete button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'common.edit', 'Modifier', 'Common - Edit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'common.submit', 'Soumettre', 'Common - Submit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'common.loading', 'Chargement...', 'Common - Loading message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'fr', 'lottery.title', 'Gagnez la maison de vos rêves', 'Lottery - Main title', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'lottery.subtitle', 'Participez à notre loterie pour gagner des propriétés incroyables', 'Lottery - Subtitle', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'fr', 'lottery.buyTicket', 'Acheter un billet', 'Lottery - Buy ticket button', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),

    -- Polish translations (NEW!)
    (gen_random_uuid(), 'pl', 'nav.home', 'Strona główna', 'Navigation - Home link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.about', 'O nas', 'Navigation - About link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.contact', 'Kontakt', 'Navigation - Contact link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.login', 'Zaloguj się', 'Navigation - Login link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.register', 'Zarejestruj się', 'Navigation - Register link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.logout', 'Wyloguj się', 'Navigation - Logout link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.profile', 'Profil', 'Navigation - Profile link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.settings', 'Ustawienia', 'Navigation - Settings link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.dashboard', 'Panel kontrolny', 'Navigation - Dashboard link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'nav.lottery', 'Loteria', 'Navigation - Lottery link', 'Navigation', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'pl', 'common.save', 'Zapisz', 'Common - Save button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.cancel', 'Anuluj', 'Common - Cancel button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.delete', 'Usuń', 'Common - Delete button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.edit', 'Edytuj', 'Common - Edit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.submit', 'Wyślij', 'Common - Submit button', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.loading', 'Ładowanie...', 'Common - Loading message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.error', 'Błąd', 'Common - Error message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.success', 'Sukces', 'Common - Success message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.warning', 'Ostrzeżenie', 'Common - Warning message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'common.info', 'Informacja', 'Common - Info message', 'Common', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'pl', 'form.email', 'Email', 'Form - Email field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.password', 'Hasło', 'Form - Password field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.confirmPassword', 'Potwierdź hasło', 'Form - Confirm password field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.firstName', 'Imię', 'Form - First name field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.lastName', 'Nazwisko', 'Form - Last name field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.phone', 'Telefon', 'Form - Phone field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.address', 'Adres', 'Form - Address field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.city', 'Miasto', 'Form - City field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.country', 'Kraj', 'Form - Country field', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'form.required', 'To pole jest wymagane', 'Form - Required validation', 'Forms', true, NOW(), NOW(), 'seeder', 'seeder'),
    
    (gen_random_uuid(), 'pl', 'lottery.title', 'Wygraj dom swoich marzeń', 'Lottery - Main title', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.subtitle', 'Weź udział w naszej loterii, aby wygrać niesamowite nieruchomości', 'Lottery - Subtitle', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.buyTicket', 'Kup bilet', 'Lottery - Buy ticket button', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.ticketPrice', 'Cena biletu', 'Lottery - Ticket price label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.drawDate', 'Data losowania', 'Lottery - Draw date label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.ticketsLeft', 'Pozostałe bilety', 'Lottery - Tickets left label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.winner', 'Zwycięzca', 'Lottery - Winner label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.results', 'Wyniki', 'Lottery - Results label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.myTickets', 'Moje bilety', 'Lottery - My tickets label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder'),
    (gen_random_uuid(), 'pl', 'lottery.history', 'Historia', 'Lottery - History label', 'Lottery', true, NOW(), NOW(), 'seeder', 'seeder')

ON CONFLICT ("LanguageCode", "Key") DO NOTHING;

-- Verify the seeding worked
SELECT 
    "LanguageCode",
    COUNT(*) as translation_count
FROM amesa_content.translations 
GROUP BY "LanguageCode"
ORDER BY "LanguageCode";

-- Show sample translations
SELECT 
    "LanguageCode",
    "Key",
    "Value",
    "Category"
FROM amesa_content.translations 
WHERE "Key" IN ('nav.home', 'lottery.title', 'common.save')
ORDER BY "LanguageCode", "Key";
