# Meta OAuth "Invalid Redirect URI" Troubleshooting

## Error Message
```
This is an invalid redirect URI for this application
You can make this URI valid by adding it to the list of valid OAuth redirect URIs above
```

## Common Causes

### 1. URI Not Added to List
The redirect URI you're trying to use isn't in the "Valid OAuth Redirect URIs" list.

### 2. Exact Match Required
Meta requires **exact match** - even small differences will fail:
- Trailing slash: `https://amesa-group.net/api/v1/oauth/meta-callback` ✅ vs `https://amesa-group.net/api/v1/oauth/meta-callback/` ❌
- Protocol mismatch: `https://` ✅ vs `http://` ❌
- Case sensitivity: Paths are case-sensitive
- Extra spaces or characters

### 3. "Use Strict Mode" Setting
If "Use Strict Mode for redirect URIs" is enabled (which it should be), exact matching is enforced.

## Solution Steps

### Step 1: Verify URI in Meta Console

1. Go to **Products** → **Facebook Login** → **Settings**
2. Check **"Valid OAuth Redirect URIs"** field
3. Verify these URIs are listed **exactly** as shown:

```
https://amesa-group.net/api/v1/oauth/meta-callback
https://www.amesa-group.net/api/v1/oauth/meta-callback
https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/meta-callback
```

### Step 2: Check for Common Mistakes

**Check these common issues:**

- ❌ **Trailing slash**: `https://amesa-group.net/api/v1/oauth/meta-callback/` (wrong)
- ✅ **No trailing slash**: `https://amesa-group.net/api/v1/oauth/meta-callback` (correct)

- ❌ **HTTP instead of HTTPS**: `http://amesa-group.net/api/v1/oauth/meta-callback` (wrong)
- ✅ **HTTPS required**: `https://amesa-group.net/api/v1/oauth/meta-callback` (correct)

- ❌ **Wrong path**: `https://amesa-group.net/oauth/meta-callback` (wrong)
- ✅ **Correct path**: `https://amesa-group.net/api/v1/oauth/meta-callback` (correct)

- ❌ **Case mismatch**: `https://amesa-group.net/API/v1/oauth/meta-callback` (wrong)
- ✅ **Correct case**: `https://amesa-group.net/api/v1/oauth/meta-callback` (correct)

### Step 3: Use Redirect URI Validator

1. In **Facebook Login** → **Settings**
2. Find **"Redirect URI Validator"** section
3. Enter your exact redirect URI: `https://amesa-group.net/api/v1/oauth/meta-callback`
4. Click **"Check URI"**
5. It will tell you if the URI is valid or what's wrong

### Step 4: Verify App Domains

1. Go to **Settings** → **Basic**
2. Check **"App Domains"** includes:
   - `amesa-group.net`
   - `www.amesa-group.net`

### Step 5: Check Your Application Code

Verify your application is using the **exact same URI**:

**Backend (Auth Service):**
```csharp
// Check OAuth controller - should redirect to:
return Redirect($"https://amesa-group.net/api/v1/oauth/meta-callback?code={code}");
```

**Frontend:**
```typescript
// Check OAuth service - should use:
const redirectUri = 'https://amesa-group.net/api/v1/oauth/meta-callback';
```

### Step 6: Save and Wait

1. After adding/updating URIs, click **"Save Changes"**
2. **Wait 2-5 minutes** for changes to propagate
3. Try again

## Quick Fix Checklist

- [ ] URI added to "Valid OAuth Redirect URIs" list
- [ ] No trailing slash in URI
- [ ] Using HTTPS (not HTTP)
- [ ] Exact path match: `/api/v1/oauth/meta-callback`
- [ ] Case matches exactly
- [ ] App Domains includes `amesa-group.net` and `www.amesa-group.net`
- [ ] Changes saved in Meta Console
- [ ] Waited 2-5 minutes after saving
- [ ] Used Redirect URI Validator to test

## Testing

### Test with Redirect URI Validator

1. Go to **Products** → **Facebook Login** → **Settings**
2. In **"Redirect URI Validator"** section
3. Enter: `https://amesa-group.net/api/v1/oauth/meta-callback`
4. Click **"Check URI"**
5. Should show: ✅ "This URI is valid" or similar

### Test from Application

1. Try Meta login from your application
2. Check browser console for errors
3. Check server logs for OAuth errors
4. Verify redirect happens to correct URI

## Still Not Working?

### Double-Check Everything

1. **Copy-paste the exact URI** from your code into Meta Console (no manual typing)
2. **Check for hidden characters** (spaces, special characters)
3. **Verify domain is live** - `amesa-group.net` must be accessible via HTTPS
4. **Check SSL certificate** - Domain must have valid SSL certificate
5. **Try the CloudFront URI first** - If `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/meta-callback` works, the issue is with the new domain

### Common Issues

**Domain not live yet:**
- If `amesa-group.net` isn't fully configured yet, Meta can't validate it
- Wait for DNS propagation and CloudFront deployment

**SSL certificate not validated:**
- Meta requires valid HTTPS
- Ensure SSL certificate is issued and domain is accessible

**App in development mode:**
- If app is in development mode, only test users can use it
- Check app status in Meta Console

## Need Help?

If still not working after checking everything:
1. Use the **Redirect URI Validator** tool in Meta Console
2. Check Meta's error message - it usually tells you what's wrong
3. Verify domain is accessible: `curl -I https://amesa-group.net/api/v1/oauth/meta-callback`
4. Check Meta Developer documentation
