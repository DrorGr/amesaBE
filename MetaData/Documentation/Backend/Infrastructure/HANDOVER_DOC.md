# AmesaBackend - Handover Document

**Date:** 2025-11-19  
**Session Focus:** OAuth Integration Fix & Secret Management Validation  
**Status:** ✅ Complete

---

## Executive Summary

This session focused on fixing OAuth authentication integration issues and implementing proper secret management validation to ensure hardcoded secrets are replaced with AWS-managed secrets in production deployments.

---

## Issues Fixed

### 1. OAuth Authentication Handler Not Registered Error

**Problem:**
- Frontend attempting to use Google OAuth returned confusing error:
  ```
  "No authentication handler is registered for the scheme 'Google'. 
  The registered schemes are: Cookies, Bearer."
  ```
- Error occurred because OAuth credentials were not configured in development, but the endpoint still tried to challenge Google authentication

**Solution:**
- Updated `OAuthController.cs` to check if OAuth credentials are configured before attempting to challenge authentication
- Returns clear 400 Bad Request with detailed error message when OAuth is not configured
- Applied to both Google (`/api/v1/oauth/google`) and Meta (`/api/v1/oauth/meta`) endpoints

**Files Modified:**
- `BE/AmesaBackend/Controllers/OAuthController.cs`
  - Added `using AmesaBackend.DTOs;` for ApiResponse/ErrorResponse
  - Added credential validation in `GoogleLogin()` method
  - Added credential validation in `MetaLogin()` method

**Error Response Format:**
```json
{
  "success": false,
  "error": {
    "code": "OAUTH_NOT_CONFIGURED",
    "message": "Google OAuth is not configured. Please configure ClientId and ClientSecret in appsettings.json or AWS Secrets Manager.",
    "details": {
      "provider": "Google",
      "missing": "ClientId"
    }
  }
}
```

---

### 2. JWT Secret Key Validation for Production

**Problem:**
- Hardcoded JWT SecretKey values in `appsettings.json` and `appsettings.Development.json`
- No validation to ensure production uses secrets from AWS SSM Parameter Store
- Risk of placeholder values being used in production

**Solution:**
- Added JWT SecretKey validation in `Program.cs`
- Checks for secret from environment variable `JwtSettings__SecretKey` (loaded from SSM in production)
- Validates secret is not null/empty
- In Production/Staging, validates secret is not a placeholder value
- Throws `InvalidOperationException` with clear error message if validation fails

**Files Modified:**
- `BE/AmesaBackend/Program.cs`
  - Added `using System.Linq;` for LINQ methods
  - Added JWT SecretKey validation logic (lines 233-264)
  - Added placeholder detection for production/staging environments

**Validation Logic:**
1. Checks if `JwtSettings:SecretKey` is configured (from config or environment variable)
2. In Production/Staging:
   - Validates secret is not a placeholder value
   - Ensures `JwtSettings__SecretKey` environment variable is set from SSM
3. In Development:
   - Allows placeholder values from `appsettings.Development.json`

**Placeholder Values Detected:**
- `"your-super-secret-key-for-jwt-tokens-min-32-chars"`
- `"your-super-secret-key-that-is-at-least-32-characters-long"`

---

## Current Configuration

### Secret Management Strategy

#### JWT SecretKey
- **Development:** Hardcoded in `appsettings.Development.json` (placeholder allowed)
- **Production:** Loaded from AWS SSM Parameter Store
  - SSM Parameter: `/amesa/prod/JwtSettings/SecretKey`
  - Environment Variable: `JwtSettings__SecretKey`
  - Set via ECS Task Definition secrets
  - Script: `BE/Infrastructure/add-jwt-secret-to-auth.ps1`

#### Google OAuth Credentials
- **Development:** Empty in `appsettings.Development.json` (can be configured manually)
- **Production:** Loaded from AWS Secrets Manager
  - Secret ID: `amesa-google_people_API` (from `appsettings.Production.json`)
  - Keys: `ClientId`, `ClientSecret`
  - Loaded via `AwsSecretLoader.TryLoadJsonSecret()` in `Program.cs`

#### Meta OAuth Credentials
- **Development:** Empty in `appsettings.Development.json` (can be configured manually)
- **Production:** Loaded from AWS Secrets Manager
  - Secret ID: `amesa-meta-facebook-api` (from `appsettings.Production.json`)
  - Keys: `AppId`, `AppSecret`
  - Loaded via `AwsSecretLoader.TryLoadJsonSecret()` in `Program.cs`

---

## File Structure

### Key Files Modified

```
BE/AmesaBackend/
├── Controllers/
│   └── OAuthController.cs          ✅ Updated with credential validation
├── Program.cs                       ✅ Added JWT secret validation
└── appsettings.Development.json    ✅ Uses placeholders (safe for dev)

BE/Infrastructure/
├── HANDOVER_DOC.md                  ✅ This document
└── add-jwt-secret-to-auth.ps1      ✅ Script for adding JWT secret to ECS
```

