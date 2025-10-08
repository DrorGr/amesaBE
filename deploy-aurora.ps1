# Amesa Backend Aurora PostgreSQL Deployment Script (PowerShell)
# This script deploys the backend to AWS ECS with Aurora PostgreSQL

param(
    [string]$AWSRegion = "eu-north-1",
    [string]$Environment = "production",
    [string]$StackName = "amesa-backend-aurora",
    [string]$ECRRepository = "amesa-backend",
    [string]$ImageTag = "latest"
)

# Function to print colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Check if required tools are installed
function Test-Prerequisites {
    Write-Status "Checking prerequisites..."
    
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        Write-Error "AWS CLI is not installed. Please install it first."
        exit 1
    }
    
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Error "Docker is not installed. Please install it first."
        exit 1
    }
    
    Write-Success "All prerequisites are installed."
}

# Get AWS account ID
function Get-AccountId {
    try {
        $accountId = aws sts get-caller-identity --query Account --output text
        if (-not $accountId) {
            Write-Error "Failed to get AWS account ID. Please check your AWS credentials."
            exit 1
        }
        Write-Status "AWS Account ID: $accountId"
        return $accountId
    }
    catch {
        Write-Error "Failed to get AWS account ID: $_"
        exit 1
    }
}

# Create ECR repository if it doesn't exist
function New-ECRRepository {
    param([string]$AccountId)
    
    Write-Status "Creating ECR repository if it doesn't exist..."
    
    try {
        aws ecr describe-repositories --repository-names $ECRRepository --region $AWSRegion 2>$null
        if ($LASTEXITCODE -ne 0) {
            aws ecr create-repository --repository-name $ECRRepository --region $AWSRegion
            Write-Success "ECR repository created: $ECRRepository"
        } else {
            Write-Status "ECR repository already exists: $ECRRepository"
        }
    }
    catch {
        Write-Error "Failed to create ECR repository: $_"
        exit 1
    }
}

# Build and push Docker image
function Build-AndPushImage {
    param([string]$AccountId)
    
    Write-Status "Building and pushing Docker image..."
    
    try {
        # Login to ECR
        aws ecr get-login-password --region $AWSRegion | docker login --username AWS --password-stdin "$AccountId.dkr.ecr.$AWSRegion.amazonaws.com"
        
        # Build image
        docker build -t "$ECRRepository`:$ImageTag" -f AmesaBackend/Dockerfile AmesaBackend/
        
        # Tag image
        docker tag "$ECRRepository`:$ImageTag" "$AccountId.dkr.ecr.$AWSRegion.amazonaws.com/$ECRRepository`:$ImageTag"
        
        # Push image
        docker push "$AccountId.dkr.ecr.$AWSRegion.amazonaws.com/$ECRRepository`:$ImageTag"
        
        Write-Success "Docker image pushed successfully"
    }
    catch {
        Write-Error "Failed to build and push Docker image: $_"
        exit 1
    }
}

# Deploy CloudFormation stack
function Deploy-Infrastructure {
    Write-Status "Deploying infrastructure with CloudFormation..."
    
    try {
        # Generate a secure password
        $dbPassword = -join ((1..32) | ForEach {[char]((65..90) + (97..122) + (48..57) | Get-Random)})
        
        # Check if stack exists
        aws cloudformation describe-stacks --stack-name $StackName --region $AWSRegion 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Updating existing CloudFormation stack..."
            aws cloudformation update-stack `
                --stack-name $StackName `
                --template-body file://aws-infrastructure.yaml `
                --parameters ParameterKey=Environment,ParameterValue=$Environment ParameterKey=DatabasePassword,ParameterValue=$dbPassword `
                --capabilities CAPABILITY_IAM `
                --region $AWSRegion
        } else {
            Write-Status "Creating new CloudFormation stack..."
            aws cloudformation create-stack `
                --stack-name $StackName `
                --template-body file://aws-infrastructure.yaml `
                --parameters ParameterKey=Environment,ParameterValue=$Environment ParameterKey=DatabasePassword,ParameterValue=$dbPassword `
                --capabilities CAPABILITY_IAM `
                --region $AWSRegion
        }
        
        Write-Status "Waiting for CloudFormation stack to complete..."
        aws cloudformation wait stack-create-complete --stack-name $StackName --region $AWSRegion
        if ($LASTEXITCODE -ne 0) {
            aws cloudformation wait stack-update-complete --stack-name $StackName --region $AWSRegion
        }
        
        Write-Success "Infrastructure deployed successfully"
    }
    catch {
        Write-Error "Failed to deploy infrastructure: $_"
        exit 1
    }
}

# Get stack outputs
function Get-StackOutputs {
    Write-Status "Getting stack outputs..."
    
    try {
        $outputs = aws cloudformation describe-stacks --stack-name $StackName --region $AWSRegion --query 'Stacks[0].Outputs' --output json | ConvertFrom-Json
        
        $dbEndpoint = ($outputs | Where-Object { $_.OutputKey -eq "DatabaseEndpoint" }).OutputValue
        $redisEndpoint = ($outputs | Where-Object { $_.OutputKey -eq "RedisEndpoint" }).OutputValue
        $albDns = ($outputs | Where-Object { $_.OutputKey -eq "LoadBalancerDNS" }).OutputValue
        $ecsCluster = ($outputs | Where-Object { $_.OutputKey -eq "ECSClusterName" }).OutputValue
        $s3Bucket = ($outputs | Where-Object { $_.OutputKey -eq "S3BucketName" }).OutputValue
        
        Write-Success "Stack outputs retrieved"
        Write-Host "Database Endpoint: $dbEndpoint"
        Write-Host "Redis Endpoint: $redisEndpoint"
        Write-Host "Load Balancer DNS: $albDns"
        Write-Host "ECS Cluster: $ecsCluster"
        Write-Host "S3 Bucket: $s3Bucket"
        
        return @{
            DatabaseEndpoint = $dbEndpoint
            RedisEndpoint = $redisEndpoint
            LoadBalancerDNS = $albDns
            ECSClusterName = $ecsCluster
            S3BucketName = $s3Bucket
        }
    }
    catch {
        Write-Error "Failed to get stack outputs: $_"
        exit 1
    }
}

