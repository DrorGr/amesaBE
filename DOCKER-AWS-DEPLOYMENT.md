# Amesa Backend - Docker & AWS Deployment Guide

This guide covers deploying the Amesa Backend to AWS using Docker containers with ECS Fargate.

## Prerequisites

### Required Tools
- Docker Desktop
- AWS CLI v2
- jq (for JSON processing)
- Git

### AWS Account Setup
1. Create an AWS account
2. Configure AWS CLI with your credentials
3. Ensure you have the necessary IAM permissions

## Quick Start

### 1. Local Development with Docker

```bash
# Clone the repository
git clone <your-repo-url>
cd AmesaBase/backend

# Copy environment file
cp env.example .env

# Edit environment variables
nano .env

# Start development environment
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.dev.yml logs -f api-dev
```

### 2. Production Deployment to AWS

```bash
# Make deployment script executable
chmod +x aws-deploy.sh

# Deploy to AWS
./aws-deploy.sh --region us-east-1 --cluster amesa-cluster
```

## Detailed Setup

### Environment Configuration

Create a `.env` file based on `env.example`:

```bash
# Database Configuration
DB_PASSWORD=your_secure_database_password
DB_HOST=your-rds-endpoint.amazonaws.com
DB_NAME=amesa_lottery
DB_USER=amesa_user
DB_PORT=5432

# Redis Configuration
REDIS_PASSWORD=your_redis_password
REDIS_CONNECTION_STRING=your-elasticache-endpoint:6379,password=your_redis_password

# JWT Configuration
JWT_SECRET_KEY=your-super-secret-key-that-is-at-least-32-characters-long
JWT_ISSUER=AmesaLottery
JWT_AUDIENCE=AmesaLotteryUsers

# Frontend URLs
FRONTEND_URL=https://amesa.com
FRONTEND_URL_WWW=https://www.amesa.com
API_URL=https://api.amesa.com

# Email Configuration
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
FROM_EMAIL=noreply@amesa.com
FROM_NAME=Amesa Lottery

# Payment Configuration
STRIPE_PUBLISHABLE_KEY=pk_live_your_stripe_publishable_key
STRIPE_SECRET_KEY=sk_live_your_stripe_secret_key
STRIPE_WEBHOOK_SECRET=whsec_your_webhook_secret
PAYPAL_CLIENT_ID=your_paypal_client_id
PAYPAL_CLIENT_SECRET=your_paypal_client_secret
PAYPAL_ENVIRONMENT=production

# File Storage Configuration
FILE_STORAGE_PROVIDER=AWS
AWS_ACCESS_KEY_ID=your_aws_access_key_id
AWS_SECRET_ACCESS_KEY=your_aws_secret_access_key
AWS_REGION=us-east-1
AWS_S3_BUCKET=amesa-uploads

# QR Code Configuration
QR_CODE_SECRET_KEY=your-qr-code-secret-key-change-this-in-production
```

### AWS Infrastructure Setup

#### 1. Deploy Infrastructure with CloudFormation

```bash
# Deploy the infrastructure stack
aws cloudformation create-stack \
  --stack-name amesa-backend-infrastructure \
  --template-body file://aws-infrastructure.yaml \
  --parameters ParameterKey=Environment,ParameterValue=production \
  --capabilities CAPABILITY_IAM

# Wait for stack creation to complete
aws cloudformation wait stack-create-complete \
  --stack-name amesa-backend-infrastructure
```

#### 2. Store Secrets in AWS Secrets Manager

