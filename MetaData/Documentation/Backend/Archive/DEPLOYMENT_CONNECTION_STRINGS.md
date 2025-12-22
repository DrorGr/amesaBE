# 🔐 Connection Strings Configuration

**Status**: ✅ Updated in all services

---

## ✅ Connection Strings Updated

All 8 services have been updated with:

### RDS Aurora Connection
- **Endpoint**: `amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com:5432`
- **Database**: `postgres`
- **Username**: `dror`
- **Password**: ⚠️ **CHANGE_ME** (needs to be updated with actual password)
- **SearchPath**: Service-specific schema (e.g., `amesa_auth`, `amesa_payment`, etc.)

### Redis Connection
- **Endpoint**: `amesa-redis.3ogg7e.0001.eun1.cache.amazonaws.com:6379`
- **Status**: ✅ Configured in all services

---

## ⚠️ IMPORTANT: Update Passwords

**Before deploying**, update the database password in all `appsettings.json` files:

1. **Get the actual Aurora password** from AWS Secrets Manager or your secure storage
2. **Replace `CHANGE_ME`** in all connection strings:
   - `BE/AmesaBackend.Auth/appsettings.json`
   - `BE/AmesaBackend.Payment/appsettings.json`
   - `BE/AmesaBackend.Lottery/appsettings.json`
   - `BE/AmesaBackend.Content/appsettings.json`
   - `BE/AmesaBackend.Notification/appsettings.json`
   - `BE/AmesaBackend.LotteryResults/appsettings.json`
   - `BE/AmesaBackend.Analytics/appsettings.json`

**Or use environment variables** in ECS task definitions to inject secrets securely.

---

## Service-Specific Schemas

Each service uses its own schema:

| Service | Schema |
|---------|--------|
| Auth | `amesa_auth` |
| Payment | `amesa_payment` |
| Lottery | `amesa_lottery` |
| Content | `amesa_content` |
| Notification | `amesa_notification` |
| Lottery Results | `amesa_lottery_results` |
| Analytics | `amesa_analytics` |

---

## Connection String Format

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=postgres;Username=dror;Password=<ACTUAL_PASSWORD>;SearchPath=amesa_auth;",
    "Redis": "amesa-redis.3ogg7e.0001.eun1.cache.amazonaws.com:6379"
  }
}
```

---

**Status**: Connection strings configured. **Update passwords before deployment!**

