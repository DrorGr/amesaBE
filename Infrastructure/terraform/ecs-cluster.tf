# ECS Cluster and Service Discovery for Microservices

# ECS Cluster
resource "aws_ecs_cluster" "amesa_cluster" {
  name = "amesa-microservices-cluster"

  setting {
    name  = "containerInsights"
    value = "enabled"
  }

  tags = {
    Environment = var.environment
    Project     = "AmesaBackend"
  }
}

# Cloud Map Service Discovery Namespace
resource "aws_service_discovery_private_dns_namespace" "amesa_namespace" {
  name        = "amesa.local"
  description = "Service discovery namespace for Amesa microservices"
  vpc         = var.vpc_id
}

# Service Discovery Services for each microservice
resource "aws_service_discovery_service" "auth_service" {
  name = "auth-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "lottery_service" {
  name = "lottery-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "payment_service" {
  name = "payment-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "notification_service" {
  name = "notification-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "content_service" {
  name = "content-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "lottery_results_service" {
  name = "lottery-results-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "analytics_service" {
  name = "analytics-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

resource "aws_service_discovery_service" "admin_service" {
  name = "admin-service"

  dns_config {
    namespace_id = aws_service_discovery_private_dns_namespace.amesa_namespace.id

    dns_records {
      ttl  = 10
      type = "A"
    }

    routing_policy = "MULTIVALUE"
  }

  health_check_grace_period_seconds = 30
}

# Variables
variable "vpc_id" {
  description = "VPC ID for service discovery"
  type        = string
}

