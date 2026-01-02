# Custom Domain Setup Guide for AmesaBase

This guide explains how to configure a custom domain purchased from GoDaddy (or any registrar) to serve your AmesaBase application through AWS CloudFront.

**Based on AWS Documentation:**
- [CloudFront Custom Domain Names (CNAMEs)](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CNAMEs.html)
- [Add Alternate Domain Name](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CreatingCNAME.html)
- [Configure Alternate Domain Names and HTTPS](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-procedures.html)
- [SSL/TLS Certificate Requirements](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html)

## Current Infrastructure

- **CloudFront Distribution**: `E3GU3QXUR43ZOH` (`dpqbvdgnenckf.cloudfront.net`)
  - Serves frontend (Angular) from S3
  - Routes `/api/*` and `/ws/*` to backend ALB
  - Currently uses CloudFront default certificate
  
- **Application Load Balancer**: `amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com`
  - Routes to 8 microservices via path-based routing
  - HTTP only (port 80)

## Prerequisites

1. Domain purchased from GoDaddy (or any registrar)
2. AWS CLI configured with appropriate permissions:
   - `acm:RequestCertificate`
   - `acm:DescribeCertificate`
   - `cloudfront:GetDistribution`
   - `cloudfront:UpdateDistribution`
   - `route53:*` (if using Route 53)
3. Access to domain DNS management (GoDaddy DNS panel)

## Quick Start

### Option 1: CloudFront Console (Easiest - Recommended for First Time)

The CloudFront console can automatically create and configure certificates:

1. **Sign in to AWS Console** → CloudFront
2. **Select your distribution** (`E3GU3QXUR43ZOH`)
3. **On the General tab**, choose **Add a domain**
4. **Enter your domain(s)** (up to 5 domains per distribution)
5. **For TLS certificate**:
   - Choose **Automatically create a certificate** (recommended)
   - Or choose **Manually create it in ACM** if you prefer
6. **Update DNS records** in GoDaddy with the validation records provided
7. **Validate certificate** after DNS records are added
8. **Add domains** to complete the setup
9. **Configure DNS** to point to CloudFront (see Step 3 below)

**Reference**: [AWS Documentation - Add Alternate Domain Name](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CreatingCNAME.html)

### Option 2: Automated Script (Recommended for Automation)

Use the PowerShell script to automate the setup:

```powershell
cd MetaData/Scripts
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "www"
```

**Parameters:**
- `-DomainName`: Your root domain (e.g., `amesa.com`)
- `-Subdomain`: Subdomain to use (e.g., `www`, `api`, or `@` for root)
- `-IncludeRoot`: Include root domain in certificate (for `www` + root)
- `-CloudFrontDistributionId`: CloudFront distribution ID (default: `E3GU3QXUR43ZOH`)

**Examples:**
```powershell
# Setup www.yourdomain.com
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "www" -IncludeRoot

# Setup api.yourdomain.com
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "api"

# Setup root domain (yourdomain.com)
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "@"
```

### Option 3: Manual Setup (CLI/API)

Follow the steps below for manual configuration using AWS CLI.

## Step-by-Step Manual Setup

### Step 1: Request SSL Certificate in AWS Certificate Manager

**CRITICAL**: CloudFront requires SSL certificates to be in the **us-east-1** region. Certificates in other regions cannot be used with CloudFront.

