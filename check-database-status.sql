-- Quick database status check
-- Run this to see what data exists in the database

-- Check Languages
SELECT 'Languages' as table_name, COUNT(*) as record_count FROM amesa_content.languages
UNION ALL
-- Check Translations  
SELECT 'Translations' as table_name, COUNT(*) as record_count FROM amesa_content.translations
UNION ALL
-- Check Users
SELECT 'Users' as table_name, COUNT(*) as record_count FROM amesa_auth.users
UNION ALL
-- Check Houses
SELECT 'Houses' as table_name, COUNT(*) as record_count FROM amesa_lottery.houses
UNION ALL
-- Check House Images
SELECT 'House Images' as table_name, COUNT(*) as record_count FROM amesa_lottery.house_images
UNION ALL
-- Check User Addresses
SELECT 'User Addresses' as table_name, COUNT(*) as record_count FROM amesa_auth.user_addresses
UNION ALL
-- Check User Phones
SELECT 'User Phones' as table_name, COUNT(*) as record_count FROM amesa_auth.user_phones;

-- Show sample data from each table
SELECT 'Sample Languages:' as info;
SELECT code, name FROM amesa_content.languages LIMIT 3;

SELECT 'Sample Translations:' as info;
SELECT language_code, key, value FROM amesa_content.translations LIMIT 3;

SELECT 'Sample Users:' as info;
SELECT id, email, first_name, last_name FROM amesa_auth.users LIMIT 3;

SELECT 'Sample Houses:' as info;
SELECT id, title, price, city FROM amesa_lottery.houses LIMIT 3;
