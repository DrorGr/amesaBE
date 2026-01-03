# CORS Fix Summary - amesa-group.net

## Problem Identified

API requests failing with `ERR_NAME_NOT_RESOLVED` and CORS errors because:
1. Backend CORS configuration was using `AddAmesaCors` extension method that wasn't working properly
2. CORS headers not being returned in responses
3. Browser blocking requests due to missing CORS headers

## Root Cause

The `AddAmesaCors` extension method wasn't properly configuring CORS, resulting in:
- No `Access-Control-Allow-Origin` headers
- No `Access-Control-Allow-Credentials` headers
- CORS preflight (OPTIONS) requests not handled correctly

## Fix Applied

### Backend CORS Configuration (Fixed)

**File**: `BE/AmesaBackend.Lottery/Program.cs`

**Changed from:**
```csharp
builder.Services.AddAmesaCors(builder.Configuration);
```

**Changed to:**
```csharp
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
    ?? new[] { "https://dpqbvdgnenckf.cloudfront.net", "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**Allowed Origins** (from `appsettings.json`):
- `https://dpqbvdgnenckf.cloudfront.net`
- `https://amesa-group.net`
- `https://www.amesa-group.net`
- `http://localhost:4200`

## CloudFront Configuration

**Status**: ✅ Correctly configured
- **Origin Request Policy**: `Managed-AllViewer` - Forwards all headers including `Origin`
- **Response Headers Policy**: Configured (may need CORS headers added)
- **Cache Behavior**: `/api/*` routes to backend ALB

## Next Steps

1. ✅ **Backend CORS fixed** - Explicit CORS configuration added
2. ⏳ **Rebuild and redeploy** backend services
3. ⏳ **Test CORS** after deployment
4. ⏳ **Update other services** if they have the same issue

## Testing After Deployment

```bash
# Test CORS preflight
curl -H "Origin: https://amesa-group.net" \
     -H "Access-Control-Request-Method: GET" \
     -X OPTIONS \
     https://amesa-group.net/api/v1/houses \
     -v

# Should return:
# Access-Control-Allow-Origin: https://amesa-group.net
# Access-Control-Allow-Methods: GET, POST, ...
# Access-Control-Allow-Credentials: true

# Test actual request
curl -H "Origin: https://amesa-group.net" \
     -X GET \
     https://amesa-group.net/api/v1/houses \
     -v

# Should return:
# Access-Control-Allow-Origin: https://amesa-group.net
# Access-Control-Allow-Credentials: true
# HTTP/1.1 200 OK
# [JSON data]
```

## Other Services to Check

If other services use `AddAmesaCors`, they may need the same fix:
- `AmesaBackend.Auth`
- `AmesaBackend.Payment`
- `AmesaBackend.Notification`
- `AmesaBackend.Content`
- `AmesaBackend.LotteryResults`
- `AmesaBackend.Analytics`
- `AmesaBackend.Admin`

## Files Changed

1. `BE/AmesaBackend.Lottery/Program.cs` - Fixed CORS configuration
2. `BE/AmesaBackend.Lottery/appsettings.json` - Added new domains to AllowedOrigins
3. `BE/AmesaBackend/appsettings.Production.json` - Added new domains to AllowedOrigins
4. `FE/src/environments/environment.prod.ts` - Updated API URLs

## Deployment

After pushing changes:
1. CI/CD will rebuild backend services
2. Services will be redeployed to ECS
3. CORS should work correctly after deployment
