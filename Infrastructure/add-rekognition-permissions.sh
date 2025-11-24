#!/bin/bash
# Add AWS Rekognition Permissions to ECS Task Role
# This script adds the necessary Rekognition permissions for ID verification

set -e

# Configuration
# Note: Based on task definition, the role is "ecsTaskExecutionRole"
# If your task uses a different role, update this variable
TASK_ROLE_NAME="ecsTaskExecutionRole"  # Update if your task uses a different role
ACCOUNT_ID="129394705401"
REGION="eu-north-1"
POLICY_NAME="RekognitionIDVerificationPolicy"

echo "========================================="
echo "AWS Rekognition IAM Permissions Setup"
echo "========================================="
echo ""

# Check if AWS CLI is installed
echo "Checking AWS CLI installation..."
if ! command -v aws &> /dev/null; then
    echo "✗ AWS CLI not found. Please install AWS CLI first."
    echo "  Download from: https://aws.amazon.com/cli/"
    exit 1
fi
echo "✓ AWS CLI found: $(aws --version)"

# Check AWS credentials
echo ""
echo "Checking AWS credentials..."
CALLER_IDENTITY=$(aws sts get-caller-identity --region $REGION 2>&1)
if [ $? -ne 0 ]; then
    echo "✗ AWS credentials not configured or invalid"
    echo "  Run: aws configure"
    exit 1
fi

ACCOUNT=$(echo $CALLER_IDENTITY | jq -r '.Account')
echo "✓ AWS credentials configured"
echo "  Account ID: $ACCOUNT"

if [ "$ACCOUNT" != "$ACCOUNT_ID" ]; then
    echo "⚠ Warning: Account ID mismatch. Expected: $ACCOUNT_ID, Got: $ACCOUNT"
fi

# Create policy JSON file
POLICY_JSON=$(cat <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "rekognition:DetectFaces",
        "rekognition:CompareFaces",
        "rekognition:DetectText"
      ],
      "Resource": "*"
    }
  ]
}
EOF
)

echo "$POLICY_JSON" > /tmp/rekognition-policy.json

echo ""
echo "Policy to be attached:"
echo "$POLICY_JSON"
echo ""

# Check if policy already exists
echo "Checking if policy already exists..."
EXISTING_POLICIES=$(aws iam list-role-policies --role-name $TASK_ROLE_NAME --region $REGION 2>&1)

if echo "$EXISTING_POLICIES" | jq -e ".PolicyNames[] | select(. == \"$POLICY_NAME\")" > /dev/null 2>&1; then
    echo "⚠ Policy '$POLICY_NAME' already exists. Updating..."
    
    aws iam put-role-policy \
        --role-name $TASK_ROLE_NAME \
        --policy-name $POLICY_NAME \
        --policy-document file:///tmp/rekognition-policy.json \
        --region $REGION
    
    if [ $? -eq 0 ]; then
        echo "✓ Policy updated successfully"
    else
        echo "✗ Failed to update policy"
        exit 1
    fi
else
    echo "Creating new policy..."
    
    aws iam put-role-policy \
        --role-name $TASK_ROLE_NAME \
        --policy-name $POLICY_NAME \
        --policy-document file:///tmp/rekognition-policy.json \
        --region $REGION
    
    if [ $? -eq 0 ]; then
        echo "✓ Policy created successfully"
    else
        echo "✗ Failed to create policy"
        exit 1
    fi
fi

# Verify policy was attached
echo ""
echo "Verifying policy attachment..."
POLICY_DOCUMENT=$(aws iam get-role-policy \
    --role-name $TASK_ROLE_NAME \
    --policy-name $POLICY_NAME \
    --region $REGION 2>&1)

if [ $? -eq 0 ]; then
    echo "✓ Policy verified successfully"
    echo ""
    echo "Policy Details:"
    echo "  Role: $TASK_ROLE_NAME"
    echo "  Policy Name: $POLICY_NAME"
    echo "  Permissions:"
    echo "    - rekognition:DetectFaces"
    echo "    - rekognition:CompareFaces"
    echo "    - rekognition:DetectText"
else
    echo "⚠ Could not verify policy (this may be normal if permissions are limited)"
fi

# Cleanup
rm -f /tmp/rekognition-policy.json

echo ""
echo "========================================="
echo "✓ Setup Complete!"
echo "========================================="
echo ""
echo "Next Steps:"
echo "1. Verify the policy in AWS Console: IAM → Roles → $TASK_ROLE_NAME"
echo "2. Test Rekognition access from ECS task (check logs after deployment)"
echo "3. Deploy amesa-auth-service to apply changes"
echo ""

