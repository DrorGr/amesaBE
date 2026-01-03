# Update CORS and Frontend URLs for amesa-group.net

## Problem
The API is returning CORS errors because:
1. Backend CORS configuration doesn't include the new custom domain
2. Frontend is still using the old CloudFront URL

## Solution

### Step 1: Update Backend CORS Configuration

Add the new domain to `AllowedOrigins` in all backend service `appsettings.json` files:

**Files to update:**
- `BE/AmesaBackend.Lottery/appsettings.json`
- `BE/AmesaBackend.Auth/appsettings.json`
- `BE/AmesaBackend.Payment/appsettings.json`
- `BE/AmesaBackend.Notification/appsettings.json`
- `BE/AmesaBackend.Content/appsettings.json`
- `BE/AmesaBackend.LotteryResults/appsettings.json`
- `BE/AmesaBackend.Analytics/appsettings.json`
- `BE/AmesaBackend.Admin/appsettings.json`

**Update pattern:**
```json
"AllowedOrigins": [
  "https://dpqbvdgnenckf.cloudfront.net",
  "https://amesa-group.net",
  "https://www.amesa-group.net",
  "http://localhost:4200"
]
```

### Step 2: Update Frontend Environment Files

Update `FE/src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://amesa-group.net/api/v1',
  backendUrl: 'https://amesa-group.net/api/v1',
  frontendUrl: 'https://amesa-group.net',
  logLevel: 'error',
  recaptchaSiteKey: '' // Set your Google reCAPTCHA v3 site key here
};
```

**OR** if you want to keep both URLs for backward compatibility:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://amesa-group.net/api/v1', // Primary
  backendUrl: 'https://amesa-group.net/api/v1',
  frontendUrl: 'https://amesa-group.net',
  // Fallback to CloudFront if needed
  fallbackApiUrl: 'https://dpqbvdgnenckf.cloudfront.net/api/v1',
  logLevel: 'error',
  recaptchaSiteKey: ''
};
```

### Step 3: Update Production Environment Variables (ECS)

If CORS origins are configured via environment variables in ECS task definitions, update them:

**Environment variables to add/update:**
- `Cors__AllowedOrigins__0=https://dpqbvdgnenckf.cloudfront.net`
- `Cors__AllowedOrigins__1=https://amesa-group.net`
- `Cors__AllowedOrigins__2=https://www.amesa-group.net`

**OR** if using JSON format:
- `AllowedOrigins=["https://dpqbvdgnenckf.cloudfront.net","https://amesa-group.net","https://www.amesa-group.net"]`

## Quick Fix Commands

### Update All Backend appsettings.json Files

```powershell
# Update Lottery service
$lotteryConfig = Get-Content "BE/AmesaBackend.Lottery/appsettings.json" | ConvertFrom-Json
$lotteryConfig.AllowedOrigins += "https://amesa-group.net"
$lotteryConfig.AllowedOrigins += "https://www.amesa-group.net"
$lotteryConfig | ConvertTo-Json -Depth 10 | Set-Content "BE/AmesaBackend.Lottery/appsettings.json"

# Repeat for other services...
```

### Update Frontend Environment

```powershell
# Update production environment
$envContent = Get-Content "FE/src/environments/environment.prod.ts" -Raw
$envContent = $envContent -replace "https://dpqbvdgnenckf\.cloudfront\.net", "https://amesa-group.net"
$envContent | Set-Content "FE/src/environments/environment.prod.ts"
```

## Verification

After updating:

1. **Rebuild and redeploy** backend services
2. **Rebuild and redeploy** frontend
3. **Test API calls** from `https://amesa-group.net`
4. **Check browser console** for CORS errors
5. **Verify** API responses include CORS headers:
   - `Access-Control-Allow-Origin: https://amesa-group.net`
   - `Access-Control-Allow-Credentials: true`

## Testing

```bash
# Test CORS headers
curl -H "Origin: https://amesa-group.net" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS \
     https://amesa-group.net/api/v1/houses \
     -v

# Should return:
# Access-Control-Allow-Origin: https://amesa-group.net
# Access-Control-Allow-Credentials: true
```

## Important Notes

1. **Keep old CloudFront URL** in AllowedOrigins during transition
2. **Update all services** - CORS must be configured in each microservice
3. **Environment variables** - If using ECS, update task definitions
4. **Frontend rebuild** - Frontend changes require rebuild and redeploy
5. **Test thoroughly** - Verify all API endpoints work with new domain
