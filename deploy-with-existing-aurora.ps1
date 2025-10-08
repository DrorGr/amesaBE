# Amesa Backend Deployment Script for Existing Aurora Database
# This script deploys the backend to AWS ECS using your existing Aurora PostgreSQL database

param(
    [string]$AWSRegion = "eu-north-1",
    [string]$Environment = "production",
    [string]$StackName = "amesa-backend-infrastructure",
    [string]$ECRRepository = "amesabe",
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
        $accountId = "129394705401"  # Your actual AWS account ID
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

# Deploy infrastructure (without database)
function Deploy-Infrastructure {
    Write-Status "Deploying infrastructure (ECS, ALB, etc.)..."
    
    try {
        # Create a simplified CloudFormation template for infrastructure only
        $infrastructureTemplate = @"
AWSTemplateFormatVersion: '2010-09-09'
Description: 'Amesa Backend Infrastructure (ECS, ALB, etc.)'

Parameters:
  Environment:
    Type: String
    Default: production
    AllowedValues: [development, staging, production]
    Description: Environment name

Resources:
  # VPC (use existing or create new)
  VPC:
    Type: AWS::EC2::VPC
    Properties:
      CidrBlock: 10.0.0.0/16
      EnableDnsHostnames: true
      EnableDnsSupport: true
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-vpc'

  InternetGateway:
    Type: AWS::EC2::InternetGateway
    Properties:
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-igw'

  InternetGatewayAttachment:
    Type: AWS::EC2::VPCGatewayAttachment
    Properties:
      InternetGatewayId: !Ref InternetGateway
      VpcId: !Ref VPC

  PublicSubnet1:
    Type: AWS::EC2::Subnet
    Properties:
      VpcId: !Ref VPC
      AvailabilityZone: !Select [0, !GetAZs '']
      CidrBlock: 10.0.1.0/24
      MapPublicIpOnLaunch: true
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-public-subnet-1'

  PublicSubnet2:
    Type: AWS::EC2::Subnet
    Properties:
      VpcId: !Ref VPC
      AvailabilityZone: !Select [1, !GetAZs '']
      CidrBlock: 10.0.2.0/24
      MapPublicIpOnLaunch: true
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-public-subnet-2'

  PublicRouteTable:
    Type: AWS::EC2::RouteTable
    Properties:
      VpcId: !Ref VPC
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-public-routes'

  DefaultPublicRoute:
    Type: AWS::EC2::Route
    DependsOn: InternetGatewayAttachment
    Properties:
      RouteTableId: !Ref PublicRouteTable
      DestinationCidrBlock: 0.0.0.0/0
      GatewayId: !Ref InternetGateway

  PublicSubnet1RouteTableAssociation:
    Type: AWS::EC2::SubnetRouteTableAssociation
    Properties:
      RouteTableId: !Ref PublicRouteTable
      SubnetId: !Ref PublicSubnet1

  PublicSubnet2RouteTableAssociation:
    Type: AWS::EC2::SubnetRouteTableAssociation
    Properties:
      RouteTableId: !Ref PublicRouteTable
      SubnetId: !Ref PublicSubnet2

  # Security Groups
  ALBSecurityGroup:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: Security group for Application Load Balancer
      VpcId: !Ref VPC
      SecurityGroupIngress:
        - IpProtocol: tcp
          FromPort: 80
          ToPort: 80
          CidrIp: 0.0.0.0/0
        - IpProtocol: tcp
          FromPort: 443
          ToPort: 443
          CidrIp: 0.0.0.0/0
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-alb-sg'

  ECSSecurityGroup:
    Type: AWS::EC2::SecurityGroup
    Properties:
      GroupDescription: Security group for ECS tasks
      VpcId: !Ref VPC
      SecurityGroupIngress:
        - IpProtocol: tcp
          FromPort: 80
          ToPort: 80
          SourceSecurityGroupId: !Ref ALBSecurityGroup
      Tags:
        - Key: Name
          Value: !Sub '${Environment}-amesa-ecs-sg'

  # Application Load Balancer
  ApplicationLoadBalancer:
    Type: AWS::ElasticLoadBalancingV2::LoadBalancer
    Properties:
      Name: !Sub '${Environment}-amesa-alb'
      Scheme: internet-facing
      Type: application
      Subnets:
        - !Ref PublicSubnet1
        - !Ref PublicSubnet2
      SecurityGroups:
        - !Ref ALBSecurityGroup

  ALBTargetGroup:
    Type: AWS::ElasticLoadBalancingV2::TargetGroup
    Properties:
      Name: !Sub '${Environment}-amesa-tg'
      Port: 80
      Protocol: HTTP
      VpcId: !Ref VPC
      TargetType: ip
      HealthCheckPath: /health
      HealthCheckProtocol: HTTP
      HealthCheckIntervalSeconds: 30
      HealthCheckTimeoutSeconds: 5
      HealthyThresholdCount: 2
      UnhealthyThresholdCount: 3

  ALBListener:
    Type: AWS::ElasticLoadBalancingV2::Listener
    Properties:
      DefaultActions:
        - Type: forward
          TargetGroupArn: !Ref ALBTargetGroup
      LoadBalancerArn: !Ref ApplicationLoadBalancer
      Port: 80
      Protocol: HTTP

  # ECS Cluster
  ECSCluster:
    Type: AWS::ECS::Cluster
    Properties:
      ClusterName: !Sub '${Environment}-amesa-cluster'
      CapacityProviders:
        - FARGATE
        - FARGATE_SPOT
      DefaultCapacityProviderStrategy:
        - CapacityProvider: FARGATE
          Weight: 1

  # CloudWatch Log Group
  LogGroup:
    Type: AWS::Logs::LogGroup
    Properties:
      LogGroupName: !Sub '/ecs/${Environment}-amesa-backend'
      RetentionInDays: 30

  # ECS Task Execution Role
  ECSTaskExecutionRole:
    Type: AWS::IAM::Role
    Properties:
      AssumeRolePolicyDocument:
        Version: '2012-10-17'
        Statement:
          - Effect: Allow
            Principal:
              Service: ecs-tasks.amazonaws.com
            Action: sts:AssumeRole
      ManagedPolicyArns:
        - arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy
      Policies:
        - PolicyName: SecretsManagerAccess
          PolicyDocument:
            Version: '2012-10-17'
            Statement:
              - Effect: Allow
                Action:
                  - secretsmanager:GetSecretValue
                Resource: !Sub 'arn:aws:secretsmanager:${AWS::Region}:${AWS::AccountId}:secret:amesa/*'

  # ECS Task Role
  ECSTaskRole:
    Type: AWS::IAM::Role
    Properties:
      AssumeRolePolicyDocument:
        Version: '2012-10-17'
        Statement:
          - Effect: Allow
            Principal:
              Service: ecs-tasks.amazonaws.com
            Action: sts:AssumeRole
      Policies:
        - PolicyName: S3Access
          PolicyDocument:
            Version: '2012-10-17'
            Statement:
              - Effect: Allow
                Action:
                  - s3:GetObject
                  - s3:PutObject
                  - s3:DeleteObject
                Resource: !Sub 'arn:aws:s3:::${S3Bucket}/*'
        - PolicyName: CloudWatchLogs
          PolicyDocument:
            Version: '2012-10-17'
            Statement:
              - Effect: Allow
                Action:
                  - logs:CreateLogStream
                  - logs:PutLogEvents
                Resource: !Sub '${LogGroup.Arn}:*'

  # S3 Bucket for file uploads
  S3Bucket:
    Type: AWS::S3::Bucket
    Properties:
      BucketName: !Sub '${Environment}-amesa-uploads-${AWS::AccountId}'
      PublicAccessBlockConfiguration:
        BlockPublicAcls: true
        BlockPublicPolicy: true
        IgnorePublicAcls: true
        RestrictPublicBuckets: true
      VersioningConfiguration:
        Status: Enabled

Outputs:
  VPCId:
    Description: VPC ID
    Value: !Ref VPC
    Export:
      Name: !Sub '${Environment}-amesa-vpc-id'

  LoadBalancerDNS:
    Description: Application Load Balancer DNS Name
    Value: !GetAtt ApplicationLoadBalancer.DNSName
    Export:
      Name: !Sub '${Environment}-amesa-alb-dns'

  ECSClusterName:
    Description: ECS Cluster Name
    Value: !Ref ECSCluster
    Export:
      Name: !Sub '${Environment}-amesa-ecs-cluster'

  S3BucketName:
    Description: S3 Bucket for file uploads
    Value: !Ref S3Bucket
    Export:
      Name: !Sub '${Environment}-amesa-s3-bucket'

  ALBTargetGroupArn:
    Description: ALB Target Group ARN
    Value: !Ref ALBTargetGroup
    Export:
      Name: !Sub '${Environment}-amesa-target-group-arn'

  PublicSubnet1Id:
    Description: Public Subnet 1 ID
    Value: !Ref PublicSubnet1
    Export:
      Name: !Sub '${Environment}-amesa-public-subnet-1-id'

  PublicSubnet2Id:
    Description: Public Subnet 2 ID
    Value: !Ref PublicSubnet2
    Export:
      Name: !Sub '${Environment}-amesa-public-subnet-2-id'

  ECSSecurityGroupId:
    Description: ECS Security Group ID
    Value: !Ref ECSSecurityGroup
    Export:
      Name: !Sub '${Environment}-amesa-ecs-sg-id'
"@

        # Save the template to a file
        $infrastructureTemplate | Out-File -FilePath "infrastructure-only.yaml" -Encoding UTF8

        # Deploy the infrastructure
        aws cloudformation create-stack `
            --stack-name $StackName `
            --template-body file://infrastructure-only.yaml `
            --parameters ParameterKey=Environment,ParameterValue=$Environment `
            --capabilities CAPABILITY_IAM `
            --region $AWSRegion

        Write-Status "Waiting for CloudFormation stack to complete..."
        aws cloudformation wait stack-create-complete --stack-name $StackName --region $AWSRegion

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
        
        $albDns = ($outputs | Where-Object { $_.OutputKey -eq "LoadBalancerDNS" }).OutputValue
        $ecsCluster = ($outputs | Where-Object { $_.OutputKey -eq "ECSClusterName" }).OutputValue
        $s3Bucket = ($outputs | Where-Object { $_.OutputKey -eq "S3BucketName" }).OutputValue
        $targetGroupArn = ($outputs | Where-Object { $_.OutputKey -eq "ALBTargetGroupArn" }).OutputValue
        $subnet1Id = ($outputs | Where-Object { $_.OutputKey -eq "PublicSubnet1Id" }).OutputValue
        $subnet2Id = ($outputs | Where-Object { $_.OutputKey -eq "PublicSubnet2Id" }).OutputValue
        $securityGroupId = ($outputs | Where-Object { $_.OutputKey -eq "ECSSecurityGroupId" }).OutputValue
        
        Write-Success "Stack outputs retrieved"
        Write-Host "Load Balancer DNS: $albDns"
        Write-Host "ECS Cluster: $ecsCluster"
        Write-Host "S3 Bucket: $s3Bucket"
        
        return @{
            LoadBalancerDNS = $albDns
            ECSClusterName = $ecsCluster
            S3BucketName = $s3Bucket
            ALBTargetGroupArn = $targetGroupArn
            PublicSubnet1Id = $subnet1Id
            PublicSubnet2Id = $subnet2Id
            ECSSecurityGroupId = $securityGroupId
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
        
        # Create service
        aws ecs create-service `
            --cluster $Outputs.ECSClusterName `
            --service-name amesa-backend-service `
            --task-definition $taskDefArn `
            --desired-count 2 `
            --launch-type FARGATE `
            --network-configuration "awsvpcConfiguration={subnets=[$($Outputs.PublicSubnet1Id),$($Outputs.PublicSubnet2Id)],securityGroups=[$($Outputs.ECSSecurityGroupId)],assignPublicIp=ENABLED}" `
            --load-balancers "targetGroupArn=$($Outputs.ALBTargetGroupArn),containerName=amesa-backend,containerPort=80" `
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
    if (Test-Path "infrastructure-only.yaml") {
        Remove-Item "infrastructure-only.yaml"
    }
    Write-Success "Cleanup completed"
}

# Main deployment function
function Start-Deployment {
    Write-Status "Starting Amesa Backend deployment with existing Aurora database..."
    
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
    Write-Warning "Make sure to set up your secrets in AWS Secrets Manager before the application can connect to the database."
}

# Run main function
Start-Deployment
