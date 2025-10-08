# Amesa Backend Aurora PostgreSQL Deployment Guide

This guide will help you deploy your Amesa Backend to AWS ECS with Aurora PostgreSQL database.

## Prerequisites

Before starting the deployment, ensure you have the following installed and configured:

### Required Tools
- **AWS CLI** (v2.x recommended)
- **Docker** (latest version)
- **jq** (for JSON processing on Linux/Mac)
- **PowerShell** (for Windows users)

### AWS Configuration
1. **AWS Account**: You need an active AWS account with appropriate permissions
2. **AWS CLI Configuration**: Run `aws configure` to set up your credentials
3. **Required AWS Permissions**: Your AWS user/role needs permissions for:
   - ECS (Elastic Container Service)
   - ECR (Elastic Container Registry)
   - CloudFormation
   - RDS (Aurora PostgreSQL)
   - ElastiCache (Redis)
   - VPC, Security Groups, Subnets
   - IAM (for creating roles)
   - S3 (for file storage)
   - Secrets Manager

## Deployment Options

### Option 1: Automated Deployment (Recommended)

#### For Linux/Mac Users:
```bash
cd backend
chmod +x deploy-aurora.sh
./deploy-aurora.sh
```

#### For Windows Users:
```powershell
cd backend
.\deploy-aurora.ps1
```

### Option 2: Manual Step-by-Step Deployment

If you prefer to run each step manually or need to customize the deployment:

#### Step 1: Create ECR Repository
```bash
aws ecr create-repository --repository-name amesa-backend --region us-east-1
```

#### Step 2: Build and Push Docker Image
```bash
# Login to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com

# Build image
docker build -t amesa-backend:latest -f AmesaBackend/Dockerfile AmesaBackend/

# Tag image
docker tag amesa-backend:latest YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/amesa-backend:latest

# Push image
docker push YOUR_ACCOUNT_ID.dkr.ecr.us-east-1.amazonaws.com/amesa-backend:latest
```

#### Step 3: Deploy Infrastructure
```bash
aws cloudformation create-stack \
  --stack-name amesa-backend-aurora \
  --template-body file://aws-infrastructure.yaml \
  --parameters ParameterKey=Environment,ParameterValue=production \
              ParameterKey=DatabasePassword,ParameterValue=YOUR_SECURE_PASSWORD \
  --capabilities CAPABILITY_IAM \
  --region us-east-1
```

#### Step 4: Wait for Stack Creation
```bash
aws cloudformation wait stack-create-complete --stack-name amesa-backend-aurora --region us-east-1
```

#### Step 5: Get Stack Outputs
```bash
aws cloudformation describe-stacks \
  --stack-name amesa-backend-aurora \
  --region us-east-1 \
  --query 'Stacks[0].Outputs'
```

#### Step 6: Update Task Definition
Update the `ecs-task-definition.json` file with the actual values from the stack outputs:
- Replace `YOUR_ACCOUNT_ID` with your AWS account ID
- Replace `YOUR_AURORA_CLUSTER_ENDPOINT` with the database endpoint
- Replace `YOUR_ELASTICACHE_ENDPOINT` with the Redis endpoint

#### Step 7: Register Task Definition
```bash
aws ecs register-task-definition --cli-input-json file://ecs-task-definition.json --region us-east-1
```

#### Step 8: Create ECS Service
```bash
aws ecs create-service \
  --cluster production-amesa-cluster \
  --service-name amesa-backend-service \
  --task-definition amesa-backend-task:1 \
  --desired-count 2 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-12345,subnet-67890],securityGroups=[sg-12345],assignPublicIp=ENABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:us-east-1:ACCOUNT:targetgroup/production-amesa-tg/ID,containerName=amesa-backend,containerPort=80" \
  --region us-east-1
```

## Configuration Details

### Aurora PostgreSQL Configuration
- **Engine**: Aurora PostgreSQL 15.4
- **Instance Class**: db.r6g.large (2 instances for high availability)
- **Storage**: Aurora managed storage (auto-scaling)
- **Backup**: 7 days retention
- **Encryption**: Enabled at rest
- **Multi-AZ**: Enabled for high availability

### ECS Configuration
- **Launch Type**: Fargate
- **CPU**: 512 units (0.5 vCPU)
- **Memory**: 1024 MB (1 GB)
- **Desired Count**: 2 tasks for high availability
- **Health Check**: HTTP endpoint `/health`

### Security Configuration
- **VPC**: Custom VPC with public and private subnets
- **Security Groups**: Restrictive rules allowing only necessary traffic
- **Secrets**: Stored in AWS Secrets Manager
- **IAM Roles**: Least privilege access

## Environment Variables

The following environment variables are configured in the ECS task definition:

