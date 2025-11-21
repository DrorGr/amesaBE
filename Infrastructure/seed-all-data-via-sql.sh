#!/bin/bash
set -e

export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

echo "========================================"
echo "Populating All Schemas with Data"
echo "========================================"
echo ""

# Install PostgreSQL client if needed
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL client..."
    if command -v apk &> /dev/null; then
        apk add --no-cache postgresql-client
    elif command -v apt-get &> /dev/null; then
        apt-get update && apt-get install -y postgresql-client
    fi
fi

# Test connection
echo "Testing database connection..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" >/dev/null 2>&1 || {
    echo "Cannot connect to database"
    exit 1
}
echo "✅ Database connection successful"
echo ""

# Seed amesa_content schema (Languages)
echo "🌱 Seeding amesa_content schema..."
psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" <<'EOF'
SET search_path TO amesa_content;

-- Insert Languages
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

SELECT COUNT(*) as languages_count FROM "Languages";
EOF

LANG_COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM amesa_content.\"Languages\";" 2>/dev/null || echo "0")
echo "  ✅ Languages: $LANG_COUNT"
echo ""

# Check other schemas for table existence and provide guidance
echo "📋 Schema Status:"
echo ""

SCHEMAS=("amesa_auth" "amesa_content" "amesa_lottery" "amesa_lottery_results" "amesa_payment" "amesa_notification" "amesa_analytics")

for schema in "${SCHEMAS[@]}"; do
    table_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$schema';" 2>/dev/null || echo "0")
    echo "  $schema: $table_count table(s)"
    
    if [ "$table_count" -eq "0" ]; then
        echo "    ⚠️  No tables - run migrations first"
    else
        # Count rows in main tables
        case $schema in
            "amesa_auth")
                user_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM $schema.\"Users\";" 2>/dev/null || echo "0")
                echo "    Users: $user_count"
                ;;
            "amesa_content")
                trans_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM $schema.\"Translations\";" 2>/dev/null || echo "0")
                echo "    Translations: $trans_count"
                ;;
            "amesa_lottery")
                house_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM $schema.\"Houses\";" 2>/dev/null || echo "0")
                echo "    Houses: $house_count"
                ;;
        esac
    fi
done

echo ""
echo "========================================"
echo "✅ Data population check completed!"
echo "========================================"
echo ""
echo "Note: For full data seeding (users, houses, etc.),"
echo "      use the .NET DatabaseSeeder which handles:"
echo "      - Password hashing"
echo "      - Complex relationships"
echo "      - Comprehensive translations"
echo ""

