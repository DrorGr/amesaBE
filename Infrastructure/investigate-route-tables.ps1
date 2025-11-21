# Investigate Route Tables Between ALB and ECS Subnets
# Task 2: Network connectivity diagnosis

param(
    [string]$Region = "eu-north-1",
    [string]$VPCId = "vpc-0faeeb78eded33ccf"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Task 2: Investigating Route Tables ===" -ForegroundColor Cyan
Write-Host ""

# Get ALB subnets
Write-Host "Getting ALB subnets..." -ForegroundColor Yellow
$albArn = "arn:aws:elasticloadbalancing:eu-north-1:129394705401:loadbalancer/app/amesa-backend-alb/d4dbb08b12e385fe"
$albSubnets = aws elbv2 describe-load-balancers --region $Region --load-balancer-arns $albArn --query "LoadBalancers[0].AvailabilityZones[*].SubnetId" --output text
$albSubnetList = $albSubnets -split "`t" | Where-Object { $_ }

Write-Host "ALB Subnets:" -ForegroundColor Green
$albSubnetList | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }

# Get ECS service subnets
Write-Host "`nGetting ECS service subnets..." -ForegroundColor Yellow
$ecsService = aws ecs describe-services --region $Region --cluster Amesa --services amesa-auth-service --query "services[0].networkConfiguration.awsvpcConfiguration.subnets" --output text
$ecsSubnetList = $ecsService -split "`t" | Where-Object { $_ }

Write-Host "ECS Subnets:" -ForegroundColor Green
$ecsSubnetList | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }

Write-Host "`n=== Route Table Analysis ===" -ForegroundColor Cyan

foreach ($subnet in $albSubnetList) {
    Write-Host "`nALB Subnet: $subnet" -ForegroundColor Yellow
    
    # Get route table
    $routeTable = aws ec2 describe-route-tables --region $Region --filters "Name=association.subnet-id,Values=$subnet" --query "RouteTables[0].RouteTableId" --output text
    
    if ($routeTable -and $routeTable -ne "None") {
        Write-Host "  Route Table: $routeTable" -ForegroundColor Green
        
        # Get routes
        $routes = aws ec2 describe-route-tables --region $Region --route-table-ids $routeTable --query "RouteTables[0].Routes[*].{Destination:DestinationCidrBlock,Gateway:GatewayId,Target:NetworkInterfaceId}" --output json | ConvertFrom-Json
        
        Write-Host "  Routes:" -ForegroundColor Gray
        $routes | ForEach-Object {
            if ($_.Destination -eq "0.0.0.0/0") {
                Write-Host "    $($_.Destination) -> $($_.Gateway) (Internet Gateway)" -ForegroundColor Gray
            } else {
                Write-Host "    $($_.Destination) -> $($_.Gateway)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "  Route Table: Using default VPC route table" -ForegroundColor Yellow
        
        # Get default route table
        $defaultRt = aws ec2 describe-route-tables --region $Region --filters "Name=vpc-id,Values=$VPCId" "Name=association.main,Values=true" --query "RouteTables[0].RouteTableId" --output text
        Write-Host "  Default Route Table: $defaultRt" -ForegroundColor Gray
    }
}

foreach ($subnet in $ecsSubnetList) {
    Write-Host "`nECS Subnet: $subnet" -ForegroundColor Yellow
    
    # Get route table
    $routeTable = aws ec2 describe-route-tables --region $Region --filters "Name=association.subnet-id,Values=$subnet" --query "RouteTables[0].RouteTableId" --output text
    
    if ($routeTable -and $routeTable -ne "None") {
        Write-Host "  Route Table: $routeTable" -ForegroundColor Green
        
        # Get routes
        $routes = aws ec2 describe-route-tables --region $Region --route-table-ids $routeTable --query "RouteTables[0].Routes[*].{Destination:DestinationCidrBlock,Gateway:GatewayId,Target:NetworkInterfaceId}" --output json | ConvertFrom-Json
        
        Write-Host "  Routes:" -ForegroundColor Gray
        $routes | ForEach-Object {
            if ($_.Destination -eq "0.0.0.0/0") {
                Write-Host "    $($_.Destination) -> $($_.Gateway) (NAT Gateway or Internet Gateway)" -ForegroundColor Gray
            } else {
                Write-Host "    $($_.Destination) -> $($_.Gateway)" -ForegroundColor Gray
            }
        }
    } else {
        Write-Host "  Route Table: Using default VPC route table" -ForegroundColor Yellow
    }
}

Write-Host "`n=== Analysis ===" -ForegroundColor Cyan
Write-Host "Check if:" -ForegroundColor Yellow
Write-Host "1. ALB subnets can reach ECS subnets (local VPC routes)" -ForegroundColor Gray
Write-Host "2. Both have routes to each other's CIDR blocks" -ForegroundColor Gray
Write-Host "3. NAT Gateway is configured if ECS is in private subnets" -ForegroundColor Gray

Write-Host "`n=== Recommendations ===" -ForegroundColor Cyan
Write-Host "If routes are missing:" -ForegroundColor Yellow
Write-Host "- Add local VPC routes (usually automatic)" -ForegroundColor Gray
Write-Host "- Ensure both subnets can communicate within VPC" -ForegroundColor Gray
Write-Host "- Check security group rules allow traffic" -ForegroundColor Gray

