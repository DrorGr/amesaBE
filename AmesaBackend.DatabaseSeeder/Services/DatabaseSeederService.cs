using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AmesaBackend.Data;
using AmesaBackend.Models;
using AmesaBackend.DatabaseSeeder.Models;
using AmesaBackend.DatabaseSeeder.Services;
using static AmesaBackend.Models.GenderType;
using static AmesaBackend.Models.UserStatus;
using static AmesaBackend.Models.UserVerificationStatus;
using static AmesaBackend.Models.AuthProvider;
using static AmesaBackend.Models.LotteryStatus;
using static AmesaBackend.Models.MediaType;

namespace AmesaBackend.DatabaseSeeder.Services
{
    public class DatabaseSeederService
    {
        private readonly AmesaDbContext _context;
        private readonly ILogger<DatabaseSeederService> _logger;
        private readonly SeederSettings _settings;

        public DatabaseSeederService(
            AmesaDbContext context,
            ILogger<DatabaseSeederService> logger,
            IOptions<SeederSettings> settings)
        {
            _context = context;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task SeedDatabaseAsync()
        {
            try
        {
                _logger.LogInformation("Starting database seeding process...");
                _logger.LogInformation($"Environment: {_settings.Environment}");
                _logger.LogInformation($"Truncate existing data: {_settings.TruncateExistingData}");

                // Ensure database exists and is accessible
                await EnsureDatabaseAsync();

                // Truncate existing data if configured
                if (_settings.TruncateExistingData)
                {
                    await TruncateExistingDataAsync();
                }

                // Seed data in correct order (respecting foreign key constraints)
                if (_settings.SeedLanguages)
                {
                    await SeedLanguagesAsync();
                }

                if (_settings.SeedTranslations)
                {
                    await SeedTranslationsAsync();
                }

                if (_settings.SeedUsers)
                {
                    await SeedUsersAsync();
                }

                if (_settings.SeedHouses)
                {
                    await SeedHousesAsync();
                }

                _logger.LogInformation("Database seeding completed successfully!");
                await LogSeedingResultsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during database seeding");
                throw;
            }
        }

        private async Task EnsureDatabaseAsync()
        {
            _logger.LogInformation("Ensuring database exists and is accessible...");
            
            try
            {
                await _context.Database.OpenConnectionAsync();
                await _context.Database.CloseConnectionAsync();
                _logger.LogInformation("Database connection successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to database");
                throw;
            }
        }

        private async Task TruncateExistingDataAsync()
        {
            _logger.LogInformation("Truncating existing data...");

            try
            {
                // Truncate in correct order to respect foreign key constraints
                // Using the correct schema-qualified table names from migrations
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_lottery.house_images CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_lottery.houses CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_auth.user_phones CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_auth.user_addresses CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE amesa_auth.users CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE public.translations CASCADE;");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE public.languages CASCADE;");

                _logger.LogInformation("Existing data truncated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error truncating existing data");
                throw;
            }
        }

