# CloudFront Cache Invalidation Script
# Invalidates the frontend CloudFront distribution cache

param(
    [string]$DistributionId = "E3GU3QXUR43ZOH",
    [string]$Paths = "/*"
)

Write-Host "Invalidating CloudFront cache for distribution: $DistributionId" -ForegroundColor Cyan
Write-Host "Paths: $Paths" -ForegroundColor Yellow

try {
    # Create invalidation
    $invalidation = aws cloudfront create-invalidation `
        --distribution-id $DistributionId `
        --paths $Paths `
        --output json

    if ($LASTEXITCODE -eq 0) {
        $invalidationObj = $invalidation | ConvertFrom-Json
        $invalidationId = $invalidationObj.Invalidation.Id
        
        Write-Host "`n✅ Cache invalidation created successfully!" -ForegroundColor Green
        Write-Host "Invalidation ID: $invalidationId" -ForegroundColor Green
        Write-Host "Status: $($invalidationObj.Invalidation.Status)" -ForegroundColor Green
        Write-Host "`nThe cache will be cleared within a few minutes." -ForegroundColor Yellow
        Write-Host "You can check status with:" -ForegroundColor Yellow
        Write-Host "  aws cloudfront get-invalidation --distribution-id $DistributionId --id $invalidationId" -ForegroundColor Gray
    } else {
        Write-Host "`n❌ Failed to create invalidation" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "`n❌ Error: $_" -ForegroundColor Red
    Write-Host "`nMake sure AWS CLI is installed and configured:" -ForegroundColor Yellow
    Write-Host "  aws configure" -ForegroundColor Gray
    exit 1
}