```bash
# Store database password
aws secretsmanager create-secret \
  --name "amesa/db-password" \
  --description "Database password for Amesa backend" \
  --secret-string "your_secure_database_password"

# Store JWT secret
aws secretsmanager create-secret \
  --name "amesa/jwt-secret" \
  --description "JWT secret key for Amesa backend" \
  --secret-string "your-super-secret-key-that-is-at-least-32-characters-long"

# Store SMTP credentials
aws secretsmanager create-secret \
  --name "amesa/smtp-username" \
  --description "SMTP username for email service" \
  --secret-string "your-email@gmail.com"

aws secretsmanager create-secret \
  --name "amesa/smtp-password" \
  --description "SMTP password for email service" \
  --secret-string "your-app-password"

# Store Stripe keys
aws secretsmanager create-secret \
  --name "amesa/stripe-publishable-key" \
  --description "Stripe publishable key" \
  --secret-string "pk_live_your_stripe_publishable_key"

aws secretsmanager create-secret \
  --name "amesa/stripe-secret-key" \
  --description "Stripe secret key" \
  --secret-string "sk_live_your_stripe_secret_key"

aws secretsmanager create-secret \
  --name "amesa/stripe-webhook-secret" \
  --description "Stripe webhook secret" \
  --secret-string "whsec_your_webhook_secret"

# Store PayPal credentials
aws secretsmanager create-secret \
  --name "amesa/paypal-client-id" \
  --description "PayPal client ID" \
  --secret-string "your_paypal_client_id"

aws secretsmanager create-secret \
  --name "amesa/paypal-client-secret" \
  --description "PayPal client secret" \
  --secret-string "your_paypal_client_secret"

# Store AWS credentials
aws secretsmanager create-secret \
  --name "amesa/aws-access-key-id" \
  --description "AWS access key ID for S3 access" \
  --secret-string "your_aws_access_key_id"

aws secretsmanager create-secret \
  --name "amesa/aws-secret-access-key" \
  --description "AWS secret access key for S3 access" \
  --secret-string "your_aws_secret_access_key"

# Store QR code secret
aws secretsmanager create-secret \
  --name "amesa/qr-code-secret" \
  --description "QR code secret key" \
  --secret-string "your-qr-code-secret-key-change-this-in-production"
```

#### 3. Update Task Definition

Edit `ecs-task-definition.json` and replace the following placeholders:
- `YOUR_ACCOUNT_ID` with your AWS account ID
- `YOUR_RDS_ENDPOINT` with your RDS endpoint
- `YOUR_ELASTICACHE_ENDPOINT` with your ElastiCache endpoint

#### 4. Register Task Definition

```bash
# Register the task definition
aws ecs register-task-definition \
  --cli-input-json file://ecs-task-definition.json
```

#### 5. Create ECS Service

```bash
# Create ECS service
aws ecs create-service \
  --cluster production-amesa-cluster \
  --service-name amesa-backend-service \
  --task-definition amesa-backend-task:1 \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345,subnet-67890],securityGroups=[sg-12345],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:us-east-1:123456789012:targetgroup/production-amesa-tg/1234567890123456,containerName=amesa-backend,containerPort=80"
```

### Database Migration

#### 1. Connect to RDS and Run Migrations

```bash
# Connect to RDS instance
psql -h your-rds-endpoint.amazonaws.com -U amesa_user -d amesa_lottery

# Run the database schema
\i database-schema.sql
```

#### 2. Alternative: Use ECS Task for Migration

```bash
# Create a one-time ECS task to run migrations
aws ecs run-task \
  --cluster production-amesa-cluster \
  --task-definition amesa-backend-task:1 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345],securityGroups=[sg-12345],assignPublicIp=ENABLED}" \
  --overrides '{
    "containerOverrides": [
      {
        "name": "amesa-backend",
        "command": ["dotnet", "ef", "database", "update"]
      }
    ]
  }'
```

## Monitoring and Logging

### CloudWatch Logs

Logs are automatically sent to CloudWatch. View them in the AWS Console:
- Log Group: `/ecs/production-amesa-backend`
- Log Stream: `ecs/amesa-backend/{task-id}`

### Health Checks

The application includes health checks at `/health` endpoint:
- ECS health check: Every 30 seconds
- ALB health check: Every 30 seconds

### Monitoring Commands

```bash
# Check ECS service status
aws ecs describe-services \
  --cluster production-amesa-cluster \
  --services amesa-backend-service

# View recent logs
aws logs tail /ecs/production-amesa-backend --follow

# Check ALB target health
aws elbv2 describe-target-health \
  --target-group-arn arn:aws:elasticloadbalancing:us-east-1:123456789012:targetgroup/production-amesa-tg/1234567890123456
```

