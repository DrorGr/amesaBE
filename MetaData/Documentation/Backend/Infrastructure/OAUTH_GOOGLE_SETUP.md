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

### Error: `invalid_client` or `invalid_client;Description=Unauthorized`
- **Cause**: Client ID or Client Secret is incorrect, doesn't match Google Cloud Console, or the OAuth client type is wrong
- **Symptoms**: 
  - OAuth redirect works (user sees Google login)
  - User authorizes the app
  - Error occurs when exchanging authorization code for tokens
- **Solutions**:
  1. **Verify AWS Secrets Manager Secret**:
     - Go to AWS Secrets Manager
     - Find secret: `amesa-google_people_API`
     - Verify it contains:
       ```json
       {
         "ClientId": "your-client-id-here.apps.googleusercontent.com",
         "ClientSecret": "your-client-secret-here"
       }
       ```
  2. **Verify Google Cloud Console OAuth Client**:
     - Go to: https://console.cloud.google.com/apis/credentials
     - Find your OAuth 2.0 Client ID
     - Verify the **Client ID** matches exactly what's in AWS Secrets Manager
     - Verify the **Client Secret** matches exactly (click "Show" to reveal)
  3. **Check OAuth Client Type**:
     - The OAuth client must be of type **"Web application"** (not Desktop app, iOS, Android, etc.)
     - Go to your OAuth client in Google Cloud Console
     - Verify it's configured as "Web application"
  4. **Verify Client Secret Format**:
     - Client Secret should NOT have spaces or extra characters
     - Copy it directly from Google Cloud Console (use "Show" button)
     - Ensure no trailing/leading whitespace
  5. **Check Application Status**:
     - Ensure the OAuth consent screen is published (if required)
     - Verify the application is not in testing mode with restricted users (unless you're testing)
  6. **Check Logs**:
     - After deployment, check ECS logs: `aws logs tail /ecs/amesa-auth-service --follow`
     - Look for "OAuth ClientId loaded" message
     - Look for "INVALID_CLIENT ERROR" details

### Error: `access_denied`
- **Cause**: User denied permission or OAuth scope issue
- **Solution**: Check OAuth scopes in Google Cloud Console

## 📚 Related Files

- `BE/AmesaBackend.Auth/Program.cs` - OAuth configuration
- `BE/AmesaBackend/Program.cs` - OAuth configuration (main backend)
- AWS Secrets Manager: `amesa-google_people_API` - Contains ClientId and ClientSecret

