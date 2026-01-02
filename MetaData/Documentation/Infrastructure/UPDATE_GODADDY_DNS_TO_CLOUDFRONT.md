# Update GoDaddy DNS to Point to CloudFront

## Problem
The domain `amesa-group.net` is currently showing GoDaddy's "Coming Soon" parking page instead of your CloudFront distribution. This means DNS is pointing to GoDaddy's servers, not CloudFront.

## Solution
Update DNS records in GoDaddy to point to your CloudFront distribution.

## CloudFront Distribution Details

- **Distribution ID**: `E3GU3QXUR43ZOH`
- **CloudFront Domain**: `dpqbvdgnenckf.cloudfront.net`
- **Custom Domains**: `amesa-group.net`, `www.amesa-group.net`
- **Certificate**: ✅ Applied and working

## Step-by-Step: Update GoDaddy DNS

### Step 1: Access GoDaddy DNS Management

1. Go to: https://dcc.godaddy.com/manage/amesa-group.net/dns
2. Click on the **"DNS"** tab

### Step 2: Remove/Update GoDaddy Parking Page Records

**Find and remove/update these records:**
- Any **A record** pointing to GoDaddy parking page
- Any **CNAME** record pointing to GoDaddy parking page
- Any redirect records

### Step 3: Add DNS Records for CloudFront

#### For Root Domain (amesa-group.net)

**Option A: A Record (Alias) - Recommended if GoDaddy supports it**

1. Click **"Add"** or **"Add Record"**
2. Select **"A"** record type
3. Enter:
   - **Name**: `@` (or leave blank for root domain)
   - **Value**: `dpqbvdgnenckf.cloudfront.net`
   - **TTL**: 3600 (or default)
4. Click **"Save"**

**Option B: CNAME Record - If GoDaddy supports root CNAME**

1. Click **"Add"** or **"Add Record"**
2. Select **"CNAME"** record type
3. Enter:
   - **Name**: `@` (or leave blank for root domain)
   - **Value**: `dpqbvdgnenckf.cloudfront.net`
   - **TTL**: 3600 (or default)
4. Click **"Save"**

**Note**: Some registrars don't support CNAME at root. If GoDaddy doesn't allow it, you may need to:
- Use Route 53 for DNS (migrate nameservers)
- Or only use `www.amesa-group.net` (subdomain)

#### For WWW Subdomain (www.amesa-group.net)

1. Click **"Add"** or **"Add Record"**
2. Select **"CNAME"** record type
3. Enter:
   - **Name**: `www`
   - **Value**: `dpqbvdgnenckf.cloudfront.net`
   - **TTL**: 3600 (or default)
4. Click **"Save"**

### Step 4: Keep Validation Records

**IMPORTANT**: Keep the ACM validation CNAME records:
- `_499302bb3112df78f12792e6ebf6a030` → `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
- `_c953380c12adfaa9ba5c216ea4cbcc93` → `_72367a63ccb85fc0ce6f1ccd27b3ed3e.jkddzztszm.acm-validations.aws.`

These are needed for certificate validation. Don't delete them.

## Final DNS Configuration

After updating, your GoDaddy DNS should have:

### Required Records:

1. **A or CNAME for root domain**:
   - Name: `@` (or blank)
   - Type: A (Alias) or CNAME
   - Value: `dpqbvdgnenckf.cloudfront.net`

2. **CNAME for www**:
   - Name: `www`
   - Type: CNAME
   - Value: `dpqbvdgnenckf.cloudfront.net`

3. **CNAME for certificate validation** (keep these):
   - Name: `_499302bb3112df78f12792e6ebf6a030`
   - Value: `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
   
   - Name: `_c953380c12adfaa9ba5c216ea4cbcc93`
   - Value: `_72367a63ccb85fc0ce6f1ccd27b3ed3e.jkddzztszm.acm-validations.aws.`

4. **NS records** (GoDaddy name servers - don't change):
   - `ns23.domaincontrol.com.`
   - `ns24.domaincontrol.com.`

### Remove These:

- Any A records pointing to GoDaddy parking page
- Any CNAME records pointing to GoDaddy parking page
- Any redirect records to `/lander` or parking pages

## Verification

After updating DNS:

1. **Wait 5-15 minutes** for DNS propagation
2. **Test the domain**:
   ```bash
   # Check DNS resolution
   nslookup amesa-group.net
   dig amesa-group.net
   
   # Test HTTPS
   curl -I https://amesa-group.net
   ```

3. **Expected result**: Should show your CloudFront distribution, not GoDaddy parking page

## Troubleshooting

### Still Showing GoDaddy Parking Page

1. **Check DNS propagation**: Use https://dnschecker.org/
2. **Clear browser cache**: Hard refresh (Ctrl+F5)
3. **Check for redirects**: Look for any redirect records in GoDaddy
4. **Verify CloudFront**: Check CloudFront distribution is deployed

### Root Domain Not Working

If root domain (`amesa-group.net`) doesn't work with CNAME:
- GoDaddy may not support root CNAME
- Options:
  1. Use Route 53 for DNS (recommended)
  2. Only use `www.amesa-group.net` (subdomain works with CNAME)

### CloudFront Not Responding

1. **Check CloudFront status**: Should be "Deployed"
2. **Verify alternate domain names**: Both domains should be in CloudFront
3. **Check certificate**: Should be "Issued" in ACM
4. **Test CloudFront domain directly**: `https://dpqbvdgnenckf.cloudfront.net`

## Next Steps After DNS Update

Once DNS is pointing to CloudFront:

1. **Wait for DNS propagation** (5-60 minutes)
2. **Test your domain**: `https://amesa-group.net`
3. **Verify SSL certificate**: Should show valid certificate
4. **Test all endpoints**:
   - Frontend: `https://amesa-group.net`
   - API: `https://amesa-group.net/api/v1/*`
   - WebSocket: `https://amesa-group.net/ws/*`

## References

- [CloudFront Custom Domain Setup](./CUSTOM_DOMAIN_SETUP_GUIDE.md)
- [GoDaddy DNS Management](https://www.godaddy.com/help/manage-dns-680)