## Scaling and Performance

### Auto Scaling

Create an auto-scaling configuration:

```bash
# Register scalable target
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/production-amesa-cluster/amesa-backend-service \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 2 \
  --max-capacity 10

# Create scaling policy
aws application-autoscaling put-scaling-policy \
  --service-namespace ecs \
  --resource-id service/production-amesa-cluster/amesa-backend-service \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-name amesa-backend-scaling-policy \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration '{
    "TargetValue": 70.0,
    "PredefinedMetricSpecification": {
      "PredefinedMetricType": "ECSServiceAverageCPUUtilization"
    }
  }'
```

### Performance Optimization

1. **Database Connection Pooling**: Configured in `appsettings.Production.json`
2. **Redis Caching**: Enabled for session storage and caching
3. **Response Compression**: Enabled in the application
4. **CDN**: Consider using CloudFront for static assets

## Security Best Practices

### 1. Network Security
- ECS tasks run in private subnets
- Database and Redis are not publicly accessible
- Security groups restrict access to necessary ports only

### 2. Secrets Management
- All sensitive data stored in AWS Secrets Manager
- No secrets in environment variables or code
- IAM roles for secure access to AWS services

### 3. SSL/TLS
- Use Application Load Balancer with SSL certificate
- Redirect HTTP to HTTPS
- Security headers configured in Nginx

### 4. Container Security
- Non-root user in Docker container
- Minimal base image (Alpine Linux)
- Regular security updates

## Troubleshooting

### Common Issues

#### 1. Container Won't Start
```bash
# Check ECS task logs
aws logs get-log-events \
  --log-group-name /ecs/production-amesa-backend \
  --log-stream-name ecs/amesa-backend/{task-id}
```

#### 2. Database Connection Issues
- Verify RDS endpoint and security groups
- Check database credentials in Secrets Manager
- Ensure ECS task has access to RDS subnet

#### 3. Health Check Failures
- Verify application is listening on port 80
- Check health endpoint responds correctly
- Review security group rules

#### 4. High Memory Usage
- Monitor CloudWatch metrics
- Consider increasing task memory allocation
- Review application for memory leaks

### Debug Commands

```bash
# Connect to running container
aws ecs execute-command \
  --cluster production-amesa-cluster \
  --task {task-id} \
  --container amesa-backend \
  --interactive \
  --command "/bin/sh"

# View ECS service events
aws ecs describe-services \
  --cluster production-amesa-cluster \
  --services amesa-backend-service \
  --query 'services[0].events'

# Check target group health
aws elbv2 describe-target-health \
  --target-group-arn {target-group-arn}
```

## Backup and Recovery

### Database Backups
- RDS automated backups enabled (7 days retention)
- Manual snapshots can be created for longer retention
- Point-in-time recovery available

### Application Backups
- Docker images stored in ECR
- Infrastructure defined in CloudFormation
- Configuration in version control

### Disaster Recovery
1. Create RDS read replica in different region
2. Set up cross-region backup for S3
3. Document recovery procedures
4. Test recovery process regularly

## Cost Optimization

### 1. Use Fargate Spot
- Configure Fargate Spot for non-critical workloads
- Can reduce costs by up to 70%

### 2. Right-size Resources
- Monitor CPU and memory usage
- Adjust task definition based on actual usage
- Use CloudWatch metrics for optimization

### 3. Scheduled Scaling
- Scale down during low-traffic periods
- Use AWS Lambda for scheduled scaling

## Maintenance

### Regular Tasks
1. **Weekly**: Review CloudWatch metrics and logs
2. **Monthly**: Update base Docker images
3. **Quarterly**: Review and rotate secrets
4. **Annually**: Review and update infrastructure

### Updates and Deployments
1. Build new Docker image
2. Push to ECR
3. Update ECS service with new task definition
4. Monitor deployment health
5. Rollback if issues detected

This deployment setup provides a robust, scalable, and secure foundation for the Amesa Backend on AWS.

