-- Comprehensive translations for all 470 frontend keys across 5 languages
-- This file contains translations for: English (en), Hebrew (he), Arabic (ar), Spanish (es), French (fr)

INSERT INTO amesa_content.translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
    -- Navigation
    (uuid_generate_v4(), 'en', 'nav.lotteries', 'Lotteries', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.lotteries', 'הגרלות', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.lotteries', 'اليانصيب', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.lotteries', 'Loterías', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.lotteries', 'Loteries', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.promotions', 'Promotions', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.promotions', 'מבצעים', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.promotions', 'العروض الترويجية', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.promotions', 'Promociones', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.promotions', 'Promotions', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.winners', 'Winners', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.winners', 'זוכים', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.winners', 'الفائزون', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.winners', 'Ganadores', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.winners', 'Gagnants', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.signIn', 'Sign In', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.signIn', 'התחברות', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.signIn', 'تسجيل الدخول', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.signIn', 'Iniciar Sesión', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.signIn', 'Se Connecter', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.logout', 'Logout', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.logout', 'התנתקות', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.logout', 'تسجيل الخروج', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.logout', 'Cerrar Sesión', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.logout', 'Se Déconnecter', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.welcome', 'Welcome', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.welcome', 'ברוך הבא', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.welcome', 'مرحبا', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.welcome', 'Bienvenido', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.welcome', 'Bienvenue', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.memberSettings', 'Member Settings', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.memberSettings', 'הגדרות חבר', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.memberSettings', 'إعدادات العضو', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.memberSettings', 'Configuración de Miembro', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.memberSettings', 'Paramètres du Membre', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'nav.home', 'Home', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'nav.home', 'בית', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'nav.home', 'الرئيسية', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'nav.home', 'Inicio', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'nav.home', 'Accueil', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),

    -- Hero Section
    (uuid_generate_v4(), 'en', 'hero.title', 'Win Your Dream Home', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'hero.title', 'זכה בבית החלומות שלך', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'hero.title', 'اربح منزل أحلامك', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'hero.title', 'Gana la Casa de Tus Sueños', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'hero.title', 'Gagnez la Maison de Vos Rêves', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'hero.browseLotteries', 'Browse Lotteries', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'hero.browseLotteries', 'עיין בהגרלות', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'hero.browseLotteries', 'تصفح اليانصيب', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'hero.browseLotteries', 'Explorar Loterías', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'hero.browseLotteries', 'Parcourir les Loteries', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'hero.howItWorks', 'How It Works', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'hero.howItWorks', 'איך זה עובד', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'hero.howItWorks', 'كيف يعمل', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'hero.howItWorks', 'Cómo Funciona', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'hero.howItWorks', 'Comment Ça Marche', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),

    -- Authentication
    (uuid_generate_v4(), 'en', 'auth.signIn', 'Sign In', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.signIn', 'התחברות', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.signIn', 'تسجيل الدخول', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.signIn', 'Iniciar Sesión', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.signIn', 'Se Connecter', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.signUp', 'Sign Up', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.signUp', 'הרשמה', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.signUp', 'التسجيل', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.signUp', 'Registrarse', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.signUp', 'S''inscrire', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.createAccount', 'Create Account', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.createAccount', 'צור חשבון', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.createAccount', 'إنشاء حساب', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.createAccount', 'Crear Cuenta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.createAccount', 'Créer un Compte', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.email', 'Email', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.email', 'אימייל', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.email', 'البريد الإلكتروني', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.email', 'Correo Electrónico', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.email', 'Email', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.password', 'Password', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.password', 'סיסמה', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.password', 'كلمة المرور', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.password', 'Contraseña', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.password', 'Mot de Passe', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.fullName', 'Full Name', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.fullName', 'שם מלא', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.fullName', 'الاسم الكامل', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.fullName', 'Nombre Completo', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.fullName', 'Nom Complet', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.forgotPassword', 'Forgot Password?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.forgotPassword', 'שכחת סיסמה?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.forgotPassword', 'نسيت كلمة المرور؟', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.forgotPassword', '¿Olvidaste tu Contraseña?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.forgotPassword', 'Mot de Passe Oublié?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.processing', 'Processing...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.processing', 'מעבד...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.processing', 'جاري المعالجة...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.processing', 'Procesando...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.processing', 'Traitement...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.dontHaveAccount', 'Don''t have an account?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.dontHaveAccount', 'אין לך חשבון?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.dontHaveAccount', 'ليس لديك حساب؟', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.dontHaveAccount', '¿No tienes una cuenta?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.dontHaveAccount', 'Vous n''avez pas de compte?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.alreadyHaveAccount', 'Already have an account?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.alreadyHaveAccount', 'כבר יש לך חשבון?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.alreadyHaveAccount', 'هل لديك حساب بالفعل؟', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.alreadyHaveAccount', '¿Ya tienes una cuenta?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.alreadyHaveAccount', 'Vous avez déjà un compte?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.continueWithGoogle', 'Continue with Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.continueWithGoogle', 'המשך עם Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.continueWithGoogle', 'المتابعة مع Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.continueWithGoogle', 'Continuar con Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.continueWithGoogle', 'Continuer avec Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.continueWithMeta', 'Continue with Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.continueWithMeta', 'המשך עם Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.continueWithMeta', 'المتابعة مع Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.continueWithMeta', 'Continuar con Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.continueWithMeta', 'Continuer avec Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.continueWithApple', 'Continue with Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.continueWithApple', 'המשך עם Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.continueWithApple', 'المتابعة مع Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.continueWithApple', 'Continuar con Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.continueWithApple', 'Continuer avec Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    
    (uuid_generate_v4(), 'en', 'auth.or', 'OR', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'he', 'auth.or', 'או', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'ar', 'auth.or', 'أو', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'es', 'auth.or', 'O', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'fr', 'auth.or', 'OU', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System');
