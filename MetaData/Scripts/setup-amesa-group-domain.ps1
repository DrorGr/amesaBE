# Quick Setup Script for amesa-group.net
# This script sets up the custom domain for AmesaBase CloudFront distribution

param(
    [Parameter(Mandatory=$false)]
    [switch]$IncludeWWW = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeRoot = $true
)

$DomainName = "amesa-group.net"
$CloudFrontDistributionId = "E3GU3QXUR43ZOH"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setting up amesa-group.net for AmesaBase" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Determine domains to include
$domainsToInclude = @()
if ($IncludeRoot) {
    $domainsToInclude += $DomainName
    Write-Host "Including root domain: $DomainName" -ForegroundColor Green
}
if ($IncludeWWW) {
    $domainsToInclude += "www.$DomainName"
    Write-Host "Including www subdomain: www.$DomainName" -ForegroundColor Green
}

if ($domainsToInclude.Count -eq 0) {
    Write-Host "ERROR: At least one domain must be included" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Domains to configure: $($domainsToInclude -join ', ')" -ForegroundColor Yellow
Write-Host ""

# Step 1: Request SSL Certificate
Write-Host "Step 1: Requesting SSL Certificate in ACM (us-east-1)..." -ForegroundColor Yellow

$certificateArn = $null
$primaryDomain = $domainsToInclude[0]
$sanDomains = $domainsToInclude | Select-Object -Skip 1

# Check for existing certificate
$existingCert = aws acm list-certificates --region us-east-1 --query "CertificateSummaryList[?DomainName=='$primaryDomain' || DomainName=='www.$DomainName' || DomainName=='$DomainName'].CertificateArn" --output text

if ($existingCert) {
    Write-Host "  Found existing certificate(s):" -ForegroundColor Yellow
    $existingCert | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
    
    $certStatus = aws acm describe-certificate --certificate-arn $existingCert --region us-east-1 --query "Certificate.Status" --output text 2>$null
    if ($certStatus -eq "ISSUED") {
        Write-Host "  Certificate Status: ISSUED" -ForegroundColor Green
        $certificateArn = $existingCert
    } else {
        Write-Host "  Certificate Status: $certStatus" -ForegroundColor Yellow
        Write-Host "  Will request new certificate..." -ForegroundColor Gray
    }
}

if (-not $certificateArn) {
    Write-Host "  Requesting new certificate..." -ForegroundColor Gray
    
    $certCmd = "aws acm request-certificate --domain-name $primaryDomain --validation-method DNS --region us-east-1"
    
    if ($sanDomains.Count -gt 0) {
        $sanList = ($sanDomains | ForEach-Object { "`"$_`"" }) -join ","
        $certCmd += " --subject-alternative-names $($sanList -replace ' ', '')"
    }
    
    Write-Host "  Command: $certCmd" -ForegroundColor Gray
    $certResponse = Invoke-Expression $certCmd | ConvertFrom-Json
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ERROR: Failed to request certificate" -ForegroundColor Red
        exit 1
    }
    
    $certificateArn = $certResponse.CertificateArn
    Write-Host "  Certificate requested: $certificateArn" -ForegroundColor Green
    Write-Host ""
    
    # Get validation records
    Write-Host "  Waiting for validation records..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "DNS VALIDATION RECORDS - ADD TO GODADDY" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Go to: https://dcc.godaddy.com/manage/amesa-group.net/dns" -ForegroundColor Yellow
    Write-Host ""
    
    $validationRecords = aws acm describe-certificate `
        --certificate-arn $certificateArn `
        --region us-east-1 `
        --query "Certificate.DomainValidationOptions[*].[DomainName,ResourceRecord.Name,ResourceRecord.Value,ResourceRecord.Type]" `
        --output json | ConvertFrom-Json
    
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
    Write-Host "1. Click the 'DNS' tab in GoDaddy" -ForegroundColor White
    Write-Host "2. Click 'Add' to create a new record" -ForegroundColor White
    Write-Host "3. Add each CNAME record shown above" -ForegroundColor White
    Write-Host "4. Wait for validation (usually 5-30 minutes)" -ForegroundColor White
    Write-Host "5. Run this script again to continue" -ForegroundColor White
    Write-Host ""
    
    $continue = Read-Host "Have you added the DNS records? Continue with CloudFront update? (y/n)"
    if ($continue -ne "y") {
        Write-Host ""
        Write-Host "Certificate ARN: $certificateArn" -ForegroundColor Green
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

# Step 2: Update CloudFront Distribution
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

# Update aliases
$config.Aliases.Quantity = $domainsToInclude.Count
$config.Aliases.Items = $domainsToInclude

# Update viewer certificate
$config.ViewerCertificate = @{
    CloudFrontDefaultCertificate = $false
    ACMCertificateArn = $certificateArn
    SSLSupportMethod = "sni-only"
    MinimumProtocolVersion = "TLSv1.2_2021"
    CertificateSource = "acm"
}

# Save config
$configJson = $config | ConvertTo-Json -Depth 10
$configJson | Out-File -FilePath "cloudfront-amesa-group-config.json" -Encoding utf8

Write-Host "  Updated config saved to: cloudfront-amesa-group-config.json" -ForegroundColor Gray
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
    --distribution-config file://cloudfront-amesa-group-config.json `
    --if-match $etag `
    --region us-east-1 `
    --output json | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "  ERROR: Failed to update CloudFront distribution" -ForegroundColor Red
    exit 1
}

Write-Host "  CloudFront distribution update initiated!" -ForegroundColor Green
Write-Host ""

# Step 3: DNS Configuration Instructions
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DNS CONFIGURATION - ADD TO GODADDY" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Go to: https://dcc.godaddy.com/manage/amesa-group.net/dns" -ForegroundColor Yellow
Write-Host ""

$cloudfrontDomain = "dpqbvdgnenckf.cloudfront.net"

if ($IncludeRoot) {
    Write-Host "For ROOT domain (amesa-group.net):" -ForegroundColor Yellow
    Write-Host "  Type: A (Alias) or CNAME" -ForegroundColor White
    Write-Host "  Name: @ (or leave blank)" -ForegroundColor White
    Write-Host "  Value: $cloudfrontDomain" -ForegroundColor White
    Write-Host "  TTL: 3600" -ForegroundColor White
    Write-Host ""
}

if ($IncludeWWW) {
    Write-Host "For WWW subdomain (www.amesa-group.net):" -ForegroundColor Yellow
    Write-Host "  Type: CNAME" -ForegroundColor White
    Write-Host "  Name: www" -ForegroundColor White
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
Write-Host "4. Test your domain:" -ForegroundColor White
if ($IncludeRoot) {
    Write-Host "   - https://amesa-group.net" -ForegroundColor Gray
}
if ($IncludeWWW) {
    Write-Host "   - https://www.amesa-group.net" -ForegroundColor Gray
}
Write-Host ""
Write-Host "Certificate ARN: $certificateArn" -ForegroundColor Gray
Write-Host "CloudFront Distribution: $CloudFrontDistributionId" -ForegroundColor Gray
Write-Host ""
