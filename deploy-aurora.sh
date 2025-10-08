#!/bin/bash

# Amesa Backend Aurora PostgreSQL Deployment Script
# This script deploys the backend to AWS ECS with Aurora PostgreSQL

set -e

# Configuration
AWS_REGION="eu-north-1"
ENVIRONMENT="production"
STACK_NAME="amesa-backend-aurora"
ECR_REPOSITORY="amesa-backend"
IMAGE_TAG="latest"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if required tools are installed
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    if ! command -v aws &> /dev/null; then
        print_error "AWS CLI is not installed. Please install it first."
        exit 1
    fi
    
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install it first."
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        print_error "jq is not installed. Please install it first."
        exit 1
    fi
    
    print_success "All prerequisites are installed."
}

# Get AWS account ID
get_account_id() {
    ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
    if [ -z "$ACCOUNT_ID" ]; then
        print_error "Failed to get AWS account ID. Please check your AWS credentials."
        exit 1
    fi
    print_status "AWS Account ID: $ACCOUNT_ID"
}

# Create ECR repository if it doesn't exist
create_ecr_repository() {
    print_status "Creating ECR repository if it doesn't exist..."
    
    if ! aws ecr describe-repositories --repository-names $ECR_REPOSITORY --region $AWS_REGION &> /dev/null; then
        aws ecr create-repository --repository-name $ECR_REPOSITORY --region $AWS_REGION
        print_success "ECR repository created: $ECR_REPOSITORY"
    else
        print_status "ECR repository already exists: $ECR_REPOSITORY"
    fi
}

# Build and push Docker image
build_and_push_image() {
    print_status "Building and pushing Docker image..."
    
    # Login to ECR
    aws ecr get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin $ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com
    
    # Build image
    docker build -t $ECR_REPOSITORY:$IMAGE_TAG -f AmesaBackend/Dockerfile AmesaBackend/
    
    # Tag image
    docker tag $ECR_REPOSITORY:$IMAGE_TAG $ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$IMAGE_TAG
    
    # Push image
    docker push $ACCOUNT_ID.dkr.ecr.$AWS_REGION.amazonaws.com/$ECR_REPOSITORY:$IMAGE_TAG
    
    print_success "Docker image pushed successfully"
}

# Deploy CloudFormation stack
deploy_infrastructure() {
    print_status "Deploying infrastructure with CloudFormation..."
    
    # Check if stack exists
    if aws cloudformation describe-stacks --stack-name $STACK_NAME --region $AWS_REGION &> /dev/null; then
        print_status "Updating existing CloudFormation stack..."
        aws cloudformation update-stack \
            --stack-name $STACK_NAME \
            --template-body file://aws-infrastructure.yaml \
            --parameters ParameterKey=Environment,ParameterValue=$ENVIRONMENT \
                        ParameterKey=DatabasePassword,ParameterValue=$(openssl rand -base64 32) \
            --capabilities CAPABILITY_IAM \
            --region $AWS_REGION
    else
        print_status "Creating new CloudFormation stack..."
        aws cloudformation create-stack \
            --stack-name $STACK_NAME \
            --template-body file://aws-infrastructure.yaml \
            --parameters ParameterKey=Environment,ParameterValue=$ENVIRONMENT \
                        ParameterKey=DatabasePassword,ParameterValue=$(openssl rand -base64 32) \
            --capabilities CAPABILITY_IAM \
            --region $AWS_REGION
    fi
    
    print_status "Waiting for CloudFormation stack to complete..."
    aws cloudformation wait stack-create-complete --stack-name $STACK_NAME --region $AWS_REGION || \
    aws cloudformation wait stack-update-complete --stack-name $STACK_NAME --region $AWS_REGION
    
    print_success "Infrastructure deployed successfully"
}

