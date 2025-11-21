#!/bin/bash
# Script to create ElastiCache Redis cluster for Amesa microservices

set -e

REGION="eu-north-1"
VPC_ID="vpc-0faeeb78eded33ccf"
SUBNET_IDS="subnet-0d29edd8bb4038b7e subnet-04c4073858bc4ae3f subnet-0018fdecfe1e1dea4"
ECS_SG="sg-05a65ed059a1d14f8"

echo "Creating ElastiCache Redis cluster for Amesa microservices..."

# Create subnet group
echo "Creating cache subnet group..."
aws elasticache create-cache-subnet-group \
    --cache-subnet-group-name amesa-redis-subnet-group \
    --cache-subnet-group-description "Subnet group for Amesa Redis" \
    --subnet-ids $SUBNET_IDS \
    --region $REGION || echo "Subnet group may already exist"

# Get or create security group
echo "Creating security group for Redis..."
SG_ID=$(aws ec2 describe-security-groups \
    --filters "Name=group-name,Values=amesa-redis-sg" "Name=vpc-id,Values=$VPC_ID" \
    --region $REGION \
    --query "SecurityGroups[0].GroupId" \
    --output text)

if [ "$SG_ID" == "None" ] || [ -z "$SG_ID" ]; then
    SG_ID=$(aws ec2 create-security-group \
        --group-name amesa-redis-sg \
        --description "Security group for Amesa ElastiCache Redis" \
        --vpc-id $VPC_ID \
        --region $REGION \
        --query "GroupId" \
        --output text)
    echo "Created security group: $SG_ID"
else
    echo "Using existing security group: $SG_ID"
fi

# Add ingress rule for ECS tasks
echo "Adding ingress rule for ECS tasks..."
aws ec2 authorize-security-group-ingress \
    --group-id $SG_ID \
    --protocol tcp \
    --port 6379 \
    --source-group $ECS_SG \
    --region $REGION 2>&1 | grep -v "already exists" || echo "Ingress rule may already exist"

# Create Redis cluster
echo "Creating Redis cache cluster..."
aws elasticache create-cache-cluster \
    --cache-cluster-id amesa-redis \
    --engine redis \
    --engine-version 7.0 \
    --cache-node-type cache.t3.micro \
    --num-cache-nodes 1 \
    --cache-subnet-group-name amesa-redis-subnet-group \
    --security-group-ids $SG_ID \
    --region $REGION

echo "✅ Redis cluster creation initiated!"
echo "Check status with: aws elasticache describe-cache-clusters --cache-cluster-id amesa-redis --region $REGION"

