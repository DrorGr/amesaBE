# Meta/Facebook OAuth Redirect URI Setup - Step by Step

## Quick Steps to Add Redirect URIs in Meta Developer Console

### Step 1: Access Meta Developer Console

1. Go to [Meta for Developers](https://developers.facebook.com/)
2. **Sign in** with your Facebook account
3. If you don't have an app yet, click **"My Apps"** → **"Create App"**
4. If you already have an app, click **"My Apps"** and select your app

### Step 2: Navigate to Settings

1. In your app dashboard, look for the left sidebar
2. Click on **"Settings"** (gear icon)
3. Click on **"Basic"** (under Settings)

### Step 3: Add Valid OAuth Redirect URIs

1. Scroll down to find **"Valid OAuth Redirect URIs"** section
2. You'll see a text area or input field for redirect URIs
3. **Add each URI on a new line** (or click "Add URI" if it's a list):

```
https://amesa-group.net/api/v1/oauth/meta-callback
https://www.amesa-group.net/api/v1/oauth/meta-callback
https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/meta-callback
```

4. **Keep the old CloudFront URI** for backward compatibility during transition

### Step 4: Update App Domains

1. In the same **"Basic"** settings page
2. Find **"App Domains"** section
3. **Add your domains** (one per line or comma-separated):

```
amesa-group.net
www.amesa-group.net
```

4. **Note**: Don't include `https://` or paths, just the domain names

### Step 5: Update Site URL (Optional but Recommended)

1. Still in **"Basic"** settings
2. Find **"Site URL"** field
3. Enter your primary domain:

```
https://amesa-group.net
```

OR if you prefer www:

```
https://www.amesa-group.net
```

### Step 6: Save Changes

1. Scroll to the bottom of the page
2. Click **"Save Changes"** button
3. You may need to enter your Facebook password to confirm

## Visual Guide

### Where to Find Settings

```
Meta Developer Console
├── My Apps
    └── [Your App Name]
        ├── Dashboard
        ├── Settings ⚙️
        │   ├── Basic ← Click here
        │   ├── Advanced
        │   └── ...
        └── ...
```

### Settings Page Layout

```
Settings → Basic
├── App ID
├── App Secret
├── App Domains ← Add: amesa-group.net, www.amesa-group.net
├── Privacy Policy URL
├── User Data Deletion
├── Site URL ← Add: https://amesa-group.net
└── Valid OAuth Redirect URIs ← Add callback URLs here
```

## Complete Configuration

### Valid OAuth Redirect URIs

Add these three URIs:

1. `https://amesa-group.net/api/v1/oauth/meta-callback`
2. `https://www.amesa-group.net/api/v1/oauth/meta-callback`
3. `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/meta-callback` (keep for backward compatibility)

### App Domains

Add these domains:

1. `amesa-group.net`
2. `www.amesa-group.net`

### Site URL

Set to your primary domain:

- `https://amesa-group.net` (or `https://www.amesa-group.net` if you prefer www)

## Important Notes

1. **Exact Match Required**: The redirect URI must match exactly (including `https://` and the full path)
2. **No Trailing Slash**: Don't add a trailing slash at the end
3. **Case Sensitive**: Domain names are case-insensitive, but paths are case-sensitive
4. **Save Changes**: Always click "Save Changes" after making updates
5. **Password Confirmation**: Meta may ask for your password to confirm changes

## Troubleshooting

### Can't Find "Valid OAuth Redirect URIs"

1. Make sure you're in **Settings → Basic**
2. Scroll down - it's usually near the bottom
3. If you don't see it, your app might need to be configured for "Facebook Login" product
4. Go to **Products** → **Facebook Login** → **Settings** → **Valid OAuth Redirect URIs**

### App Domains Field Not Available

1. Some app types don't require App Domains
2. If you can't find it, focus on the **Valid OAuth Redirect URIs** field
3. That's the most important one for OAuth to work

### Changes Not Saving

1. Make sure you're an admin of the app
2. Check if you need to verify your identity
3. Try refreshing the page and saving again
4. Check for any error messages

## Testing After Setup

1. **Wait a few minutes** for changes to propagate
2. **Test Meta login** from your application
3. **Verify redirect** works to `https://amesa-group.net/api/v1/oauth/meta-callback`
4. **Check for errors** in browser console or server logs

## Security Notes

- **Keep App Secret secure** - Never commit it to code
- **Use HTTPS only** - Never use `http://` for production redirect URIs
- **Remove old URIs** - After verifying new domain works, remove CloudFront URI (optional)

## Quick Reference URLs

- **Meta Developer Console**: https://developers.facebook.com/
- **Your App Settings**: https://developers.facebook.com/apps/[YOUR_APP_ID]/settings/basic/
- **Facebook Login Documentation**: https://developers.facebook.com/docs/facebook-login/web