# Get stack outputs
get_stack_outputs() {
    print_status "Getting stack outputs..."
    
    DB_ENDPOINT=$(aws cloudformation describe-stacks \
        --stack-name $STACK_NAME \
        --region $AWS_REGION \
        --query 'Stacks[0].Outputs[?OutputKey==`DatabaseEndpoint`].OutputValue' \
        --output text)
    
    REDIS_ENDPOINT=$(aws cloudformation describe-stacks \
        --stack-name $STACK_NAME \
        --region $AWS_REGION \
        --query 'Stacks[0].Outputs[?OutputKey==`RedisEndpoint`].OutputValue' \
        --output text)
    
    ALB_DNS=$(aws cloudformation describe-stacks \
        --stack-name $STACK_NAME \
        --region $AWS_REGION \
        --query 'Stacks[0].Outputs[?OutputKey==`LoadBalancerDNS`].OutputValue' \
        --output text)
    
    ECS_CLUSTER=$(aws cloudformation describe-stacks \
        --stack-name $STACK_NAME \
        --region $AWS_REGION \
        --query 'Stacks[0].Outputs[?OutputKey==`ECSClusterName`].OutputValue' \
        --output text)
    
    S3_BUCKET=$(aws cloudformation describe-stacks \
        --stack-name $STACK_NAME \
        --region $AWS_REGION \
        --query 'Stacks[0].Outputs[?OutputKey==`S3BucketName`].OutputValue' \
        --output text)
    
    print_success "Stack outputs retrieved"
    echo "Database Endpoint: $DB_ENDPOINT"
    echo "Redis Endpoint: $REDIS_ENDPOINT"
    echo "Load Balancer DNS: $ALB_DNS"
    echo "ECS Cluster: $ECS_CLUSTER"
    echo "S3 Bucket: $S3_BUCKET"
}

# Update ECS task definition with actual values
update_task_definition() {
    print_status "Updating ECS task definition with actual values..."
    
    # Create a temporary task definition file
    cp ecs-task-definition.json ecs-task-definition-temp.json
    
    # Replace placeholders with actual values
    sed -i "s/YOUR_ACCOUNT_ID/$ACCOUNT_ID/g" ecs-task-definition-temp.json
    sed -i "s/YOUR_AURORA_CLUSTER_ENDPOINT/$DB_ENDPOINT/g" ecs-task-definition-temp.json
    sed -i "s/YOUR_ELASTICACHE_ENDPOINT/$REDIS_ENDPOINT/g" ecs-task-definition-temp.json
    sed -i "s/amesa-uploads/$S3_BUCKET/g" ecs-task-definition-temp.json
    
    print_success "Task definition updated"
}

# Register ECS task definition
register_task_definition() {
    print_status "Registering ECS task definition..."
    
    aws ecs register-task-definition \
        --cli-input-json file://ecs-task-definition-temp.json \
        --region $AWS_REGION
    
    print_success "Task definition registered"
}

# Create ECS service
create_ecs_service() {
    print_status "Creating ECS service..."
    
    # Get the latest task definition ARN
    TASK_DEFINITION_ARN=$(aws ecs list-task-definitions \
        --family-prefix amesa-backend-task \
        --status ACTIVE \
        --sort DESC \
        --max-items 1 \
        --region $AWS_REGION \
        --query 'taskDefinitionArns[0]' \
        --output text)
    
    # Get ALB target group ARN
    TARGET_GROUP_ARN=$(aws elbv2 describe-target-groups \
        --names "$ENVIRONMENT-amesa-tg" \
        --region $AWS_REGION \
        --query 'TargetGroups[0].TargetGroupArn' \
        --output text)
    
    # Create service
    aws ecs create-service \
        --cluster $ECS_CLUSTER \
        --service-name amesa-backend-service \
        --task-definition $TASK_DEFINITION_ARN \
        --desired-count 2 \
        --launch-type FARGATE \
        --network-configuration "awsvpcConfiguration={subnets=[subnet-12345,subnet-67890],securityGroups=[sg-12345],assignPublicIp=ENABLED}" \
        --load-balancers "targetGroupArn=$TARGET_GROUP_ARN,containerName=amesa-backend,containerPort=80" \
        --region $AWS_REGION
    
    print_success "ECS service created"
}

# Clean up temporary files
cleanup() {
    print_status "Cleaning up temporary files..."
    rm -f ecs-task-definition-temp.json
    print_success "Cleanup completed"
}

# Main deployment function
main() {
    print_status "Starting Amesa Backend Aurora PostgreSQL deployment..."
    
    check_prerequisites
    get_account_id
    create_ecr_repository
    build_and_push_image
    deploy_infrastructure
    get_stack_outputs
    update_task_definition
    register_task_definition
    create_ecs_service
    cleanup
    
    print_success "Deployment completed successfully!"
    print_status "Your application should be available at: http://$ALB_DNS"
    print_warning "Note: It may take a few minutes for the service to be fully ready."
}

# Run main function
main "$@"
