#!/bin/bash

# AWS Deployment Script for Amesa Backend
# This script deploys the backend to AWS ECS using Docker

set -e

# Configuration
AWS_REGION=${AWS_REGION:-us-east-1}
ECR_REPOSITORY=${ECR_REPOSITORY:-amesa-backend}
ECS_CLUSTER=${ECS_CLUSTER:-amesa-cluster}
ECS_SERVICE=${ECS_SERVICE:-amesa-backend-service}
ECS_TASK_DEFINITION=${ECS_TASK_DEFINITION:-amesa-backend-task}

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
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
        print_warning "jq is not installed. Some features may not work properly."
    fi
    
    print_status "Prerequisites check completed."
}

# Build and push Docker image to ECR
build_and_push_image() {
    print_status "Building and pushing Docker image to ECR..."
    
    # Get AWS account ID
    AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
    ECR_URI="${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPOSITORY}"
    
    # Login to ECR
    print_status "Logging in to ECR..."
    aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ECR_URI}
    
    # Create ECR repository if it doesn't exist
    if ! aws ecr describe-repositories --repository-names ${ECR_REPOSITORY} --region ${AWS_REGION} &> /dev/null; then
        print_status "Creating ECR repository..."
        aws ecr create-repository --repository-name ${ECR_REPOSITORY} --region ${AWS_REGION}
    fi
    
    # Build Docker image
    print_status "Building Docker image..."
    docker build -t ${ECR_REPOSITORY} ./AmesaBackend
    
    # Tag image for ECR
    docker tag ${ECR_REPOSITORY}:latest ${ECR_URI}:latest
    
    # Push image to ECR
    print_status "Pushing image to ECR..."
    docker push ${ECR_URI}:latest
    
    print_status "Image pushed successfully to ${ECR_URI}:latest"
}

# Update ECS service
update_ecs_service() {
    print_status "Updating ECS service..."
    
    # Get current task definition
    CURRENT_TASK_DEF=$(aws ecs describe-task-definition --task-definition ${ECS_TASK_DEFINITION} --region ${AWS_REGION})
    
    # Create new task definition with updated image
    NEW_TASK_DEF=$(echo $CURRENT_TASK_DEF | jq --arg IMAGE "${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com/${ECR_REPOSITORY}:latest" '.taskDefinition | .containerDefinitions[0].image = $IMAGE | del(.taskDefinitionArn, .revision, .status, .requiresAttributes, .placementConstraints, .compatibilities, .registeredAt, .registeredBy)')
    
    # Register new task definition
    NEW_TASK_DEF_ARN=$(aws ecs register-task-definition --cli-input-json "$NEW_TASK_DEF" --region ${AWS_REGION} --query 'taskDefinition.taskDefinitionArn' --output text)
    
    # Update ECS service
    aws ecs update-service \
        --cluster ${ECS_CLUSTER} \
        --service ${ECS_SERVICE} \
        --task-definition ${NEW_TASK_DEF_ARN} \
        --region ${AWS_REGION} > /dev/null
    
    print_status "ECS service updated with new task definition: ${NEW_TASK_DEF_ARN}"
}

# Wait for deployment to complete
wait_for_deployment() {
    print_status "Waiting for deployment to complete..."
    
    aws ecs wait services-stable \
        --cluster ${ECS_CLUSTER} \
        --services ${ECS_SERVICE} \
        --region ${AWS_REGION}
    
    print_status "Deployment completed successfully!"
}

# Main deployment function
deploy() {
    print_status "Starting AWS deployment..."
    
    check_prerequisites
    build_and_push_image
    update_ecs_service
    wait_for_deployment
    
    print_status "Deployment completed successfully!"
}

# Show usage information
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -r, --region REGION     AWS region (default: us-east-1)"
    echo "  -e, --ecr-repo REPO     ECR repository name (default: amesa-backend)"
    echo "  -c, --cluster CLUSTER   ECS cluster name (default: amesa-cluster)"
    echo "  -s, --service SERVICE   ECS service name (default: amesa-backend-service)"
    echo "  -t, --task-def TASK     ECS task definition name (default: amesa-backend-task)"
    echo "  -h, --help             Show this help message"
    echo ""
    echo "Environment Variables:"
    echo "  AWS_REGION             AWS region"
    echo "  ECR_REPOSITORY         ECR repository name"
    echo "  ECS_CLUSTER            ECS cluster name"
    echo "  ECS_SERVICE            ECS service name"
    echo "  ECS_TASK_DEFINITION    ECS task definition name"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -r|--region)
            AWS_REGION="$2"
            shift 2
            ;;
        -e|--ecr-repo)
            ECR_REPOSITORY="$2"
            shift 2
            ;;
        -c|--cluster)
            ECS_CLUSTER="$2"
            shift 2
            ;;
        -s|--service)
            ECS_SERVICE="$2"
            shift 2
            ;;
        -t|--task-def)
            ECS_TASK_DEFINITION="$2"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

# Run deployment
deploy