### Database Configuration
- `DB_HOST`: Aurora cluster endpoint
- `DB_NAME`: amesa_lottery
- `DB_USER`: amesa_user
- `DB_PORT`: 5432
- `DB_PASSWORD`: Retrieved from Secrets Manager

### Application Configuration
- `ASPNETCORE_ENVIRONMENT`: Production
- `JWT_SECRET_KEY`: Retrieved from Secrets Manager
- `REDIS_CONNECTION_STRING`: ElastiCache endpoint
- `AWS_REGION`: us-east-1
- `AWS_S3_BUCKET`: S3 bucket for file uploads

### External Services
- `STRIPE_*`: Payment processing keys
- `PAYPAL_*`: PayPal integration keys
- `SMTP_*`: Email service configuration

## Secrets Management

The following secrets are stored in AWS Secrets Manager:
- Database password
- JWT secret key
- SMTP credentials
- Stripe API keys
- PayPal API keys
- AWS access keys
- QR code secret key

## Monitoring and Logging

### CloudWatch Logs
- Log group: `/ecs/production-amesa-backend`
- Retention: 30 days
- Structured logging with Serilog

### Health Checks
- Application health endpoint: `/health`
- ECS health checks every 30 seconds
- ALB health checks every 30 seconds

## Troubleshooting

### Common Issues

#### 1. ECS Tasks Failing to Start
- Check CloudWatch logs for error messages
- Verify security group rules allow traffic
- Ensure task definition has correct image URI
- Check if secrets are accessible

#### 2. Database Connection Issues
- Verify Aurora cluster is accessible from ECS tasks
- Check security group rules for port 5432
- Ensure database credentials are correct
- Verify VPC configuration

#### 3. Load Balancer Health Check Failures
- Check if application is listening on port 80
- Verify health endpoint `/health` is responding
- Check security group rules for ALB to ECS communication

### Useful Commands

#### Check ECS Service Status
```bash
aws ecs describe-services --cluster production-amesa-cluster --services amesa-backend-service --region us-east-1
```

#### View Application Logs
```bash
aws logs tail /ecs/production-amesa-backend --follow --region us-east-1
```

#### Check Task Definition
```bash
aws ecs describe-task-definition --task-definition amesa-backend-task --region us-east-1
```

#### Test Database Connection
```bash
# Connect to Aurora cluster (requires psql client)
psql -h YOUR_AURORA_ENDPOINT -U amesa_user -d amesa_lottery
```

## Scaling and Performance

### Horizontal Scaling
- Increase `desired-count` in ECS service to scale out
- Aurora automatically handles read replicas
- ALB distributes traffic across tasks

### Vertical Scaling
- Update task definition with higher CPU/memory
- Aurora supports instance class upgrades
- Consider Aurora Serverless for variable workloads

## Cost Optimization

### Aurora Cost Optimization
- Use Aurora Serverless for development/staging
- Enable Aurora Auto Scaling
- Monitor and optimize query performance
- Use appropriate instance classes

### ECS Cost Optimization
- Use Fargate Spot for non-critical workloads
- Right-size CPU and memory allocation
- Monitor resource utilization
- Use CloudWatch metrics for optimization

## Security Best Practices

1. **Network Security**
   - Use private subnets for databases
   - Implement security groups with least privilege
   - Enable VPC Flow Logs

2. **Data Security**
   - Encrypt data at rest and in transit
   - Use AWS Secrets Manager for sensitive data
   - Implement proper IAM roles and policies

3. **Application Security**
   - Regular security updates
   - Input validation and sanitization
   - Rate limiting and DDoS protection

## Backup and Disaster Recovery

### Database Backups
- Aurora automated backups (7 days retention)
- Point-in-time recovery capability
- Cross-region backup replication (optional)

### Application Backups
- ECR image versioning
- Infrastructure as Code (CloudFormation)
- Configuration backup in version control

## Support and Maintenance

### Regular Maintenance Tasks
- Monitor CloudWatch metrics and alarms
- Review and update security groups
- Update application dependencies
- Review and rotate secrets

### Monitoring Setup
- Set up CloudWatch alarms for key metrics
- Configure SNS notifications for critical alerts
- Monitor Aurora performance insights
- Track ECS service health

## Next Steps

After successful deployment:

1. **Configure DNS**: Point your domain to the ALB DNS name
2. **SSL Certificate**: Add HTTPS listener with SSL certificate
3. **Monitoring**: Set up comprehensive monitoring and alerting
4. **Backup Strategy**: Implement cross-region backups if needed
5. **Performance Testing**: Load test your application
6. **Security Audit**: Conduct security review and penetration testing

For additional support or questions, refer to the AWS documentation or contact your system administrator.


