# Current Work Status - AmesaBackend

## 🚀 Google OAuth Integration & Secrets Hardening (2025-11-08)

The recent focus has been on enabling Google sign-in across dev/stage environments while keeping credentials secure and preventing unintended local database seeding.

---

## ✅ Completed Items

### 🔐 Google OAuth Flow
- Added dedicated `External` authentication cookie (SameSite=None, Secure) to persist OAuth state during CloudFront/ECS round-trips.
- Confirmed `CloudFront-Forwarded-Proto` header handling so Google receives HTTPS redirect URIs.
- OAuth client credentials are now pulled from AWS Secrets Manager (`amesa-google_people_API`). Logs confirm the secret is loaded at startup.

### 🗂️ Secrets & Configuration
- Documented that staging/production tasks rely on the AWS secret; no credentials should live in `appsettings.*`.
- Added startup logging to verify which Google client ID is present (length only, no sensitive data).

### 🧪 Local Seeding Guard
- Wrapped SQLite seeding logic in `#if RUN_DATABASE_SEED` so hosted builds do not run seeders.
- Added documentation and command examples for enabling the seeder via `dotnet run -c Debug /p:DefineConstants="RUN_DATABASE_SEED"`.

---

## 🧭 Recommended Workflow

### Running the API Locally
```bash
cd BE/AmesaBackend
dotnet run --project AmesaBackend
```

### Running with SQLite Seeding
```bash
dotnet run --project AmesaBackend -c Debug /p:DefineConstants="RUN_DATABASE_SEED"
```

### Verifying Google OAuth
1. Ensure the AWS secret `amesa-google_people_API` has the latest ClientId/ClientSecret pair.
2. Restart the staging ECS task.
3. Check CloudWatch logs for `Google OAuth client ID loaded` and attempt a Google login.

---

## 🔭 Next Considerations
- Finish Facebook/Apple OAuth once Google flow is stable.
- Rotate Google client secrets regularly and document rotation steps.
- Expand automated tests around OAuth controllers once live tokens are available.
- Evaluate enabling seeding through a CLI switch instead of compile symbols in the future.

---

## 📌 Quick Reference
- Secrets live in AWS Secrets Manager (`amesa-google_people_API`).
- OAuth state lives in the `External` cookie configured in `Program.cs`.
- Seeding requires `RUN_DATABASE_SEED` build symbol; it no longer runs by default.
- Context docs (`.cursorrules`, `CONTEXT_QUICK_REFERENCE.md`, `CURRENT_STATUS_SUMMARY.md`) updated with latest instructions.