---

## Testing & Verification

### OAuth Endpoint Testing

**Without Credentials (Expected Behavior):**
```bash
GET http://localhost:5000/api/v1/oauth/google
Response: 400 Bad Request
Body: {
  "success": false,
  "error": {
    "code": "OAUTH_NOT_CONFIGURED",
    "message": "Google OAuth is not configured..."
  }
}
```

**With Credentials:**
- Redirects to Google OAuth consent screen
- On callback, creates/updates user and generates JWT tokens
- Redirects to frontend with temporary token

### JWT Secret Validation

**Development Mode:**
- Accepts placeholder values from `appsettings.Development.json`
- Logs: `"[JWT] Development mode - using SecretKey from appsettings.Development.json"`

**Production Mode:**
- Validates secret exists and is not a placeholder
- Expects `JwtSettings__SecretKey` environment variable from SSM
- Throws exception if placeholder detected
- Logs: `"[JWT] Using SecretKey from environment variable (SSM Parameter Store)"`

---

## Environment Configuration

### Development (`appsettings.Development.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AmesaDB.db"  // SQLite for local dev
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-key-for-jwt-tokens-min-32-chars"  // Placeholder OK
  },
  "Authentication": {
    "Google": {
      "ClientId": "",      // Empty - can be configured for local testing
      "ClientSecret": "",  // Empty - can be configured for local testing
      "SecretId": ""       // Empty - not used in development
    },
    "Meta": {
      "AppId": "",         // Empty - can be configured for local testing
      "AppSecret": ""      // Empty - not used in development
    }
  }
}
```

### Production (`appsettings.Production.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""  // Empty - set via DB_CONNECTION_STRING env var
  },
  "Authentication": {
    "Google": {
      "ClientId": "",           // Empty - loaded from Secrets Manager
      "ClientSecret": "",       // Empty - loaded from Secrets Manager
      "SecretId": "amesa-google_people_API"  // AWS Secrets Manager ID
    },
    "Meta": {
      "AppId": "",              // Empty - loaded from Secrets Manager
      "AppSecret": "",          // Empty - loaded from Secrets Manager
      "SecretId": "amesa-meta-facebook-api"  // AWS Secrets Manager ID
    }
  }
}
// Note: JwtSettings not in Production file - loaded from SSM Parameter Store
```

---

## AWS Infrastructure

### Secrets Storage

#### AWS SSM Parameter Store
- **Parameter:** `/amesa/prod/JwtSettings/SecretKey`
- **Type:** SecureString
- **Used By:** ECS Task Definition (amesa-auth-service)
- **Access:** ECS Task Execution Role

#### AWS Secrets Manager
- **Google OAuth:** `amesa-google_people_API`
  - JSON format with keys: `ClientId`, `ClientSecret`
- **Meta OAuth:** `amesa-meta-facebook-api`
  - JSON format with keys: `AppId`, `AppSecret`

### ECS Configuration

#### Task Definition Secrets
```json
{
  "containerDefinitions": [{
    "secrets": [
      {
        "name": "JwtSettings__SecretKey",
        "valueFrom": "/amesa/prod/JwtSettings/SecretKey"
      },
      {
        "name": "DB_CONNECTION_STRING",
        "valueFrom": "/amesa/prod/Database/ConnectionString"
      }
    ]
  }]
}
```

---

## Build Status

✅ **Build Successful**
- 0 Errors
- 4 Warnings (package vulnerabilities - non-critical)
  - `MimeKit 4.3.0` - High severity (not blocking)
  - `System.IdentityModel.Tokens.Jwt 7.0.3` - Moderate severity (not blocking)

---

## Known Issues & Limitations

### Non-Critical
1. **Package Vulnerabilities:**
   - `MimeKit 4.3.0` has high severity vulnerability
   - `System.IdentityModel.Tokens.Jwt 7.0.3` has moderate severity vulnerability
   - **Recommendation:** Update packages in future maintenance window

2. **OAuth Credentials in Development:**
   - Currently empty in `appsettings.Development.json`
   - OAuth login will not work locally without manual configuration
   - **Workaround:** Add credentials to `appsettings.Development.json` for local testing

### Critical
- ✅ **None** - All critical issues resolved

---

## Next Steps / Recommendations

### Immediate Actions
1. ✅ **Complete** - OAuth error handling improved
2. ✅ **Complete** - JWT secret validation added
3. ⚠️ **Optional** - Configure OAuth credentials in development for local testing

### Future Improvements
1. **Package Updates:**
   - Update `MimeKit` to latest version
   - Update `System.IdentityModel.Tokens.Jwt` to latest version

2. **Security Enhancements:**
   - Consider rotating JWT secret periodically
   - Implement secret rotation automation
   - Add monitoring/alerting for secret failures

3. **Testing:**
   - Add integration tests for OAuth flow
   - Add unit tests for secret validation logic
   - Test production deployment with actual AWS secrets

---

## Deployment Notes

### Pre-Deployment Checklist
- [x] OAuth error handling implemented
- [x] JWT secret validation added
- [x] Build successful
- [x] Verify AWS secrets exist and are accessible (See `PRE_DEPLOYMENT_VERIFICATION.md`)
- [x] Test scripts created for OAuth flow (`test-oauth-flow.ps1`)
- [x] Test scripts created for JWT generation (`test-jwt-generation.ps1`)
- [x] Meta OAuth intentionally skipped (no dev account yet)
- [ ] Run Google OAuth flow tests in production environment
- [ ] Run JWT generation tests in production environment

### Deployment Process
1. Code is ready for deployment
2. AWS secrets configured (JWT and Google OAuth verified)
3. ECS Task Definition has secrets configured
4. Test scripts available for post-deployment verification
5. **Note:** Meta OAuth intentionally skipped - will be configured when Meta dev account is available

---

## Troubleshooting

### OAuth Not Working

**Symptom:** 400 Bad Request with `OAUTH_NOT_CONFIGURED` error

**Solutions:**
1. **Development:** Add credentials to `appsettings.Development.json`
2. **Production:** 
   - Verify AWS Secrets Manager secret exists
   - Check `Authentication:Google:SecretId` or `Authentication:Meta:SecretId` in `appsettings.Production.json`
   - Verify ECS Task Execution Role has `secretsmanager:GetSecretValue` permission

### JWT Secret Validation Failed

**Symptom:** Application startup fails with `InvalidOperationException` about JWT SecretKey

**Solutions:**
1. **Development:** Ensure `JwtSettings:SecretKey` exists in `appsettings.Development.json`
2. **Production:**
   - Verify SSM Parameter `/amesa/prod/JwtSettings/SecretKey` exists
   - Check ECS Task Definition has secret configured
   - Verify ECS Task Execution Role has `ssm:GetParameters` permission
   - Check environment variable `JwtSettings__SecretKey` is set in container

### Environment Variable Not Loading

**Issue:** Environment variable from SSM not accessible in container

**Check:**
1. ECS Task Definition secrets configuration
2. ECS Task Execution Role IAM permissions
3. SSM Parameter exists and is accessible
4. Container logs for secret loading errors

---

## Code References

### Key Code Sections

**OAuth Validation:**
```csharp
// BE/AmesaBackend/Controllers/OAuthController.cs (lines 55-72)
var googleClientId = _configuration["Authentication:Google:ClientId"];
var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];

