-- Simple database seeder with correct schema and Polish translations
-- This script safely seeds the database without truncating existing data

-- Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Seed Languages (6 languages including Polish)
INSERT INTO amesa_content."Languages" ("Code", "Name", "NativeName", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
VALUES 
    ('en', 'English', 'English', true, true, 1, NOW(), NOW()),
    ('he', 'Hebrew', 'עברית', true, false, 2, NOW(), NOW()),
    ('ar', 'Arabic', 'العربية', true, false, 3, NOW(), NOW()),
    ('es', 'Spanish', 'Español', true, false, 4, NOW(), NOW()),
    ('fr', 'French', 'Français', true, false, 5, NOW(), NOW()),
    ('pl', 'Polish', 'Polski', true, false, 6, NOW(), NOW())
ON CONFLICT ("Code") DO NOTHING;

-- 2. Seed comprehensive translations from external file
\i comprehensive-translations.sql

-- 3. Seed Polish translations from external file  
\i polish-translations-addon.sql

-- 4. Seed test users
INSERT INTO amesa_auth."Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "Gender", "DateOfBirth", "Status", "VerificationStatus", "AuthProvider", "CreatedAt", "UpdatedAt", "CreatedBy")
VALUES 
    (uuid_generate_v4(), 'user1@amesa.com', 'John', 'Doe', '$2a$11$example.hash.here', 'Male', '1990-01-01', 'Active', 'Verified', 'Email', NOW(), NOW(), uuid_generate_v4()),
    (uuid_generate_v4(), 'user2@amesa.com', 'Jane', 'Smith', '$2a$11$example.hash.here', 'Female', '1985-05-15', 'Active', 'Verified', 'Email', NOW(), NOW(), uuid_generate_v4()),
    (uuid_generate_v4(), 'user3@amesa.com', 'Ahmed', 'Hassan', '$2a$11$example.hash.here', 'Male', '1992-08-20', 'Active', 'Verified', 'Email', NOW(), NOW(), uuid_generate_v4()),
    (uuid_generate_v4(), 'user4@amesa.com', 'Maria', 'Garcia', '$2a$11$example.hash.here', 'Female', '1988-12-10', 'Active', 'Verified', 'Email', NOW(), NOW(), uuid_generate_v4()),
    (uuid_generate_v4(), 'user5@amesa.com', 'Anna', 'Kowalski', '$2a$11$example.hash.here', 'Female', '1995-03-25', 'Active', 'Verified', 'Email', NOW(), NOW(), uuid_generate_v4())
ON CONFLICT ("Email") DO NOTHING;

-- 5. Seed houses (5 houses with correct property names)
WITH house_data AS (
    SELECT 
        uuid_generate_v4() as id,
        'Luxury Villa in Tel Aviv' as title,
        'Beautiful modern villa with sea view and premium amenities' as description,
        2500000.00 as price,
        'Tel Aviv, Israel' as location,
        '123 Rothschild Blvd, Tel Aviv' as address,
        5 as bedrooms,
        4 as bathrooms,
        3500 as squarefeet,
        'Villa' as propertytype,
        2020 as yearbuilt,
        500.00 as lotsize,
        ARRAY['Sea View', 'Swimming Pool', 'Garden', 'Parking', 'Security System'] as features,
        'Active'::lottery_status as status,
        25000 as totaltickets,
        100.00 as ticketprice,
        NOW() + INTERVAL '30 days' as lotteryenddate,
        NOW() + INTERVAL '35 days' as drawdate,
        75.00 as minimumparticipationpercentage,
        NOW() as createdat,
        NOW() as updatedat
    UNION ALL
    SELECT 
        uuid_generate_v4(),
        'Penthouse in Jerusalem',
        'Stunning penthouse with panoramic city views and luxury finishes',
        3200000.00,
        'Jerusalem, Israel',
        '456 King David St, Jerusalem',
        4,
        3,
        2800,
        'Penthouse',
        2019,
        0.00,
        ARRAY['City View', 'Balcony', 'Modern Kitchen', 'Master Suite', 'Storage'],
        'Active'::lottery_status,
        21333,
        150.00,
        NOW() + INTERVAL '45 days',
        NOW() + INTERVAL '50 days',
        75.00,
        NOW(),
        NOW()
    UNION ALL
    SELECT 
        uuid_generate_v4(),
        'Beach House in Herzliya',
        'Exclusive beachfront property with private beach access',
        4500000.00,
        'Herzliya, Israel',
        '789 Marina Blvd, Herzliya',
        6,
        5,
        4200,
        'Beach House',
        2021,
        800.00,
        ARRAY['Beach Access', 'Swimming Pool', 'Outdoor Kitchen', 'Guest House', 'Boat Dock'],
        'Active'::lottery_status,
        22500,
        200.00,
        NOW() + INTERVAL '60 days',
        NOW() + INTERVAL '65 days',
        75.00,
        NOW(),
        NOW()
    UNION ALL
    SELECT 
        uuid_generate_v4(),
        'Modern Apartment in Haifa',
        'Contemporary apartment with mountain views and modern amenities',
        1800000.00,
        'Haifa, Israel',
        '321 Carmel Ave, Haifa',
        3,
        2,
        1800,
        'Apartment',
        2018,
        0.00,
        ARRAY['Mountain View', 'Balcony', 'Modern Design', 'Parking', 'Storage'],
        'Active'::lottery_status,
        24000,
        75.00,
        NOW() + INTERVAL '75 days',
        NOW() + INTERVAL '80 days',
        75.00,
        NOW(),
        NOW()
    UNION ALL
    SELECT 
        uuid_generate_v4(),
        'Country Estate in Galilee',
        'Spacious estate surrounded by nature with vineyard and olive groves',
        2800000.00,
        'Galilee, Israel',
        '654 Nature Trail, Galilee',
        7,
        6,
        5000,
        'Estate',
        2017,
        2000.00,
        ARRAY['Vineyard', 'Olive Grove', 'Guest House', 'Stables', 'Wine Cellar'],
        'Active'::lottery_status,
        23333,
        120.00,
        NOW() + INTERVAL '90 days',
        NOW() + INTERVAL '95 days',
        75.00,
        NOW(),
        NOW()
)
INSERT INTO amesa_lottery."Houses" (
    "Id", "Title", "Description", "Price", "Location", "Address", 
    "Bedrooms", "Bathrooms", "SquareFeet", "PropertyType", "YearBuilt", "LotSize", 
    "Features", "Status", "TotalTickets", "TicketPrice", "LotteryEndDate", "DrawDate",
    "MinimumParticipationPercentage", "CreatedAt", "UpdatedAt"
)
SELECT * FROM house_data
ON CONFLICT ("Id") DO NOTHING;

-- 6. Seed house images (4 images per house: 1 primary + 3 additional)
WITH house_ids AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "CreatedAt") as house_num
    FROM amesa_lottery."Houses"
    LIMIT 5
),
image_data AS (
    SELECT 
        h."Id" as house_id,
        CASE 
            WHEN img_num = 1 THEN 'https://images.unsplash.com/photo-1613490493576-7fde63acd811?w=800'
            WHEN img_num = 2 THEN 'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?w=800'
            WHEN img_num = 3 THEN 'https://images.unsplash.com/photo-1600607687939-ce8a6c25118c?w=800'
            WHEN img_num = 4 THEN 'https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?w=800'
        END as image_url,
        'House Image ' || img_num as alt_text,
        img_num as display_order,
        CASE WHEN img_num = 1 THEN true ELSE false END as is_primary,
        'Image'::media_type as media_type
    FROM house_ids h
    CROSS JOIN generate_series(1, 4) as img_num
)
INSERT INTO amesa_lottery."HouseImages" (
    "Id", "HouseId", "ImageUrl", "AltText", "DisplayOrder", "IsPrimary", "MediaType", "CreatedAt"
)
SELECT 
    uuid_generate_v4(),
    house_id,
    image_url,
    alt_text,
    display_order,
    is_primary,
    media_type,
    NOW()
FROM image_data
ON CONFLICT DO NOTHING;

-- Final status report
SELECT 
    'Languages' as table_name, 
    COUNT(*) as record_count 
FROM amesa_content."Languages"
UNION ALL
SELECT 
    'Translations', 
    COUNT(*) 
FROM amesa_content."Translations"
UNION ALL
SELECT 
    'Polish Translations', 
    COUNT(*) 
FROM amesa_content."Translations" 
WHERE "LanguageCode" = 'pl'
UNION ALL
SELECT 
    'Users', 
    COUNT(*) 
FROM amesa_auth."Users"
UNION ALL
SELECT 
    'Houses', 
    COUNT(*) 
FROM amesa_lottery."Houses"
UNION ALL
SELECT 
    'House Images', 
    COUNT(*) 
FROM amesa_lottery."HouseImages";

-- Success message
SELECT 'Database seeding completed successfully! All tables populated with Polish translations included.' as status;
