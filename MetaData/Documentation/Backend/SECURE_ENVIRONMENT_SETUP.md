# 🔐 Secure Environment Configuration for Admin Panel

## Security Fixes Applied

### ✅ **Admin Authentication Service**
- Removed hardcoded credentials from `AdminAuthService.cs`
- Added environment variable support for `ADMIN_EMAIL` and `ADMIN_PASSWORD`
- Added configuration fallback for development

### ✅ **Database Connection Strings**
- Removed hardcoded credentials from `appsettings.Development.json`
- Updated connection string logic to use environment variables first
- Added secure fallbacks for development

### ✅ **Configuration Updates**
- Added `AdminSettings` section to `appsettings.json`
- Updated `Program.cs` to use `DB_CONNECTION_STRING` environment variable
- Updated `AdminDatabaseService.cs` to use environment variables

## Required Environment Variables

### **For Production/Staging Deployment:**

```bash
# Database Connection
DB_CONNECTION_STRING=Host=your-db-host;Database=amesa_lottery;Username=your-username;Password=your-password;Port=5432;

# Admin Panel Credentials
ADMIN_EMAIL=admin@amesa.com
ADMIN_PASSWORD=YourSecurePassword123!

# JWT Settings (if needed)
JWT_SECRET_KEY=your-super-secret-jwt-key-min-32-characters-long
```

### **For GitHub Secrets (CI/CD):**

Add these secrets to your GitHub repository:

```bash
# Development/Staging (Shared Infrastructure)
# Note: Both dev and stage use the same secrets since they share backend and database
DEV_DB_CONNECTION_STRING=Host=amesadbmain-stage.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=postgres;Password=your-stage-password;Port=5432;
DEV_ADMIN_EMAIL=admin@amesa.com
DEV_ADMIN_PASSWORD=DevStageAdminPassword123!

# Production (Separate Infrastructure)
PROD_DB_CONNECTION_STRING=Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=your-prod-password;Port=5432;
PROD_ADMIN_EMAIL=admin@amesa.com
PROD_ADMIN_PASSWORD=ProdAdminPassword123!
```

## Admin Panel Access Strategy

### **Development Environment (Shared with Staging)**
- **URL**: `https://d2rmamd755wq7j.cloudfront.net/admin`
- **Backend**: `amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com`
- **Database**: `amesadbmain-stage` (shared with staging)
- **Access**: Open for development testing
- **Credentials**: From environment variables

### **Staging Environment (Shared with Development)**
- **URL**: `https://d2ejqzjfslo5hs.cloudfront.net/admin`
- **Backend**: `amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com` (same as dev)
- **Database**: `amesadbmain-stage` (shared with development)
- **Access**: Restricted to development team
- **Credentials**: From environment variables
- **Note**: ⚠️ **Shares backend and database with development**

### **Production Environment (Isolated)**
- **URL**: `https://dpqbvdgnenckf.cloudfront.net/admin`
- **Backend**: `amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com` (separate)
- **Database**: `amesadbmain` (separate)
- **Access**: Highly restricted (IP whitelisting recommended)
- **Credentials**: From environment variables
- **Note**: ✅ **Completely isolated from development and staging**

## Security Recommendations

### **Immediate Actions:**
1. ✅ Remove hardcoded credentials from code
2. ✅ Use environment variables for all sensitive data
3. 🔄 Set up GitHub secrets for CI/CD
4. 🔄 Configure AWS Secrets Manager for production

### **Additional Security Measures:**
1. **IP Whitelisting**: Restrict admin panel access by IP address
2. **VPN Access**: Require VPN connection for admin panel access
3. **Two-Factor Authentication**: Add 2FA to admin login
4. **Audit Logging**: Log all admin panel activities
5. **Rate Limiting**: Implement rate limiting for admin endpoints
6. **SSL/TLS**: Ensure HTTPS for all admin panel access

## Deployment Commands

### **Set GitHub Secrets:**
```bash
# Development/Staging (Shared Infrastructure)
# Note: Both dev and stage use the same secrets
gh secret set DEV_DB_CONNECTION_STRING --repo DrorGr/amesaBE --body "Host=amesadbmain-stage.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=postgres;Password=YOUR_STAGE_PASSWORD;Port=5432;"
gh secret set DEV_ADMIN_EMAIL --repo DrorGr/amesaBE --body "admin@amesa.com"
gh secret set DEV_ADMIN_PASSWORD --repo DrorGr/amesaBE --body "YOUR_DEV_STAGE_PASSWORD"

# Production (Separate Infrastructure)
gh secret set PROD_DB_CONNECTION_STRING --repo DrorGr/amesaBE --body "Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=YOUR_PROD_PASSWORD;Port=5432;"
gh secret set PROD_ADMIN_EMAIL --repo DrorGr/amesaBE --body "admin@amesa.com"
gh secret set PROD_ADMIN_PASSWORD --repo DrorGr/amesaBE --body "YOUR_PROD_PASSWORD"
```

### **Update GitHub Actions Workflow:**
The workflow should pass environment variables to the Docker container:

```yaml
- name: Deploy to ECS
  run: |
    aws ecs update-service \
      --cluster ${{ secrets.ECS_CLUSTER }} \
      --service ${{ secrets.ECS_SERVICE }} \
      --task-definition $TASK_DEFINITION_ARN \
      --environment-variables \
        DB_CONNECTION_STRING=${{ secrets.DB_CONNECTION_STRING }}, \
        ADMIN_EMAIL=${{ secrets.ADMIN_EMAIL }}, \
        ADMIN_PASSWORD=${{ secrets.ADMIN_PASSWORD }}
```

## Testing the Security Fixes

### **Local Testing:**
```bash
# Set environment variables
export DB_CONNECTION_STRING="Data Source=amesa.db"
export ADMIN_EMAIL="admin@amesa.com"
export ADMIN_PASSWORD="TestPassword123!"

# Run the application
dotnet run --project AmesaBackend
```

### **Verify Admin Panel Access:**
1. Navigate to `http://localhost:8080/admin/login`
2. Login with environment variable credentials
3. Verify database switching works
4. Test all admin panel functionality

## Next Steps

1. **Set GitHub Secrets** with actual production values
2. **Update GitHub Actions** workflow to use environment variables
3. **Test Deployment** to staging environment
4. **Implement Additional Security** measures (IP whitelisting, 2FA)
5. **Monitor Admin Panel** access and activities

---

**Status**: ✅ **Security fixes applied and ready for deployment**
**Last Updated**: 2025-10-11
**Next Action**: Set GitHub secrets and test deployment
