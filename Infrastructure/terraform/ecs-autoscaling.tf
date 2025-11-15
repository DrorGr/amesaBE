# Auto-scaling configurations for ECS services

# Auth Service Auto-scaling
resource "aws_appautoscaling_target" "auth_service_target" {
  max_capacity       = 10
  min_capacity       = 1
  resource_id        = "service/${aws_ecs_cluster.amesa_cluster.name}/${aws_ecs_service.auth_service.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "auth_service_scaling" {
  name               = "auth-service-scaling-policy"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.auth_service_target.resource_id
  scalable_dimension = aws_appautoscaling_target.auth_service_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.auth_service_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 70.0
  }
}

# Payment Service Auto-scaling
resource "aws_appautoscaling_target" "payment_service_target" {
  max_capacity       = 10
  min_capacity       = 1
  resource_id        = "service/${aws_ecs_cluster.amesa_cluster.name}/${aws_ecs_service.payment_service.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "payment_service_scaling" {
  name               = "payment-service-scaling-policy"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.payment_service_target.resource_id
  scalable_dimension = aws_appautoscaling_target.payment_service_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.payment_service_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 70.0
  }
}

# Lottery Service Auto-scaling
resource "aws_appautoscaling_target" "lottery_service_target" {
  max_capacity       = 10
  min_capacity       = 1
  resource_id        = "service/${aws_ecs_cluster.amesa_cluster.name}/${aws_ecs_service.lottery_service.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"
}

resource "aws_appautoscaling_policy" "lottery_service_scaling" {
  name               = "lottery-service-scaling-policy"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.lottery_service_target.resource_id
  scalable_dimension = aws_appautoscaling_target.lottery_service_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.lottery_service_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value = 70.0
  }
}

# Add similar auto-scaling configurations for other services...

