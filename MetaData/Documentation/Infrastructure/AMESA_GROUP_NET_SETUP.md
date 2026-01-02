# amesa-group.net Custom Domain Setup

**Domain**: `amesa-group.net`  
**CloudFront Distribution**: `E3GU3QXUR43ZOH` (`dpqbvdgnenckf.cloudfront.net`)  
**Registrar**: GoDaddy  
**Status**: Ready to configure

## Quick Setup Options

### Option 1: CloudFront Console (Easiest - Recommended)

1. **Go to AWS Console** → [CloudFront](https://console.aws.amazon.com/cloudfront/v4/home)
2. **Select distribution** `E3GU3QXUR43ZOH`
3. **On the General tab**, click **"Add a domain"**
4. **Enter domains**:
   - `amesa-group.net` (root domain)
   - `www.amesa-group.net` (www subdomain)
5. **For TLS certificate**: Choose **"Automatically create a certificate"**
6. **Copy validation records** provided by CloudFront
7. **Add validation records to GoDaddy DNS**:
   - Go to: https://dcc.godaddy.com/manage/amesa-group.net/dns
   - Click "Add" for each CNAME validation record
8. **Click "Validate certificate"** after adding DNS records
9. **Click "Add domains"** when validation completes
10. **Wait for CloudFront deployment** (15-30 minutes)
11. **Add DNS records** to point domain to CloudFront (see below)

### Option 2: Automated Script

```powershell
cd MetaData/Scripts
.\setup-amesa-group-domain.ps1 -IncludeWWW -IncludeRoot
```

This script will:
- Request SSL certificate in ACM (us-east-1)
- Provide DNS validation records
- Wait for certificate validation
- Update CloudFront distribution
- Provide final DNS configuration

## DNS Configuration for GoDaddy

After CloudFront is configured, add these DNS records in GoDaddy:

### Go to DNS Management
**URL**: https://dcc.godaddy.com/manage/amesa-group.net/dns

### For Root Domain (amesa-group.net)

**Option A: A Record (Alias)** - If GoDaddy supports it:
- **Type**: A (Alias)
- **Name**: `@` (or leave blank)
- **Value**: `dpqbvdgnenckf.cloudfront.net`
- **TTL**: 3600

**Option B: CNAME** - If GoDaddy supports root CNAME:
- **Type**: CNAME
- **Name**: `@` (or leave blank)
- **Value**: `dpqbvdgnenckf.cloudfront.net`
- **TTL**: 3600

**Note**: Some registrars don't support CNAME at root. If GoDaddy doesn't support it, you may need to:
- Use Route 53 for DNS (migrate nameservers)
- Or only use `www.amesa-group.net` (subdomain)

### For WWW Subdomain (www.amesa-group.net)

- **Type**: CNAME
- **Name**: `www`
- **Value**: `dpqbvdgnenckf.cloudfront.net`
- **TTL**: 3600

## Step-by-Step Process

### Phase 1: Certificate Setup

1. **Request certificate** (via CloudFront console or script)
2. **Get validation records** (CNAME records from ACM)
3. **Add validation records to GoDaddy DNS**
4. **Wait for validation** (5-30 minutes)
5. **Verify certificate status**: Should be `ISSUED`

### Phase 2: CloudFront Configuration

1. **Add alternate domain names** to CloudFront distribution
2. **Attach SSL certificate** to distribution
3. **Deploy changes** (15-30 minutes)
4. **Verify deployment**: Status should be `Deployed`

### Phase 3: DNS Configuration

1. **Add DNS records** in GoDaddy (see above)
2. **Wait for DNS propagation** (5-60 minutes)
3. **Test domains**:
   - `https://amesa-group.net`
   - `https://www.amesa-group.net`

## Verification Commands

### Check Certificate Status
```bash
aws acm list-certificates --region us-east-1 --query "CertificateSummaryList[?contains(DomainName, 'amesa-group.net')]"
```

### Check CloudFront Aliases
```bash
aws cloudfront get-distribution --id E3GU3QXUR43ZOH --region us-east-1 --query "Distribution.DistributionConfig.Aliases"
```

### Test DNS Resolution
```bash
# Test root domain
dig amesa-group.net
nslookup amesa-group.net

# Test www subdomain
dig www.amesa-group.net
nslookup www.amesa-group.net
```

### Test HTTPS
```bash
# Test root domain
curl -I https://amesa-group.net

# Test www subdomain
curl -I https://www.amesa-group.net
```

## Troubleshooting

### Certificate Validation Fails

1. **Check DNS records are correct**:
   ```bash
   dig CNAME _validation-record-name.amesa-group.net
   ```

2. **Verify record values match ACM**:
   ```bash
   aws acm describe-certificate \
     --certificate-arn <CERT_ARN> \
     --region us-east-1 \
     --query "Certificate.DomainValidationOptions"
   ```

3. **Wait longer**: DNS propagation can take up to 48 hours (usually faster)

### CloudFront Update Fails

1. **Check ETag**: Get fresh ETag before updating
2. **Verify config format**: JSON must be valid
3. **Check permissions**: Ensure IAM user has `cloudfront:UpdateDistribution`

### DNS Not Resolving

1. **Check DNS propagation**:
   ```bash
   dig amesa-group.net
   nslookup amesa-group.net
   ```

2. **Verify CloudFront is deployed**:
   ```bash
   aws cloudfront get-distribution \
     --id E3GU3QXUR43ZOH \
     --region us-east-1 \
     --query "Distribution.Status"
   ```

3. **Check DNS records in GoDaddy**: Ensure records are correct

### SSL Certificate Errors

1. **Verify certificate is issued**:
   ```bash
   aws acm describe-certificate \
     --certificate-arn <CERT_ARN> \
     --region us-east-1 \
     --query "Certificate.Status"
   ```

2. **Check certificate is in us-east-1**: CloudFront requires us-east-1
3. **Verify domain matches**: Certificate domain must match CloudFront alias

## Expected Timeline

- **Certificate Request**: Immediate
- **Certificate Validation**: 5-30 minutes (after DNS records added)
- **CloudFront Deployment**: 15-30 minutes
- **DNS Propagation**: 5-60 minutes (usually faster)
- **Total Time**: ~30-90 minutes (depending on DNS propagation)

## After Setup

Once the domain is live, you'll need to:

1. **Update application configuration**:
   - Update API base URLs in frontend
   - Update CORS allowed origins in backend
   - Update OAuth redirect URIs

2. **Test all endpoints**:
   - Frontend: `https://amesa-group.net` or `https://www.amesa-group.net`
   - API: `https://amesa-group.net/api/v1/*`
   - WebSocket: `https://amesa-group.net/ws/*`

3. **Update documentation** with new domain

## References

- [CloudFront Custom Domain Setup Guide](../Infrastructure/CUSTOM_DOMAIN_SETUP_GUIDE.md)
- [AWS Documentation References](../Infrastructure/AWS_DOCUMENTATION_REFERENCES.md)
- [Why us-east-1 Certificates](../Infrastructure/WHY_US_EAST_1_CERTIFICATES.md)
