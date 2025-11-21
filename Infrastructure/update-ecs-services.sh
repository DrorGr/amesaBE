#!/bin/bash
# Script to update ECS services desired count to 1 after images are available

set -e

CLUSTER_NAME="Amesa"
REGION="eu-north-1"

# Services to update
declare -a SERVICES=(
    "amesa-auth-service"
    "amesa-payment-service"
    "amesa-lottery-service"
    "amesa-content-service"
    "amesa-notification-service"
    "amesa-lottery-results-service"
    "amesa-analytics-service"
    "amesa-admin-service"
)

echo "Updating ECS services desired count to 1..."

for service_name in "${SERVICES[@]}"
do
    echo "Updating $service_name..."
    
    aws ecs update-service \
        --cluster $CLUSTER_NAME \
        --service $service_name \
        --desired-count 1 \
        --region $REGION \
        --query "service.[serviceName,desiredCount,runningCount,status]" \
        --output table
    
    echo "✅ $service_name updated"
done

echo "All services updated! They will start once images are available."

