# PowerShell script to get Redis endpoint and update secret
# This script checks if Redis exists, gets the endpoint, and updates the secret

param(
    [Parameter(Mandatory=$false)]
    [string]$Region = "eu-north-1",
    
    [Parameter(Mandatory=$false)]
    [string]$SecretName = "/amesa/prod/ConnectionStrings/Redis"
)

$ErrorActionPreference = "Stop"

Write-Host "Setting up Redis connection string secret..." -ForegroundColor Cyan
Write-Host ""

# Check if Redis replication group exists
Write-Host "Checking for existing Redis cluster..." -ForegroundColor Yellow
$redisGroups = aws elasticache describe-replication-groups --region $Region --query "ReplicationGroups[?contains(ReplicationGroupId, 'amesa') || contains(ReplicationGroupId, 'redis')].{Id:ReplicationGroupId,Endpoint:NodeGroups[0].PrimaryEndpoint.Address,Port:NodeGroups[0].PrimaryEndpoint.Port,Status:Status}" --output json | ConvertFrom-Json

if ($redisGroups.Count -eq 0) {
    Write-Host "⚠️  No Redis cluster found!" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "1. Deploy Redis via Terraform:" -ForegroundColor White
    Write-Host "   cd Infrastructure/terraform" -ForegroundColor Gray
    Write-Host "   terraform apply -target=aws_elasticache_replication_group.amesa_redis" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Create manually via AWS Console:" -ForegroundColor White
    Write-Host "   - Go to ElastiCache → Redis clusters → Create" -ForegroundColor Gray
    Write-Host "   - Use subnet group: amesa-redis-subnet-group-prod" -ForegroundColor Gray
    Write-Host "   - Security group: amesa-redis-sg-prod" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Use existing Redis endpoint (if you have one):" -ForegroundColor White
    $manualEndpoint = Read-Host "Enter Redis endpoint (host:port) or press Enter to skip"
    
    if ($manualEndpoint) {
        $redisConnection = $manualEndpoint
    } else {
        Write-Host "Skipping Redis setup. You can run this script again after creating Redis." -ForegroundColor Yellow
        exit 0
    }
} else {
    $redis = $redisGroups[0]
    Write-Host "✅ Found Redis cluster: $($redis.Id)" -ForegroundColor Green
    Write-Host "   Status: $($redis.Status)" -ForegroundColor Gray
    Write-Host "   Endpoint: $($redis.Endpoint):$($redis.Port)" -ForegroundColor Gray
    
    if ($redis.Status -ne "available") {
        Write-Host "⚠️  Warning: Redis cluster is not in 'available' state. It may not be ready yet." -ForegroundColor Yellow
    }
    
    $redisConnection = "$($redis.Endpoint):$($redis.Port)"
}

# Check if secret exists
Write-Host ""
Write-Host "Checking if secret exists..." -ForegroundColor Yellow
$secretExists = aws secretsmanager describe-secret --secret-id $SecretName --region $Region 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Secret exists, updating..." -ForegroundColor Green
    $tempFile = New-TemporaryFile
    Set-Content -Path $tempFile -Value $redisConnection
    aws secretsmanager update-secret --secret-id $SecretName --secret-string file://$tempFile --region $Region | Out-Null
    Remove-Item $tempFile
    Write-Host "✅ Redis connection string updated successfully!" -ForegroundColor Green
} else {
    Write-Host "Creating new secret..." -ForegroundColor Yellow
    $tempFile = New-TemporaryFile
    Set-Content -Path $tempFile -Value $redisConnection
    aws secretsmanager create-secret --name $SecretName --description "Redis connection string for AmesaBackend microservices" --secret-string file://$tempFile --region $Region | Out-Null
    Remove-Item $tempFile
    Write-Host "✅ Redis connection string secret created successfully!" -ForegroundColor Green
}

Write-Host ""
Write-Host "Redis Connection String: $redisConnection" -ForegroundColor Cyan
Write-Host "Secret Name: $SecretName" -ForegroundColor Cyan









