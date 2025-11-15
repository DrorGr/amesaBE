# API Gateway HTTP API for Microservices
# Provides unified entry point with path-based routing

resource "aws_apigatewayv2_api" "amesa_api" {
  name          = "amesa-microservices-api"
  protocol_type = "HTTP"
  description   = "API Gateway for AmesaBackend microservices"

  cors_configuration {
    allow_origins = ["*"] # Configure based on your frontend URL
    allow_methods = ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"]
    allow_headers = ["*"]
    allow_credentials = true
    max_age         = 3600
  }

  tags = {
    Environment = var.environment
    Project     = "AmesaBackend"
  }
}

# Integration for Auth Service
resource "aws_apigatewayv2_integration" "auth_service" {
  api_id           = aws_apigatewayv2_api.amesa_api.id
  integration_type = "HTTP_PROXY"
  integration_uri  = var.auth_service_alb_listener_arn
  integration_method = "ANY"
  connection_type   = "VPC_LINK"
  connection_id     = aws_apigatewayv2_vpc_link.amesa_vpc_link.id
}

# Route for Auth Service
resource "aws_apigatewayv2_route" "auth_route" {
  api_id    = aws_apigatewayv2_api.amesa_api.id
  route_key = "ANY /api/v1/auth/{proxy+}"
  target    = "integrations/${aws_apigatewayv2_integration.auth_service.id}"
}

resource "aws_apigatewayv2_route" "oauth_route" {
  api_id    = aws_apigatewayv2_api.amesa_api.id
  route_key = "ANY /api/v1/oauth/{proxy+}"
  target    = "integrations/${aws_apigatewayv2_integration.auth_service.id}"
}

# Integration for Lottery Service
resource "aws_apigatewayv2_integration" "lottery_service" {
  api_id           = aws_apigatewayv2_api.amesa_api.id
  integration_type = "HTTP_PROXY"
  integration_uri  = var.lottery_service_alb_listener_arn
  integration_method = "ANY"
  connection_type   = "VPC_LINK"
  connection_id     = aws_apigatewayv2_vpc_link.amesa_vpc_link.id
}

resource "aws_apigatewayv2_route" "lottery_route" {
  api_id    = aws_apigatewayv2_api.amesa_api.id
  route_key = "ANY /api/v1/houses/{proxy+}"
  target    = "integrations/${aws_apigatewayv2_integration.lottery_service.id}"
}

# VPC Link for private ALB access
resource "aws_apigatewayv2_vpc_link" "amesa_vpc_link" {
  name               = "amesa-vpc-link"
  security_group_ids = [var.vpc_security_group_id]
  subnet_ids         = var.private_subnet_ids
}

# Stage
resource "aws_apigatewayv2_stage" "amesa_api_stage" {
  api_id      = aws_apigatewayv2_api.amesa_api.id
  name        = var.environment
  auto_deploy = true

  access_log_settings {
    destination_arn = aws_cloudwatch_log_group.api_gateway_logs.arn
    format = jsonencode({
      requestId      = "$context.requestId"
      ip             = "$context.identity.sourceIp"
      requestTime    = "$context.requestTime"
      httpMethod     = "$context.httpMethod"
      routeKey       = "$context.routeKey"
      status         = "$context.status"
      protocol       = "$context.protocol"
      responseLength = "$context.responseLength"
    })
  }
}

# CloudWatch Log Group
resource "aws_cloudwatch_log_group" "api_gateway_logs" {
  name              = "/aws/apigateway/amesa-microservices-api"
  retention_in_days = 7
}

# Variables (to be defined in variables.tf)
variable "environment" {
  description = "Environment name (dev, stage, prod)"
  type        = string
}

variable "auth_service_alb_listener_arn" {
  description = "ARN of Auth Service ALB listener"
  type        = string
}

variable "lottery_service_alb_listener_arn" {
  description = "ARN of Lottery Service ALB listener"
  type        = string
}

variable "vpc_security_group_id" {
  description = "Security group ID for VPC link"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for VPC link"
  type        = list(string)
}

