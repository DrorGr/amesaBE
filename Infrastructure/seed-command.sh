#!/bin/bash
set -e

export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

echo "========================================"
echo "Database Schema Seeding"
echo "========================================"
echo ""

# Install PostgreSQL client if needed
if ! command -v psql &> /dev/null; then
    echo "Installing PostgreSQL client..."
    if command -v apk &> /dev/null; then
        apk add --no-cache postgresql-client
    elif command -v apt-get &> /dev/null; then
        apt-get update && apt-get install -y postgresql-client
    elif command -v yum &> /dev/null; then
        yum install -y postgresql
    else
        echo "Cannot install psql automatically"
        exit 1
    fi
fi

echo "Testing database connection..."
if ! psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "SELECT 1;" >/dev/null 2>&1; then
    echo "Cannot connect to database"
    exit 1
fi

echo "Database connection successful!"
echo ""

# Create schemas
SCHEMAS=("amesa_auth" "amesa_content" "amesa_lottery" "amesa_lottery_results" "amesa_payment" "amesa_notification" "amesa_analytics")

for schema in "${SCHEMAS[@]}"; do
    echo "Processing schema: $schema"
    psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -c "CREATE SCHEMA IF NOT EXISTS $schema;" || {
        echo "Failed to create schema: $schema"
        exit 1
    }
    COUNT=$(psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$schema';" 2>/dev/null || echo "0")
    echo "  Tables in $schema: $COUNT"
done

echo ""
echo "========================================"
echo "All schemas created successfully!"
echo "========================================"

