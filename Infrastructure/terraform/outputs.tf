# Outputs for Infrastructure Resources

output "api_gateway_url" {
  description = "API Gateway HTTP API URL"
  value       = aws_apigatewayv2_api.amesa_api.api_endpoint
}

output "api_gateway_id" {
  description = "API Gateway HTTP API ID"
  value       = aws_apigatewayv2_api.amesa_api.id
}

output "event_bus_arn" {
  description = "EventBridge Event Bus ARN"
  value       = aws_cloudwatch_event_bus.amesa_event_bus.arn
}

output "ecs_cluster_id" {
  description = "ECS Cluster ID"
  value       = aws_ecs_cluster.amesa_cluster.id
}

output "ecs_cluster_arn" {
  description = "ECS Cluster ARN"
  value       = aws_ecs_cluster.amesa_cluster.arn
}

output "service_discovery_namespace_id" {
  description = "Cloud Map Service Discovery Namespace ID"
  value       = aws_service_discovery_private_dns_namespace.amesa_namespace.id
}

# Database Endpoints
output "auth_db_endpoint" {
  description = "Auth Service Database Endpoint"
  value       = aws_db_instance.auth_db.endpoint
  sensitive   = true
}

output "lottery_db_endpoint" {
  description = "Lottery Service Database Endpoint"
  value       = aws_db_instance.lottery_db.endpoint
  sensitive   = true
}

output "payment_db_endpoint" {
  description = "Payment Service Database Endpoint"
  value       = aws_db_instance.payment_db.endpoint
  sensitive   = true
}

output "notification_db_endpoint" {
  description = "Notification Service Database Endpoint"
  value       = aws_db_instance.notification_db.endpoint
  sensitive   = true
}

output "content_db_endpoint" {
  description = "Content Service Database Endpoint"
  value       = aws_db_instance.content_db.endpoint
  sensitive   = true
}

output "lottery_results_db_endpoint" {
  description = "Lottery Results Service Database Endpoint"
  value       = aws_db_instance.lottery_results_db.endpoint
  sensitive   = true
}

output "analytics_db_endpoint" {
  description = "Analytics Service Database Endpoint"
  value       = aws_db_instance.analytics_db.endpoint
  sensitive   = true
}

output "admin_db_endpoint" {
  description = "Admin Service Database Endpoint"
  value       = aws_db_instance.admin_db.endpoint
  sensitive   = true
}

# ALB DNS Names
output "auth_service_alb_dns" {
  description = "Auth Service ALB DNS Name"
  value       = aws_lb.auth_service_alb.dns_name
}

output "lottery_service_alb_dns" {
  description = "Lottery Service ALB DNS Name"
  value       = aws_lb.lottery_service_alb.dns_name
}

output "payment_service_alb_dns" {
  description = "Payment Service ALB DNS Name"
  value       = aws_lb.payment_service_alb.dns_name
}

output "notification_service_alb_dns" {
  description = "Notification Service ALB DNS Name"
  value       = aws_lb.notification_service_alb.dns_name
}

output "content_service_alb_dns" {
  description = "Content Service ALB DNS Name"
  value       = aws_lb.content_service_alb.dns_name
}

output "lottery_results_service_alb_dns" {
  description = "Lottery Results Service ALB DNS Name"
  value       = aws_lb.lottery_results_service_alb.dns_name
}

output "analytics_service_alb_dns" {
  description = "Analytics Service ALB DNS Name"
  value       = aws_lb.analytics_service_alb.dns_name
}

output "admin_service_alb_dns" {
  description = "Admin Service ALB DNS Name"
  value       = aws_lb.admin_service_alb.dns_name
}

# Redis Endpoint
output "redis_endpoint" {
  description = "ElastiCache Redis Primary Endpoint"
  value       = aws_elasticache_replication_group.amesa_redis.primary_endpoint_address
  sensitive   = true
}

output "redis_port" {
  description = "ElastiCache Redis Port"
  value       = aws_elasticache_replication_group.amesa_redis.port
}

