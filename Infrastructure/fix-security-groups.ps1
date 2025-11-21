# Fix Security Groups: Separate ALB and ECS Groups
# Task 3: Create separate security groups with explicit rules

param(
    [string]$Region = "eu-north-1",
    [string]$VPCId = "vpc-0faeeb78eded33ccf"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Task 3: Fixing Security Groups ===" -ForegroundColor Cyan
Write-Host ""

# Current security group (shared)
$currentSg = "sg-05c7257248728c160"

Write-Host "Current setup: ALB and ECS using same security group: $currentSg" -ForegroundColor Yellow
Write-Host ""

# Check if we should create separate groups
$createSeparate = Read-Host "Create separate security groups for ALB and ECS? (yes/no)"

if ($createSeparate -ne "yes") {
    Write-Host "Adding explicit rule to current security group..." -ForegroundColor Yellow
    
    # Add explicit rule for ALB -> ECS on port 8080
    Write-Host "Adding rule: Allow $currentSg -> $currentSg on port 8080" -ForegroundColor Cyan
    
    try {
        aws ec2 authorize-security-group-ingress `
            --region $Region `
            --group-id $currentSg `
            --protocol tcp `
            --port 8080 `
            --source-group $currentSg 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Rule added" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Rule may already exist" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "⚠️  Error: $_" -ForegroundColor Yellow
    }
    
    return
}

Write-Host "Creating separate security groups..." -ForegroundColor Cyan

# Create ALB security group
Write-Host "`n1. Creating ALB security group..." -ForegroundColor Yellow
$albSgName = "amesa-alb-sg"
$albSgDesc = "Security group for Amesa ALB - allows HTTP/HTTPS from internet"

$albSgId = aws ec2 create-security-group `
    --region $Region `
    --group-name $albSgName `
    --description $albSgDesc `
    --vpc-id $VPCId `
    --query "GroupId" `
    --output text 2>&1

if ($LASTEXITCODE -ne 0) {
    # May already exist
    $albSgId = aws ec2 describe-security-groups --region $Region --filters "Name=group-name,Values=$albSgName" "Name=vpc-id,Values=$VPCId" --query "SecurityGroups[0].GroupId" --output text
    Write-Host "  Using existing: $albSgId" -ForegroundColor Yellow
} else {
    Write-Host "  ✅ Created: $albSgId" -ForegroundColor Green
}

# Add rules to ALB SG
Write-Host "  Adding rules to ALB security group..." -ForegroundColor Cyan
aws ec2 authorize-security-group-ingress --region $Region --group-id $albSgId --protocol tcp --port 80 --cidr 0.0.0.0/0 --description "HTTP from internet" 2>&1 | Out-Null
aws ec2 authorize-security-group-ingress --region $Region --group-id $albSgId --protocol tcp --port 443 --cidr 0.0.0.0/0 --description "HTTPS from internet" 2>&1 | Out-Null
Write-Host "  ✅ ALB rules added" -ForegroundColor Green

# Create ECS security group
Write-Host "`n2. Creating ECS security group..." -ForegroundColor Yellow
$ecsSgName = "amesa-ecs-sg"
$ecsSgDesc = "Security group for Amesa ECS tasks - allows traffic from ALB"

$ecsSgId = aws ec2 create-security-group `
    --region $Region `
    --group-name $ecsSgName `
    --description $ecsSgDesc `
    --vpc-id $VPCId `
    --query "GroupId" `
    --output text 2>&1

if ($LASTEXITCODE -ne 0) {
    $ecsSgId = aws ec2 describe-security-groups --region $Region --filters "Name=group-name,Values=$ecsSgName" "Name=vpc-id,Values=$VPCId" --query "SecurityGroups[0].GroupId" --output text
    Write-Host "  Using existing: $ecsSgId" -ForegroundColor Yellow
} else {
    Write-Host "  ✅ Created: $ecsSgId" -ForegroundColor Green
}

# Add rules to ECS SG
Write-Host "  Adding rules to ECS security group..." -ForegroundColor Cyan
aws ec2 authorize-security-group-ingress --region $Region --group-id $ecsSgId --protocol tcp --port 8080 --source-group $albSgId --description "Allow ALB to reach ECS on port 8080" 2>&1 | Out-Null
Write-Host "  ✅ ECS rules added" -ForegroundColor Green

Write-Host "`n=== Next Steps ===" -ForegroundColor Cyan
Write-Host "1. Update ALB to use new security group: $albSgId" -ForegroundColor Yellow
Write-Host "2. Update ECS services to use new security group: $ecsSgId" -ForegroundColor Yellow
Write-Host "3. Test health checks again" -ForegroundColor Yellow

Write-Host "`nALB Security Group ID: $albSgId" -ForegroundColor Green
Write-Host "ECS Security Group ID: $ecsSgId" -ForegroundColor Green

