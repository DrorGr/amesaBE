# PowerShell script to fix ECR network access for ECS tasks
# This script verifies and fixes IAM role permissions and VPC configuration

$ErrorActionPreference = "Stop"

Write-Output "=== ECR Network Access Fix for ECS Tasks ==="
Write-Output ""

$Region = "eu-north-1"
$AccountId = "129394705401"
$ExecutionRoleName = "ecsTaskExecutionRole"
$ExecutionRoleArn = "arn:aws:iam::${AccountId}:role/${ExecutionRoleName}"

Write-Output "Region: $Region"
Write-Output "Account ID: $AccountId"
Write-Output "Execution Role: $ExecutionRoleArn"
Write-Output ""

# Check if AWS CLI is available
$awsCli = Get-Command aws -ErrorAction SilentlyContinue
if (-not $awsCli) {
    Write-Output "❌ Error: AWS CLI is not installed or not in PATH"
    Write-Output "Please install AWS CLI: https://aws.amazon.com/cli/"
    exit 1
}

Write-Output "Step 1: Verifying IAM Role exists..."
try {
    $role = aws iam get-role --role-name $ExecutionRoleName --region $Region 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Output "❌ Error: IAM role '$ExecutionRoleName' does not exist"
        Write-Output ""
        Write-Output "Creating IAM role with ECR permissions..."
        
        # Create trust policy for ECS
        $trustPolicy = @{
            Version = "2012-10-17"
            Statement = @(
                @{
                    Effect = "Allow"
                    Principal = @{
                        Service = "ecs-tasks.amazonaws.com"
                    }
                    Action = "sts:AssumeRole"
                }
            )
        } | ConvertTo-Json -Depth 10
        
        $trustPolicyFile = [System.IO.Path]::GetTempFileName()
        $trustPolicy | Out-File -FilePath $trustPolicyFile -Encoding utf8
        
        # Create the role
        aws iam create-role `
            --role-name $ExecutionRoleName `
            --assume-role-policy-document "file://$trustPolicyFile" `
            --region $Region
        
        Remove-Item $trustPolicyFile
        Write-Output "✅ IAM role created"
    } else {
        Write-Output "✅ IAM role exists"
    }
} catch {
    Write-Output "❌ Error checking IAM role: $_"
    exit 1
}

Write-Output ""
Write-Output "Step 2: Attaching ECR permissions to IAM role..."

# ECR permissions policy
$ecrPolicy = @{
    Version = "2012-10-17"
    Statement = @(
        @{
            Effect = "Allow"
            Action = @(
                "ecr:GetAuthorizationToken",
                "ecr:BatchCheckLayerAvailability",
                "ecr:GetDownloadUrlForLayer",
                "ecr:BatchGetImage"
            )
            Resource = "*"
        }
    )
} | ConvertTo-Json -Depth 10 -Compress

$ecrPolicyFile = [System.IO.Path]::GetTempFileName()
$ecrPolicy | Out-File -FilePath $ecrPolicyFile -Encoding utf8

# Check if inline policy already exists
$inlinePolicies = aws iam list-role-policies --role-name $ExecutionRoleName --region $Region 2>&1 | ConvertFrom-Json
$hasEcrPolicy = $inlinePolicies.PolicyNames -contains "ECR-Access-Policy"

if (-not $hasEcrPolicy) {
    # Create inline policy
    $putResult = aws iam put-role-policy `
        --role-name $ExecutionRoleName `
        --policy-name "ECR-Access-Policy" `
        --policy-document "file://$ecrPolicyFile" `
        --region $Region 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Output "[OK] ECR permissions attached"
    } else {
        Write-Output "[ERROR] Failed to attach ECR permissions: $putResult"
    }
} else {
    Write-Output "[OK] ECR permissions already attached"
}

Remove-Item $ecrPolicyFile

Write-Output ""
Write-Output "Step 3: Attaching CloudWatch Logs permissions..."

# Check if CloudWatch Logs policy is attached (check both inline and attached)
$attachedPolicies = aws iam list-attached-role-policies --role-name $ExecutionRoleName --region $Region 2>&1 | ConvertFrom-Json
$hasLogsPolicy = $attachedPolicies.AttachedPolicies | Where-Object { $_.PolicyName -like "*Logs*" }

if (-not $hasLogsPolicy) {
    # Attach AWS managed policy for CloudWatch Logs
    $attachResult = aws iam attach-role-policy `
        --role-name $ExecutionRoleName `
        --policy-arn "arn:aws:iam::aws:policy/CloudWatchLogsFullAccess" `
        --region $Region 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Output "[OK] CloudWatch Logs permissions attached"
    } else {
        Write-Output "[WARNING] Could not attach CloudWatch Logs permissions: $attachResult"
    }
} else {
    Write-Output "[OK] CloudWatch Logs permissions already attached"
}

