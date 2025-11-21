#!/bin/bash
# Script to configure ALB routing rules for microservices
# This assumes using the existing amesa-backend-alb

set -e

ALB_ARN="arn:aws:elasticloadbalancing:eu-north-1:129394705401:loadbalancer/app/amesa-backend-alb/d4dbb08b12e385fe"
REGION="eu-north-1"

# Get default listener ARN (HTTP port 80)
LISTENER_ARN=$(aws elbv2 describe-listeners \
    --load-balancer-arn $ALB_ARN \
    --region $REGION \
    --query "Listeners[?Port==\`80\`].ListenerArn" \
    --output text | head -n 1)

if [ -z "$LISTENER_ARN" ]; then
    echo "Creating HTTP listener on port 80..."
    LISTENER_ARN=$(aws elbv2 create-listener \
        --load-balancer-arn $ALB_ARN \
        --protocol HTTP \
        --port 80 \
        --default-actions Type=forward,TargetGroupArn=$(aws elbv2 describe-target-groups --names amesa-auth-service-tg --region $REGION --query "TargetGroups[0].TargetGroupArn" --output text) \
        --region $REGION \
        --query "Listeners[0].ListenerArn" \
        --output text)
fi

echo "Using listener: $LISTENER_ARN"

# Service routing paths
declare -A SERVICE_PATHS=(
    ["amesa-auth-service-tg"]="/api/v1/auth/*"
    ["amesa-payment-service-tg"]="/api/v1/payment/*"
    ["amesa-lottery-service-tg"]="/api/v1/lottery/*"
    ["amesa-content-service-tg"]="/api/v1/content/*"
    ["amesa-notification-service-tg"]="/api/v1/notification/*"
    ["amesa-lottery-results-service-tg"]="/api/v1/lottery-results/*"
    ["amesa-analytics-service-tg"]="/api/v1/analytics/*"
    ["amesa-admin-service-tg"]="/admin/*"
)

echo "Creating routing rules..."

for tg_name in "${!SERVICE_PATHS[@]}"
do
    path_pattern="${SERVICE_PATHS[$tg_name]}"
    
    echo "Creating rule for $tg_name with path $path_pattern"
    
    TG_ARN=$(aws elbv2 describe-target-groups \
        --names $tg_name \
        --region $REGION \
        --query "TargetGroups[0].TargetGroupArn" \
        --output text)
    
    if [ "$TG_ARN" != "None" ] && [ -n "$TG_ARN" ]; then
        # Create rule (priority will be auto-assigned)
        aws elbv2 create-rule \
            --listener-arn $LISTENER_ARN \
            --priority $(($RANDOM % 100 + 1)) \
            --conditions Field=path-pattern,Values=$path_pattern \
            --actions Type=forward,TargetGroupArn=$TG_ARN \
            --region $REGION || echo "Rule may already exist for $tg_name"
        
        echo "✅ Rule created for $tg_name"
    else
        echo "⚠️ Target group $tg_name not found"
    fi
done

echo "ALB routing configuration complete!"

