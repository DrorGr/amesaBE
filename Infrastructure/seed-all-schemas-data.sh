#!/bin/bash
set -e

export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

echo "========================================"
echo "Populating All Database Schemas"
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

# Function to seed a schema
seed_schema() {
    local schema=$1
    echo "🌱 Seeding schema: $schema"
    
    # Set search path
    export PGOPTIONS="-c search_path=$schema"
    
    case $schema in
        "amesa_auth")
            seed_auth_schema
            ;;
        "amesa_content")
            seed_content_schema
            ;;
        "amesa_lottery")
            seed_lottery_schema
            ;;
        "amesa_lottery_results")
            seed_lottery_results_schema
            ;;
        "amesa_payment")
            seed_payment_schema
            ;;
        "amesa_notification")
            seed_notification_schema
            ;;
        "amesa_analytics")
            seed_analytics_schema
            ;;
    esac
    
    echo "✅ Schema $schema seeded"
    echo ""
}

# Seed amesa_auth schema
seed_auth_schema() {
    echo "  Seeding authentication data..."
    
    # Check if Users table exists
    table_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_auth' AND table_name='Users' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$table_exists" != "1" ]; then
        echo "  ⚠️  Users table does not exist. Run migrations first."
        return 0
    fi
    
    # Check if data already exists
    user_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM amesa_auth.\"Users\";" 2>/dev/null || echo "0")
    
    if [ "$user_count" -gt "0" ]; then
        echo "  ⏭️  Users already exist ($user_count users). Skipping..."
        return 0
    fi
    
    echo "  📝 Inserting sample users..."
    # Note: This is a simplified version. Full seeding requires the .NET seeder
    echo "  💡 Use .NET DatabaseSeeder for full user data with proper password hashing"
}

# Seed amesa_content schema
seed_content_schema() {
    echo "  Seeding content/translations..."
    
    # Check if Languages table exists
    lang_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_content' AND table_name='Languages' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$lang_exists" != "1" ]; then
        echo "  ⚠️  Languages table does not exist. Run migrations first."
        return 0
    fi
    
    # Check if data exists
    lang_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM amesa_content.\"Languages\";" 2>/dev/null || echo "0")
    
    if [ "$lang_count" -gt "0" ]; then
        echo "  ⏭️  Languages already exist ($lang_count languages). Skipping..."
        return 0
    fi
    
    echo "  📝 Inserting languages..."
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" <<EOF
INSERT INTO amesa_content."Languages" ("Id", "Code", "Name", "NativeName", "FlagUrl", "IsActive", "IsDefault", "DisplayOrder", "CreatedAt", "UpdatedAt")
VALUES 
    (gen_random_uuid(), 'en', 'English', 'English', '🇺🇸', true, true, 1, NOW(), NOW()),
    (gen_random_uuid(), 'he', 'Hebrew', 'עברית', '🇮🇱', true, false, 2, NOW(), NOW()),
    (gen_random_uuid(), 'ar', 'Arabic', 'العربية', '🇸🇦', true, false, 3, NOW(), NOW()),
    (gen_random_uuid(), 'es', 'Spanish', 'Español', '🇪🇸', true, false, 4, NOW(), NOW()),
    (gen_random_uuid(), 'fr', 'French', 'Français', '🇫🇷', true, false, 5, NOW(), NOW()),
    (gen_random_uuid(), 'pl', 'Polish', 'Polski', '🇵🇱', true, false, 6, NOW(), NOW())
ON CONFLICT DO NOTHING;
EOF
    
    echo "  ✅ Languages inserted"
}

# Seed amesa_lottery schema
seed_lottery_schema() {
    echo "  Seeding lottery data..."
    
    # Check if Houses table exists
    houses_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_lottery' AND table_name='Houses' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$houses_exists" != "1" ]; then
        echo "  ⚠️  Houses table does not exist. Run migrations first."
        return 0
    fi
    
    house_count=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM amesa_lottery.\"Houses\";" 2>/dev/null || echo "0")
    
    if [ "$house_count" -gt "0" ]; then
        echo "  ⏭️  Houses already exist ($house_count houses). Skipping..."
        return 0
    fi
    
    echo "  💡 Use .NET DatabaseSeeder for full house data with images and lottery details"
}

# Seed amesa_lottery_results schema
seed_lottery_results_schema() {
    echo "  Seeding lottery results..."
    
    results_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_lottery_results' AND table_name='LotteryResults' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$results_exists" != "1" ]; then
        echo "  ⚠️  LotteryResults table does not exist. Run migrations first."
        return 0
    fi
    
    echo "  💡 Lottery results are generated after draws. No initial seed data needed."
}

# Seed amesa_payment schema
seed_payment_schema() {
    echo "  Seeding payment data..."
    
    payment_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_payment' AND table_name='Transactions' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$payment_exists" != "1" ]; then
        echo "  ⚠️  Payment tables do not exist. Run migrations first."
        return 0
    fi
    
    echo "  💡 Payment data is created during transactions. No initial seed data needed."
}

# Seed amesa_notification schema
seed_notification_schema() {
    echo "  Seeding notifications..."
    
    notif_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_notification' AND table_name='Notifications' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$notif_exists" != "1" ]; then
        echo "  ⚠️  Notifications table does not exist. Run migrations first."
        return 0
    fi
    
    echo "  💡 Notifications are created dynamically. No initial seed data needed."
}

# Seed amesa_analytics schema
seed_analytics_schema() {
    echo "  Seeding analytics..."
    
    analytics_exists=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT 1 FROM information_schema.tables WHERE table_schema='amesa_analytics' AND table_name='AnalyticsEvents' LIMIT 1;" 2>/dev/null || echo "0")
    
    if [ "$analytics_exists" != "1" ]; then
        echo "  ⚠️  Analytics tables do not exist. Run migrations first."
        return 0
    fi
    
    echo "  💡 Analytics data is collected over time. No initial seed data needed."
}

# Main execution
SCHEMAS=("amesa_auth" "amesa_content" "amesa_lottery" "amesa_lottery_results" "amesa_payment" "amesa_notification" "amesa_analytics")

for schema in "${SCHEMAS[@]}"; do
    seed_schema "$schema"
done

echo "========================================"
echo "✅ Schema population completed!"
echo "========================================"
echo ""
echo "Note: Full data seeding requires:"
echo "  1. Running migrations to create tables"
echo "  2. Using .NET DatabaseSeeder for complex data (users, houses, etc.)"
echo ""