        private async Task SeedLanguagesAsync()
        {
            _logger.LogInformation("Seeding languages...");

            var sql = @"
                INSERT INTO public.languages (""Code"", ""Name"", ""NativeName"", ""FlagUrl"", ""IsActive"", ""IsDefault"", ""DisplayOrder"", ""CreatedAt"", ""UpdatedAt"")
                VALUES 
                    ('en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()),
                    ('he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()),
                    ('ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()),
                    ('es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()),
                    ('fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()),
                    ('pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW())
                ON CONFLICT (""Code"") DO NOTHING;";

            await _context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("Seeded 6 languages");
        }

        private async Task SeedTranslationsAsync()
        {
            _logger.LogInformation("Seeding comprehensive translations from external SQL files...");

            // Check if translations already exist
            var existingTranslations = await _context.Translations.CountAsync();
            var polishTranslations = await _context.Translations.CountAsync(t => t.LanguageCode == "pl");
            
            _logger.LogInformation($"Existing translations: {existingTranslations}, Polish translations: {polishTranslations}");
            
            if (existingTranslations > 0 && polishTranslations > 0)
            {
                _logger.LogInformation($"Translations already exist including Polish ({polishTranslations} Polish translations found), skipping...");
                return;
            }

            try
            {
                // First, seed the main comprehensive translations (5 languages)
                var comprehensiveTranslationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "comprehensive-translations.sql");
                if (File.Exists(comprehensiveTranslationsPath))
                {
                    _logger.LogInformation("Loading comprehensive translations from file...");
                    var comprehensiveSql = await File.ReadAllTextAsync(comprehensiveTranslationsPath);
                    
                    // Add ON CONFLICT clause to handle duplicates
                    if (!comprehensiveSql.Contains("ON CONFLICT"))
                    {
                        comprehensiveSql = comprehensiveSql.TrimEnd(';') + " ON CONFLICT (\"LanguageCode\", \"Key\") DO NOTHING;";
                    }
                    
                    await _context.Database.ExecuteSqlRawAsync(comprehensiveSql);
                    _logger.LogInformation("Comprehensive translations seeded successfully");
                }
                else
                {
                    _logger.LogWarning($"Comprehensive translations file not found at: {comprehensiveTranslationsPath}");
                }

                // Then, seed Polish translations addon
                var polishTranslationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "polish-translations-addon.sql");
                if (File.Exists(polishTranslationsPath))
                {
                    _logger.LogInformation("Loading Polish translations addon from file...");
                    var polishSql = await File.ReadAllTextAsync(polishTranslationsPath);
                    
                    // Add ON CONFLICT clause to handle duplicates
                    if (!polishSql.Contains("ON CONFLICT"))
                    {
                        polishSql = polishSql.TrimEnd(';') + " ON CONFLICT (\"LanguageCode\", \"Key\") DO NOTHING;";
                    }
                    
                    await _context.Database.ExecuteSqlRawAsync(polishSql);
                    _logger.LogInformation("Polish translations seeded successfully");
                }
                else
                {
                    _logger.LogWarning($"Polish translations file not found at: {polishTranslationsPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding translations from files, falling back to inline translations");
                await SeedInlineTranslationsAsync();
            }
        }

        private async Task SeedInlineTranslationsAsync()
        {
            _logger.LogInformation("Seeding fallback inline translations...");

            var sql = @"
                INSERT INTO public.translations (""Id"", ""LanguageCode"", ""Key"", ""Value"", ""Description"", ""Category"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""CreatedBy"", ""UpdatedBy"")
                VALUES 
                    -- Navigation (5 languages)
                    (uuid_generate_v4(), 'en', 'nav.lotteries', 'Lotteries', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'nav.lotteries', 'הגרלות', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'nav.lotteries', 'اليانصيب', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'nav.lotteries', 'Loterías', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'nav.lotteries', 'Loteries', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.lotteries', 'Loterie', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'nav.promotions', 'Promotions', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'nav.promotions', 'מבצעים', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'nav.promotions', 'العروض الترويجية', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'nav.promotions', 'Promociones', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'nav.promotions', 'Promotions', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.promotions', 'Promocje', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    
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

                    -- Hero Section (5 languages)
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

                    -- Authentication (5 languages)
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
                    (uuid_generate_v4(), 'fr', 'auth.or', 'OU', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),

                    -- House/Property (5 languages)
                    (uuid_generate_v4(), 'en', 'house.currentlyViewing', 'Currently Viewing', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.currentlyViewing', 'צופה כעת', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.currentlyViewing', 'يشاهد حاليا', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.currentlyViewing', 'Viendo Actualmente', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.currentlyViewing', 'En Cours de Visualisation', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.propertyOfYourOwn', 'A property of your own', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.propertyOfYourOwn', 'נכס משלך', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.propertyOfYourOwn', 'عقار خاص بك', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.propertyOfYourOwn', 'Una propiedad propia', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.propertyOfYourOwn', 'Une propriété à vous', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.buyTicket', 'Buy Ticket', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.buyTicket', 'קנה כרטיס', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.buyTicket', 'شراء تذكرة', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.buyTicket', 'Comprar Boleto', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.buyTicket', 'Acheter un Billet', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.bed', 'bed', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.bed', 'מיטה', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.bed', 'سرير', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.bed', 'cama', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.bed', 'lit', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.bath', 'bath', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.bath', 'אמבטיה', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.bath', 'حمام', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.bath', 'baño', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.bath', 'bain', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.sqft', 'sqft', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.sqft', 'מ״ר', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.sqft', 'قدم مربع', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.sqft', 'pies²', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.sqft', 'pi²', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.city', 'City', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.city', 'עיר', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.city', 'المدينة', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.city', 'Ciudad', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.city', 'Ville', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.address', 'Address', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.address', 'כתובת', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.address', 'العنوان', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.address', 'Dirección', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.address', 'Adresse', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.odds', 'Odds', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.odds', 'סיכויים', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.odds', 'الاحتمالات', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.odds', 'Probabilidades', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.odds', 'Cotes', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.lotteryCountdown', 'Lottery Countdown', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.lotteryCountdown', 'ספירה לאחור להגרלה', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.lotteryCountdown', 'العد التنازلي لليانصيب', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.lotteryCountdown', 'Cuenta Regresiva de Lotería', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.lotteryCountdown', 'Compte à Rebours de Loterie', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.processing', 'Processing...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.processing', 'מעבד...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.processing', 'جاري المعالجة...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.processing', 'Procesando...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.processing', 'Traitement...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.signInToParticipate', 'Sign in to participate', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.signInToParticipate', 'התחבר כדי להשתתף', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.signInToParticipate', 'سجل الدخول للمشاركة', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.signInToParticipate', 'Inicia sesión para participar', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.signInToParticipate', 'Connectez-vous pour participer', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.perTicket', 'per ticket', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.perTicket', 'לכרטיס', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.perTicket', 'لكل تذكرة', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.perTicket', 'por boleto', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.perTicket', 'par billet', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.active', 'Active', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.active', 'פעיל', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.active', 'نشط', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.active', 'Activo', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.active', 'Actif', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.ended', 'Ended', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.ended', 'הסתיים', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.ended', 'انتهى', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.ended', 'Terminado', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.ended', 'Terminé', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.upcoming', 'Upcoming', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.upcoming', 'בקרוב', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.upcoming', 'قادم', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.upcoming', 'Próximo', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.upcoming', 'À Venir', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'house.onlyTicketsAvailable', 'Only %d tickets available', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'house.onlyTicketsAvailable', 'רק %d כרטיסים זמינים', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'house.onlyTicketsAvailable', 'فقط %d تذاكر متاحة', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'house.onlyTicketsAvailable', 'Solo %d boletos disponibles', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'house.onlyTicketsAvailable', 'Seulement %d billets disponibles', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),

                    -- Common (5 languages)
                    (uuid_generate_v4(), 'en', 'common.loading', 'Loading...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.loading', 'טוען...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.loading', 'جاري التحميل...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.loading', 'Cargando...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.loading', 'Chargement...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'common.error', 'Error', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.error', 'שגיאה', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.error', 'خطأ', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.error', 'Error', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.error', 'Erreur', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'common.success', 'Success', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.success', 'הצלחה', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.success', 'نجح', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.success', 'Éxito', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.success', 'Succès', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'common.save', 'Save', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.save', 'שמור', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.save', 'حفظ', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.save', 'Guardar', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.save', 'Sauvegarder', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'common.cancel', 'Cancel', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.cancel', 'בטל', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.cancel', 'إلغاء', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.cancel', 'Cancelar', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.cancel', 'Annuler', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'common.confirm', 'Confirm', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.confirm', 'אשר', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.confirm', 'تأكيد', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.confirm', 'Confirmar', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.confirm', 'Confirmer', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    
                    (uuid_generate_v4(), 'en', 'common.close', 'Close', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'he', 'common.close', 'סגור', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'ar', 'common.close', 'إغلاق', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'es', 'common.close', 'Cerrar', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'fr', 'common.close', 'Fermer', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System');";

            await _context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("Seeded 150+ comprehensive translations across 5 languages (en, he, ar, es, fr)");
            
            // Add Polish translations
            await SeedPolishTranslationsAsync();
            _logger.LogInformation("Seeded 200+ comprehensive translations across 6 languages (en, he, ar, es, fr, pl)");
        }

        private async Task SeedPolishTranslationsAsync()
        {
            _logger.LogInformation("Adding Polish translations...");

            var polishSql = @"
                INSERT INTO public.translations (""Id"", ""LanguageCode"", ""Key"", ""Value"", ""Description"", ""Category"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""CreatedBy"", ""UpdatedBy"")
                VALUES 
                    -- Navigation (Polish)
                    (uuid_generate_v4(), 'pl', 'nav.lotteries', 'Loterie', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.promotions', 'Promocje', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.winners', 'Zwycięzcy', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.signIn', 'Zaloguj się', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.logout', 'Wyloguj się', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.welcome', 'Witamy', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.memberSettings', 'Ustawienia Członka', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'nav.home', 'Strona Główna', 'Navigation', 'Navigation', true, NOW(), NOW(), 'System', 'System'),

                    -- Hero Section (Polish)
                    (uuid_generate_v4(), 'pl', 'hero.title', 'Wygraj Dom Swoich Marzeń', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'hero.browseLotteries', 'Przeglądaj Loterie', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'hero.howItWorks', 'Jak To Działa', 'Hero', 'Hero', true, NOW(), NOW(), 'System', 'System'),

                    -- Authentication (Polish)
                    (uuid_generate_v4(), 'pl', 'auth.signIn', 'Zaloguj się', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.signUp', 'Zarejestruj się', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.createAccount', 'Utwórz Konto', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.email', 'Email', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.password', 'Hasło', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.fullName', 'Imię i Nazwisko', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.forgotPassword', 'Zapomniałeś hasła?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.processing', 'Przetwarzanie...', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.dontHaveAccount', 'Nie masz konta?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.alreadyHaveAccount', 'Masz już konto?', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.continueWithGoogle', 'Kontynuuj z Google', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.continueWithMeta', 'Kontynuuj z Meta', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.continueWithApple', 'Kontynuuj z Apple', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'auth.or', 'LUB', 'Authentication', 'Authentication', true, NOW(), NOW(), 'System', 'System'),

                    -- House/Property (Polish)
                    (uuid_generate_v4(), 'pl', 'house.currentlyViewing', 'Obecnie Oglądasz', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.propertyOfYourOwn', 'Twoja własna nieruchomość', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.buyTicket', 'Kup Bilet', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.bed', 'łóżko', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.bath', 'łazienka', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.sqft', 'm²', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.city', 'Miasto', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.address', 'Adres', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.odds', 'Szanse', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.lotteryCountdown', 'Odliczanie do Loterii', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.processing', 'Przetwarzanie...', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.signInToParticipate', 'Zaloguj się, aby uczestniczyć', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.perTicket', 'za bilet', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.active', 'Aktywny', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.ended', 'Zakończony', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.upcoming', 'Nadchodzący', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'house.onlyTicketsAvailable', 'Tylko %d biletów dostępnych', 'House', 'House', true, NOW(), NOW(), 'System', 'System'),

                    -- Carousel (Polish)
                    (uuid_generate_v4(), 'pl', 'carousel.propertyValue', 'Wartość Nieruchomości', 'Carousel', 'Carousel', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'carousel.ticketPrice', 'Cena Biletu', 'Carousel', 'Carousel', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'carousel.buyTicket', 'Kup Bilet', 'Carousel', 'Carousel', true, NOW(), NOW(), 'System', 'System'),

                    -- Common (Polish)
                    (uuid_generate_v4(), 'pl', 'common.loading', 'Ładowanie...', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'common.error', 'Błąd', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'common.success', 'Sukces', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'common.save', 'Zapisz', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'common.cancel', 'Anuluj', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'common.confirm', 'Potwierdź', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System'),
                    (uuid_generate_v4(), 'pl', 'common.close', 'Zamknij', 'Common', 'Common', true, NOW(), NOW(), 'System', 'System');";

            await _context.Database.ExecuteSqlRawAsync(polishSql);
            _logger.LogInformation("Added 50+ Polish translations");
        }

        private async Task SeedUsersAsync()
        {
            _logger.LogInformation("Seeding users...");

            var sql = $@"
                DO $$
                DECLARE
                    admin_id UUID := uuid_generate_v4();
                    john_id UUID := uuid_generate_v4();
                    sarah_id UUID := uuid_generate_v4();
                BEGIN
                    -- Insert Users into amesa_auth schema
                    INSERT INTO amesa_auth.users (""Id"", ""Username"", ""Email"", ""EmailVerified"", ""Phone"", ""PhoneVerified"", ""PasswordHash"", ""FirstName"", ""LastName"", ""DateOfBirth"", ""Gender"", ""IdNumber"", ""Status"", ""VerificationStatus"", ""AuthProvider"", ""PreferredLanguage"", ""Timezone"", ""LastLoginAt"", ""TwoFactorEnabled"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES 
                        (admin_id, 'admin', 'admin@amesa.com', true, '+972501234567', true, '{PasswordHashingService.HashPassword("Admin123!")}', 'Admin', 'User', '1985-05-15'::timestamp, 'Male', '123456789', 'Active', 'FullyVerified', 'Email', 'en', 'Asia/Jerusalem', NOW() - INTERVAL '2 hours', false, NOW() - INTERVAL '30 days', NOW() - INTERVAL '2 hours'),
                        (john_id, 'john_doe', 'john.doe@example.com', true, '+972501234568', true, '{PasswordHashingService.HashPassword("Password123!")}', 'John', 'Doe', '1990-08-22'::timestamp, 'Male', '987654321', 'Active', 'FullyVerified', 'Email', 'en', 'Asia/Jerusalem', NOW() - INTERVAL '1 hour', false, NOW() - INTERVAL '15 days', NOW() - INTERVAL '1 hour'),
                        (sarah_id, 'sarah_wilson', 'sarah.wilson@example.com', true, '+972501234569', false, '{PasswordHashingService.HashPassword("Password123!")}', 'Sarah', 'Wilson', '1988-12-03'::timestamp, 'Female', '456789123', 'Active', 'EmailVerified', 'Email', 'he', 'Asia/Jerusalem', NOW() - INTERVAL '1 day', false, NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 day');

                    -- Insert User Addresses into amesa_auth schema
                    INSERT INTO amesa_auth.user_addresses (""Id"", ""UserId"", ""Type"", ""Country"", ""City"", ""Street"", ""HouseNumber"", ""ZipCode"", ""IsPrimary"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES 
                        (uuid_generate_v4(), admin_id, 'home', 'Israel', 'Tel Aviv', 'Rothschild Boulevard', '15', '6688119', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'),
                        (uuid_generate_v4(), john_id, 'home', 'Israel', 'Jerusalem', 'King George Street', '42', '9100000', true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'),
                        (uuid_generate_v4(), sarah_id, 'home', 'Israel', 'Haifa', 'Herzl Street', '88', '3100000', true, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days');

                    -- Insert User Phones into amesa_auth schema
                    INSERT INTO amesa_auth.user_phones (""Id"", ""UserId"", ""PhoneNumber"", ""IsPrimary"", ""IsVerified"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES 
                        (uuid_generate_v4(), admin_id, '+972501234567', true, true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'),
                        (uuid_generate_v4(), john_id, '+972501234568', true, true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'),
                        (uuid_generate_v4(), sarah_id, '+972501234569', true, false, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days');
                END $$;";

            await _context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("Seeded 3 users with addresses and phones");
        }

        private async Task SeedHousesAsync()
        {
            _logger.LogInformation("Seeding houses...");

            var sql = @"
                DO $$
                DECLARE
                    admin_id UUID;
                    house1_id UUID := uuid_generate_v4();
                    house2_id UUID := uuid_generate_v4();
                    house3_id UUID := uuid_generate_v4();
                    house4_id UUID := uuid_generate_v4();
                    house5_id UUID := uuid_generate_v4();
                BEGIN
                    -- Get admin user ID
                    SELECT ""Id"" INTO admin_id FROM amesa_auth.users WHERE ""Email"" = 'admin@amesa.com' LIMIT 1;

                    -- Insert Houses into amesa_lottery schema
                    INSERT INTO amesa_lottery.houses (""Id"", ""Title"", ""Description"", ""Price"", ""Location"", ""Address"", ""Bedrooms"", ""Bathrooms"", ""SquareFeet"", ""PropertyType"", ""YearBuilt"", ""LotSize"", ""Features"", ""Status"", ""TotalTickets"", ""TicketPrice"", ""LotteryStartDate"", ""LotteryEndDate"", ""DrawDate"", ""MinimumParticipationPercentage"", ""CreatedBy"", ""CreatedAt"", ""UpdatedAt"")
                    VALUES 
                        (house1_id, 'Luxury Villa in Warsaw', 'Stunning 4-bedroom villa with panoramic city views, private garden, and modern amenities. Located in the prestigious Mokotów district of Warsaw.', 1200000.00, 'Warsaw, Poland', '15 Ulica Puławska, Mokotów, Warsaw', 4, 3, 3500, 'Villa', 2020, 0.5, ARRAY['Private Garden', 'City View', 'Parking', 'Security System', 'Swimming Pool'], 'Active', 60000, 20.00, NOW() - INTERVAL '10 days', NOW() + INTERVAL '20 days', NOW() + INTERVAL '21 days', 75.00, admin_id, NOW() - INTERVAL '15 days', NOW() - INTERVAL '5 days'),
                        (house2_id, 'Modern Apartment in Kraków', 'Contemporary 3-bedroom apartment in the heart of Kraków Old Town. Features floor-to-ceiling windows and smart home technology.', 800000.00, 'Kraków, Poland', '42 Rynek Główny, Kraków', 3, 2, 1200, 'Apartment', 2022, 0.1, ARRAY['Smart Home', 'Rooftop Access', 'Gym', 'Parking'], 'Upcoming', 40000, 20.00, NOW() + INTERVAL '5 days', NOW() + INTERVAL '35 days', NOW() + INTERVAL '36 days', 80.00, admin_id, NOW() - INTERVAL '10 days', NOW() - INTERVAL '2 days'),
                        (house3_id, 'Beachfront Villa in Sopot', 'Luxurious 5-bedroom beachfront villa with direct beach access, infinity pool, and Baltic Sea views. Perfect for summer getaways.', 1500000.00, 'Sopot, Poland', '25 Molo, Sopot', 5, 4, 4200, 'Villa', 2021, 0.8, ARRAY['Beach Access', 'Infinity Pool', 'Sea View', 'Terrace', 'Parking', 'Concierge'], 'Active', 75000, 20.00, NOW() - INTERVAL '5 days', NOW() + INTERVAL '25 days', NOW() + INTERVAL '26 days', 85.00, admin_id, NOW() - INTERVAL '8 days', NOW() - INTERVAL '3 days'),
                        (house4_id, 'Historic Townhouse in Gdańsk', 'Beautifully restored 4-bedroom historic townhouse in Gdańsk Old Town. Combines traditional architecture with modern comforts.', 950000.00, 'Gdańsk, Poland', '8 Długi Targ, Old Town, Gdańsk', 4, 3, 2800, 'Townhouse', 1890, 0.3, ARRAY['Historic Architecture', 'Garden', 'Fireplace', 'Parking', 'Security', 'Restored'], 'Ended', 47500, 20.00, NOW() - INTERVAL '60 days', NOW() - INTERVAL '30 days', NOW() - INTERVAL '29 days', 70.00, admin_id, NOW() - INTERVAL '75 days', NOW() - INTERVAL '29 days'),
                        (house5_id, 'Mountain Chalet in Zakopane', 'Cozy 3-bedroom mountain chalet with stunning Tatra Mountains views. Perfect for ski enthusiasts and nature lovers.', 650000.00, 'Zakopane, Poland', '12 Krupówki, Zakopane', 3, 2, 1800, 'Chalet', 2019, 0.4, ARRAY['Mountain View', 'Fireplace', 'Ski Storage', 'Parking', 'Balcony', 'Garden'], 'Upcoming', 32500, 20.00, NOW() + INTERVAL '15 days', NOW() + INTERVAL '45 days', NOW() + INTERVAL '46 days', 75.00, admin_id, NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 day');

                    -- Insert House Images (1 main + 3 additional per house = 4 total)
                    INSERT INTO amesa_lottery.house_images (""Id"", ""HouseId"", ""ImageUrl"", ""AltText"", ""DisplayOrder"", ""IsPrimary"", ""MediaType"", ""FileSize"", ""Width"", ""Height"", ""CreatedAt"")
                    VALUES 
                        -- House 1: Luxury Villa in Warsaw
                        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1564013799919-ab600027ffc6?w=1200&h=800&fit=crop', 'Luxury Villa in Warsaw - Main View', 1, true, 'Image', 2048000, 1920, 1080, NOW()),
                        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&h=600&fit=crop', 'Luxury Villa - Living Room', 2, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop', 'Luxury Villa - Kitchen', 3, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1616594039964-ae9021a400a0?w=800&h=600&fit=crop', 'Luxury Villa - Master Bedroom', 4, false, 'Image', 1024000, 1280, 720, NOW()),
                        
                        -- House 2: Modern Apartment in Kraków
                        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=1200&h=800&fit=crop', 'Modern Apartment in Kraków - Main View', 1, true, 'Image', 2048000, 1920, 1080, NOW()),
                        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&h=600&fit=crop', 'Modern Apartment - Open Living Space', 2, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=800&h=600&fit=crop', 'Modern Apartment - Kitchen', 3, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1571508601891-ca5e7a713859?w=800&h=600&fit=crop', 'Modern Apartment - Bedroom', 4, false, 'Image', 1024000, 1280, 720, NOW()),
                        
                        -- House 3: Beachfront Villa in Sopot
                        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1512917774080-9991f1c4c750?w=1200&h=800&fit=crop', 'Beachfront Villa in Sopot - Main View', 1, true, 'Image', 2048000, 1920, 1080, NOW()),
                        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=800&h=600&fit=crop', 'Beachfront Villa - Infinity Pool', 2, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1507652313519-d4e9174996dd?w=800&h=600&fit=crop', 'Beachfront Villa - Beach Access', 3, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=800&h=600&fit=crop', 'Beachfront Villa - Terrace', 4, false, 'Image', 1024000, 1280, 720, NOW()),
                        
                        -- House 4: Historic Townhouse in Gdańsk
                        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200&h=800&fit=crop', 'Historic Townhouse in Gdańsk - Main View', 1, true, 'Image', 2048000, 1920, 1080, NOW()),
                        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800&h=600&fit=crop', 'Historic Townhouse - Living Room', 2, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=800&h=600&fit=crop', 'Historic Townhouse - Garden', 3, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800&h=600&fit=crop', 'Historic Townhouse - Kitchen', 4, false, 'Image', 1024000, 1280, 720, NOW()),
                        
                        -- House 5: Mountain Chalet in Zakopane
                        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1449824913935-59a10b8d2000?w=1200&h=800&fit=crop', 'Mountain Chalet in Zakopane - Main View', 1, true, 'Image', 2048000, 1920, 1080, NOW()),
                        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800&h=600&fit=crop', 'Mountain Chalet - Living Room with Fireplace', 2, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1571508601891-ca5e7a713859?w=800&h=600&fit=crop', 'Mountain Chalet - Cozy Bedroom', 3, false, 'Image', 1024000, 1280, 720, NOW()),
                        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1506905925346-21bda4d32df4?w=800&h=600&fit=crop', 'Mountain Chalet - Mountain View', 4, false, 'Image', 1024000, 1280, 720, NOW());
                END $$;";
            await _context.Database.ExecuteSqlRawAsync(sql);
            _logger.LogInformation("Seeded 5 houses with 20 images (4 images per house)");
        }

        private async Task LogSeedingResultsAsync()
        {
            _logger.LogInformation("=== SEEDING RESULTS ===");
            
            var languageCount = await _context.Languages.CountAsync();
            var translationCount = await _context.Translations.CountAsync();
            var userCount = await _context.Users.CountAsync();
            var addressCount = await _context.UserAddresses.CountAsync();
            var phoneCount = await _context.UserPhones.CountAsync();
            var houseCount = await _context.Houses.CountAsync();
            var imageCount = await _context.HouseImages.CountAsync();

            _logger.LogInformation($"Languages seeded: {languageCount}");
            _logger.LogInformation($"Translations seeded: {translationCount}");
            _logger.LogInformation($"Users seeded: {userCount}");
            _logger.LogInformation($"User addresses seeded: {addressCount}");
            _logger.LogInformation($"User phones seeded: {phoneCount}");
            _logger.LogInformation($"Houses seeded: {houseCount}");
            _logger.LogInformation($"House images seeded: {imageCount}");
            _logger.LogInformation("=== SEEDING COMPLETED SUCCESSFULLY ===");
        }

        private List<Translation> GetTranslationData()
        {
            var translations = new List<Translation>();
            var baseTime = DateTime.UtcNow;

            // Navigation translations
            var navigationTranslations = new Dictionary<string, string>
            {
                ["nav.lotteries"] = "Lotteries",
                ["nav.promotions"] = "Promotions",
                ["nav.howItWorks"] = "How It Works",
                ["nav.winners"] = "Winners",
                ["nav.about"] = "About",
                ["nav.sponsorship"] = "Sponsorship",
                ["nav.faq"] = "FAQ",
                ["nav.help"] = "Help",
                ["nav.partners"] = "Partners",
                ["nav.responsibleGambling"] = "Responsible Gaming",
                ["nav.register"] = "Register",
                ["nav.memberSettings"] = "My Account",
                ["nav.signIn"] = "Sign In",
                ["nav.getStarted"] = "Get Started",
                ["nav.welcome"] = "Welcome",
                ["nav.logout"] = "Logout"
            };

            foreach (var kvp in navigationTranslations)
            {
                translations.Add(new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = "en",
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Description = $"Navigation {kvp.Key.Split('.')[1]} link",
                    Category = "Navigation",
                    IsActive = true,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });
            }

            // Hero section translations
            var heroTranslations = new Dictionary<string, string>
            {
                ["hero.title"] = "Win Your Dream Home",
                ["hero.subtitle"] = "Participate in exclusive house lotteries and have a chance to win amazing properties for a fraction of their market value.",
                ["hero.browseLotteries"] = "Browse Lotteries",
                ["hero.howItWorks"] = "How It Works"
            };

            foreach (var kvp in heroTranslations)
            {
                translations.Add(new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = "en",
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Description = $"Hero section {kvp.Key.Split('.')[1]}",
                    Category = "Hero",
                    IsActive = true,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });
            }

            // Authentication translations
            var authTranslations = new Dictionary<string, string>
            {
                ["auth.signIn"] = "Sign In",
                ["auth.signUp"] = "Sign Up",
                ["auth.email"] = "Email",
                ["auth.password"] = "Password",
                ["auth.confirmPassword"] = "Confirm Password",
                ["auth.firstName"] = "First Name",
                ["auth.lastName"] = "Last Name",
                ["auth.phone"] = "Phone Number",
                ["auth.rememberMe"] = "Remember me",
                ["auth.forgotPassword"] = "Forgot your password?",
                ["auth.noAccount"] = "Don't have an account?",
                ["auth.haveAccount"] = "Already have an account?",
                ["auth.createAccount"] = "Create Account",
                ["auth.signInButton"] = "Sign In"
            };

            foreach (var kvp in authTranslations)
            {
                translations.Add(new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = "en",
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Description = $"Authentication {kvp.Key.Split('.')[1]}",
                    Category = "Authentication",
                    IsActive = true,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });
            }

            // Houses translations
            var houseTranslations = new Dictionary<string, string>
            {
                ["houses.title"] = "Available Lotteries",
                ["houses.subtitle"] = "Choose from our exclusive selection of luxury properties",
                ["houses.viewDetails"] = "View Details",
                ["houses.buyTickets"] = "Buy Tickets",
                ["houses.ticketPrice"] = "Ticket Price",
                ["houses.totalTickets"] = "Total Tickets",
                ["houses.ticketsSold"] = "Tickets Sold",
                ["houses.drawDate"] = "Draw Date",
                ["houses.propertyValue"] = "Property Value",
                ["houses.bedrooms"] = "Bedrooms",
                ["houses.bathrooms"] = "Bathrooms",
                ["houses.squareFeet"] = "Square Feet",
                ["houses.location"] = "Location",
                ["houses.features"] = "Features",
                ["houses.status.active"] = "Active",
                ["houses.status.upcoming"] = "Upcoming",
                ["houses.status.ended"] = "Ended"
            };

            foreach (var kvp in houseTranslations)
            {
                translations.Add(new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = "en",
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Description = $"Houses {kvp.Key.Split('.')[1]}",
                    Category = "Houses",
                    IsActive = true,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });
            }

            // Common translations
            var commonTranslations = new Dictionary<string, string>
            {
                ["common.loading"] = "Loading...",
                ["common.error"] = "Error",
                ["common.success"] = "Success",
                ["common.cancel"] = "Cancel",
                ["common.save"] = "Save",
                ["common.delete"] = "Delete",
                ["common.edit"] = "Edit",
                ["common.view"] = "View",
                ["common.close"] = "Close",
                ["common.back"] = "Back",
                ["common.next"] = "Next",
                ["common.previous"] = "Previous",
                ["common.search"] = "Search",
                ["common.filter"] = "Filter",
                ["common.sort"] = "Sort",
                ["common.all"] = "All",
                ["common.none"] = "None",
                ["common.yes"] = "Yes",
                ["common.no"] = "No"
            };

            foreach (var kvp in commonTranslations)
            {
                translations.Add(new Translation
                {
                    Id = Guid.NewGuid(),
                    LanguageCode = "en",
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Description = $"Common {kvp.Key.Split('.')[1]}",
                    Category = "Common",
                    IsActive = true,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime,
                    CreatedBy = "System",
                    UpdatedBy = "System"
                });
            }

            return translations;
        }
    }
}
