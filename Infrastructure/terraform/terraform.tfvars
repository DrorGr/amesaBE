# Terraform Variables Configuration for Amesa Microservices
# Generated from existing AWS infrastructure

environment = "prod"

# VPC Configuration
vpc_id = "vpc-0faeeb78eded33ccf"

# Subnet IDs - Private subnets for RDS and ECS tasks
private_subnet_ids = [
  "subnet-0d29edd8bb4038b7e",  # RDS-Pvt-subnet-2 (eu-north-1b)
  "subnet-04c4073858bc4ae3f",  # RDS-Pvt-subnet-1 (eu-north-1a)
  "subnet-0018fdecfe1e1dea4"   # RDS-Pvt-subnet-3 (eu-north-1c)
]

# Public subnets for ALB
public_subnet_ids = [
  "subnet-07b4ff79b68414a03",  # eu-north-1c
  "subnet-03524f913702f1073",  # eu-north-1a
  "subnet-02d8e5c23ab4a7092"   # eu-north-1b
]

# Security Groups - Using existing RDS security groups
# ECS tasks will use ec2-rds-2 for database access
ecs_security_group_id = "sg-05a65ed059a1d14f8"  # ec2-rds-2
vpc_security_group_id = "sg-05c7257248728c160"  # default (will create new one for VPC link)

# Database Configuration
db_instance_class = "db.t3.micro"
db_username       = "amesa_admin"
db_password       = "CHANGE_ME_SECURE_PASSWORD"  # TODO: Use AWS Secrets Manager

# Redis Configuration
redis_node_type   = "cache.t3.micro"
redis_auth_token  = "CHANGE_ME_REDIS_TOKEN"  # TODO: Use AWS Secrets Manager

# ALB Listener ARNs (will be populated after ALB creation)
auth_service_alb_listener_arn              = ""
payment_service_alb_listener_arn           = ""
lottery_service_alb_listener_arn           = ""
content_service_alb_listener_arn           = ""
notification_service_alb_listener_arn      = ""
lottery_results_service_alb_listener_arn   = ""
analytics_service_alb_listener_arn        = ""
admin_service_alb_listener_arn             = ""

# Event Target ARNs (will be populated after ECS service creation)
notification_service_event_target_arn = ""
payment_service_event_target_arn      = ""
lottery_service_event_target_arn      = ""
analytics_service_event_target_arn    = ""

