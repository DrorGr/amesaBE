# Amazon EventBridge Event Bus for Microservices Communication
# Handles asynchronous event-driven communication between services

resource "aws_cloudwatch_event_bus" "amesa_event_bus" {
  name = "amesa-event-bus"

  tags = {
    Environment = var.environment
    Project     = "AmesaBackend"
  }
}

# Event Rules for routing events to consuming services

# User Created Event → Notification Service
resource "aws_cloudwatch_event_rule" "user_created_rule" {
  name           = "user-created-to-notification"
  description    = "Route user.created events to Notification Service"
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name

  event_pattern = jsonencode({
    source      = ["amesa.auth-service"]
    detail-type = ["user.created"]
  })

  tags = {
    Environment = var.environment
  }
}

resource "aws_cloudwatch_event_target" "user_created_notification_target" {
  rule           = aws_cloudwatch_event_rule.user_created_rule.name
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name
  arn            = var.notification_service_event_target_arn
  role_arn       = aws_iam_role.eventbridge_target_role.arn
}

# Ticket Purchased Event → Payment Service
resource "aws_cloudwatch_event_rule" "ticket_purchased_rule" {
  name           = "ticket-purchased-to-payment"
  description    = "Route ticket.purchased events to Payment Service"
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name

  event_pattern = jsonencode({
    source      = ["amesa.lottery-service"]
    detail-type = ["ticket.purchased"]
  })

  tags = {
    Environment = var.environment
  }
}

resource "aws_cloudwatch_event_target" "ticket_purchased_payment_target" {
  rule           = aws_cloudwatch_event_rule.ticket_purchased_rule.name
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name
  arn            = var.payment_service_event_target_arn
  role_arn       = aws_iam_role.eventbridge_target_role.arn
}

# Payment Completed Event → Lottery Service
resource "aws_cloudwatch_event_rule" "payment_completed_rule" {
  name           = "payment-completed-to-lottery"
  description    = "Route payment.completed events to Lottery Service"
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name

  event_pattern = jsonencode({
    source      = ["amesa.payment-service"]
    detail-type = ["payment.completed"]
  })

  tags = {
    Environment = var.environment
  }
}

resource "aws_cloudwatch_event_target" "payment_completed_lottery_target" {
  rule           = aws_cloudwatch_event_rule.payment_completed_rule.name
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name
  arn            = var.lottery_service_event_target_arn
  role_arn       = aws_iam_role.eventbridge_target_role.arn
}

# Lottery Draw Completed Event → Notification & Analytics Services
resource "aws_cloudwatch_event_rule" "lottery_draw_completed_rule" {
  name           = "lottery-draw-completed"
  description    = "Route lottery.draw.completed events to multiple services"
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name

  event_pattern = jsonencode({
    source      = ["amesa.lottery-service"]
    detail-type = ["lottery.draw.completed"]
  })

  tags = {
    Environment = var.environment
  }
}

resource "aws_cloudwatch_event_target" "lottery_draw_notification_target" {
  rule           = aws_cloudwatch_event_rule.lottery_draw_completed_rule.name
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name
  arn            = var.notification_service_event_target_arn
  role_arn       = aws_iam_role.eventbridge_target_role.arn
}

resource "aws_cloudwatch_event_target" "lottery_draw_analytics_target" {
  rule           = aws_cloudwatch_event_rule.lottery_draw_completed_rule.name
  event_bus_name = aws_cloudwatch_event_bus.amesa_event_bus.name
  arn            = var.analytics_service_event_target_arn
  role_arn       = aws_iam_role.eventbridge_target_role.arn
}

# IAM Role for EventBridge to invoke targets
resource "aws_iam_role" "eventbridge_target_role" {
  name = "amesa-eventbridge-target-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "events.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Environment = var.environment
  }
}

resource "aws_iam_role_policy" "eventbridge_target_policy" {
  name = "amesa-eventbridge-target-policy"
  role = aws_iam_role.eventbridge_target_role.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "lambda:InvokeFunction",
          "sqs:SendMessage",
          "sns:Publish"
        ]
        Resource = "*"
      }
    ]
  })
}

# Variables
variable "notification_service_event_target_arn" {
  description = "ARN of Notification Service event target (SQS queue or Lambda)"
  type        = string
}

variable "payment_service_event_target_arn" {
  description = "ARN of Payment Service event target"
  type        = string
}

variable "lottery_service_event_target_arn" {
  description = "ARN of Lottery Service event target"
  type        = string
}

variable "analytics_service_event_target_arn" {
  description = "ARN of Analytics Service event target"
  type        = string
}

