-- ============================================
-- GET EXACT COLUMN NAMES FOR KEY TABLES
-- ============================================

-- 1. Languages table columns (amesa_content.languages)
SELECT 
    'LANGUAGES COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_content' AND table_name = 'languages'
ORDER BY ordinal_position;

-- 2. Translations table columns (amesa_content.translations)
SELECT 
    'TRANSLATIONS COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_content' AND table_name = 'translations'
ORDER BY ordinal_position;

-- 3. Users table columns (amesa_auth.users)
SELECT 
    'USERS COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_auth' AND table_name = 'users'
ORDER BY ordinal_position;

-- 4. User addresses table columns (amesa_auth.user_addresses)
SELECT 
    'USER_ADDRESSES COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_auth' AND table_name = 'user_addresses'
ORDER BY ordinal_position;

-- 5. User phones table columns (amesa_auth.user_phones)
SELECT 
    'USER_PHONES COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_auth' AND table_name = 'user_phones'
ORDER BY ordinal_position;

-- 6. Houses table columns (amesa_lottery.houses)
SELECT 
    'HOUSES COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_lottery' AND table_name = 'houses'
ORDER BY ordinal_position;

-- 7. House images table columns (amesa_lottery.house_images)
SELECT 
    'HOUSE_IMAGES COLUMNS:' AS section,
    column_name AS exact_column_name,
    data_type,
    is_nullable,
    ordinal_position
FROM information_schema.columns 
WHERE table_schema = 'amesa_lottery' AND table_name = 'house_images'
ORDER BY ordinal_position;
