-- Complete Database Seeding Script
-- Seeds all schemas with relevant data
-- Note: Some complex data (house images, comprehensive translations) requires .NET seeder

-- ============================================
-- amesa_content schema: Languages
-- ============================================
SET search_path TO amesa_content;

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'en');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'he');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'ar');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'es');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'fr');

INSERT INTO "Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW()
WHERE NOT EXISTS (SELECT 1 FROM "Languages" WHERE "Code" = 'pl');

SELECT '✅ Languages seeded: ' || COUNT(*)::text FROM "Languages";

-- ============================================
-- amesa_auth schema: Users
-- Password hashes: Admin123! = SC4iAugHld+2khAkLAJwINPkymeZOSkiIba6rWrWGuc=
--                  Password123! = ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=
-- ============================================
SET search_path TO amesa_auth;

-- Admin user
INSERT INTO "Users" ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "PreferredLanguage", "Timezone", "LastLoginAt", "CreatedAt", "UpdatedAt")
SELECT 
    gen_random_uuid(),
    'admin',
    'admin@amesa.com',
    true,
    '+972501234567',
    true,
    'SC4iAugHld+2khAkLAJwINPkymeZOSkiIba6rWrWGuc=',
    'Admin',
    'User',
    '1985-05-15'::timestamp,
    0, -- Male
    '123456789',
    0, -- Active
    2, -- FullyVerified
    0, -- Email
    'en',
    'Asia/Jerusalem',
    NOW() - INTERVAL '2 hours',
    NOW() - INTERVAL '30 days',
    NOW() - INTERVAL '2 hours'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'admin' OR "Email" = 'admin@amesa.com');

-- Get admin user ID for addresses
DO $$
DECLARE
    admin_user_id UUID;
BEGIN
    SELECT "Id" INTO admin_user_id FROM "Users" WHERE "Username" = 'admin' LIMIT 1;
    
    IF admin_user_id IS NOT NULL THEN
        -- Admin address
        INSERT INTO "UserAddresses" ("Id", "UserId", "Type", "Country", "City", "Street", "HouseNumber", "ZipCode", "IsPrimary", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), admin_user_id, 'home', 'Israel', 'Tel Aviv', 'Rothschild Boulevard', '15', '6688119', true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'
        WHERE NOT EXISTS (SELECT 1 FROM "UserAddresses" WHERE "UserId" = admin_user_id AND "Type" = 'home');
        
        -- Admin phone
        INSERT INTO "UserPhones" ("Id", "UserId", "PhoneNumber", "IsPrimary", "IsVerified", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), admin_user_id, '+972501234567', true, true, NOW() - INTERVAL '30 days', NOW() - INTERVAL '30 days'
        WHERE NOT EXISTS (SELECT 1 FROM "UserPhones" WHERE "UserId" = admin_user_id AND "PhoneNumber" = '+972501234567');
    END IF;
END $$;

-- Regular users
INSERT INTO "Users" ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "PreferredLanguage", "Timezone", "LastLoginAt", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'john_doe', 'john.doe@example.com', true, '+972501234568', true, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'John', 'Doe', '1990-08-22'::timestamp, 0, '987654321', 0, 2, 0, 'en', 'Asia/Jerusalem', NOW() - INTERVAL '1 hour', NOW() - INTERVAL '15 days', NOW() - INTERVAL '1 hour'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'john_doe');

INSERT INTO "Users" ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "PreferredLanguage", "Timezone", "LastLoginAt", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'sarah_wilson', 'sarah.wilson@example.com', true, '+972501234569', false, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Sarah', 'Wilson', '1988-12-03'::timestamp, 1, '456789123', 0, 1, 0, 'he', 'Asia/Jerusalem', NOW() - INTERVAL '1 day', NOW() - INTERVAL '10 days', NOW() - INTERVAL '1 day'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'sarah_wilson');

INSERT INTO "Users" ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "PreferredLanguage", "Timezone", "LastLoginAt", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'ahmed_hassan', 'ahmed.hassan@example.com', true, '+972501234570', true, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Ahmed', 'Hassan', '1992-03-18'::timestamp, 0, '789123456', 0, 2, 0, 'ar', 'Asia/Jerusalem', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '20 days', NOW() - INTERVAL '3 hours'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'ahmed_hassan');

INSERT INTO "Users" ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "PreferredLanguage", "Timezone", "LastLoginAt", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'maria_garcia', 'maria.garcia@example.com', false, '+972501234571', false, 'ZdHohjh3AkKMxE9dqlLTy3/VN4Y7dwKqOkuWDUE92OA=', 'Maria', 'Garcia', '1995-07-25'::timestamp, 1, '321654987', 1, 0, 0, 'es', 'Asia/Jerusalem', NULL, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'maria_garcia');

-- Add addresses and phones for other users
DO $$
DECLARE
    user_rec RECORD;
BEGIN
    FOR user_rec IN SELECT "Id", "Username", "Phone" FROM "Users" WHERE "Username" IN ('john_doe', 'sarah_wilson', 'ahmed_hassan') LOOP
        -- Addresses
        INSERT INTO "UserAddresses" ("Id", "UserId", "Type", "Country", "City", "Street", "HouseNumber", "ZipCode", "IsPrimary", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), user_rec."Id", 'home', 'Israel', 
            CASE user_rec."Username"
                WHEN 'john_doe' THEN 'Jerusalem'
                WHEN 'sarah_wilson' THEN 'Haifa'
                WHEN 'ahmed_hassan' THEN 'Tel Aviv'
            END,
            CASE user_rec."Username"
                WHEN 'john_doe' THEN 'King George Street'
                WHEN 'sarah_wilson' THEN 'Herzl Street'
                WHEN 'ahmed_hassan' THEN 'Dizengoff Street'
            END,
            CASE user_rec."Username"
                WHEN 'john_doe' THEN '42'
                WHEN 'sarah_wilson' THEN '88'
                WHEN 'ahmed_hassan' THEN '100'
            END,
            CASE user_rec."Username"
                WHEN 'john_doe' THEN '9100000'
                WHEN 'sarah_wilson' THEN '3100000'
                WHEN 'ahmed_hassan' THEN '6436100'
            END,
            true, NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'
        WHERE NOT EXISTS (SELECT 1 FROM "UserAddresses" WHERE "UserId" = user_rec."Id" AND "Type" = 'home');
        
        -- Phones
        INSERT INTO "UserPhones" ("Id", "UserId", "PhoneNumber", "IsPrimary", "IsVerified", "CreatedAt", "UpdatedAt")
        SELECT gen_random_uuid(), user_rec."Id", user_rec."Phone", true, 
            CASE WHEN user_rec."Username" = 'sarah_wilson' THEN false ELSE true END,
            NOW() - INTERVAL '15 days', NOW() - INTERVAL '15 days'
        WHERE NOT EXISTS (SELECT 1 FROM "UserPhones" WHERE "UserId" = user_rec."Id" AND "PhoneNumber" = user_rec."Phone");
    END LOOP;
END $$;

SELECT '✅ Users seeded: ' || COUNT(*)::text FROM "Users";

-- ============================================
-- Summary
-- ============================================
SELECT 'Seeding completed!' AS status;

