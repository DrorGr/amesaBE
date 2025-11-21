#!/bin/bash
# Script to build and push Docker images for all microservices to ECR

set -e

REGION="eu-north-1"
AWS_ACCOUNT_ID="129394705401"
ECR_BASE="${AWS_ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

# Services to build
declare -a SERVICES=(
    "AmesaBackend.Auth:amesa-auth-service"
    "AmesaBackend.Payment:amesa-payment-service"
    "AmesaBackend.Lottery:amesa-lottery-service"
    "AmesaBackend.Content:amesa-content-service"
    "AmesaBackend.Notification:amesa-notification-service"
    "AmesaBackend.LotteryResults:amesa-lottery-results-service"
    "AmesaBackend.Analytics:amesa-analytics-service"
    "AmesaBackend.Admin:amesa-admin-service"
)

echo "Logging in to ECR..."
aws ecr get-login-password --region $REGION | docker login --username AWS --password-stdin $ECR_BASE

echo "Building and pushing Docker images..."

for service_config in "${SERVICES[@]}"
do
    IFS=':' read -r project_path ecr_repo <<< "$service_config"
    
    echo "=========================================="
    echo "Processing: $project_path -> $ecr_repo"
    echo "=========================================="
    
    IMAGE_URI="${ECR_BASE}/${ecr_repo}"
    
    # Build the image
    echo "Building Docker image for $project_path..."
    docker build -t $ecr_repo:latest -f BE/$project_path/Dockerfile BE/
    
    # Tag for ECR
    docker tag $ecr_repo:latest ${IMAGE_URI}:latest
    docker tag $ecr_repo:latest ${IMAGE_URI}:$(date +%Y%m%d-%H%M%S)
    
    # Push to ECR
    echo "Pushing to ECR: ${IMAGE_URI}"
    docker push ${IMAGE_URI}:latest
    docker push ${IMAGE_URI}:$(date +%Y%m%d-%H%M%S)
    
    echo "✅ $ecr_repo pushed successfully"
done

echo "=========================================="
echo "All Docker images built and pushed!"
echo "=========================================="

