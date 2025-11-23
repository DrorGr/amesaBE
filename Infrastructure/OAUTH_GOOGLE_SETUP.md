# Google OAuth Configuration Guide

## 🔴 Current Issue: `redirect_uri_mismatch`

The OAuth error `redirect_uri_mismatch` occurs when the redirect URI in Google Cloud Console doesn't match what the application is sending.

## 📋 Required Redirect URIs

You need to add the following redirect URIs to your Google Cloud Console OAuth 2.0 Client:

### Production Environment:
```
https://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/oauth/google-callback
https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/google-callback
http://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/google-callback
```

**Note:** The HTTP version is included as a temporary fallback. The code has been updated to force HTTPS, but adding the HTTP version ensures compatibility during the transition.

### Development Environment (Local):
```
http://localhost:5000/api/v1/oauth/google-callback
http://localhost:5001/api/v1/oauth/google-callback
```

## ⚠️ Common Mistakes to Avoid

1. **Missing callback path**: The URI must end with `/api/v1/oauth/google-callback`
   - ❌ Wrong: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com`
   - ✅ Correct: `http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/oauth/google-callback`

2. **Typo in domain**: Ensure `.com` not `.cor`
   - ❌ Wrong: `https://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.cor`
   - ✅ Correct: `https://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com`

3. **Both HTTP and HTTPS**: Add both if your ALB supports both protocols

## 🔧 Configuration Steps

### 1. Go to Google Cloud Console
1. Navigate to: https://console.cloud.google.com/
2. Select your project (the one with the OAuth credentials)
3. Go to **APIs & Services** → **Credentials**
4. Find your OAuth 2.0 Client ID (the one used by `amesa-google_people_API` secret)

### 2. Add Authorized Redirect URIs
1. Click on your OAuth 2.0 Client ID
2. Scroll to **Authorized redirect URIs**
3. Click **+ ADD URI**
4. Add each of the URIs listed above
5. Click **SAVE**

### 3. Verify Configuration
The redirect URIs should match exactly:
- ✅ Protocol: `https://` for production, `http://` for local
- ✅ Domain: Exact match (no trailing slashes)
- ✅ Path: `/api/v1/oauth/google-callback` (exact match)

## 📝 Current Application Configuration

### Callback Path (in code):
```csharp
options.CallbackPath = "/api/v1/oauth/google-callback";
```

### Backend URLs:
- **Production ALB**: `amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com`
- **Production CloudFront**: `dpqbvdgnenckf.cloudfront.net`
- **Local Development**: `localhost:5000` or `localhost:5001`

## ⚠️ Important Notes

1. **Both ALB and CloudFront URIs**: Add both because:
   - CloudFront may route `/api/*` to the ALB
   - Direct ALB access might be used
   - Google needs to match the exact URI sent

2. **HTTPS Required**: Production must use `https://` (not `http://`)

3. **No Trailing Slash**: The redirect URI should NOT end with a slash

4. **Case Sensitive**: URIs are case-sensitive, ensure exact match

## 🧪 Testing After Configuration

1. Wait 1-2 minutes for Google to update the configuration
2. Try OAuth login again
3. Check browser console for any errors
4. Verify the redirect URI in the error message matches what you configured

## 🔍 Troubleshooting

### Error: `redirect_uri_mismatch`
- **Cause**: Redirect URI not registered in Google Cloud Console
- **Solution**: Add the exact redirect URI from the error message to Google Cloud Console

### Error: `invalid_client`
- **Cause**: Client ID or Client Secret incorrect
- **Solution**: Verify credentials in AWS Secrets Manager match Google Cloud Console

### Error: `access_denied`
- **Cause**: User denied permission or OAuth scope issue
- **Solution**: Check OAuth scopes in Google Cloud Console

## 📚 Related Files

- `BE/AmesaBackend.Auth/Program.cs` - OAuth configuration
- `BE/AmesaBackend/Program.cs` - OAuth configuration (main backend)
- AWS Secrets Manager: `amesa-google_people_API` - Contains ClientId and ClientSecret

