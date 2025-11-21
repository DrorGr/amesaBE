#!/bin/bash
# Script to update connection strings in appsettings.json files
# This adds schema-specific SearchPath to RDS connection strings

set -e

AURORA_ENDPOINT="amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com"
AURORA_PORT="5432"
# TODO: Update these with actual values from AWS Secrets Manager
DB_USERNAME="dror"
DB_PASSWORD="CHANGE_ME"
DB_NAME="postgres"  # Default database name, adjust if different

# Service to schema mapping
declare -A SERVICE_SCHEMAS=(
    ["AmesaBackend.Auth"]="amesa_auth"
    ["AmesaBackend.Payment"]="amesa_payment"
    ["AmesaBackend.Lottery"]="amesa_lottery"
    ["AmesaBackend.Content"]="amesa_content"
    ["AmesaBackend.Notification"]="amesa_notification"
    ["AmesaBackend.LotteryResults"]="amesa_lottery_results"
    ["AmesaBackend.Analytics"]="amesa_analytics"
)

# Redis endpoint (update after Redis cluster is available)
REDIS_ENDPOINT=""  # Will be populated from Redis cluster

echo "Updating connection strings for all services..."

# Get Redis endpoint if available
if [ -z "$REDIS_ENDPOINT" ]; then
    REDIS_ENDPOINT=$(aws elasticache describe-cache-clusters \
        --cache-cluster-id amesa-redis \
        --region eu-north-1 \
        --show-cache-node-info \
        --query "CacheClusters[0].CacheNodes[0].Endpoint.Address" \
        --output text 2>/dev/null || echo "")
    
    if [ -n "$REDIS_ENDPOINT" ] && [ "$REDIS_ENDPOINT" != "None" ]; then
        REDIS_PORT=$(aws elasticache describe-cache-clusters \
            --cache-cluster-id amesa-redis \
            --region eu-north-1 \
            --show-cache-node-info \
            --query "CacheClusters[0].CacheNodes[0].Endpoint.Port" \
            --output text)
        REDIS_CONNECTION="${REDIS_ENDPOINT}:${REDIS_PORT}"
        echo "Redis endpoint found: $REDIS_CONNECTION"
    else
        echo "⚠️ Redis cluster not available yet, skipping Redis connection string"
        REDIS_CONNECTION=""
    fi
fi

for service_path in "${!SERVICE_SCHEMAS[@]}"
do
    schema="${SERVICE_SCHEMAS[$service_path]}"
    appsettings_file="BE/$service_path/appsettings.json"
    
    if [ -f "$appsettings_file" ]; then
        echo "Updating $appsettings_file with schema $schema"
        
        # Create connection string with SearchPath
        CONNECTION_STRING="Host=${AURORA_ENDPOINT};Port=${AURORA_PORT};Database=${DB_NAME};Username=${DB_USERNAME};Password=${DB_PASSWORD};SearchPath=${schema};"
        
        # Update DefaultConnection (using jq if available, or sed)
        if command -v jq &> /dev/null; then
            # Use jq to update JSON
            jq ".ConnectionStrings.DefaultConnection = \"$CONNECTION_STRING\"" "$appsettings_file" > "${appsettings_file}.tmp"
            mv "${appsettings_file}.tmp" "$appsettings_file"
        else
            # Fallback to sed (less reliable for JSON)
            sed -i.bak "s|\"DefaultConnection\": \".*\"|\"DefaultConnection\": \"$CONNECTION_STRING\"|" "$appsettings_file"
        fi
        
        # Update Redis connection string if available
        if [ -n "$REDIS_CONNECTION" ]; then
            if command -v jq &> /dev/null; then
                jq ".ConnectionStrings.Redis = \"$REDIS_CONNECTION\"" "$appsettings_file" > "${appsettings_file}.tmp"
                mv "${appsettings_file}.tmp" "$appsettings_file"
            else
                sed -i.bak "s|\"Redis\": \".*\"|\"Redis\": \"$REDIS_CONNECTION\"|" "$appsettings_file"
            fi
        fi
        
        echo "✅ Updated $service_path"
    else
        echo "⚠️ File not found: $appsettings_file"
    fi
done

echo "Connection strings updated!"
echo ""
echo "⚠️ IMPORTANT: Update DB_USERNAME and DB_PASSWORD in this script with actual values"
echo "⚠️ Consider using AWS Secrets Manager for sensitive credentials"