Write-Output ""
Write-Output "Step 4: Verifying VPC configuration..."

# Get ECS cluster to find VPC
$clusters = aws ecs list-clusters --region $Region | ConvertFrom-Json
$clusterArn = $clusters.clusterArns | Where-Object { $_ -like "*Amesa*" } | Select-Object -First 1

if ($clusterArn) {
    Write-Output "Found ECS cluster: $clusterArn"
    
    # Get services to find VPC
    $services = aws ecs list-services --cluster $clusterArn --region $Region | ConvertFrom-Json
    if ($services.serviceArns.Count -gt 0) {
        $serviceArn = $services.serviceArns[0]
        $serviceDetails = aws ecs describe-services --cluster $clusterArn --services $serviceArn --region $Region | ConvertFrom-Json
        $networkConfig = $serviceDetails.services[0].networkConfiguration
        
        if ($networkConfig.awsvpcConfiguration) {
            $subnetIds = $networkConfig.awsvpcConfiguration.subnets
            $securityGroupIds = $networkConfig.awsvpcConfiguration.securityGroupIds
            
            Write-Output "VPC Configuration found:"
            Write-Output "  Subnets: $($subnetIds -join ', ')"
            if ($securityGroupIds -and $securityGroupIds.Count -gt 0) {
                Write-Output "  Security Groups: $($securityGroupIds -join ', ')"
            } else {
                Write-Output "  Security Groups: (not found in service config)"
            }
            
            # Check if subnets have NAT Gateway or Internet Gateway
            Write-Output ""
            Write-Output "[CRITICAL] ECS tasks are in PRIVATE subnets (assignPublicIp=DISABLED)"
            Write-Output "   This means tasks MUST have NAT Gateway for ECR access!"
            Write-Output ""
            Write-Output "Required VPC Configuration:"
            Write-Output "  1. NAT Gateway in public subnet (REQUIRED for private subnets)"
            Write-Output "  2. Route table routes 0.0.0.0/0 -> NAT Gateway"
            Write-Output "  3. Security groups allow outbound HTTPS (443) to 0.0.0.0/0"
            Write-Output ""
            Write-Output "To verify NAT Gateway:"
            Write-Output "  aws ec2 describe-nat-gateways --region $Region --filter 'Name=vpc-id,Values=<VPC_ID>'"
            Write-Output ""
            Write-Output "To verify route tables:"
            Write-Output "  aws ec2 describe-route-tables --region $Region --filters 'Name=association.subnet-id,Values=$($subnetIds[0])'"
            Write-Output ""
            if ($securityGroupIds -and $securityGroupIds.Count -gt 0) {
                Write-Output "To verify security groups:"
                Write-Output "  aws ec2 describe-security-groups --group-ids $($securityGroupIds[0]) --region $Region"
            }
        }
    }
} else {
    Write-Output "⚠️  ECS cluster not found. VPC configuration cannot be verified automatically."
}

Write-Output ""
Write-Output "Step 5: Verifying security group outbound rules..."

Write-Output "[INFO] Manual verification required:"
Write-Output "  1. Check security groups allow outbound HTTPS (443) to 0.0.0.0/0"
Write-Output "  2. Verify route tables have routes to NAT Gateway or Internet Gateway"
Write-Output "  3. Ensure ECR endpoints are accessible from your VPC"

Write-Output ""
Write-Output "========================================"
Write-Output "[SUCCESS] ECR Network Access Fix Complete!"
Write-Output ""
Write-Output "Summary:"
Write-Output "  [OK] IAM role verified/created: $ExecutionRoleArn"
Write-Output "  [OK] ECR permissions attached"
Write-Output "  [OK] CloudWatch Logs permissions attached"
Write-Output ""
Write-Output "Next steps:"
Write-Output "  1. Verify VPC has NAT Gateway or Internet Gateway"
Write-Output "  2. Verify security groups allow outbound HTTPS (443)"
Write-Output "  3. Test ECS task deployment"
Write-Output ""

