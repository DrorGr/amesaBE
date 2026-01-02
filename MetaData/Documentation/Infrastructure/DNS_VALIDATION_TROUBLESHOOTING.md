# DNS Validation Troubleshooting for amesa-group.net

## Current Status

- **Certificate ARN**: `arn:aws:acm:us-east-1:129394705401:certificate/bb2c961b-a316-4ba5-9a92-35430e2a146d`
- **Status**: PENDING_VALIDATION
- **DNS Records Added**: ✅ Both CNAME records are in GoDaddy DNS

## Common Issues and Solutions

### Issue 1: DNS Propagation Delay

**Problem**: DNS records are added but not yet propagating globally.

**Solution**: 
- DNS changes can take **5-60 minutes** to propagate
- Sometimes takes up to **48 hours** (rare)
- Usually resolves within **15-30 minutes**

**Check Status**:
```bash
# Check with Google DNS
nslookup -type=CNAME _499302bb3112df78f12792e6ebf6a030.amesa-group.net 8.8.8.8
nslookup -type=CNAME _c953380c12adfaa9ba5c216ea4cbcc93.www.amesa-group.net 8.8.8.8

# Or use dig
dig CNAME _499302bb3112df78f12792e6ebf6a030.amesa-group.net @8.8.8.8
dig CNAME _c953380c12adfaa9ba5c216ea4cbcc93.www.amesa-group.net @8.8.8.8
```

### Issue 2: Missing Trailing Dot in Value

**Problem**: The CNAME value must end with a trailing dot (`.`).

**Check in GoDaddy**:
- Value should be: `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
- **NOT**: `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws` (missing dot)

**Solution**: Edit the record in GoDaddy and ensure the value ends with a dot.

### Issue 3: Record Name Format

**Problem**: The record name should NOT include the domain.

**Correct Format**:
- Name: `_499302bb3112df78f12792e6ebf6a030` (just the prefix)
- **NOT**: `_499302bb3112df78f12792e6ebf6a030.amesa-group.net`

**Solution**: In GoDaddy, the Name field should only contain the validation prefix, not the full domain.

### Issue 4: TTL Too High

**Problem**: High TTL values can delay propagation.

**Solution**: 
- Set TTL to **3600** (1 hour) or lower
- Or use default TTL

### Issue 5: GoDaddy DNS Cache

**Problem**: GoDaddy's DNS cache might not have updated yet.

**Solution**:
1. Wait 10-15 minutes after adding records
2. Clear browser cache
3. Try validating again in CloudFront console

## Step-by-Step Troubleshooting

### Step 1: Verify Records in GoDaddy

1. Go to: https://dcc.godaddy.com/manage/amesa-group.net/dns
2. Verify both CNAME records exist:
   - `_499302bb3112df78f12792e6ebf6a030` → `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
   - `_c953380c12adfaa9ba5c216ea4cbcc93` → `_72367a63ccb85fc0ce6f1ccd27b3ed3e.jkddzztszm.acm-validations.aws.`
3. Check that values end with a dot (`.`)
4. Check that names are correct (with underscore prefix)

### Step 2: Test DNS Resolution

Use online DNS checkers:
- https://dnschecker.org/
- https://www.whatsmydns.net/
- https://mxtoolbox.com/CNAMELookup.aspx

Or use command line:
```bash
# Windows PowerShell
Resolve-DnsName -Type CNAME "_499302bb3112df78f12792e6ebf6a030.amesa-group.net" -Server 8.8.8.8
Resolve-DnsName -Type CNAME "_c953380c12adfaa9ba5c216ea4cbcc93.www.amesa-group.net" -Server 8.8.8.8
```

### Step 3: Check Certificate Status

```bash
aws acm describe-certificate \
  --certificate-arn arn:aws:acm:us-east-1:129394705401:certificate/bb2c961b-a316-4ba5-9a92-35430e2a146d \
  --region us-east-1 \
  --query "Certificate.DomainValidationOptions[*].[DomainName,ValidationStatus,ValidationMethod]"
```

### Step 4: Re-validate in CloudFront Console

1. Go to AWS CloudFront console
2. Click on your distribution
3. Go to the domain configuration
4. Click **"Validate certificate"** again
5. Wait 5-10 minutes and check again

## Expected Timeline

- **Immediate**: Records added to GoDaddy
- **5-15 minutes**: DNS propagation starts
- **15-30 minutes**: Most DNS servers updated
- **30-60 minutes**: Global propagation complete
- **After propagation**: ACM validation (usually within 5 minutes)

## Manual Validation Check

You can manually trigger validation by checking the certificate:

```bash
aws acm describe-certificate \
  --certificate-arn arn:aws:acm:us-east-1:129394705401:certificate/bb2c961b-a316-4ba5-9a92-35430e2a146d \
  --region us-east-1 \
  --query "Certificate.{Status:Status,DomainValidationOptions:DomainValidationOptions[*].[DomainName,ValidationStatus]}"
```

## If Still Not Validating After 1 Hour

1. **Double-check record format** in GoDaddy
2. **Delete and re-add records** (sometimes helps)
3. **Check for typos** in names or values
4. **Verify trailing dots** in values
5. **Contact AWS Support** if issue persists

## Alternative: Use Email Validation (Not Recommended)

If DNS validation continues to fail, you can switch to email validation:
- ACM will send validation emails to registered domain contacts
- Check email addresses in GoDaddy domain settings
- Click validation link in email

**Note**: DNS validation is preferred and more reliable.

## Next Steps

Once validation succeeds:
1. Certificate status will change to `ISSUED`
2. CloudFront will automatically add domains to distribution
3. Wait for CloudFront deployment (15-30 minutes)
4. Add final DNS records to point domain to CloudFront
