# Add JWT Secret to Lottery Service Task Definition
# This script updates the amesa-lottery-service task definition to include JWT secret from SSM

param(
    [string]$Region = "eu-north-1",
    [string]$Cluster = "Amesa",
    [string]$ServiceName = "amesa-lottery-service"
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Adding JWT Secret to Lottery Service" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Get current task definition
Write-Host "Step 1: Getting current task definition..." -ForegroundColor Yellow
$tdArn = aws ecs describe-services --region $Region --cluster $Cluster --services $ServiceName --query "services[0].taskDefinition" --output text

if (-not $tdArn -or $tdArn -eq "None") {
    Write-Host "❌ Error: Service not found or no task definition: $ServiceName" -ForegroundColor Red
    exit 1
}

Write-Host "  Current Task Definition: $tdArn" -ForegroundColor Gray

# Get full task definition
$td = aws ecs describe-task-definition --region $Region --task-definition $tdArn --query "taskDefinition" | ConvertFrom-Json

# Remove read-only fields
$td.PSObject.Properties.Remove('taskDefinitionArn')
$td.PSObject.Properties.Remove('revision')
$td.PSObject.Properties.Remove('status')
$td.PSObject.Properties.Remove('requiresAttributes')
$td.PSObject.Properties.Remove('compatibilities')
$td.PSObject.Properties.Remove('registeredAt')
$td.PSObject.Properties.Remove('registeredBy')
$td.PSObject.Properties.Remove('inferenceAccelerators')

# Ensure secrets array exists
if (-not $td.containerDefinitions[0].secrets) {
    $td.containerDefinitions[0] | Add-Member -Name secrets -MemberType NoteProperty -Value @()
}

# Check if JWT secret already exists
$hasJwtSecret = $td.containerDefinitions[0].secrets | Where-Object { $_.name -eq "JwtSettings__SecretKey" }
$hasDbSecret = $td.containerDefinitions[0].secrets | Where-Object { $_.name -eq "ConnectionStrings__DefaultConnection" }

# Add JWT secret if not present
if (-not $hasJwtSecret) {
    Write-Host ""
    Write-Host "Step 2: Adding JWT secret..." -ForegroundColor Yellow
    $td.containerDefinitions[0].secrets += @{
        name = "JwtSettings__SecretKey"
        valueFrom = "/amesa/prod/JwtSettings/SecretKey"
    }
    Write-Host "  ✅ Added JwtSettings__SecretKey from /amesa/prod/JwtSettings/SecretKey" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Step 2: JWT secret already configured" -ForegroundColor Green
    Write-Host "  Secret: $($hasJwtSecret.name) from $($hasJwtSecret.valueFrom)" -ForegroundColor Gray
}

# Add database connection string if not present
if (-not $hasDbSecret) {
    Write-Host ""
    Write-Host "Step 3: Adding database connection string..." -ForegroundColor Yellow
    $td.containerDefinitions[0].secrets += @{
        name = "ConnectionStrings__DefaultConnection"
        valueFrom = "/amesa/prod/ConnectionStrings/Lottery"
    }
    Write-Host "  ✅ Added ConnectionStrings__DefaultConnection from /amesa/prod/ConnectionStrings/Lottery" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Step 3: Database connection string already configured" -ForegroundColor Green
    Write-Host "  Secret: $($hasDbSecret.name) from $($hasDbSecret.valueFrom)" -ForegroundColor Gray
}

# Build registration payload
Write-Host ""
Write-Host "Step 4: Registering updated task definition..." -ForegroundColor Yellow
$payload = [ordered]@{
    family                 = $td.family
    taskRoleArn            = $td.taskRoleArn
    executionRoleArn       = $td.executionRoleArn
    networkMode            = $td.networkMode
    containerDefinitions   = $td.containerDefinitions
    requiresCompatibilities = $td.requiresCompatibilities
    cpu                    = $td.cpu
    memory                 = $td.memory
}
if ($td.volumes) { $payload.volumes = $td.volumes }
if ($td.placementConstraints) { $payload.placementConstraints = $td.placementConstraints }
if ($td.runtimePlatform) { $payload.runtimePlatform = $td.runtimePlatform }

$json = ($payload | ConvertTo-Json -Depth 100 -Compress)
$tmp = [System.IO.Path]::GetTempFileName()
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$sw = New-Object System.IO.StreamWriter($tmp, $false, $utf8NoBom)
$sw.Write($json)
$sw.Close()

# Register new task definition
$newTdArn = aws ecs register-task-definition --region $Region --cli-input-json "file://$tmp" --query "taskDefinition.taskDefinitionArn" --output text

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Task definition registered: $newTdArn" -ForegroundColor Green
} else {
    Write-Host "  ❌ Failed to register task definition" -ForegroundColor Red
    Remove-Item $tmp
    exit 1
}

Remove-Item $tmp

# Update service to use new task definition
Write-Host ""
Write-Host "Step 5: Updating ECS service..." -ForegroundColor Yellow
$updateResult = aws ecs update-service `
    --region $Region `
    --cluster $Cluster `
    --service $ServiceName `
    --task-definition $newTdArn `
    --force-new-deployment `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -eq 0) {
    Write-Host "  ✅ Service update initiated" -ForegroundColor Green
    Write-Host "  Service ARN: $($updateResult.service.serviceArn)" -ForegroundColor Gray
    Write-Host "  New Task Definition: $newTdArn" -ForegroundColor Gray
} else {
    Write-Host "  ❌ Failed to update service" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ JWT Secret configured: JwtSettings__SecretKey" -ForegroundColor Green
Write-Host "✅ Database connection configured: ConnectionStrings__DefaultConnection" -ForegroundColor Green
Write-Host "✅ Task definition updated and service deployment initiated" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Monitor service deployment in AWS Console" -ForegroundColor White
Write-Host "2. Check service logs: aws logs tail /ecs/amesa-lottery-service --follow --region $Region" -ForegroundColor White
Write-Host "3. Verify service starts successfully (should no longer show JWT SecretKey error)" -ForegroundColor White
Write-Host ""

