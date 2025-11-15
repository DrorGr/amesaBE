variable "environment" {
  description = "Environment name (dev, stage, prod)"
  type        = string
  default     = "dev"
}

variable "vpc_id" {
  description = "VPC ID for resources"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs"
  type        = list(string)
}

variable "public_subnet_ids" {
  description = "Public subnet IDs"
  type        = list(string)
}

variable "ecs_security_group_id" {
  description = "Security group ID for ECS tasks"
  type        = string
}

variable "vpc_security_group_id" {
  description = "Security group ID for VPC link"
  type        = string
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}

variable "db_username" {
  description = "Database master username"
  type        = string
  sensitive   = true
}

variable "db_password" {
  description = "Database master password"
  type        = string
  sensitive   = true
}

variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "redis_auth_token" {
  description = "Redis AUTH token"
  type        = string
  sensitive   = true
}

# ALB Listener ARNs (outputs from ALB resources)
variable "auth_service_alb_listener_arn" {
  description = "ARN of Auth Service ALB listener"
  type        = string
  default     = ""
}

variable "lottery_service_alb_listener_arn" {
  description = "ARN of Lottery Service ALB listener"
  type        = string
  default     = ""
}

variable "payment_service_alb_listener_arn" {
  description = "ARN of Payment Service ALB listener"
  type        = string
  default     = ""
}

variable "notification_service_alb_listener_arn" {
  description = "ARN of Notification Service ALB listener"
  type        = string
  default     = ""
}

variable "content_service_alb_listener_arn" {
  description = "ARN of Content Service ALB listener"
  type        = string
  default     = ""
}

variable "lottery_results_service_alb_listener_arn" {
  description = "ARN of Lottery Results Service ALB listener"
  type        = string
  default     = ""
}

variable "analytics_service_alb_listener_arn" {
  description = "ARN of Analytics Service ALB listener"
  type        = string
  default     = ""
}

variable "admin_service_alb_listener_arn" {
  description = "ARN of Admin Service ALB listener"
  type        = string
  default     = ""
}

# Event Target ARNs
variable "notification_service_event_target_arn" {
  description = "ARN of Notification Service event target"
  type        = string
  default     = ""
}

variable "payment_service_event_target_arn" {
  description = "ARN of Payment Service event target"
  type        = string
  default     = ""
}

variable "lottery_service_event_target_arn" {
  description = "ARN of Lottery Service event target"
  type        = string
  default     = ""
}

variable "analytics_service_event_target_arn" {
  description = "ARN of Analytics Service event target"
  type        = string
  default     = ""
}

