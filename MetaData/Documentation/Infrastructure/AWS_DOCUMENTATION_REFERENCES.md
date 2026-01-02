# AWS Documentation References for Custom Domain Setup

This document contains key references from AWS official documentation for setting up custom domains with CloudFront.

## Key Documentation Pages

### CloudFront Custom Domain Setup
1. **Use custom URLs by adding alternate domain names (CNAMEs)**
   - URL: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CNAMEs.html
   - Covers: Alternate domain names, CNAME records, wildcards, TLS certificates

2. **Add an alternate domain name**
   - URL: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CreatingCNAME.html
   - Covers: Step-by-step console procedure, automatic certificate creation

3. **Configure alternate domain names and HTTPS**
   - URL: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-procedures.html
   - Covers: Getting certificates, importing certificates, updating CloudFront

4. **Requirements for using SSL/TLS certificates with CloudFront**
   - URL: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html
   - Covers: Certificate requirements, region requirements, key types, protocol versions

### Route 53 Integration
5. **Routing traffic to CloudFront distribution**
   - URL: https://docs.aws.amazon.com/Route53/latest/DeveloperGuide/routing-to-cloudfront-distribution.html
   - Covers: Alias records, root domain support, HTTPS records

## Key Requirements from AWS Documentation

### Certificate Region Requirement
**CRITICAL**: CloudFront requires SSL certificates to be in the **US East (N. Virginia) Region (`us-east-1`)**.

> "To use a certificate in AWS Certificate Manager (ACM) to require HTTPS between viewers and CloudFront, make sure you request (or import) the certificate in the US East (N. Virginia) Region (`us-east-1`)."

**Source**: [Certificate Requirements](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html#https-requirements-aws-region)

### Certificate Format
- **Format**: X.509 PEM format (default for ACM)
- **Key Types**: RSA and ECDSA supported
- **Intermediate Certificates**: Must include all intermediate certificates in chain (not root)

### SSL Support Method
- **SNI-only** (recommended): Server Name Indication, no additional cost
- **VIP** (legacy): Dedicated IP, additional cost

### Minimum TLS Protocol Version
- **TLSv1.2_2021** (recommended): Most secure, recommended by AWS
- **TLSv1** (legacy): Less secure, not recommended

### Domain Name Limits
- **Alternate Domain Names**: Up to 5 per CloudFront distribution
- **Wildcards**: Supported in alternate domain names

### CloudFront Console Automatic Certificate Creation
The CloudFront console can automatically create and configure certificates:

> "For **TLS certificate**, if CloudFront can't find an existing AWS Certificate Manager (ACM) certificate for your domain in your AWS account in the `us-east-1` AWS Region, you can choose to automatically create a certificate or manually create it in ACM."

**Source**: [Add Alternate Domain Name](https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CreatingCNAME.html)

### Route 53 Alias Records
Route 53 alias records are free for CloudFront distributions:

> "Route 53 doesn't charge for alias queries to CloudFront distributions or other AWS resources."

**Source**: [Routing to CloudFront](https://docs.aws.amazon.com/Route53/latest/DeveloperGuide/routing-to-cloudfront-distribution.html)

### DNS Configuration Options

#### For Subdomains (e.g., www.example.com)
- **CNAME record**: Point to CloudFront distribution domain
- **Route 53 Alias**: Can use alias record (free queries)

#### For Root Domain (example.com)
- **Route 53 Alias**: A record with alias target (recommended)
- **CNAME**: Some registrars support root CNAME, but not all
- **GoDaddy**: Typically requires A record or use Route 53

## Best Practices from AWS Documentation

1. **Use ACM Certificates**: Recommended over third-party certificates
2. **Use SNI-only SSL**: No additional cost, recommended
3. **Use TLSv1.2_2021**: Most secure minimum protocol version
4. **Automatic Certificate Creation**: Use CloudFront console for easiest setup
5. **Route 53 Alias Records**: Free and support root domains
6. **Certificate Auto-Renewal**: ACM certificates auto-renew (no action needed)

## Troubleshooting References

- **HTTP 502 Bad Gateway**: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/http-502-bad-gateway.html
- **Troubleshooting Distributions**: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/troubleshooting-distributions.html
- **Remove Alternate Domain Name**: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/alternate-domain-names-remove-domain.html

## Related Services Documentation

- **AWS Certificate Manager User Guide**: https://docs.aws.amazon.com/acm/latest/userguide/
- **Route 53 Developer Guide**: https://docs.aws.amazon.com/Route53/latest/DeveloperGuide/
- **CloudFront Developer Guide**: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/
