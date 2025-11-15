# ElastiCache Redis for Distributed Caching

# ElastiCache Subnet Group
resource "aws_elasticache_subnet_group" "amesa_redis_subnet_group" {
  name       = "amesa-redis-subnet-group-${var.environment}"
  subnet_ids = var.private_subnet_ids

  tags = {
    Name = "Amesa Redis Subnet Group"
  }
}

# ElastiCache Parameter Group
resource "aws_elasticache_parameter_group" "amesa_redis_params" {
  name   = "amesa-redis-params-${var.environment}"
  family = "redis7"

  parameter {
    name  = "maxmemory-policy"
    value = "allkeys-lru"
  }
}

# ElastiCache Replication Group (Redis Cluster)
resource "aws_elasticache_replication_group" "amesa_redis" {
  replication_group_id       = "amesa-redis-${var.environment}"
  description                = "Redis cluster for AmesaBackend microservices"
  
  engine                     = "redis"
  engine_version             = "7.0"
  node_type                  = var.redis_node_type
  port                       = 6379
  parameter_group_name       = aws_elasticache_parameter_group.amesa_redis_params.name
  
  num_cache_clusters         = var.environment == "prod" ? 2 : 1
  automatic_failover_enabled = var.environment == "prod"
  multi_az_enabled           = var.environment == "prod"
  
  subnet_group_name          = aws_elasticache_subnet_group.amesa_redis_subnet_group.name
  security_group_ids          = [aws_security_group.redis_sg.id]
  
  at_rest_encryption_enabled = true
  transit_encryption_enabled  = true
  auth_token                 = var.redis_auth_token

  snapshot_retention_limit = 7
  snapshot_window          = "03:00-05:00"

  maintenance_window         = "mon:05:00-mon:07:00"
  auto_minor_version_upgrade = true

  tags = {
    Environment = var.environment
    Project     = "AmesaBackend"
  }
}

# Security Group for Redis
resource "aws_security_group" "redis_sg" {
  name        = "amesa-redis-sg-${var.environment}"
  description = "Security group for Amesa ElastiCache Redis"
  vpc_id      = var.vpc_id

  ingress {
    description     = "Redis from ECS tasks"
    from_port       = 6379
    to_port         = 6379
    protocol        = "tcp"
    security_groups = [var.ecs_security_group_id]
  }

  egress {
    description = "Allow all outbound"
    from_port   = 0
    to_port     = 0
    protocol     = "-1"
    cidr_blocks  = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "amesa-redis-sg"
    Environment = var.environment
  }
}

# Variables
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

