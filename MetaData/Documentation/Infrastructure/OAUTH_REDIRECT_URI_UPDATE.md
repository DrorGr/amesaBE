# OAuth Redirect URI Update Guide for amesa-group.net

After setting up the custom domain `amesa-group.net`, you **MUST** update the OAuth redirect URIs in both Google Console and Meta Developer Console.

## Current OAuth Configuration

### Current Redirect URIs (CloudFront Domain)
- **Google OAuth Callback**: `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/google-callback`
- **Meta OAuth Callback**: `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/meta-callback`

### New Redirect URIs (Custom Domain)
- **Google OAuth Callback**: `https://amesa-group.net/api/v1/oauth/google-callback`
- **Meta OAuth Callback**: `https://amesa-group.net/api/v1/oauth/meta-callback`

**OR** if using www subdomain:
- **Google OAuth Callback**: `https://www.amesa-group.net/api/v1/oauth/google-callback`
- **Meta OAuth Callback**: `https://www.amesa-group.net/api/v1/oauth/meta-callback`

## Step 1: Update Google OAuth Console

### 1.1 Access Google Cloud Console

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Select your project (or create one if needed)
3. Navigate to **APIs & Services** → **Credentials**
4. Find your **OAuth 2.0 Client ID** (the one used for amesa-group.net)

### 1.2 Update Authorized Redirect URIs

1. Click on your OAuth 2.0 Client ID
2. Scroll to **Authorized redirect URIs**
3. **Add** the new redirect URI:
   - `https://amesa-group.net/api/v1/oauth/google-callback`
   - OR `https://www.amesa-group.net/api/v1/oauth/google-callback` (if using www)
4. **Keep the old URI** (for backward compatibility during transition):
   - `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/google-callback`
5. Click **Save**

### 1.3 Verify Configuration

- **Authorized JavaScript origins** should include:
  - `https://amesa-group.net`
  - `https://www.amesa-group.net` (if using www)
  - `https://dpqbvdgnenckf.cloudfront.net` (keep for backward compatibility)

### 1.4 Test Google OAuth

After updating:
1. Test Google login from your application
2. Verify redirect works correctly
3. Once confirmed working, you can remove the old CloudFront URI (optional)

## Step 2: Update Meta/Facebook Developer Console

### 2.1 Access Meta Developer Console

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. Select your app (or create one if needed)
3. Navigate to **Settings** → **Basic**

### 2.2 Update Valid OAuth Redirect URIs

1. Scroll to **Valid OAuth Redirect URIs**
2. **Add** the new redirect URI:
   - `https://amesa-group.net/api/v1/oauth/meta-callback`
   - OR `https://www.amesa-group.net/api/v1/oauth/meta-callback` (if using www)
3. **Keep the old URI** (for backward compatibility during transition):
   - `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/meta-callback`
4. Click **Save Changes**

### 2.3 Update App Domains

1. In **Settings** → **Basic**, find **App Domains**
2. **Add** your domain:
   - `amesa-group.net`
   - `www.amesa-group.net` (if using www)
3. Click **Save Changes**

### 2.4 Update Site URL (if applicable)

1. In **Settings** → **Basic**, find **Site URL**
2. Update to:
   - `https://amesa-group.net`
   - OR `https://www.amesa-group.net` (if using www)
3. Click **Save Changes**

### 2.5 Test Meta OAuth

After updating:
1. Test Meta/Facebook login from your application
2. Verify redirect works correctly
3. Once confirmed working, you can remove the old CloudFront URI (optional)

## Step 3: Update Application Configuration

### 3.1 Update CORS Configuration

Update CORS allowed origins in your backend services:

**Location**: `BE/AmesaBackend.Auth/Program.cs` or service configuration

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://amesa-group.net",
            "https://www.amesa-group.net",
            "https://dpqbvdgnenckf.cloudfront.net" // Keep for backward compatibility
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});
```

### 3.2 Update Frontend API URLs

Update API base URLs in Angular frontend:

**Location**: `FE/src/environments/environment.prod.ts`

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://amesa-group.net/api/v1',
  // OR if using www:
  // apiUrl: 'https://www.amesa-group.net/api/v1',
  // ...
};
```

### 3.3 Update OAuth Configuration (if hardcoded)

If OAuth redirect URIs are hardcoded anywhere in the codebase:

**Search for**:
- `dpqbvdgnenckf.cloudfront.net/api/v1/oauth`
- `google-callback`
- `meta-callback`

**Replace with**:
- `amesa-group.net/api/v1/oauth` (or `www.amesa-group.net/api/v1/oauth`)

## Step 4: Update AWS Secrets Manager (if applicable)

If OAuth redirect URIs are stored in AWS Secrets Manager:

### 4.1 Google OAuth Secret

**Secret Name**: `amesa-google_people_API` (or as configured)

Update the secret to include new redirect URI in configuration.

### 4.2 Meta OAuth Secret

**Secret Name**: `amesa-meta-oauth`

Update the secret to include new redirect URI in configuration.

## Verification Checklist

After updating OAuth redirect URIs:

- [ ] Google OAuth redirect URI added to Google Console
- [ ] Meta OAuth redirect URI added to Meta Developer Console
- [ ] App Domains updated in Meta Console
- [ ] CORS configuration updated in backend
- [ ] Frontend API URLs updated
- [ ] Google OAuth login tested and working
- [ ] Meta OAuth login tested and working
- [ ] Old CloudFront URIs removed (after verification)

## Testing

### Test Google OAuth

1. Navigate to your application
2. Click "Login with Google"
3. Complete OAuth flow
4. Verify redirect works to `https://amesa-group.net/api/v1/oauth/google-callback`
5. Verify authentication succeeds

### Test Meta OAuth

1. Navigate to your application
2. Click "Login with Meta/Facebook"
3. Complete OAuth flow
4. Verify redirect works to `https://amesa-group.net/api/v1/oauth/meta-callback`
5. Verify authentication succeeds

## Rollback Plan

If issues occur:

1. **Keep old CloudFront URIs** in both Google and Meta consoles during transition
2. **Revert frontend** to use CloudFront domain if needed
3. **Monitor logs** for OAuth errors
4. **Gradually migrate** users to new domain

## Important Notes

1. **Both URIs Required**: Keep both old and new URIs during transition period
2. **DNS Propagation**: Wait for DNS to fully propagate before testing
3. **SSL Certificate**: Ensure SSL certificate is validated before testing OAuth
4. **CORS**: Update CORS to allow new domain
5. **Testing**: Test thoroughly before removing old URIs

## Timeline

1. **Immediate**: Add new redirect URIs to Google and Meta consoles (keep old ones)
2. **After DNS Propagation**: Test OAuth flows with new domain
3. **After Verification**: Remove old CloudFront URIs (optional, for cleanup)
4. **Monitor**: Watch for OAuth errors in logs

## References

- [Google OAuth 2.0 Setup](https://developers.google.com/identity/protocols/oauth2)
- [Meta OAuth Setup](https://developers.facebook.com/docs/facebook-login/web)
- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/)
