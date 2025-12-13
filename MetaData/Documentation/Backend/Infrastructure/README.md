# Infrastructure as Code

This directory contains Infrastructure as Code (IaC) definitions for the AmesaBackend microservices architecture on AWS.

## Structure

```
Infrastructure/
├── terraform/          # Terraform configurations
│   ├── api-gateway.tf
│   ├── eventbridge.tf
│   ├── ecs-cluster.tf
│   ├── rds.tf
│   ├── alb.tf
│   └── elasticache.tf
└── cloudformation/     # CloudFormation templates (alternative)
    ├── api-gateway.yaml
    ├── eventbridge.yaml
    └── ecs-services.yaml
```

## AWS Resources

### API Gateway
- HTTP API Gateway for unified entry point
- Path-based routing to microservices
- Rate limiting and throttling
- Request/response transformation

### EventBridge
- Custom event bus: `amesa-event-bus`
- Event rules for routing
- Event schemas for validation

### ECS
- Single ECS cluster: `amesa-microservices-cluster`
- Fargate launch type
- Cloud Map service discovery
- Auto-scaling policies

### RDS
- Separate PostgreSQL instances per service (or schemas initially)
- Multi-AZ for production
- Automated backups

### ALB
- Application Load Balancer per service
- Health checks
- SSL/TLS termination

### ElastiCache
- Redis cluster for distributed caching
- Multi-AZ for high availability

## Deployment

### Prerequisites
- AWS CLI configured
- Terraform 1.5+ (if using Terraform)
- Appropriate IAM permissions

### Terraform Deployment
```bash
cd terraform
terraform init
terraform plan
terraform apply
```

### CloudFormation Deployment
```bash
aws cloudformation create-stack \
  --stack-name amesa-microservices \
  --template-body file://cloudformation/main.yaml \
  --capabilities CAPABILITY_IAM
```

