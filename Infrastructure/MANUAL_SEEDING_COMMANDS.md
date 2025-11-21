# Manual Database Seeding Commands

## Quick Start

Run these commands to populate all schemas with data:

### Step 1: Connect to Container

```powershell
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text
aws ecs execute-command --cluster Amesa --task $taskArn --container amesa-lottery-service-container --interactive --region eu-north-1
```

### Step 2: Once Connected, Run These Commands

```bash
# Set database connection
export PGPASSWORD='u1fwn3s9'
export DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
export DB_NAME='amesa_prod'
export DB_USER='amesa_admin'
export DB_PORT=5432

# Install PostgreSQL client if needed
which psql || apk add --no-cache postgresql-client

# Test connection
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -c "SELECT 1;"
```

### Step 3: Seed Languages (amesa_content schema)

```bash
psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME <<'EOF'
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

SELECT 'Languages seeded: ' || COUNT(*)::text FROM "Languages";
EOF
```

### Step 4: Check Schema Status

```bash
for schema in amesa_auth amesa_content amesa_lottery amesa_lottery_results amesa_payment amesa_notification amesa_analytics; do
    echo "Schema: $schema"
    table_count=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$schema';" 2>/dev/null || echo "0")
    echo "  Tables: $table_count"
    
    if [ "$table_count" -gt "0" ]; then
        case $schema in
            "amesa_auth")
                count=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc "SELECT COUNT(*) FROM $schema.\"Users\";" 2>/dev/null || echo "0")
                echo "  Users: $count"
                ;;
            "amesa_content")
                lang_count=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc "SELECT COUNT(*) FROM $schema.\"Languages\";" 2>/dev/null || echo "0")
                trans_count=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc "SELECT COUNT(*) FROM $schema.\"Translations\";" 2>/dev/null || echo "0")
                echo "  Languages: $lang_count, Translations: $trans_count"
                ;;
            "amesa_lottery")
                house_count=$(psql -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME -tAc "SELECT COUNT(*) FROM $schema.\"Houses\";" 2>/dev/null || echo "0")
                echo "  Houses: $house_count"
                ;;
        esac
    fi
    echo ""
done
```

## For Complex Data (Users, Houses, etc.)

The .NET DatabaseSeeder handles:
- ✅ Password hashing (SHA256)
- ✅ Complex relationships
- ✅ Comprehensive translations
- ✅ House images
- ✅ Lottery tickets and draws

**To use .NET seeder:**

1. Ensure migrations are run for each schema
2. Run the seeder with appropriate SearchPath for each schema:

```bash
# For amesa_auth schema
export DB_CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SearchPath=amesa_auth;"
dotnet run -- --seeder

# For amesa_lottery schema  
export DB_CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SearchPath=amesa_lottery;"
dotnet run -- --seeder
```

## What Gets Seeded

### amesa_content
- ✅ 6 Languages (English, Hebrew, Arabic, Spanish, French, Polish)
- ⚠️  Translations (requires .NET seeder for comprehensive data)

### amesa_auth
- ⚠️  Users (requires .NET seeder for password hashing)
- ⚠️  User addresses and phones

### amesa_lottery
- ⚠️  Houses (requires .NET seeder for images and complex data)
- ⚠️  Lottery tickets
- ⚠️  Lottery draws

### amesa_lottery_results
- ⚠️  Results (generated after draws)

### amesa_payment
- ⚠️  Transactions (created during purchases)

### amesa_notification
- ⚠️  Notifications (created dynamically)

### amesa_analytics
- ⚠️  Analytics events (collected over time)

## Summary

**What you can seed via SQL:**
- ✅ Languages (simple data)

**What requires .NET seeder:**
- Users (password hashing)
- Houses (complex data, images)
- Translations (comprehensive data)
- Lottery tickets and draws
- All other complex relationships

