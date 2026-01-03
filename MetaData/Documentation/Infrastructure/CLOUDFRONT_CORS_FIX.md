# CloudFront CORS Fix - Origin Request Policy

## Problem

API requests are failing with CORS errors because:
1. CloudFront Origin Request Policy is not forwarding the `Origin` header
2. Backend cannot return proper CORS headers without the Origin header
3. Browser blocks the response due to missing CORS headers

## Root Cause

The CloudFront origin request policy `216adef6-5c7f-47e4-b989-5492eafa07d3` has:
- `HeadersConfig: null` - Not forwarding any headers
- This means the `Origin` header is not being forwarded to the backend
- Backend cannot determine which origin to allow, so no CORS headers are returned

## Solution

### Option 1: Update CloudFront to Use CORS-S3Origin Policy (Recommended)

Use AWS managed policy that forwards necessary headers for CORS:

```bash
# Get the CORS-S3Origin policy ID
aws cloudfront list-origin-request-policies --region us-east-1 \
  --query "OriginRequestPolicyList.Items[?Name=='CORS-S3Origin'].Id" \
  --output text
```

Then update the CloudFront distribution cache behavior to use this policy.

### Option 2: Create Custom Origin Request Policy

Create a policy that forwards the Origin header:

```bash
aws cloudfront create-origin-request-policy \
  --origin-request-policy-config '{
    "Name": "Amesa-CORS-Policy",
    "Comment": "Forward Origin header for CORS",
    "HeadersConfig": {
      "HeaderBehavior": "whitelist",
      "Headers": {
        "Quantity": 1,
        "Items": ["Origin"]
      }
    },
    "QueryStringsConfig": {
      "QueryStringBehavior": "all"
    },
    "CookiesConfig": {
      "CookieBehavior": "none"
    }
  }' \
  --region us-east-1
```

### Option 3: Update Existing Policy (If Possible)

If the policy allows updates, modify it to forward the Origin header.

## Current CloudFront Configuration

**Distribution ID**: `E3GU3QXUR43ZOH`
**Cache Behavior**: `/api/*`
**Origin Request Policy ID**: `216adef6-5c7f-47e4-b989-5492eafa07d3`
**Issue**: Policy doesn't forward Origin header

## Immediate Fix

Update the `/api/*` cache behavior to use a policy that forwards the Origin header:

1. **Get CORS-S3Origin policy** (AWS managed):
   ```bash
   aws cloudfront list-origin-request-policies --region us-east-1 \
     --query "OriginRequestPolicyList.Items[?Name=='CORS-S3Origin']"
   ```

2. **Update CloudFront distribution** to use the CORS policy for `/api/*` cache behavior

3. **Invalidate CloudFront cache** after update

## Verification

After updating:

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
```

## Backend CORS Configuration

The backend CORS is now explicitly configured in `Program.cs`:

```csharp
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

## Next Steps

1. ✅ Backend CORS configuration updated
2. ⏳ Update CloudFront origin request policy
3. ⏳ Invalidate CloudFront cache
4. ⏳ Test CORS from browser