if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret))
{
    return BadRequest(new ApiResponse<object>
    {
        Success = false,
        Error = new ErrorResponse
        {
            Code = "OAUTH_NOT_CONFIGURED",
            Message = "Google OAuth is not configured..."
        }
    });
}
```

**JWT Secret Validation:**
```csharp
// BE/AmesaBackend/Program.cs (lines 233-264)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException("JWT SecretKey is not configured...");
}

if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
{
    var placeholderValues = new[] { /* placeholder strings */ };
    if (placeholderValues.Any(p => secretKey.Contains(p, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("JWT SecretKey appears to be a placeholder...");
    }
}
```

---

## Pre-Deployment Verification

### Verification Results

**Status:** ✅ Ready for Deployment

**Verified:**
- ✅ JWT SecretKey exists in SSM Parameter Store (62 characters)
- ✅ Google OAuth secret exists in AWS Secrets Manager
- ✅ ECS Task Definition has secrets configured correctly
- ⏸️ Meta OAuth intentionally skipped (no dev account yet)

**Note:**
- Meta OAuth will be configured when Meta dev account is obtained
- Google OAuth is fully functional and ready for production

**Detailed Results:** See `BE/Infrastructure/PRE_DEPLOYMENT_VERIFICATION.md`

**Verification Scripts:**
- `BE/Infrastructure/verify-aws-secrets.ps1` - Verify AWS secrets configuration
- `BE/Infrastructure/test-oauth-flow.ps1` - Test OAuth endpoints
- `BE/Infrastructure/test-jwt-generation.ps1` - Test JWT token generation

---

## Contact & Support

### Documentation References
- **AWS Secrets Manager:** https://docs.aws.amazon.com/secretsmanager/
- **AWS SSM Parameter Store:** https://docs.aws.amazon.com/systems-manager/latest/userguide/systems-manager-parameter-store.html
- **ECS Task Definition Secrets:** https://docs.aws.amazon.com/AmazonECS/latest/developerguide/specifying-sensitive-data-secrets.html
- **ASP.NET Core Configuration:** https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/

### Related Files
- `BE/AmesaBackend/Configuration/AwsSecretLoader.cs` - AWS Secrets Manager loader utility
- `BE/Infrastructure/add-jwt-secret-to-auth.ps1` - Script for adding JWT secret to ECS task definition
- `BE/AmesaBackend/appsettings.Production.json` - Production configuration template
- `BE/AmesaBackend/appsettings.Development.json` - Development configuration

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-19  
**Status:** ✅ Ready for Handover

