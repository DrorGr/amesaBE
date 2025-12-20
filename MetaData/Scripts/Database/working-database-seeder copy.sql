-- ============================================
-- WORKING DATABASE SEEDER - CORRECT TABLE NAMES
-- Based on actual database structure discovered
-- ============================================

-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Set timezone to UTC
SET timezone = 'UTC';

-- ============================================
-- SCHEMA: amesa_content - Languages & Translations
-- ============================================
SET search_path TO amesa_content;

-- Clear existing data (using correct lowercase table names)
TRUNCATE TABLE translations CASCADE;
TRUNCATE TABLE languages CASCADE;

-- Insert Languages
INSERT INTO languages (id, code, name, native_name, flag_url, is_active, is_default, display_order, created_at, updated_at)
VALUES 
    (uuid_generate_v4(), 'en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()),
    (uuid_generate_v4(), 'he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()),
    (uuid_generate_v4(), 'ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()),
    (uuid_generate_v4(), 'es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()),
    (uuid_generate_v4(), 'fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()),
    (uuid_generate_v4(), 'pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW());

-- Insert comprehensive translations
INSERT INTO translations (id, language_code, key, value, description, category, created_by, updated_by, created_at, updated_at)
VALUES 
    -- Navigation translations
    (uuid_generate_v4(), 'en', 'nav.lotteries', 'Lotteries', 'Navigation lotteries link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.promotions', 'Promotions', 'Navigation promotions link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.howItWorks', 'How It Works', 'Navigation how it works link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.winners', 'Winners', 'Navigation winners link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.about', 'About', 'Navigation about link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.sponsorship', 'Sponsorship', 'Navigation sponsorship link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.faq', 'FAQ', 'Navigation FAQ link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.help', 'Help', 'Navigation help link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.partners', 'Partners', 'Navigation partners link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.responsibleGambling', 'Responsible Gaming', 'Navigation responsible gambling link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.register', 'Register', 'Navigation register button', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.memberSettings', 'My Account', 'Navigation member settings link', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.signIn', 'Sign In', 'Navigation sign in button', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.getStarted', 'Get Started', 'Navigation get started button', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.welcome', 'Welcome', 'Navigation welcome text', 'Navigation', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'nav.logout', 'Logout', 'Navigation logout button', 'Navigation', 'System', 'System', NOW(), NOW()),

    -- Hero Section
    (uuid_generate_v4(), 'en', 'hero.title', 'Win Your Dream Home', 'Hero section title', 'Hero', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'hero.subtitle', 'Participate in exclusive house lotteries and have a chance to win amazing properties for a fraction of their market value.', 'Hero section subtitle', 'Hero', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'hero.browseLotteries', 'Browse Lotteries', 'Hero section browse lotteries button', 'Hero', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'hero.howItWorks', 'How It Works', 'Hero section how it works button', 'Hero', 'System', 'System', NOW(), NOW()),

    -- How It Works
    (uuid_generate_v4(), 'en', 'howItWorks.heroTitle', 'How Amesa Lottery Works', 'How it works hero title', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.heroSubtitle', 'Your path to winning a dream home is simple and transparent.', 'How it works hero subtitle', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.simpleProcess', 'Simple Process', 'How it works simple process title', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.introduction', 'Participating in our house lotteries is straightforward and secure. Follow these simple steps to get started on your journey to homeownership.', 'How it works introduction', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.step1Title', 'Choose Your Lottery', 'How it works step 1 title', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.step1Desc', 'Browse our exclusive selection of luxury homes. Each property is a separate lottery with a limited number of tickets.', 'How it works step 1 description', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.step2Title', 'Buy Tickets', 'How it works step 2 title', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.step2Desc', 'Purchase tickets for your chosen lottery. The more tickets you buy, the higher your chances of winning. All transactions are secure and transparent.', 'How it works step 2 description', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.step3Title', 'Win & Own', 'How it works step 3 title', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.step3Desc', 'If you win, you become the proud owner of your dream property with all legal fees covered.', 'How it works step 3 description', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.readyToStart', 'Ready to Get Started?', 'How it works ready to start title', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.ctaDescription', 'Join thousands of participants who are already on their way to winning their dream homes.', 'How it works CTA description', 'HowItWorks', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'howItWorks.browseLotteries', 'Browse Available Lotteries', 'How it works browse lotteries button', 'HowItWorks', 'System', 'System', NOW(), NOW()),

    -- Footer
    (uuid_generate_v4(), 'en', 'footer.company', 'Company', 'Footer company section title', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.about', 'About Us', 'Footer about us link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.careers', 'Careers', 'Footer careers link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.press', 'Press', 'Footer press link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.blog', 'Blog', 'Footer blog link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.support', 'Support', 'Footer support section title', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.helpCenter', 'Help Center', 'Footer help center link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.contactUs', 'Contact Us', 'Footer contact us link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.faq', 'FAQ', 'Footer FAQ link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.legal', 'Legal', 'Footer legal section title', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.privacy', 'Privacy Policy', 'Footer privacy policy link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.terms', 'Terms of Service', 'Footer terms of service link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.responsible', 'Responsible Gaming', 'Footer responsible gaming link', 'Footer', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'footer.copyright', '© 2024 Amesa Lottery. All rights reserved.', 'Footer copyright text', 'Footer', 'System', 'System', NOW(), NOW()),

    -- Authentication
    (uuid_generate_v4(), 'en', 'auth.signIn', 'Sign In', 'Sign in page title', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.signUp', 'Sign Up', 'Sign up page title', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.email', 'Email', 'Email field label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.password', 'Password', 'Password field label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.confirmPassword', 'Confirm Password', 'Confirm password field label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.firstName', 'First Name', 'First name field label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.lastName', 'Last Name', 'Last name field label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.phone', 'Phone Number', 'Phone number field label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.rememberMe', 'Remember me', 'Remember me checkbox label', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.forgotPassword', 'Forgot your password?', 'Forgot password link', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.noAccount', 'Don''t have an account?', 'No account text', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.haveAccount', 'Already have an account?', 'Have account text', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.createAccount', 'Create Account', 'Create account button', 'Authentication', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'auth.signInButton', 'Sign In', 'Sign in button', 'Authentication', 'System', 'System', NOW(), NOW()),

    -- Houses
    (uuid_generate_v4(), 'en', 'houses.title', 'Available Lotteries', 'Houses page title', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.subtitle', 'Choose from our exclusive selection of luxury properties', 'Houses page subtitle', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.viewDetails', 'View Details', 'View house details button', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.buyTickets', 'Buy Tickets', 'Buy tickets button', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.ticketPrice', 'Ticket Price', 'Ticket price label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.totalTickets', 'Total Tickets', 'Total tickets label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.ticketsSold', 'Tickets Sold', 'Tickets sold label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.drawDate', 'Draw Date', 'Draw date label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.propertyValue', 'Property Value', 'Property value label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.bedrooms', 'Bedrooms', 'Bedrooms label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.bathrooms', 'Bathrooms', 'Bathrooms label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.squareFeet', 'Square Feet', 'Square feet label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.location', 'Location', 'Location label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.features', 'Features', 'Features label', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.status.active', 'Active', 'Active lottery status', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.status.upcoming', 'Upcoming', 'Upcoming lottery status', 'Houses', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'houses.status.ended', 'Ended', 'Ended lottery status', 'Houses', 'System', 'System', NOW(), NOW()),

    -- Common
    (uuid_generate_v4(), 'en', 'common.loading', 'Loading...', 'Loading text', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.error', 'Error', 'Error text', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.success', 'Success', 'Success text', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.cancel', 'Cancel', 'Cancel button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.save', 'Save', 'Save button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.delete', 'Delete', 'Delete button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.edit', 'Edit', 'Edit button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.view', 'View', 'View button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.close', 'Close', 'Close button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.back', 'Back', 'Back button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.next', 'Next', 'Next button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.previous', 'Previous', 'Previous button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.search', 'Search', 'Search placeholder', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.filter', 'Filter', 'Filter button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.sort', 'Sort', 'Sort button', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.all', 'All', 'All option', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.none', 'None', 'None option', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.yes', 'Yes', 'Yes option', 'Common', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'common.no', 'No', 'No option', 'Common', 'System', 'System', NOW(), NOW()),

    -- Chatbot
    (uuid_generate_v4(), 'en', 'chatbot.greeting', 'Hello! How can I help you today?', 'Chatbot greeting message', 'Chatbot', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'chatbot.placeholder', 'Type your message...', 'Chatbot input placeholder', 'Chatbot', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'chatbot.send', 'Send', 'Chatbot send button', 'Chatbot', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'chatbot.typing', 'Typing...', 'Chatbot typing indicator', 'Chatbot', 'System', 'System', NOW(), NOW()),

    -- Accessibility
    (uuid_generate_v4(), 'en', 'accessibility.skipToContent', 'Skip to main content', 'Skip to content link', 'Accessibility', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'accessibility.openMenu', 'Open menu', 'Open menu button aria label', 'Accessibility', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'accessibility.closeMenu', 'Close menu', 'Close menu button aria label', 'Accessibility', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'accessibility.toggleLanguage', 'Toggle language menu', 'Language toggle aria label', 'Accessibility', 'System', 'System', NOW(), NOW()),
    (uuid_generate_v4(), 'en', 'accessibility.userMenu', 'User menu', 'User menu aria label', 'Accessibility', 'System', 'System', NOW(), NOW());

-- ============================================
-- SCHEMA: amesa_auth - Users and Related Data
-- ============================================
SET search_path TO amesa_auth;

-- Clear existing data (using correct lowercase table names)
TRUNCATE TABLE user_phones CASCADE;
TRUNCATE TABLE user_addresses CASCADE;
TRUNCATE TABLE users CASCADE;

-- Insert Users with proper password hashes
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
    INSERT INTO users (id, username, email, email_verified, phone, phone_verified, password_hash, first_name, last_name, date_of_birth, gender, id_number, status, verification_status, auth_provider, preferred_language, timezone, last_login_at, created_at, updated_at)
    VALUES 
        (admin_id, 'admin', 'admin@amesa.com', true, '+972501234567', true, 'SC4iAugHld+2khAkLAJwINPkymeZOSkiIba6rWrWGuc=', 'Admin', 'User', '1985-05-15'::timestamp, 0, '123456789', 0, 2, 0, 'en', 'Asia/Jerusalem', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '30 days', NOW() - INTERVAL '2 hours'),
        (john_id, 'john_doe', 'john.doe@example.com', true, '+972501234568', true, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'John', 'Doe', '1990-08-22'::timestamp, 0, '987654321', 0, 2, 0, 'en', 'Asia/Jerusalem', NOW() - INTERVAL '1 hour', NOW() - INTERVAL '15 days', NOW() - INTERVAL '1 hour'),
        (sarah_id, 'sarah_wilson', 'sarah.wilson@example.com', true, '+972501234569', false, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Sarah', 'Wilson', '1988-12-03'::timestamp, 1, '456789123', 0, 1, 0, 'he', 'Asia/Jerusalem', NOW() - INTERVAL '1 day', NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 day'),
        (ahmed_id, 'ahmed_hassan', 'ahmed.hassan@example.com', true, '+972501234570', true, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Ahmed', 'Hassan', '1992-03-18'::timestamp, 0, '789123456', 0, 2, 0, 'ar', 'Asia/Jerusalem', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '20 days', NOW() - INTERVAL '3 hours'),
        (maria_id, 'maria_garcia', 'maria.garcia@example.com', false, '+972501234571', false, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Maria', 'Garcia', '1995-07-25'::timestamp, 1, '321654987', 1, 0, 0, 'es', 'Asia/Jerusalem', NULL, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days');

    -- Insert User Addresses
    INSERT INTO user_addresses (id, user_id, type, country, city, street, house_number, zip_code, is_primary, created_at, updated_at)
    VALUES 
        (uuid_generate_v4(), admin_id, 'home', 'Israel', 'Tel Aviv', 'Rothschild Boulevard', '15', '6688119', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'),
        (uuid_generate_v4(), john_id, 'home', 'Israel', 'Jerusalem', 'King George Street', '42', '9100000', true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), sarah_id, 'home', 'Israel', 'Haifa', 'Herzl Street', '88', '3100000', true, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), ahmed_id, 'home', 'Israel', 'Tel Aviv', 'Dizengoff Street', '100', '6436100', true, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days');

    -- Insert User Phones
    INSERT INTO user_phones (id, user_id, phone_number, is_primary, is_verified, created_at, updated_at)
    VALUES 
        (uuid_generate_v4(), admin_id, '+972501234567', true, true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'),
        (uuid_generate_v4(), john_id, '+972501234568', true, true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), sarah_id, '+972501234569', true, false, NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), ahmed_id, '+972501234570', true, true, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days');
END $$;

-- ============================================
-- SCHEMA: amesa_lottery - Houses and Images
-- ============================================
SET search_path TO amesa_lottery;

-- Clear existing data
TRUNCATE TABLE house_images CASCADE;
TRUNCATE TABLE houses CASCADE;

-- Get admin user ID for house creation
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
    SELECT id INTO admin_user_id FROM amesa_auth.users WHERE username = 'admin' LIMIT 1;

    -- Insert Houses
    INSERT INTO houses (id, title, description, price, location, address, bedrooms, bathrooms, square_feet, property_type, year_built, lot_size, features, status, total_tickets, ticket_price, lottery_start_date, lottery_end_date, draw_date, minimum_participation_percentage, created_by, created_at, updated_at)
    VALUES 
        (house1_id, 'Luxury Villa in Warsaw', 'Stunning 4-bedroom villa with panoramic city views, private garden, and modern amenities. Located in the prestigious Mokotów district of Warsaw.', 1200000.00, 'Warsaw, Poland', '15 Ulica Puławska, Mokotów, Warsaw', 4, 3, 3500, 'Villa', 2020, 0.5, ARRAY['Private Garden', 'City View', 'Parking', 'Security System', 'Air Conditioning', 'Modern Kitchen'], 0, 60000, 20.00, NOW() - INTERVAL '10 days', NOW() + INTERVAL '20 days', NOW() + INTERVAL '21 days', 75.00, admin_user_id, NOW() - INTERVAL '15 days', NOW() - INTERVAL '5 days'),
        
        (house2_id, 'Modern Apartment in Kraków', 'Contemporary 3-bedroom apartment in the heart of Kraków Old Town. Features floor-to-ceiling windows, smart home technology, and access to rooftop amenities.', 800000.00, 'Kraków, Poland', '42 Rynek Główny, Kraków', 3, 2, 1200, 'Apartment', 2022, 0.1, ARRAY['Smart Home', 'Rooftop Access', 'Gym', 'Parking', 'Balcony', 'Old Town View'], 1, 40000, 20.00, NOW() + INTERVAL '5 days', NOW() + INTERVAL '35 days', NOW() + INTERVAL '36 days', 80.00, admin_user_id, NOW() - INTERVAL '10 days', NOW() - INTERVAL '2 days'),
        
        (house3_id, 'Historic House in Gdańsk', 'Beautifully restored 5-bedroom historic house in the Old Town of Gdańsk. Combines traditional architecture with modern comforts.', 900000.00, 'Gdańsk, Poland', '8 Długi Targ, Old Town, Gdańsk', 5, 4, 2800, 'Historic House', 1890, 0.3, ARRAY['Historic Architecture', 'Garden', 'Terrace', 'Parking', 'Security', 'Restored'], 2, 45000, 20.00, NOW() - INTERVAL '60 days', NOW() - INTERVAL '30 days', NOW() - INTERVAL '29 days', 70.00, admin_user_id, NOW() - INTERVAL '75 days', NOW() - INTERVAL '29 days'),
        
        (house4_id, 'Beachfront Condo in Sopot', 'Luxurious 2-bedroom beachfront condo with direct beach access, infinity pool, and Baltic Sea views.', 600000.00, 'Sopot, Poland', '25 Molo, Sopot', 2, 2, 900, 'Condo', 2021, 0.05, ARRAY['Beach Access', 'Infinity Pool', 'Sea View', 'Balcony', 'Parking', 'Concierge'], 0, 30000, 20.00, NOW() - INTERVAL '5 days', NOW() + INTERVAL '25 days', NOW() + INTERVAL '26 days', 85.00, admin_user_id, NOW() - INTERVAL '8 days', NOW() - INTERVAL '3 days'),
        
        (house5_id, 'Mountain Villa in Zakopane', 'Spectacular 5-bedroom villa perched in the Tatra Mountains with breathtaking views of the peaks. Features traditional wooden architecture with modern luxury.', 1000000.00, 'Zakopane, Poland', '12 Krupówki, Zakopane', 5, 4, 3200, 'Villa', 2019, 0.8, ARRAY['Mountain View', 'Wooden Architecture', 'Garden', 'Fireplace', 'Parking', 'Ski Storage'], 0, 50000, 20.00, NOW() - INTERVAL '3 days', NOW() + INTERVAL '27 days', NOW() + INTERVAL '28 days', 80.00, admin_user_id, NOW() - INTERVAL '6 days', NOW() - INTERVAL '1 day'),
        
        (house6_id, 'Modern Penthouse in Wrocław', 'Stunning 4-bedroom penthouse with panoramic views of Wrocław Old Town and the Oder River. Features floor-to-ceiling windows and a private rooftop terrace.', 700000.00, 'Wrocław, Poland', '88 Rynek, Wrocław', 4, 3, 1800, 'Penthouse', 2023, 0.2, ARRAY['River View', 'Rooftop Terrace', 'Smart Home', 'Elevator', 'Parking', 'Concierge'], 1, 35000, 20.00, NOW() + INTERVAL '7 days', NOW() + INTERVAL '37 days', NOW() + INTERVAL '38 days', 85.00, admin_user_id, NOW() - INTERVAL '5 days', NOW() - INTERVAL '1 day'),
        
        (house7_id, 'Lake House in Mazury', 'Unique 3-bedroom lake house with private dock and stunning lake views. Perfect for those seeking tranquility and luxury by the water.', 500000.00, 'Mazury, Poland', '45 Lake View Road, Mazury', 3, 3, 1500, 'Villa', 2020, 0.6, ARRAY['Lake View', 'Private Dock', 'Water Access', 'Solar Panels', 'Parking', 'Outdoor Kitchen'], 0, 25000, 20.00, NOW() - INTERVAL '2 days', NOW() + INTERVAL '28 days', NOW() + INTERVAL '29 days', 75.00, admin_user_id, NOW() - INTERVAL '7 days', NOW() - INTERVAL '2 days'),
        
        (house8_id, 'Historic Mansion in Poznań', 'Magnificent 6-bedroom historic mansion in the heart of Poznań Old Town. Restored to perfection with original architectural details and modern amenities.', 1100000.00, 'Poznań, Poland', '3 Stary Rynek, Poznań', 6, 5, 4200, 'Historic Mansion', 1875, 0.4, ARRAY['Historic Architecture', 'Old Town Location', 'Garden', 'Terrace', 'Parking', 'Restored'], 1, 55000, 20.00, NOW() + INTERVAL '10 days', NOW() + INTERVAL '40 days', NOW() + INTERVAL '41 days', 70.00, admin_user_id, NOW() - INTERVAL '4 days', NOW() - INTERVAL '1 day');

    -- Insert House Images (5 images per house: 1 main + 4 small)
    INSERT INTO house_images (id, house_id, image_url, alt_text, display_order, is_primary, media_type, file_size, width, height, created_at)
    VALUES 
        -- House 1 - Luxury Villa in Warsaw
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=1200', 'Luxury Villa in Warsaw - Main Exterior View', 1, true, 0, 2048000, 1920, 1080, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Luxury Villa - Living Room', 2, false, 0, 475000, 600, 400, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Luxury Villa - Swimming Pool', 3, false, 0, 550000, 600, 400, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Luxury Villa - Master Bedroom', 4, false, 0, 525000, 600, 400, NOW() - INTERVAL '15 days'),
        (uuid_generate_v4(), house1_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Luxury Villa - Kitchen', 5, false, 0, 490000, 600, 400, NOW() - INTERVAL '15 days'),
        
        -- House 2 - Modern Apartment in Kraków
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200', 'Modern Apartment in Kraków - Main Living Room', 1, true, 0, 1960000, 1920, 1080, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Modern Apartment - Kitchen', 2, false, 0, 460000, 600, 400, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Modern Apartment - Bedroom', 3, false, 0, 475000, 600, 400, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Modern Apartment - Rooftop View', 4, false, 0, 550000, 600, 400, NOW() - INTERVAL '10 days'),
        (uuid_generate_v4(), house2_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400', 'Modern Apartment - City View', 5, false, 0, 500000, 600, 400, NOW() - INTERVAL '10 days'),
        
        -- House 3 - Historic House in Gdańsk
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200', 'Historic House in Gdańsk - Main Exterior', 1, true, 0, 2100000, 1920, 1080, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Historic House - Interior', 2, false, 0, 550000, 600, 400, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Historic House - Bedroom', 3, false, 0, 500000, 600, 400, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Historic House - Traditional Kitchen', 4, false, 0, 475000, 600, 400, NOW() - INTERVAL '75 days'),
        (uuid_generate_v4(), house3_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Historic House - Garden', 5, false, 0, 540000, 600, 400, NOW() - INTERVAL '75 days'),
        
        -- House 4 - Beachfront Condo in Sopot
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=1200', 'Beachfront Condo in Sopot - Main Sea View', 1, true, 0, 2300000, 1920, 1080, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400', 'Beachfront Condo - Infinity Pool', 2, false, 0, 540000, 600, 400, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Beachfront Condo - Living Room', 3, false, 0, 490000, 600, 400, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Beachfront Condo - Bedroom', 4, false, 0, 510000, 600, 400, NOW() - INTERVAL '8 days'),
        (uuid_generate_v4(), house4_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Beachfront Condo - Balcony View', 5, false, 0, 550000, 600, 400, NOW() - INTERVAL '8 days'),
        
        -- House 5 - Mountain Villa in Zakopane
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=1200', 'Mountain Villa in Zakopane - Main Exterior View', 1, true, 0, 2400000, 1920, 1080, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=400', 'Mountain Villa - Wooden Architecture', 2, false, 0, 575000, 600, 400, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Mountain Villa - Living Room with Fireplace', 3, false, 0, 540000, 600, 400, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Mountain Villa - Kitchen', 4, false, 0, 490000, 600, 400, NOW() - INTERVAL '6 days'),
        (uuid_generate_v4(), house5_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Mountain Villa - Garden View', 5, false, 0, 550000, 600, 400, NOW() - INTERVAL '6 days'),
        
        -- House 6 - Modern Penthouse in Wrocław
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200', 'Modern Penthouse in Wrocław - Main Living Room', 1, true, 0, 2360000, 1920, 1080, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400', 'Modern Penthouse - City View', 2, false, 0, 600000, 600, 400, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Modern Penthouse - Master Bedroom', 3, false, 0, 525000, 600, 400, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400', 'Modern Penthouse - Rooftop Terrace', 4, false, 0, 560000, 600, 400, NOW() - INTERVAL '5 days'),
        (uuid_generate_v4(), house6_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Modern Penthouse - Kitchen', 5, false, 0, 490000, 600, 400, NOW() - INTERVAL '5 days'),
        
        -- House 7 - Lake House in Mazury
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=1200', 'Lake House in Mazury - Main Exterior View', 1, true, 0, 2500000, 1920, 1080, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1571896349842-33c89424de2d?w=400', 'Lake House - Private Dock', 2, false, 0, 590000, 600, 400, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Lake House - Living Room', 3, false, 0, 540000, 600, 400, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Lake House - Bedroom', 4, false, 0, 510000, 600, 400, NOW() - INTERVAL '7 days'),
        (uuid_generate_v4(), house7_id, 'https://images.unsplash.com/photo-1544984243-ec57ea16fe25?w=400', 'Lake House - Lake View', 5, false, 0, 600000, 600, 400, NOW() - INTERVAL '7 days'),
        
        -- House 8 - Historic Mansion in Poznań
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1570129477492-45c003edd2be?w=1200', 'Historic Mansion in Poznań - Main Exterior', 1, true, 0, 2600000, 1920, 1080, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=400', 'Historic Mansion - Grand Living Room', 2, false, 0, 625000, 600, 400, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1600607687644-c7171b42498b?w=400', 'Historic Mansion - Master Suite', 3, false, 0, 575000, 600, 400, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=400', 'Historic Mansion - Garden Terrace', 4, false, 0, 600000, 600, 400, NOW() - INTERVAL '4 days'),
        (uuid_generate_v4(), house8_id, 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=400', 'Historic Mansion - Kitchen', 5, false, 0, 550000, 600, 400, NOW() - INTERVAL '4 days');

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
SELECT 'All data has been seeded across all schemas.' AS message;
SELECT 'Frontend should now display houses, translations, and all demo data.' AS result;
