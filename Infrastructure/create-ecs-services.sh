#!/bin/bash
# Script to create ECS services for all microservices
# Run this after task definitions are registered and Docker images are pushed to ECR

set -e

CLUSTER_NAME="Amesa"
REGION="eu-north-1"
VPC_ID="vpc-0faeeb78eded33ccf"
SUBNET_IDS="subnet-0d29edd8bb4038b7e,subnet-04c4073858bc4ae3f,subnet-0018fdecfe1e1dea4"
SECURITY_GROUP="sg-05a65ed059a1d14f8"

# Service configurations: service_name:task_definition:desired_count
declare -a SERVICES=(
    "amesa-auth-service:amesa-auth-service:1"
    "amesa-payment-service:amesa-payment-service:1"
    "amesa-lottery-service:amesa-lottery-service:1"
    "amesa-content-service:amesa-content-service:1"
    "amesa-notification-service:amesa-notification-service:1"
    "amesa-lottery-results-service:amesa-lottery-results-service:1"
    "amesa-analytics-service:amesa-analytics-service:1"
    "amesa-admin-service:amesa-admin-service:1"
)

echo "Creating ECS services in cluster: $CLUSTER_NAME"

for service_config in "${SERVICES[@]}"
do
    IFS=':' read -r service_name task_def desired_count <<< "$service_config"
    
    echo "Creating service: $service_name"
    
    aws ecs create-service \
        --cluster $CLUSTER_NAME \
        --service-name $service_name \
        --task-definition $task_def \
        --desired-count $desired_count \
        --launch-type FARGATE \
        --network-configuration "awsvpcConfiguration={subnets=[$SUBNET_IDS],securityGroups=[$SECURITY_GROUP],assignPublicIp=DISABLED}" \
        --region $REGION \
        --enable-execute-command \
        --health-check-grace-period-seconds 60
    
    echo "✅ Service $service_name created"
done

echo "All ECS services created successfully!"