**Reference**: [AWS Documentation - Certificate Requirements](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html#https-requirements-aws-region)

1. **Navigate to AWS Certificate Manager** (us-east-1 region):
   ```bash
   aws acm request-certificate \
     --domain-name www.yourdomain.com \
     --subject-alternative-names yourdomain.com \
     --validation-method DNS \
     --region us-east-1
   ```

2. **Get validation records**:
   ```bash
   CERT_ARN="arn:aws:acm:us-east-1:ACCOUNT:certificate/ID"
   aws acm describe-certificate \
     --certificate-arn $CERT_ARN \
     --region us-east-1 \
     --query "Certificate.DomainValidationOptions[*].[DomainName,ResourceRecord.Name,ResourceRecord.Value,ResourceRecord.Type]"
   ```

3. **Add CNAME records to GoDaddy DNS**:
   - Go to GoDaddy DNS Management
   - Add CNAME records for each validation domain
   - Wait for validation (usually 5-30 minutes)

4. **Verify certificate status**:
   ```bash
   aws acm describe-certificate \
     --certificate-arn $CERT_ARN \
     --region us-east-1 \
     --query "Certificate.Status"
   ```
   Status should be `ISSUED` before proceeding.

### Step 2: Update CloudFront Distribution

1. **Get current distribution config**:
   ```bash
   aws cloudfront get-distribution-config \
     --id E3GU3QXUR43ZOH \
     --region us-east-1 \
     --output json > cloudfront-config.json
   ```

2. **Edit the config**:
   - Set `Aliases.Items` to your domain(s): `["www.yourdomain.com", "yourdomain.com"]`
   - **Important**: CloudFront supports up to 5 alternate domain names per distribution
   - Update `ViewerCertificate`:
     ```json
     {
       "CloudFrontDefaultCertificate": false,
       "ACMCertificateArn": "arn:aws:acm:us-east-1:ACCOUNT:certificate/ID",
       "SSLSupportMethod": "sni-only",  // Recommended: no additional cost
       "MinimumProtocolVersion": "TLSv1.2_2021",  // Recommended: secure
       "CertificateSource": "acm"
     }
     ```
   - **SSLSupportMethod**: 
     - `"sni-only"` (recommended): Server Name Indication, no additional cost
     - `"vip"` (legacy): Dedicated IP, additional cost
   - **MinimumProtocolVersion**: 
     - `"TLSv1.2_2021"` (recommended): Most secure
     - `"TLSv1"` (legacy): Less secure, not recommended

3. **Update distribution**:
   ```bash
   ETAG=$(aws cloudfront get-distribution-config \
     --id E3GU3QXUR43ZOH \
     --region us-east-1 \
     --query "ETag" --output text)
   
   aws cloudfront update-distribution \
     --id E3GU3QXUR43ZOH \
     --distribution-config file://cloudfront-config.json \
     --if-match $ETAG \
     --region us-east-1
   ```

4. **Wait for deployment** (15-30 minutes):
   ```bash
   aws cloudfront get-distribution \
     --id E3GU3QXUR43ZOH \
     --region us-east-1 \
     --query "Distribution.Status"
   ```
   Status should be `Deployed` before DNS changes.

### Step 3: Configure DNS in GoDaddy

#### For Subdomain (e.g., www.yourdomain.com):

1. Go to GoDaddy DNS Management
2. Add CNAME record:
   - **Type**: CNAME
   - **Name**: `www` (or your subdomain)
   - **Value**: `dpqbvdgnenckf.cloudfront.net`
   - **TTL**: 3600 (or default)

#### For Root Domain (yourdomain.com):

**Option A: Use A Record (Alias)** - Recommended if GoDaddy supports it:
- **Type**: A (Alias)
- **Name**: `@` (or leave blank)
- **Value**: `dpqbvdgnenckf.cloudfront.net`
- **TTL**: 3600

**Option B: Use CNAME** - If GoDaddy supports root CNAME:
- **Type**: CNAME
- **Name**: `@` (or leave blank)
- **Value**: `dpqbvdgnenckf.cloudfront.net`
- **TTL**: 3600

**Note**: Some registrars don't support CNAME at root. In that case, use Route 53 (see Option 3 below).

### Step 4: Verify Setup

1. **Wait for DNS propagation** (5-60 minutes)
2. **Test your domain**:
   ```bash
   curl -I https://www.yourdomain.com
   ```
3. **Check SSL certificate**:
   ```bash
   openssl s_client -connect www.yourdomain.com:443 -servername www.yourdomain.com
   ```

## Option 3: Use Route 53 (AWS Native DNS)

If you prefer to manage DNS in AWS:

### Step 1: Create Hosted Zone

```bash
aws route53 create-hosted-zone \
  --name yourdomain.com \
  --caller-reference $(date +%s)
```

### Step 2: Update Nameservers in GoDaddy

1. Get Route 53 nameservers:
   ```bash
   aws route53 get-hosted-zone \
     --id /hostedzone/ZONE_ID \
     --query "DelegationSet.NameServers"
   ```

2. Update nameservers in GoDaddy:
   - Go to GoDaddy Domain Settings
   - Update nameservers to Route 53 nameservers

### Step 3: Create DNS Records in Route 53

```bash
# For subdomain
aws route53 change-resource-record-sets \
  --hosted-zone-id ZONE_ID \
  --change-batch '{
    "Changes": [{
      "Action": "CREATE",
      "ResourceRecordSet": {
        "Name": "www.yourdomain.com",
        "Type": "CNAME",
        "TTL": 300,
        "ResourceRecords": [{"Value": "dpqbvdgnenckf.cloudfront.net"}]
      }
    }]
  }'

# For root domain (Alias)
aws route53 change-resource-record-sets \
  --hosted-zone-id ZONE_ID \
  --change-batch '{
    "Changes": [{
      "Action": "CREATE",
      "ResourceRecordSet": {
        "Name": "yourdomain.com",
        "Type": "A",
        "AliasTarget": {
          "HostedZoneId": "Z2FDTNDATAQYW2",
          "DNSName": "dpqbvdgnenckf.cloudfront.net",
          "EvaluateTargetHealth": false
        }
      }
    }]
  }'
```

## Common Domain Configurations

### Configuration 1: Single Domain (www.yourdomain.com)

```powershell
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "www"
```

**DNS Records:**
- CNAME: `www` → `dpqbvdgnenckf.cloudfront.net`

### Configuration 2: Root + www (yourdomain.com + www.yourdomain.com)

```powershell
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "www" -IncludeRoot
```

**DNS Records:**
- A (Alias): `@` → `dpqbvdgnenckf.cloudfront.net`
- CNAME: `www` → `dpqbvdgnenckf.cloudfront.net`

### Configuration 3: API Subdomain (api.yourdomain.com)

```powershell
.\setup-custom-domain.ps1 -DomainName "yourdomain.com" -Subdomain "api"
```

**DNS Records:**
- CNAME: `api` → `dpqbvdgnenckf.cloudfront.net`

**Note**: If you want separate domains for frontend and API, you'll need separate CloudFront distributions or use path-based routing (current setup).

## Updating Application Configuration

After setting up the custom domain, update your application configuration:

### Backend Services

Search for hardcoded URLs:
```bash
grep -r "dpqbvdgnenckf.cloudfront.net" BE/
grep -r "amesa-backend-alb-509078867" BE/
```

Update to use your custom domain or environment variables.

### Frontend

Update API base URLs in Angular:
```bash
grep -r "dpqbvdgnenckf.cloudfront.net" FE/
```

Update `environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://www.yourdomain.com/api/v1',
  // or
  apiUrl: 'https://api.yourdomain.com/api/v1',
  // ...
};
```

### CORS Configuration

Update CORS allowed origins in backend services:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://www.yourdomain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

### OAuth Redirect URIs

Update OAuth redirect URIs in:
- Google OAuth Console
- Meta/Facebook Developer Console
- AWS Secrets Manager (if stored there)

Example:
- Old: `https://dpqbvdgnenckf.cloudfront.net/api/v1/oauth/google-callback`
- New: `https://www.yourdomain.com/api/v1/oauth/google-callback`

## Troubleshooting

### Certificate Validation Fails

1. **Check DNS records are correct**:
   ```bash
   dig CNAME _validation-record-name.yourdomain.com
   ```

2. **Verify record values match ACM**:
   ```bash
   aws acm describe-certificate \
     --certificate-arn $CERT_ARN \
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
   dig www.yourdomain.com
   nslookup www.yourdomain.com
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
     --certificate-arn $CERT_ARN \
     --region us-east-1 \
     --query "Certificate.Status"
   ```

2. **Check certificate is in us-east-1**: CloudFront requires us-east-1
3. **Verify domain matches**: Certificate domain must match CloudFront alias

## Important Notes

### Certificate Requirements
- **Certificate Region**: CloudFront requires certificates in `us-east-1` (US East N. Virginia) - **CRITICAL**
  - **Why?** CloudFront is a global service with centralized certificate management in `us-east-1`
  - **Impact?** None - your app stays in `eu-north-1`, only certificate metadata is in `us-east-1`
  - **Performance?** No impact - certificates are replicated to edge locations globally
  - See [Why us-east-1 Certificates](./WHY_US_EAST_1_CERTIFICATES.md) for detailed explanation
- **Certificate Format**: X.509 PEM format (default for ACM)
- **Key Types**: Supports RSA and ECDSA
- **Certificate Limit**: CloudFront supports up to 5 alternate domain names per distribution
- **Auto-Renewal**: ACM certificates auto-renew (no action needed)

### Deployment & Timing
- **CloudFront Deployment**: Updates take 15-30 minutes to deploy
- **DNS Propagation**: Can take 5-60 minutes (usually faster)
- **Certificate Validation**: Usually 5-30 minutes after DNS records are added
- **HTTPS Only**: CloudFront redirects HTTP to HTTPS automatically

### SSL Configuration
- **SSLSupportMethod**: `sni-only` (recommended, no additional cost) or `vip` (legacy, additional cost)
- **MinimumProtocolVersion**: `TLSv1.2_2021` (recommended) or `TLSv1` (legacy)

### Previous Attempts
- Old certificates for `amesa.com` and `amesa-group.com` failed - start fresh with new certificate request

**Reference**: [AWS Documentation - SSL/TLS Certificate Requirements](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html)

## Cost Considerations

- **ACM Certificate**: Free
- **CloudFront Custom Domain**: No additional cost
- **SNI-only SSL**: No additional cost (recommended)
- **VIP SSL**: Additional cost (legacy, not recommended)
- **Route 53** (if used): $0.50/month per hosted zone + $0.40/million queries
- **Route 53 Alias Queries**: Free (no charge for alias queries to CloudFront)
- **GoDaddy DNS**: Free (included with domain)

## Security Best Practices

1. **Use HTTPS only**: CloudFront redirects HTTP to HTTPS
2. **Minimum TLS version**: Use TLS 1.2 or higher
3. **Certificate validation**: Always use DNS validation (not email)
4. **Certificate renewal**: ACM auto-renews certificates (no action needed)

## References

- [AWS Certificate Manager Documentation](https://docs.aws.amazon.com/acm/)
- [CloudFront Custom Domain Setup](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CNAMEs.html)
- [Route 53 DNS Management](https://docs.aws.amazon.com/route53/)
- [GoDaddy DNS Management](https://www.godaddy.com/help/manage-dns-680)

## Support

If you encounter issues:
1. Check AWS CloudWatch logs
2. Verify DNS records with `dig` or `nslookup`
3. Check CloudFront distribution status
4. Review ACM certificate validation status
