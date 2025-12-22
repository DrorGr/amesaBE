# 🚀 Staging Deployment Checklist

## Pre-Deployment Steps

### ✅ Step 1: Verify GitHub Secrets are Set

Before deploying, we need to set the following GitHub secrets for the staging environment:

```bash
# Development/Staging secrets (shared infrastructure)
DEV_DB_CONNECTION_STRING
DEV_ADMIN_EMAIL  
DEV_ADMIN_PASSWORD
```

**To set these secrets:**

1. Go to: https://github.com/DrorGr/amesaBE/settings/secrets/actions

2. Add the following secrets:

```bash
# DEV_DB_CONNECTION_STRING
Host=amesadbmain-stage.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=postgres;Password=YOUR_ACTUAL_PASSWORD;Port=5432;

# DEV_ADMIN_EMAIL
admin@amesa.com

# DEV_ADMIN_PASSWORD
YOUR_SECURE_PASSWORD_HERE
```

### ✅ Step 2: Review Changes

**Security Fixes:**
- ✅ Removed hardcoded credentials from AdminAuthService.cs
- ✅ Removed hardcoded credentials from Program.cs
- ✅ Removed hardcoded credentials from ProgramSeeder.cs
- ✅ Removed hardcoded credentials from appsettings.Development.json
- ✅ Deleted insecure PowerShell scripts
- ✅ Secured remaining PowerShell scripts

**Admin Panel Updates:**
- ✅ Removed environment switching functionality
- ✅ Added environment display component
- ✅ Simplified database service
- ✅ Auto-detection of environment

**Files to be committed:**
```
Modified:
- AmesaBackend/AmesaBackend.csproj
- AmesaBackend/Program.cs
- AmesaBackend/ProgramSeeder.cs
- AmesaBackend/appsettings.json
- seed-database-simple.ps1
- seed-database.ps1

Deleted:
- create-and-seed-db.ps1
- create-db-simple.ps1
- direct-seed.ps1
- quick-seed.ps1

New:
- AmesaBackend/Admin/ (entire admin panel)
- AmesaBackend/Services/AdminAuthService.cs
- AmesaBackend/Services/AdminDatabaseService.cs
- AmesaBackend/Services/IAdminAuthService.cs
- AmesaBackend/Services/IAdminDatabaseService.cs
- AmesaBackend/appsettings.Development.json
- Multiple documentation files
```

### ✅ Step 3: Commit Changes

```bash
cd BE

# Stage all changes
git add -A

# Commit with descriptive message
git commit -m "feat: Secure admin panel with environment-based configuration

Security improvements:
- Remove all hardcoded credentials
- Add environment variable support for admin auth
- Add environment variable support for database connections
- Secure all PowerShell scripts
- Delete insecure scripts with hardcoded credentials

Admin panel enhancements:
- Remove environment switching (dev/stage share infrastructure)
- Add clear environment display
- Simplify service layer
- Auto-detect environment from deployment

Documentation:
- Add comprehensive security documentation
- Add deployment guides
- Add AWS infrastructure analysis
- Add secure environment setup guide"
```

### ✅ Step 4: Push to Dev Branch First (Testing)

```bash
# Make sure we're on main branch
git checkout main

# Create/checkout dev branch
git checkout dev

# Merge changes from main
git merge main

# Push to dev branch (triggers auto-deploy to dev environment)
git push origin dev
```

### ✅ Step 5: Monitor Dev Deployment

1. Check GitHub Actions: https://github.com/DrorGr/amesaBE/actions
2. Wait for deployment to complete
3. Check ECS service: `amesa-backend-stage-service`

### ✅ Step 6: Test in Dev Environment

**Test Admin Panel Access:**
```
URL: https://d2rmamd755wq7j.cloudfront.net/admin
```

**Test Checklist:**
- [ ] Admin panel loads successfully
- [ ] Environment badge shows "Development / Staging"
- [ ] Database info shows "amesadbmain-stage"
- [ ] Backend info shows "amesa-backend-stage-service"
- [ ] Login works with credentials from GitHub secrets
- [ ] Can access dashboard
- [ ] Can view users
- [ ] Can view content
- [ ] All admin features work

### ✅ Step 7: Push to Stage Branch

```bash
# Checkout stage branch
git checkout stage

# Merge from dev (or main)
git merge dev

# Push to stage branch (triggers auto-deploy to stage environment)
git push origin stage
```

### ✅ Step 8: Monitor Stage Deployment

1. Check GitHub Actions: https://github.com/DrorGr/amesaBE/actions
2. Wait for deployment to complete
3. Verify ECS service updated

### ✅ Step 9: Test in Stage Environment

**Test Admin Panel Access:**
```
URL: https://d2ejqzjfslo5hs.cloudfront.net/admin
```

**Test Checklist:**
- [ ] Admin panel loads successfully
- [ ] Environment badge shows "Development / Staging"
- [ ] Database info shows "amesadbmain-stage" (same as dev)
- [ ] Backend info shows "amesa-backend-stage-service" (same as dev)
- [ ] Login works with credentials from GitHub secrets
- [ ] All functionality works same as dev
- [ ] No errors in browser console
- [ ] No errors in CloudWatch logs

### ✅ Step 10: Verify Backend Health

```bash
# Check dev backend
curl https://d2rmamd755wq7j.cloudfront.net/health

# Check stage backend  
curl https://d2ejqzjfslo5hs.cloudfront.net/health

# Should both return: {"status":"healthy"}
```

## Post-Deployment Verification

### Check ECS Service
```bash
aws ecs describe-services \
  --cluster Amesa \
  --services amesa-backend-stage-service \
  --region eu-north-1
```

### Check CloudWatch Logs
1. Go to AWS Console → CloudWatch → Log Groups
2. Find: `/ecs/amesa-backend-staging`
3. Check for any errors or warnings

### Check Admin Panel Logs
Look for authentication attempts and environment detection:
- `[AdminAuthService] AuthenticateAsync called`
- `[AdminAuthService] Comparing email`
- `[AdminAuthService] Credentials match`

## Rollback Plan (if needed)

If deployment fails or issues are found:

```bash
# Revert to previous commit
git revert HEAD

# Push revert
git push origin stage

# Or force push previous working commit
git reset --hard <previous-commit-hash>
git push --force origin stage
```

## Success Criteria

- ✅ Deployment completes without errors
- ✅ Admin panel accessible at both dev and stage URLs
- ✅ Environment correctly detected and displayed
- ✅ Authentication works with environment variables
- ✅ No hardcoded credentials in deployed code
- ✅ All admin features functional
- ✅ No errors in logs
- ✅ Health checks passing

## Notes

- **Dev and Stage share infrastructure**: Both URLs connect to the same backend service and database
- **GitHub Secrets**: DEV_* secrets are used for both dev and stage branches
- **Environment Variable**: `ASPNETCORE_ENVIRONMENT` should be set to "Development" for staging
- **Database**: Both connect to `amesadbmain-stage` cluster

---

**Ready to Deploy?** Follow steps 1-10 above! 🚀
