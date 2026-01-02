# Custom Domain Setup Script for AmesaBase
# This script helps configure a custom domain for CloudFront distribution
# Based on AWS Documentation: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/CreatingCNAME.html
# 
# Prerequisites:
# 1. Domain purchased from GoDaddy (or any registrar)
# 2. AWS CLI configured with appropriate permissions:
#    - acm:RequestCertificate, acm:DescribeCertificate
#    - cloudfront:GetDistribution, cloudfront:UpdateDistribution
# 3. Access to domain DNS management
#
# Important Notes:
# - ACM certificates for CloudFront MUST be in us-east-1 region
# - CloudFront supports up to 5 alternate domain names per distribution
# - Minimum TLS version: TLSv1.2_2021 (recommended)
# - CloudFront console can automatically create certificates (alternative to this script)

param(
    [Parameter(Mandatory=$true)]
    [string]$DomainName,
    
    [Parameter(Mandatory=$false)]
    [string]$Subdomain = "www",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeRoot = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$CloudFrontDistributionId = "E3GU3QXUR43ZOH",
    
    [Parameter(Mandatory=$false)]
    [switch]$UseCloudFrontConsole = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AmesaBase Custom Domain Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Validate domain format
if ($DomainName -notmatch '^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*\.[a-zA-Z]{2,}$') {
    Write-Host "ERROR: Invalid domain name format: $DomainName" -ForegroundColor Red
    exit 1
}

$fullDomain = if ($Subdomain -eq "@" -or $Subdomain -eq "") { $DomainName } else { "$Subdomain.$DomainName" }
Write-Host "Target Domain: $fullDomain" -ForegroundColor Green
Write-Host "CloudFront Distribution: $CloudFrontDistributionId" -ForegroundColor Green
Write-Host ""

# Step 1: Request SSL Certificate in ACM (us-east-1 for CloudFront)
Write-Host "Step 1: Requesting SSL Certificate in ACM (us-east-1)..." -ForegroundColor Yellow

$certificateDomains = @($fullDomain)
if ($IncludeRoot) {
    $certificateDomains += $DomainName
    Write-Host "  Including root domain: $DomainName" -ForegroundColor Gray
}

$certificateArn = $null
$existingCert = aws acm list-certificates --region us-east-1 --query "CertificateSummaryList[?DomainName=='$fullDomain' || DomainName=='$DomainName'].CertificateArn" --output text

if ($existingCert) {
    Write-Host "  Found existing certificate: $existingCert" -ForegroundColor Yellow
    $certStatus = aws acm describe-certificate --certificate-arn $existingCert --region us-east-1 --query "Certificate.Status" --output text
    Write-Host "  Certificate Status: $certStatus" -ForegroundColor $(if ($certStatus -eq "ISSUED") { "Green" } else { "Yellow" })
    
    if ($certStatus -eq "ISSUED") {
        $certificateArn = $existingCert
        Write-Host "  Using existing certificate" -ForegroundColor Green
    } else {
        Write-Host "  WARNING: Existing certificate is not issued. You may need to validate it." -ForegroundColor Yellow
        Write-Host "  Certificate ARN: $existingCert" -ForegroundColor Gray
        $useExisting = Read-Host "  Use this certificate? (y/n)"
        if ($useExisting -eq "y") {
            $certificateArn = $existingCert
        }
    }
}

if (-not $certificateArn) {
    Write-Host "  Requesting new certificate..." -ForegroundColor Gray
    
    $certRequestJson = @{
        DomainName = $fullDomain
        ValidationMethod = "DNS"
    } | ConvertTo-Json
    
    if ($certificateDomains.Count -gt 1) {
        $certRequestJson = @{
            DomainName = $fullDomain
            SubjectAlternativeNames = @($DomainName)
            ValidationMethod = "DNS"
        } | ConvertTo-Json
    }
    
    $certRequestJson | Out-File -FilePath "cert-request.json" -Encoding utf8
    
    Write-Host "  Creating certificate request..." -ForegroundColor Gray
    $certResponse = aws acm request-certificate `
        --domain-name $fullDomain `
        --validation-method DNS `
        --region us-east-1 `
        --output json
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: Failed to request certificate" -ForegroundColor Red
        exit 1
    }
    
    $certificateArn = ($certResponse | ConvertFrom-Json).CertificateArn
    Write-Host "  Certificate requested: $certificateArn" -ForegroundColor Green
    Write-Host ""
    
    # Get validation records
    Write-Host "  Waiting for validation records..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    $validationRecords = aws acm describe-certificate `
        --certificate-arn $certificateArn `
        --region us-east-1 `
        --query "Certificate.DomainValidationOptions[*].[DomainName,ResourceRecord.Name,ResourceRecord.Value,ResourceRecord.Type]" `
        --output json | ConvertFrom-Json
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "DNS VALIDATION RECORDS - ADD TO GODADDY" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    
    for ($i = 0; $i -lt $validationRecords.Count; $i += 4) {
        $domain = $validationRecords[$i]
        $recordName = $validationRecords[$i + 1]
        $recordValue = $validationRecords[$i + 2]
        $recordType = $validationRecords[$i + 3]
        
        Write-Host "Domain: $domain" -ForegroundColor Yellow
        Write-Host "  Type: $recordType" -ForegroundColor White
        Write-Host "  Name: $recordName" -ForegroundColor White
        Write-Host "  Value: $recordValue" -ForegroundColor White
        Write-Host ""
    }
    
    Write-Host "Instructions:" -ForegroundColor Cyan
    Write-Host "1. Go to GoDaddy DNS Management" -ForegroundColor White
    Write-Host "2. Add the CNAME records shown above" -ForegroundColor White
    Write-Host "3. Wait for validation (usually 5-30 minutes)" -ForegroundColor White
    Write-Host "4. Run this script again with the same parameters to continue" -ForegroundColor White
    Write-Host ""
    
    $continue = Read-Host "Have you added the DNS records? Continue with CloudFront update? (y/n)"
    if ($continue -ne "y") {
        Write-Host "Certificate ARN saved: $certificateArn" -ForegroundColor Green
        Write-Host "Run this script again after DNS validation completes." -ForegroundColor Yellow
        exit 0
    }
    
    # Wait for certificate validation
    Write-Host "  Waiting for certificate validation..." -ForegroundColor Yellow
    $maxWait = 60 # 60 minutes
    $waitCount = 0
    
    do {
        Start-Sleep -Seconds 30
        $status = aws acm describe-certificate --certificate-arn $certificateArn --region us-east-1 --query "Certificate.Status" --output text
        Write-Host "  Status: $status (waiting...)" -ForegroundColor Gray
        
        $waitCount++
        if ($waitCount -ge $maxWait) {
            Write-Host "  WARNING: Certificate validation taking longer than expected" -ForegroundColor Yellow
            Write-Host "  You can continue manually after validation completes" -ForegroundColor Yellow
            break
        }
    } while ($status -ne "ISSUED")
    
    if ($status -eq "ISSUED") {
        Write-Host "  Certificate validated successfully!" -ForegroundColor Green
    }
}

# Step 2: Get current CloudFront distribution config
Write-Host ""
Write-Host "Step 2: Updating CloudFront Distribution..." -ForegroundColor Yellow

Write-Host "  Fetching current distribution config..." -ForegroundColor Gray
$distConfig = aws cloudfront get-distribution-config --id $CloudFrontDistributionId --region us-east-1 --output json | ConvertFrom-Json

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Failed to get CloudFront distribution config" -ForegroundColor Red
    exit 1
}

$etag = $distConfig.ETag
$config = $distConfig.DistributionConfig

# Step 3: Update CloudFront with custom domain
Write-Host "  Updating distribution with custom domain..." -ForegroundColor Gray

# Add alias
$aliases = @($fullDomain)
if ($IncludeRoot) {
    $aliases += $DomainName
}

$config.Aliases.Quantity = $aliases.Count
$config.Aliases.Items = $aliases

# Update viewer certificate
# Based on AWS docs: https://docs.aws.amazon.com/AmazonCloudFront/latest/DeveloperGuide/cnames-and-https-requirements.html
# SSLSupportMethod: "sni-only" (recommended) or "vip" (legacy, requires dedicated IP)
# MinimumProtocolVersion: "TLSv1.2_2021" (recommended) or "TLSv1" (legacy)
$config.ViewerCertificate = @{
    CloudFrontDefaultCertificate = $false
    ACMCertificateArn = $certificateArn
    SSLSupportMethod = "sni-only"  # SNI-only is recommended (no additional cost)
    MinimumProtocolVersion = "TLSv1.2_2021"  # Recommended minimum TLS version
    CertificateSource = "acm"
}

# Save config to file
$configJson = $config | ConvertTo-Json -Depth 10
$configJson | Out-File -FilePath "cloudfront-updated-config.json" -Encoding utf8

Write-Host "  Updated config saved to: cloudfront-updated-config.json" -ForegroundColor Gray
Write-Host ""
Write-Host "  WARNING: CloudFront distribution updates can take 15-30 minutes to deploy" -ForegroundColor Yellow
Write-Host ""

$proceed = Read-Host "Proceed with CloudFront update? (y/n)"
if ($proceed -ne "y") {
    Write-Host "Config file saved. Update manually when ready." -ForegroundColor Yellow
    exit 0
}

# Update distribution
Write-Host "  Updating CloudFront distribution..." -ForegroundColor Gray
aws cloudfront update-distribution `
    --id $CloudFrontDistributionId `
    --distribution-config file://cloudfront-updated-config.json `
    --if-match $etag `
    --region us-east-1 `
    --output json | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Failed to update CloudFront distribution" -ForegroundColor Red
    exit 1
}

