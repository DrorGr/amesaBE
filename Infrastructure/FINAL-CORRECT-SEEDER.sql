-- ============================================
-- FINAL CORRECT DATABASE SEEDER
-- Using EXACT table and column names from database discovery
-- ============================================

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Set timezone to UTC
SET timezone = 'UTC';

-- ============================================
-- SCHEMA: amesa_content - Languages & Translations
-- ============================================
SET search_path TO amesa_content;

-- Clear existing data
TRUNCATE TABLE translations CASCADE;
TRUNCATE TABLE languages CASCADE;

-- Insert Languages (using exact column names from discovery)
INSERT INTO languages ("Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
VALUES 
    ('en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()),
    ('he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()),
    ('ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()),
    ('es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()),
    ('fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()),
    ('pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW());

-- Insert comprehensive translations (using exact column names from discovery)
INSERT INTO translations ("Id", "LanguageCode", "Key", "Value", "Description", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy")
VALUES 
    -- Navigation translations
    (uuid_generate_v4(), 'en', 'nav.lotteries', 'Lotteries', 'Navigation lotteries link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.promotions', 'Promotions', 'Navigation promotions link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.howItWorks', 'How It Works', 'Navigation how it works link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.winners', 'Winners', 'Navigation winners link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.about', 'About', 'Navigation about link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.sponsorship', 'Sponsorship', 'Navigation sponsorship link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.faq', 'FAQ', 'Navigation FAQ link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.help', 'Help', 'Navigation help link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.partners', 'Partners', 'Navigation partners link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.responsibleGambling', 'Responsible Gaming', 'Navigation responsible gambling link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.register', 'Register', 'Navigation register button', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.memberSettings', 'My Account', 'Navigation member settings link', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.signIn', 'Sign In', 'Navigation sign in button', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.getStarted', 'Get Started', 'Navigation get started button', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.welcome', 'Welcome', 'Navigation welcome text', 'Navigation', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'nav.logout', 'Logout', 'Navigation logout button', 'Navigation', true, NOW(), NOW(), 'System', 'System'),

    -- Hero Section
    (uuid_generate_v4(), 'en', 'hero.title', 'Win Your Dream Home', 'Hero section title', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'hero.subtitle', 'Participate in exclusive house lotteries and have a chance to win amazing properties for a fraction of their market value.', 'Hero section subtitle', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'hero.browseLotteries', 'Browse Lotteries', 'Hero section browse lotteries button', 'Hero', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'hero.howItWorks', 'How It Works', 'Hero section how it works button', 'Hero', true, NOW(), NOW(), 'System', 'System'),

    -- How It Works
    (uuid_generate_v4(), 'en', 'howItWorks.heroTitle', 'How Amesa Lottery Works', 'How it works hero title', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.heroSubtitle', 'Your path to winning a dream home is simple and transparent.', 'How it works hero subtitle', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.simpleProcess', 'Simple Process', 'How it works simple process title', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.introduction', 'Participating in our house lotteries is straightforward and secure. Follow these simple steps to get started on your journey to homeownership.', 'How it works introduction', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.step1Title', 'Choose Your Lottery', 'How it works step 1 title', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.step1Desc', 'Browse our exclusive selection of luxury homes. Each property is a separate lottery with a limited number of tickets.', 'How it works step 1 description', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.step2Title', 'Buy Tickets', 'How it works step 2 title', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.step2Desc', 'Purchase tickets for your chosen lottery. The more tickets you buy, the higher your chances of winning. All transactions are secure and transparent.', 'How it works step 2 description', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.step3Title', 'Win & Own', 'How it works step 3 title', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.step3Desc', 'If you win, you become the proud owner of your dream property with all legal fees covered.', 'How it works step 3 description', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.readyToStart', 'Ready to Get Started?', 'How it works ready to start title', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.ctaDescription', 'Join thousands of participants who are already on their way to winning their dream homes.', 'How it works CTA description', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'howItWorks.browseLotteries', 'Browse Available Lotteries', 'How it works browse lotteries button', 'HowItWorks', true, NOW(), NOW(), 'System', 'System'),

    -- Footer
    (uuid_generate_v4(), 'en', 'footer.company', 'Company', 'Footer company section title', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.about', 'About Us', 'Footer about us link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.careers', 'Careers', 'Footer careers link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.press', 'Press', 'Footer press link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.blog', 'Blog', 'Footer blog link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.support', 'Support', 'Footer support section title', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.helpCenter', 'Help Center', 'Footer help center link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.contactUs', 'Contact Us', 'Footer contact us link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.faq', 'FAQ', 'Footer FAQ link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.legal', 'Legal', 'Footer legal section title', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.privacy', 'Privacy Policy', 'Footer privacy policy link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.terms', 'Terms of Service', 'Footer terms of service link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.responsible', 'Responsible Gaming', 'Footer responsible gaming link', 'Footer', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'footer.copyright', '© 2024 Amesa Lottery. All rights reserved.', 'Footer copyright text', 'Footer', true, NOW(), NOW(), 'System', 'System'),

    -- Authentication
    (uuid_generate_v4(), 'en', 'auth.signIn', 'Sign In', 'Sign in page title', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.signUp', 'Sign Up', 'Sign up page title', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.email', 'Email', 'Email field label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.password', 'Password', 'Password field label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.confirmPassword', 'Confirm Password', 'Confirm password field label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.firstName', 'First Name', 'First name field label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.lastName', 'Last Name', 'Last name field label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.phone', 'Phone Number', 'Phone number field label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.rememberMe', 'Remember me', 'Remember me checkbox label', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.forgotPassword', 'Forgot your password?', 'Forgot password link', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.noAccount', 'Don''t have an account?', 'No account text', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.haveAccount', 'Already have an account?', 'Have account text', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.createAccount', 'Create Account', 'Create account button', 'Authentication', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'auth.signInButton', 'Sign In', 'Sign in button', 'Authentication', true, NOW(), NOW(), 'System', 'System'),

    -- Houses
    (uuid_generate_v4(), 'en', 'houses.title', 'Available Lotteries', 'Houses page title', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.subtitle', 'Choose from our exclusive selection of luxury properties', 'Houses page subtitle', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.viewDetails', 'View Details', 'View house details button', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.buyTickets', 'Buy Tickets', 'Buy tickets button', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.ticketPrice', 'Ticket Price', 'Ticket price label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.totalTickets', 'Total Tickets', 'Total tickets label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.ticketsSold', 'Tickets Sold', 'Tickets sold label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.drawDate', 'Draw Date', 'Draw date label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.propertyValue', 'Property Value', 'Property value label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.bedrooms', 'Bedrooms', 'Bedrooms label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.bathrooms', 'Bathrooms', 'Bathrooms label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.squareFeet', 'Square Feet', 'Square feet label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.location', 'Location', 'Location label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.features', 'Features', 'Features label', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.status.active', 'Active', 'Active lottery status', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.status.upcoming', 'Upcoming', 'Upcoming lottery status', 'Houses', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'houses.status.ended', 'Ended', 'Ended lottery status', 'Houses', true, NOW(), NOW(), 'System', 'System'),

    -- Common
    (uuid_generate_v4(), 'en', 'common.loading', 'Loading...', 'Loading text', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.error', 'Error', 'Error text', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.success', 'Success', 'Success text', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.cancel', 'Cancel', 'Cancel button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.save', 'Save', 'Save button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.delete', 'Delete', 'Delete button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.edit', 'Edit', 'Edit button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.view', 'View', 'View button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.close', 'Close', 'Close button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.back', 'Back', 'Back button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.next', 'Next', 'Next button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.previous', 'Previous', 'Previous button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.search', 'Search', 'Search placeholder', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.filter', 'Filter', 'Filter button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.sort', 'Sort', 'Sort button', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.all', 'All', 'All option', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.none', 'None', 'None option', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.yes', 'Yes', 'Yes option', 'Common', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'common.no', 'No', 'No option', 'Common', true, NOW(), NOW(), 'System', 'System'),

    -- Chatbot
    (uuid_generate_v4(), 'en', 'chatbot.greeting', 'Hello! How can I help you today?', 'Chatbot greeting message', 'Chatbot', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'chatbot.placeholder', 'Type your message...', 'Chatbot input placeholder', 'Chatbot', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'chatbot.send', 'Send', 'Chatbot send button', 'Chatbot', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'chatbot.typing', 'Typing...', 'Chatbot typing indicator', 'Chatbot', true, NOW(), NOW(), 'System', 'System'),

    -- Accessibility
    (uuid_generate_v4(), 'en', 'accessibility.skipToContent', 'Skip to main content', 'Skip to content link', 'Accessibility', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'accessibility.openMenu', 'Open menu', 'Open menu button aria label', 'Accessibility', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'accessibility.closeMenu', 'Close menu', 'Close menu button aria label', 'Accessibility', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'accessibility.toggleLanguage', 'Toggle language menu', 'Language toggle aria label', 'Accessibility', true, NOW(), NOW(), 'System', 'System'),
    (uuid_generate_v4(), 'en', 'accessibility.userMenu', 'User menu', 'User menu aria label', 'Accessibility', true, NOW(), NOW(), 'System', 'System');

-- ============================================
-- SCHEMA: amesa_auth - Users and Related Data
-- ============================================
SET search_path TO amesa_auth;

-- Clear existing data
TRUNCATE TABLE user_phones CASCADE;
TRUNCATE TABLE user_addresses CASCADE;
TRUNCATE TABLE users CASCADE;

-- Insert Users with proper password hashes (using exact column names from discovery)
-- Password hashes: Admin123! = SC4iAugHld+2khAkLAJwINPkymeZOSkiIba6rWrWGuc=
--                  Password123! = ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=

DO $$
DECLARE
    admin_id UUID := uuid_generate_v4();
    john_id UUID := uuid_generate_v4();
    sarah_id UUID := uuid_generate_v4();
    ahmed_id UUID := uuid_generate_v4();
    maria_id UUID := uuid_generate_v4();
BEGIN
    -- Insert Users
    INSERT INTO users ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "ProviderId", "ProfileImageUrl", "PreferredLanguage", "Timezone", "LastLoginAt", "EmailVerificationToken", "PhoneVerificationToken", "PasswordResetToken", "PasswordResetExpiresAt", "TwoFactorEnabled", "TwoFactorSecret", "CreatedAt", "UpdatedAt", "DeletedAt")
    VALUES 
        (admin_id, 'admin', 'admin@amesa.com', true, '+972501234567', true, 'SC4iAugHld+2khAkLAJwINPkymeZOSkiIba6rWrWGuc=', 'Admin', 'User', '1985-05-15'::timestamp, 'Male', '123456789', 'Active', 'Verified', 'Local', NULL, NULL, 'en', 'Asia/Jerusalem', NOW() - INTERVAL '2 hours', NULL, NULL, NULL, NULL, false, NULL, NOW() - INTERVAL '30 days', NOW() - INTERVAL '2 hours', NULL),
        (john_id, 'john_doe', 'john.doe@example.com', true, '+972501234568', true, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'John', 'Doe', '1990-08-22'::timestamp, 'Male', '987654321', 'Active', 'Verified', 'Local', NULL, NULL, 'en', 'Asia/Jerusalem', NOW() - INTERVAL '1 hour', NULL, NULL, NULL, NULL, false, NULL, NOW() - INTERVAL '15 days', NOW() - INTERVAL '1 hour', NULL),
        (sarah_id, 'sarah_wilson', 'sarah.wilson@example.com', true, '+972501234569', false, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Sarah', 'Wilson', '1988-12-03'::timestamp, 'Female', '456789123', 'Active', 'Pending', 'Local', NULL, NULL, 'he', 'Asia/Jerusalem', NOW() - INTERVAL '1 day', NULL, NULL, NULL, NULL, false, NULL, NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 day', NULL),
        (ahmed_id, 'ahmed_hassan', 'ahmed.hassan@example.com', true, '+972501234570', true, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Ahmed', 'Hassan', '1992-03-18'::timestamp, 'Male', '789123456', 'Active', 'Verified', 'Local', NULL, NULL, 'ar', 'Asia/Jerusalem', NOW() - INTERVAL '3 hours', NULL, NULL, NULL, NULL, false, NULL, NOW() - INTERVAL '20 days', NOW() - INTERVAL '3 hours', NULL),
        (maria_id, 'maria_garcia', 'maria.garcia@example.com', false, '+972501234571', false, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Maria', 'Garcia', '1995-07-25'::timestamp, 'Female', '321654987', 'Inactive', 'Unverified', 'Local', NULL, NULL, 'es', 'Asia/Jerusalem', NULL, NULL, NULL, NULL, NULL, false, NULL, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', NULL);

    -- Insert User Addresses (using exact column names from discovery)
    INSERT INTO user_addresses ("Id", "UserId", "Type", "Country", "City", "Street", "HouseNumber", "ZipCode", "IsPrimary", "CreatedAt", "UpdatedAt", "UserId1")
    VALUES 
        (uuid_generate_v4(), admin_id, 'home', 'Israel', 'Tel Aviv', 'Rothschild Boulevard', '15', '6688119', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days', NULL),
        (uuid_generate_v4(), john_id, 'home', 'Israel', 'Jerusalem', 'King George Street', '42', '9100000', true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days', NULL),
        (uuid_generate_v4(), sarah_id, 'home', 'Israel', 'Haifa', 'Herzl Street', '88', '3100000', true, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days', NULL),
        (uuid_generate_v4(), ahmed_id, 'home', 'Israel', 'Tel Aviv', 'Dizengoff Street', '100', '6436100', true, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days', NULL);

    -- Insert User Phones (using exact column names from discovery)
    INSERT INTO user_phones ("Id", "UserId", "PhoneNumber", "IsPrimary", "IsVerified", "VerificationCode", "VerificationExpiresAt", "CreatedAt", "UpdatedAt", "UserId1")
    VALUES 
        (uuid_generate_v4(), admin_id, '+972501234567', true, true, NULL, NULL, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days', NULL),
        (uuid_generate_v4(), john_id, '+972501234568', true, true, NULL, NULL, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days', NULL),
        (uuid_generate_v4(), sarah_id, '+972501234569', true, false, NULL, NULL, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days', NULL),
        (uuid_generate_v4(), ahmed_id, '+972501234570', true, true, NULL, NULL, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days', NULL);
END $$;

-- ============================================
-- SCHEMA: amesa_lottery - Houses and Images
-- ============================================
SET search_path TO amesa_lottery;

-- Clear existing data
TRUNCATE TABLE house_images CASCADE;
TRUNCATE TABLE houses CASCADE;

-- Get admin user ID for house creation (using exact column names from discovery)
DO $$
DECLARE
    admin_user_id UUID;
    house1_id UUID := uuid_generate_v4();
    house2_id UUID := uuid_generate_v4();
    house3_id UUID := uuid_generate_v4();
    house4_id UUID := uuid_generate_v4();
    house5_id UUID := uuid_generate_v4();
    house6_id UUID := uuid_generate_v4();
    house7_id UUID := uuid_generate_v4();
    house8_id UUID := uuid_generate_v4();
BEGIN
    -- Get admin user ID
    SELECT "Id" INTO admin_user_id FROM amesa_auth.users WHERE "Username" = 'admin' LIMIT 1;

    -- Insert Houses (using exact column names from discovery)
    INSERT INTO houses ("Id", "Title", "Description", "Price", "Location", "Address", "Bedrooms", "Bathrooms", "SquareFeet", "PropertyType", "YearBuilt", "LotSize", "Features", "Status", "TotalTickets", "TicketPrice", "LotteryStartDate", "LotteryEndDate", "DrawDate", "MinimumParticipationPercentage", "CreatedBy", "CreatedAt", "UpdatedAt", "DeletedAt")
    VALUES 
        (house1_id, 'Luxury Villa in Warsaw', 'Stunning 4-bedroom villa with panoramic city views, private garden, and modern amenities. Located in the prestigious Mokotów district of Warsaw.', 1200000.00, 'Warsaw, Poland', '15 Ulica Puławska, Mokotów, Warsaw', 4, 3, 3500, 'Villa', 2020, 0.5, ARRAY['Private Garden', 'City View', 'Parking', 'Security System', 'Air Conditioning', 'Modern Kitchen'], 'Active', 60000, 20.00, NOW() - INTERVAL '10 days', NOW() + INTERVAL '20 days', NOW() + INTERVAL '21 days', 75.00, admin_user_id, NOW() - INTERVAL '15 days', NOW() - INTERVAL '5 days', NULL),
        
        (house2_id, 'Modern Apartment in Kraków', 'Contemporary 3-bedroom apartment in the heart of Kraków Old Town. Features floor-to-ceiling windows, smart home technology, and access to rooftop amenities.', 800000.00, 'Kraków, Poland', '42 Rynek Główny, Kraków', 3, 2, 1200, 'Apartment', 2022, 0.1, ARRAY['Smart Home', 'Rooftop Access', 'Gym', 'Parking', 'Balcony', 'Old Town View'], 'Upcoming', 40000, 20.00, NOW() + INTERVAL '5 days', NOW() + INTERVAL '35 days', NOW() + INTERVAL '36 days', 80.00, admin_user_id, NOW() - INTERVAL '10 days', NOW() - INTERVAL '2 days', NULL),
        
        (house3_id, 'Historic House in Gdańsk', 'Beautifully restored 5-bedroom historic house in the Old Town of Gdańsk. Combines traditional architecture with modern comforts.', 900000.00, 'Gdańsk, Poland', '8 Długi Targ, Old Town, Gdańsk', 5, 4, 2800, 'Historic House', 1890, 0.3, ARRAY['Historic Architecture', 'Garden', 'Terrace', 'Parking', 'Security', 'Restored'], 'Ended', 45000, 20.00, NOW() - INTERVAL '60 days', NOW() - INTERVAL '30 days', NOW() - INTERVAL '29 days', 70.00, admin_user_id, NOW() - INTERVAL '75 days', NOW() - INTERVAL '29 days', NULL),
        
        (house4_id, 'Beachfront Condo in Sopot', 'Luxurious 2-bedroom beachfront condo with direct beach access, infinity pool, and Baltic Sea views.', 600000.00, 'Sopot, Poland', '25 Molo, Sopot', 2, 2, 900, 'Condo', 2021, 0.05, ARRAY['Beach Access', 'Infinity Pool', 'Sea View', 'Balcony', 'Parking', 'Concierge'], 'Active', 30000, 20.00, NOW() - INTERVAL '5 days', NOW() + INTERVAL '25 days', NOW() + INTERVAL '26 days', 85.00, admin_user_id, NOW() - INTERVAL '8 days', NOW() - INTERVAL '3 days', NULL),
        
        (house5_id, 'Mountain Villa in Zakopane', 'Spectacular 5-bedroom villa perched in the Tatra Mountains with breathtaking views of the peaks. Features traditional wooden architecture with modern luxury.', 1000000.00, 'Zakopane, Poland', '12 Krupówki, Zakopane', 5, 4, 3200, 'Villa', 2019, 0.8, ARRAY['Mountain View', 'Wooden Architecture', 'Garden', 'Fireplace', 'Parking', 'Ski Storage'], 'Active', 50000, 20.00, NOW() - INTERVAL '3 days', NOW() + INTERVAL '27 days', NOW() + INTERVAL '28 days', 80.00, admin_user_id, NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day', NULL),
        
        (house6_id, 'Modern Penthouse in Wrocław', 'Stunning 4-bedroom penthouse with panoramic views of Wrocław Old Town and the Oder River. Features floor-to-ceiling windows and a private rooftop terrace.', 700000.00, 'Wrocław, Poland', '88 Rynek, Wrocław', 4, 3, 1800, 'Penthouse', 2023, 0.2, ARRAY['River View', 'Rooftop Terrace', 'Smart Home', 'Elevator', 'Parking', 'Concierge'], 'Upcoming', 35000, 20.00, NOW() + INTERVAL '7 days', NOW() + INTERVAL '37 days', NOW() + INTERVAL '38 days', 85.00, admin_user_id, NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 day', NULL),
        
        (house7_id, 'Lake House in Mazury', 'Unique 3-bedroom lake house with private dock and stunning lake views. Perfect for those seeking tranquility and luxury by the water.', 500000.00, 'Mazury, Poland', '45 Lake View Road, Mazury', 3, 3, 1500, 'Villa', 2020, 0.6, ARRAY['Lake View', 'Private Dock', 'Water Access', 'Solar Panels', 'Parking', 'Outdoor Kitchen'], 'Active', 25000, 20.00, NOW() - INTERVAL '2 days', NOW() + INTERVAL '28 days', NOW() + INTERVAL '29 days', 75.00, admin_user_id, NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days', NULL),
        
        (house8_id, 'Historic Mansion in Poznań', 'Magnificent 6-bedroom historic mansion in the heart of Poznań Old Town. Restored to perfection with original architectural details and modern amenities.', 1100000.00, 'Poznań, Poland', '3 Stary Rynek, Poznań', 6, 5, 4200, 'Historic Mansion', 1875, 0.4, ARRAY['Historic Architecture', 'Old Town Location', 'Garden', 'Terrace', 'Parking', 'Restored'], 'Upcoming', 55000, 20.00, NOW() + INTERVAL '10 days', NOW() + INTERVAL '40 days', NOW() + INTERVAL '41 days', 70.00, admin_user_id, NOW() - INTERVAL '4 days', NOW() - INTERVAL '1 day', NULL);

    -- Insert House Images (5 images per house: 1 main + 4 small) (using exact column names from discovery)
    INSERT INTO house_images ("Id", "HouseId", "ImageUrl", "AltText", "DisplayOrder", "IsPrimary", "MediaType", "FileSize", "Width", "Height", "CreatedAt")
    VALUES 
        -- House 1 - Luxury Villa in Warsaw
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=1200', 'Luxury Villa in Warsaw - Main Exterior View', 1, true, 'Image', 2048000, 1920, 1080, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Luxury Villa - Living Room', 2, false, 'Image', 475000, 600, 400, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Luxury Villa - Swimming Pool', 3, false, 'Image', 550000, 600, 400, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Luxury Villa - Master Bedroom', 4, false, 'Image', 525000, 600, 400, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Luxury Villa - Kitchen', 5, false, 'Image', 490000, 600, 400, NOW() - INTERVAL '15 days'),
        
        -- House 2 - Modern Apartment in Kraków
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200', 'Modern Apartment in Kraków - Main Living Room', 1, true, 'Image', 1960000, 1920, 1080, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Modern Apartment - Kitchen', 2, false, 'Image', 460000, 600, 400, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Modern Apartment - Bedroom', 3, false, 'Image', 475000, 600, 400, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Modern Apartment - Rooftop View', 4, false, 'Image', 550000, 600, 400, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400', 'Modern Apartment - City View', 5, false, 'Image', 500000, 600, 400, NOW() - INTERVAL '10 days'),
        
        -- House 3 - Historic House in Gdańsk
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200', 'Historic House in Gdańsk - Main Exterior', 1, true, 'Image', 2100000, 1920, 1080, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Historic House - Interior', 2, false, 'Image', 550000, 600, 400, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Historic House - Bedroom', 3, false, 'Image', 500000, 600, 400, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Historic House - Traditional Kitchen', 4, false, 'Image', 475000, 600, 400, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Historic House - Garden', 5, false, 'Image', 540000, 600, 400, NOW() - INTERVAL '75 days'),
        
        -- House 4 - Beachfront Condo in Sopot
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=1200', 'Beachfront Condo in Sopot - Main Sea View', 1, true, 'Image', 2300000, 1920, 1080, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400', 'Beachfront Condo - Infinity Pool', 2, false, 'Image', 540000, 600, 400, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Beachfront Condo - Living Room', 3, false, 'Image', 490000, 600, 400, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Beachfront Condo - Bedroom', 4, false, 'Image', 510000, 600, 400, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Beachfront Condo - Balcony View', 5, false, 'Image', 550000, 600, 400, NOW() - INTERVAL '8 days'),
        
        -- House 5 - Mountain Villa in Zakopane
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=1200', 'Mountain Villa in Zakopane - Main Exterior View', 1, true, 'Image', 2400000, 1920, 1080, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=400', 'Mountain Villa - Wooden Architecture', 2, false, 'Image', 575000, 600, 400, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Mountain Villa - Living Room with Fireplace', 3, false, 'Image', 540000, 600, 400, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Mountain Villa - Kitchen', 4, false, 'Image', 490000, 600, 400, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Mountain Villa - Garden View', 5, false, 'Image', 550000, 600, 400, NOW() - INTERVAL '6 days'),
        
        -- House 6 - Modern Penthouse in Wrocław
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200', 'Modern Penthouse in Wrocław - Main Living Room', 1, true, 'Image', 2360000, 1920, 1080, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400', 'Modern Penthouse - City View', 2, false, 'Image', 600000, 600, 400, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Modern Penthouse - Master Bedroom', 3, false, 'Image', 525000, 600, 400, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400', 'Modern Penthouse - Rooftop Terrace', 4, false, 'Image', 560000, 600, 400, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Modern Penthouse - Kitchen', 5, false, 'Image', 490000, 600, 400, NOW() - INTERVAL '5 days'),
        
        -- House 7 - Lake House in Mazury
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=1200', 'Lake House in Mazury - Main Exterior View', 1, true, 'Image', 2500000, 1920, 1080, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400', 'Lake House - Private Dock', 2, false, 'Image', 590000, 600, 400, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Lake House - Living Room', 3, false, 'Image', 540000, 600, 400, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Lake House - Bedroom', 4, false, 'Image', 510000, 600, 400, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400', 'Lake House - Lake View', 5, false, 'Image', 600000, 600, 400, NOW() - INTERVAL '7 days'),
        
        -- House 8 - Historic Mansion in Poznań
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200', 'Historic Mansion in Poznań - Main Exterior', 1, true, 'Image', 2600000, 1920, 1080, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Historic Mansion - Grand Living Room', 2, false, 'Image', 625000, 600, 400, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Historic Mansion - Master Suite', 3, false, 'Image', 575000, 600, 400, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Historic Mansion - Garden Terrace', 4, false, 'Image', 600000, 600, 400, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Historic Mansion - Kitchen', 5, false, 'Image', 550000, 600, 400, NOW() - INTERVAL '4 days');

END $$;

-- ============================================
-- SUMMARY AND VERIFICATION
-- ============================================

-- Show results
SELECT 'Languages seeded: ' || COUNT(*)::text AS result FROM amesa_content.languages;
SELECT 'Translations seeded: ' || COUNT(*)::text AS result FROM amesa_content.translations;
SELECT 'Users seeded: ' || COUNT(*)::text AS result FROM amesa_auth.users;
SELECT 'User addresses seeded: ' || COUNT(*)::text AS result FROM amesa_auth.user_addresses;
SELECT 'User phones seeded: ' || COUNT(*)::text AS result FROM amesa_auth.user_phones;
SELECT 'Houses seeded: ' || COUNT(*)::text AS result FROM amesa_lottery.houses;
SELECT 'House images seeded: ' || COUNT(*)::text AS result FROM amesa_lottery.house_images;

-- Final success message
SELECT 'COMPREHENSIVE DATABASE SEEDING COMPLETED SUCCESSFULLY!' AS status;
SELECT 'All data has been seeded using EXACT column names from database discovery.' AS message;
SELECT 'Frontend should now display houses, translations, and all demo data perfectly!' AS result;
