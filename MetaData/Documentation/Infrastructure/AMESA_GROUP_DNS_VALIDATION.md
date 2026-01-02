# DNS Validation Records for amesa-group.net

**Certificate ARN**: `arn:aws:acm:us-east-1:129394705401:certificate/bb2c961b-a316-4ba5-9a92-35430e2a146d`  
**Status**: PENDING_VALIDATION  
**Action Required**: Add the following CNAME records to GoDaddy DNS

## GoDaddy DNS Management

**URL**: https://dcc.godaddy.com/manage/amesa-group.net/dns

## DNS Validation Records to Add

### Record 1: amesa-group.net

- **Type**: CNAME
- **Name**: `_499302bb3112df78f12792e6ebf6a030`
- **Value**: `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
- **TTL**: 3600 (or default)

**Full Record Name**: `_499302bb3112df78f12792e6ebf6a030.amesa-group.net`

### Record 2: www.amesa-group.net

- **Type**: CNAME
- **Name**: `_c953380c12adfaa9ba5c216ea4cbcc93`
- **Value**: `_72367a63ccb85fc0ce6f1ccd27b3ed3e.jkddzztszm.acm-validations.aws.`
- **TTL**: 3600 (or default)

**Full Record Name**: `_c953380c12adfaa9ba5c216ea4cbcc93.www.amesa-group.net`

## Step-by-Step Instructions

### 1. Go to GoDaddy DNS Management

1. Open: https://dcc.godaddy.com/manage/amesa-group.net/dns
2. Click on the **"DNS"** tab (if not already selected)

### 2. Add First Validation Record

1. Click **"Add"** button (or **"Add Record"**)
2. Select **"CNAME"** as the record type
3. Enter the following:
   - **Name**: `_499302bb3112df78f12792e6ebf6a030`
   - **Value**: `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
   - **TTL**: 3600 (or leave as default)
4. Click **"Save"** or **"Add Record"**

### 3. Add Second Validation Record

1. Click **"Add"** button again
2. Select **"CNAME"** as the record type
3. Enter the following:
   - **Name**: `_c953380c12adfaa9ba5c216ea4cbcc93`
   - **Value**: `_72367a63ccb85fc0ce6f1ccd27b3ed3e.jkddzztszm.acm-validations.aws.`
   - **TTL**: 3600 (or leave as default)
4. Click **"Save"** or **"Add Record"**

### 4. Verify Records Added

You should now see both CNAME records in your DNS records list:
- `_499302bb3112df78f12792e6ebf6a030` → `_03da8e02bfcffb5030f069c0b2c89bae.jkddzztszm.acm-validations.aws.`
- `_c953380c12adfaa9ba5c216ea4cbcc93` → `_72367a63ccb85fc0ce6f1ccd27b3ed3e.jkddzztszm.acm-validations.aws.`

### 5. Return to CloudFront Console

1. Go back to AWS CloudFront console
2. Click **"Validate certificate"** button
3. Wait for validation (usually 5-30 minutes)

## Verification

After adding the records, you can verify they're working:

```bash
# Check first record
dig CNAME _499302bb3112df78f12792e6ebf6a030.amesa-group.net

# Check second record
dig CNAME _c953380c12adfaa9ba5c216ea4cbcc93.www.amesa-group.net
```

Or use nslookup:
```bash
nslookup -type=CNAME _499302bb3112df78f12792e6ebf6a030.amesa-group.net
nslookup -type=CNAME _c953380c12adfaa9ba5c216ea4cbcc93.www.amesa-group.net
```

## Expected Timeline

- **DNS Propagation**: 5-30 minutes (usually faster)
- **Certificate Validation**: 5-30 minutes after DNS records are added
- **Total**: Usually 10-60 minutes

## Troubleshooting

### Records Not Showing Up

1. **Wait a few minutes**: DNS changes can take time to propagate
2. **Check record names**: Make sure you entered the exact name (including the underscore)
3. **Check record values**: Make sure you included the full value ending with `.acm-validations.aws.`
4. **Verify in GoDaddy**: Check that both records appear in your DNS records list

### Validation Still Pending

1. **Wait longer**: Validation can take up to 30 minutes
2. **Check DNS propagation**: Use `dig` or `nslookup` to verify records are resolving
3. **Verify record format**: Make sure there are no typos in names or values
4. **Check certificate status**:
   ```bash
   aws acm describe-certificate \
     --certificate-arn arn:aws:acm:us-east-1:129394705401:certificate/bb2c961b-a316-4ba5-9a92-35430e2a146d \
     --region us-east-1 \
     --query "Certificate.DomainValidationOptions[*].[DomainName,ValidationStatus]"
   ```

## Next Steps After Validation

Once the certificate is validated:

1. **CloudFront will automatically add the domains** to your distribution
2. **Wait for CloudFront deployment** (15-30 minutes)
3. **Add final DNS records** to point domain to CloudFront:
   - CNAME for `www.amesa-group.net` → `dpqbvdgnenckf.cloudfront.net`
   - A record or CNAME for `amesa-group.net` → `dpqbvdgnenckf.cloudfront.net`

See [AMESA_GROUP_NET_SETUP.md](./AMESA_GROUP_NET_SETUP.md) for complete setup instructions.
