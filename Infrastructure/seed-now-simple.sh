#!/bin/bash
set -e

export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

echo "========================================"
echo "Database Seeding"
echo "========================================"
echo ""

# Install psql if needed
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL client..."
    apk add --no-cache postgresql-client || apt-get update && apt-get install -y postgresql-client
fi

# Test connection
echo "Testing connection..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" >/dev/null 2>&1 || {
    echo "Cannot connect to database"
    exit 1
}
echo "✅ Connected"
echo ""

# Run the SQL file
echo "Executing seeding script..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -f /dev/stdin <<'SQL_EOF'
-- Complete Database Seeding Script
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

SELECT '✅ Languages: ' || COUNT(*)::text FROM "Languages";

SET search_path TO amesa_auth;

INSERT INTO "Users" ("Id", "Username", "Email", "EmailVerified", "Phone", "PhoneVerified", "PasswordHash", "FirstName", "LastName", "DateOfBirth", "Gender", "IdNumber", "Status", "VerificationStatus", "AuthProvider", "PreferredLanguage", "Timezone", "LastLoginAt", "CreatedAt", "UpdatedAt")
SELECT gen_random_uuid(), 'admin', 'admin@amesa.com', true, '+972501234567', true, 'SC4iAugHld+2khAkLAJwINPkymeZOSkiIba6rWrWGuc=', 'Admin', 'User', '1985-05-15'::timestamp, 0, '123456789', 0, 2, 0, 'en', 'Asia/Jerusalem', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '30 days', NOW() - INTERVAL '2 hours'
WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Username" = 'admin');

SELECT '✅ Users seeded: ' || COUNT(*)::text FROM "Users";
SELECT 'Seeding completed!' AS status;
SQL_EOF

echo ""
echo "========================================"
echo "✅ Seeding completed!"
echo "========================================"

