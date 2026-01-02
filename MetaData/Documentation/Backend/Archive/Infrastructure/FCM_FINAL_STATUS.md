# FCM SNS Platform Setup - Final Status

## Current Situation

**Status**: ❌ **Blocked** - AWS SNS continues to reject the `firebase-adminsdk` service account JSON credentials.

**Error**: `Invalid parameter: Attributes Reason: Platform credentials are invalid`

## What We've Tried

1. ✅ Used `firebase-adminsdk-fbsvc@amesa-oauth.iam.gserviceaccount.com` service account JSON
2. ✅ Granted FCM permissions to the service account
3. ✅ Verified JSON file format is correct
4. ✅ Tried both GCM and FCM platform types
5. ❌ Still getting "Platform credentials are invalid" error

## Root Cause Analysis

AWS SNS validates FCM credentials by making a **real-time API call to Google's FCM service**. The error suggests one of:

1. **Service account doesn't have correct IAM role** - Even if you granted permissions, the exact role name matters
2. **FCM API not enabled** - The Firebase Cloud Messaging API must be enabled in the Google Cloud project
3. **Service account key is invalid** - The JSON key might be expired or revoked
4. **AWS SNS format requirement** - There might be a specific format AWS expects that we're not meeting

## Recommended Next Steps

### Option 1: Verify via Google Cloud Console (Most Important)

1. **Check Service Account Roles**:
   - Go to: https://console.cloud.google.com/iam-admin/serviceaccounts?project=amesa-oauth
   - Click: `firebase-adminsdk-fbsvc@amesa-oauth.iam.gserviceaccount.com`
   - Go to "Permissions" tab
   - **Verify** it has exactly: `Firebase Cloud Messaging API Admin` role
   - If it shows a different role name, that's the issue

2. **Verify FCM API is Enabled**:
   - Go to: https://console.cloud.google.com/apis/library/fcm.googleapis.com?project=amesa-oauth
   - Should show "API enabled" status
   - If not, click "Enable"

3. **Test Service Account Credentials**:
   - Try using the service account to make a test FCM call
   - This will verify the credentials work with Google's API

### Option 2: Use AWS Console (Better Error Messages)

The AWS Console often provides more detailed error messages:

1. **Go to**: https://console.aws.amazon.com/sns/v3/home?region=eu-north-1#/mobile
2. **Click**: "Create platform application"
3. **Select**: "Firebase Cloud Messaging (FCM)"
4. **Choose**: "Token-based authentication (recommended)"
5. **Upload**: `Infrastructure/firebase-adminsdk-key.json`
6. **Read the error message** - It may tell you exactly what's wrong

### Option 3: Download Private Key from Firebase Console

Instead of using the service account key, try downloading the "Private key" directly from Firebase Console:

1. **Go to**: https://console.firebase.google.com/project/amesa-oauth/settings/cloudmessaging
2. **In "Cloud Messaging API (V1)" section**, look for **"Private key"** download button
3. **Download** that JSON file
4. **Use that file** instead of the service account key

This "Private key" is specifically generated for FCM and might work better with AWS SNS.

### Option 4: Create New Service Account Key

The current key might be invalid. Try creating a new key:

1. Go to service account: `firebase-adminsdk-fbsvc`
2. Go to "Keys" tab
3. Delete the old key (if safe to do so)
4. Create a new JSON key
5. Use the new key

## Current Files

- `Infrastructure/firebase-adminsdk-key.json` - Service account JSON (currently being rejected)
- `Infrastructure/FCM_TROUBLESHOOTING.md` - Detailed troubleshooting guide
- `Infrastructure/FCM_SERVICE_ACCOUNT_SETUP.md` - Setup instructions

## What Works

✅ **Infrastructure is ready**:
- ECS task definitions updated
- Secrets configured
- Database migrations ready
- Code implementation complete

❌ **Blocked on**: SNS FCM platform creation

## Workaround (If Needed)

If SNS FCM setup continues to fail, you can:

1. **Skip SNS for now** - The Notification service code will handle missing platform ARN gracefully
2. **Use direct FCM integration** - Modify `PushChannelProvider.cs` to use Firebase Admin SDK directly instead of SNS
3. **Set up later** - Complete the rest of the infrastructure and come back to FCM setup

---

**Next Action**: Try Option 2 (AWS Console) to get detailed error message, or Option 3 (Firebase Private key) as it's specifically designed for FCM.









