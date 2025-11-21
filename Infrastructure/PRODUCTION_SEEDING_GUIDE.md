# Production Database Seeding Guide

## ⚠️ IMPORTANT: Automatic Seeding Disabled in Production

To prevent high database costs, **automatic seeding is disabled in production**. All services now skip `EnsureCreatedAsync()` and seeding operations when `ASPNETCORE_ENVIRONMENT=Production`.

## Manual Seeding for Production

### Option 1: One-Time Manual Seeding (Recommended)

Run the comprehensive seeder once after deployment:

```bash
# Connect to ECS container
aws ecs execute-command --cluster amesa-cluster --task <task-id> --container amesa-backend --interactive --command "/bin/bash"

# Inside container, run seeder
cd /app
dotnet AmesaBackend.dll --seeder
```

### Option 2: SQL Scripts (Basic Data Only)

For basic data without full demo content:

```sql
-- Run these scripts in order:
-- 1. Languages and basic users
\i /path/to/complete-seed-all-data.sql

-- 2. Essential translations (19 keys)
\i /path/to/seed-amesa-content-translations.sql
```

### Option 3: Database Migrations (Future)

For production deployments, use EF Core migrations:

```bash
# Generate migration
dotnet ef migrations add InitialCreate --project AmesaBackend

# Apply to production database
dotnet ef database update --project AmesaBackend --connection "your-prod-connection-string"
```

## What Was Changed

### Services Modified:
- ✅ **AmesaBackend** - Removed auto-seeding in production
- ✅ **AmesaBackend.Auth** - Added production guards
- ✅ **AmesaBackend.Content** - Added production guards  
- ✅ **AmesaBackend.Lottery** - Added production guards
- ✅ **AmesaBackend.Payment** - Added production guards
- ✅ **AmesaBackend.Notification** - Added production guards
- ✅ **AmesaBackend.LotteryResults** - Added production guards
- ✅ **AmesaBackend.Analytics** - Added production guards

### Changes Made:
1. **Environment Guards**: All services check `IsDevelopment()` before seeding
2. **Production Logging**: Clear messages about skipping seeding
3. **Cost Prevention**: No more automatic `EnsureCreatedAsync()` in production
4. **Manual Control**: Seeding only when explicitly requested

## Cost Impact

### Before (High Cost):
- 8 services × Every deployment × Database operations
- Automatic seeding on every container restart
- High RDS CPU/IO usage
- Expensive database costs

### After (Low Cost):
- ✅ No automatic seeding in production
- ✅ One-time manual seeding only
- ✅ Reduced database operations
- ✅ Lower RDS costs

## Verification

Check logs to confirm seeding is skipped:

```bash
# Should see these messages in production:
"Production mode: Skipping EnsureCreated (use migrations)"
"Production mode: Skipping automatic seeding (use manual seeding or migrations)"
```

## Emergency Seeding

If you need to seed data in production:

```bash
# Method 1: Via ECS Exec
aws ecs execute-command --cluster amesa-cluster --task <task-id> --container amesa-backend --interactive --command "dotnet AmesaBackend.dll --seeder"

# Method 2: Via SQL (connect to RDS)
psql -h your-rds-endpoint -U username -d amesa_prod -f seed-script.sql
```

## Next Steps

1. **Deploy Changes**: All services now have production guards
2. **Monitor Costs**: Database costs should decrease significantly  
3. **One-Time Seed**: Run manual seeding once if needed
4. **Implement Migrations**: For future schema changes

---

**Last Updated**: 2025-11-21
**Status**: Production seeding disabled, manual seeding available