# Update ECS task definition with actual values
function Update-TaskDefinition {
    param([string]$AccountId, [hashtable]$Outputs)
    
    Write-Status "Updating ECS task definition with actual values..."
    
    try {
        # Read the task definition file
        $taskDef = Get-Content ecs-task-definition.json -Raw | ConvertFrom-Json
        
        # Update the image URI
        $taskDef.containerDefinitions[0].image = "$AccountId.dkr.ecr.$AWSRegion.amazonaws.com/$ECRRepository`:$ImageTag"
        
        # Update environment variables
        $envVars = $taskDef.containerDefinitions[0].environment
        ($envVars | Where-Object { $_.name -eq "DB_HOST" }).value = $Outputs.DatabaseEndpoint
        ($envVars | Where-Object { $_.name -eq "REDIS_CONNECTION_STRING" }).value = "$($Outputs.RedisEndpoint):6379"
        ($envVars | Where-Object { $_.name -eq "AWS_S3_BUCKET" }).value = $Outputs.S3BucketName
        
        # Update secrets ARNs
        $secrets = $taskDef.containerDefinitions[0].secrets
        foreach ($secret in $secrets) {
            $secret.valueFrom = $secret.valueFrom -replace "YOUR_ACCOUNT_ID", $AccountId
        }
        
        # Update execution and task role ARNs
        $taskDef.executionRoleArn = $taskDef.executionRoleArn -replace "YOUR_ACCOUNT_ID", $AccountId
        $taskDef.taskRoleArn = $taskDef.taskRoleArn -replace "YOUR_ACCOUNT_ID", $AccountId
        
        # Save the updated task definition
        $taskDef | ConvertTo-Json -Depth 10 | Set-Content ecs-task-definition-temp.json
        
        Write-Success "Task definition updated"
    }
    catch {
        Write-Error "Failed to update task definition: $_"
        exit 1
    }
}

# Register ECS task definition
function Register-TaskDefinition {
    Write-Status "Registering ECS task definition..."
    
    try {
        aws ecs register-task-definition --cli-input-json file://ecs-task-definition-temp.json --region $AWSRegion
        Write-Success "Task definition registered"
    }
    catch {
        Write-Error "Failed to register task definition: $_"
        exit 1
    }
}

# Create ECS service
function New-ECSService {
    param([hashtable]$Outputs)
    
    Write-Status "Creating ECS service..."
    
    try {
        # Get the latest task definition ARN
        $taskDefArn = aws ecs list-task-definitions --family-prefix amesa-backend-task --status ACTIVE --sort DESC --max-items 1 --region $AWSRegion --query 'taskDefinitionArns[0]' --output text
        
        # Get ALB target group ARN
        $targetGroupArn = aws elbv2 describe-target-groups --names "$Environment-amesa-tg" --region $AWSRegion --query 'TargetGroups[0].TargetGroupArn' --output text
        
        # Get subnet IDs (you'll need to update these with actual subnet IDs from your VPC)
        $subnets = aws ec2 describe-subnets --filters "Name=vpc-id,Values=$(aws ec2 describe-vpcs --filters "Name=tag:Name,Values=$Environment-amesa-vpc" --query 'Vpcs[0].VpcId' --output text)" --query 'Subnets[?MapPublicIpOnLaunch==`true`].SubnetId' --output text
        
        # Get security group ID
        $securityGroupId = aws ec2 describe-security-groups --filters "Name=group-name,Values=$Environment-amesa-ecs-sg" --query 'SecurityGroups[0].GroupId' --output text
        
        # Create service
        aws ecs create-service `
            --cluster $Outputs.ECSClusterName `
            --service-name amesa-backend-service `
            --task-definition $taskDefArn `
            --desired-count 2 `
            --launch-type FARGATE `
            --network-configuration "awsvpcConfiguration={subnets=[$($subnets -join ',')],securityGroups=[$securityGroupId],assignPublicIp=ENABLED}" `
            --load-balancers "targetGroupArn=$targetGroupArn,containerName=amesa-backend,containerPort=80" `
            --region $AWSRegion
        
        Write-Success "ECS service created"
    }
    catch {
        Write-Error "Failed to create ECS service: $_"
        exit 1
    }
}

# Clean up temporary files
function Remove-TempFiles {
    Write-Status "Cleaning up temporary files..."
    if (Test-Path "ecs-task-definition-temp.json") {
        Remove-Item "ecs-task-definition-temp.json"
    }
    Write-Success "Cleanup completed"
}

# Main deployment function
function Start-Deployment {
    Write-Status "Starting Amesa Backend Aurora PostgreSQL deployment..."
    
    Test-Prerequisites
    $accountId = Get-AccountId
    New-ECRRepository -AccountId $accountId
    Build-AndPushImage -AccountId $accountId
    Deploy-Infrastructure
    $outputs = Get-StackOutputs
    Update-TaskDefinition -AccountId $accountId -Outputs $outputs
    Register-TaskDefinition
    New-ECSService -Outputs $outputs
    Remove-TempFiles
    
    Write-Success "Deployment completed successfully!"
    Write-Status "Your application should be available at: http://$($outputs.LoadBalancerDNS)"
    Write-Warning "Note: It may take a few minutes for the service to be fully ready."
}

# Run main function
Start-Deployment