Write-Host "  CloudFront distribution update initiated!" -ForegroundColor Green
Write-Host ""

# Step 4: DNS Configuration Instructions
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DNS CONFIGURATION - ADD TO GODADDY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$cloudfrontDomain = aws cloudfront get-distribution --id $CloudFrontDistributionId --region us-east-1 --query "Distribution.DomainName" --output text

if ($Subdomain -eq "@" -or $Subdomain -eq "") {
    # Root domain
    Write-Host "For ROOT domain ($DomainName):" -ForegroundColor Yellow
    Write-Host "  Type: A (Alias)" -ForegroundColor White
    Write-Host "  Name: @ (or leave blank)" -ForegroundColor White
    Write-Host "  Value: $cloudfrontDomain" -ForegroundColor White
    Write-Host "  TTL: 3600" -ForegroundColor White
    Write-Host ""
    Write-Host "  OR use CNAME (if GoDaddy supports root CNAME):" -ForegroundColor Gray
    Write-Host "  Type: CNAME" -ForegroundColor White
    Write-Host "  Name: @ (or leave blank)" -ForegroundColor White
    Write-Host "  Value: $cloudfrontDomain" -ForegroundColor White
    Write-Host ""
} else {
    # Subdomain
    Write-Host "For SUBDOMAIN ($fullDomain):" -ForegroundColor Yellow
    Write-Host "  Type: CNAME" -ForegroundColor White
    Write-Host "  Name: $Subdomain" -ForegroundColor White
    Write-Host "  Value: $cloudfrontDomain" -ForegroundColor White
    Write-Host "  TTL: 3600" -ForegroundColor White
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SETUP COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Add the DNS records shown above to GoDaddy" -ForegroundColor White
Write-Host "2. Wait for DNS propagation (usually 5-60 minutes)" -ForegroundColor White
Write-Host "3. Wait for CloudFront deployment (15-30 minutes)" -ForegroundColor White
Write-Host "4. Test your domain: https://$fullDomain" -ForegroundColor White
Write-Host ""
Write-Host "Certificate ARN: $certificateArn" -ForegroundColor Gray
Write-Host "CloudFront Distribution: $CloudFrontDistributionId" -ForegroundColor Gray
Write-Host ""
