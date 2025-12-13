# Manual Setup Guide for Amesa Backend with Aurora PostgreSQL

Since AWS CLI isn't installed, here's a manual setup guide to get your backend deployed.

## Step 1: Install AWS CLI

### Option A: Download and Install
1. Go to: https://aws.amazon.com/cli/
2. Download the Windows installer
3. Run the installer and follow the setup wizard

### Option B: Using PowerShell (if you have Chocolatey)
```powershell
choco install awscli
```

### Option C: Using PowerShell (if you have Scoop)
```powershell
scoop install aws
```

## Step 2: Configure AWS CLI

After installing AWS CLI, run:
```powershell
aws configure
```

You'll need:
- **AWS Access Key ID**: Your AWS access key
- **AWS Secret Access Key**: Your AWS secret key
- **Default region name**: `eu-north-1`
- **Default output format**: `json`

## Step 3: Set Up Secrets in AWS Secrets Manager

Once AWS CLI is configured, you can either:

### Option A: Use the automated script
```powershell
.\setup-secrets-quick.ps1
```

### Option B: Create secrets manually via AWS Console
1. Go to AWS Secrets Manager in the AWS Console
2. Create the following secrets in the `eu-north-1` region:

#### Secret 1: `amesa/db-password`
- **Secret value**: `aAXa406L6qdqfTU6o8vr`
- **Description**: Aurora PostgreSQL database password

#### Secret 2: `amesa/jwt-secret`
- **Secret value**: Generate a random 64-character string
- **Description**: JWT signing secret key

#### Secret 3: `amesa/qr-code-secret`
- **Secret value**: Generate a random 64-character string
- **Description**: QR code generation secret key

#### Secret 4: `amesa/smtp-username`
- **Secret value**: Your SMTP username (or placeholder)
- **Description**: SMTP server username

#### Secret 5: `amesa/smtp-password`
- **Secret value**: Your SMTP password (or placeholder)
- **Description**: SMTP server password

#### Secret 6: `amesa/stripe-publishable-key`
- **Secret value**: Your Stripe publishable key (or placeholder)
- **Description**: Stripe publishable key

#### Secret 7: `amesa/stripe-secret-key`
- **Secret value**: Your Stripe secret key (or placeholder)
- **Description**: Stripe secret key

#### Secret 8: `amesa/stripe-webhook-secret`
- **Secret value**: Your Stripe webhook secret (or placeholder)
- **Description**: Stripe webhook secret

#### Secret 9: `amesa/paypal-client-id`
- **Secret value**: Your PayPal client ID (or placeholder)
- **Description**: PayPal client ID

#### Secret 10: `amesa/paypal-client-secret`
- **Secret value**: Your PayPal client secret (or placeholder)
- **Description**: PayPal client secret

#### Secret 11: `amesa/aws-access-key-id`
- **Secret value**: Your AWS access key ID (or placeholder)
- **Description**: AWS access key ID

#### Secret 12: `amesa/aws-secret-access-key`
- **Secret value**: Your AWS secret access key (or placeholder)
- **Description**: AWS secret access key

## Step 4: Deploy the Application

Once secrets are set up, run:
```powershell
.\deploy-with-existing-aurora.ps1
```

## Alternative: Manual Deployment Steps

If you prefer to do everything manually:

### 1. Create ECR Repository
```powershell
aws ecr create-repository --repository-name amesa-backend --region eu-north-1
```

### 2. Build and Push Docker Image
```powershell
# Get your account ID
$accountId = aws sts get-caller-identity --query Account --output text

# Login to ECR
aws ecr get-login-password --region eu-north-1 | docker login --username AWS --password-stdin "$accountId.dkr.ecr.eu-north-1.amazonaws.com"

# Build image
docker build -t amesa-backend:latest -f AmesaBackend/Dockerfile AmesaBackend/

# Tag image
docker tag amesa-backend:latest "$accountId.dkr.ecr.eu-north-1.amazonaws.com/amesa-backend:latest"

# Push image
docker push "$accountId.dkr.ecr.eu-north-1.amazonaws.com/amesa-backend:latest"
```

### 3. Deploy Infrastructure
```powershell
aws cloudformation create-stack --stack-name amesa-backend-infrastructure --template-body file://aws-infrastructure.yaml --capabilities CAPABILITY_IAM --region eu-north-1
```

### 4. Wait for Stack Creation
```powershell
aws cloudformation wait stack-create-complete --stack-name amesa-backend-infrastructure --region eu-north-1
```

### 5. Get Stack Outputs
```powershell
aws cloudformation describe-stacks --stack-name amesa-backend-infrastructure --region eu-north-1 --query 'Stacks[0].Outputs'
```

### 6. Update Task Definition
Update the `ecs-task-definition.json` file with:
- Your actual AWS account ID (replace `YOUR_ACCOUNT_ID`)
- The actual values from the stack outputs

### 7. Register Task Definition
```powershell
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json --region eu-north-1
```

### 8. Create ECS Service
```powershell
# Get the latest task definition ARN
$taskDefArn = aws ecs list-task-definitions --family-prefix amesa-backend-task --status ACTIVE --sort DESC --max-items 1 --region eu-north-1 --query 'taskDefinitionArns[0]' --output text

# Get stack outputs
$outputs = aws cloudformation describe-stacks --stack-name amesa-backend-infrastructure --region eu-north-1 --query 'Stacks[0].Outputs' --output json | ConvertFrom-Json

# Create service (you'll need to get the actual subnet and security group IDs from the stack outputs)
aws ecs create-service --cluster production-amesa-cluster --service-name amesa-backend-service --task-definition $taskDefArn --desired-count 2 --launch-type FARGATE --network-configuration "awsvpcConfiguration={subnets=[subnet-12345,subnet-67890],securityGroups=[sg-12345],assignPublicIp=ENABLED}" --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:eu-north-1:ACCOUNT:targetgroup/production-amesa-tg/ID,containerName=amesa-backend,containerPort=80" --region eu-north-1
```

## Important Notes

1. **Database Connection**: Your Aurora database is publicly accessible, so the ECS tasks should be able to connect to it.

2. **Security Groups**: You may need to update your Aurora security group to allow connections from the ECS tasks. Add an inbound rule for port 5432 from the ECS security group.

3. **Database Name**: Your Aurora cluster doesn't have a specific database name. You may need to:
   - Create a database named `amesa_lottery` in your Aurora cluster
   - Or update the `DB_NAME` environment variable in the task definition

4. **Region**: All resources are configured for `eu-north-1` region to match your Aurora database.

## Troubleshooting

- If you get permission errors, make sure your AWS user has the necessary permissions for ECS, ECR, CloudFormation, and Secrets Manager.
- If the ECS tasks fail to start, check the CloudWatch logs for error messages.
- If database connection fails, verify the security group rules and network configuration.

## Next Steps

After successful deployment:
1. Test the application at the ALB DNS name
2. Set up monitoring and alerting
3. Configure your domain to point to the ALB
4. Set up SSL certificate for HTTPS
5. Update placeholder secrets with actual values


