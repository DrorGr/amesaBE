# Application Load Balancers for each microservice

# Auth Service ALB
resource "aws_lb" "auth_service_alb" {
  name               = "amesa-auth-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "auth-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "auth_service_tg" {
  name     = "amesa-auth-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "auth-service"
  }
}

resource "aws_lb_listener" "auth_service_listener" {
  load_balancer_arn = aws_lb.auth_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.auth_service_tg.arn
  }
}

# Lottery Service ALB
resource "aws_lb" "lottery_service_alb" {
  name               = "amesa-lottery-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "lottery-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "lottery_service_tg" {
  name     = "amesa-lottery-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "lottery-service"
  }
}

resource "aws_lb_listener" "lottery_service_listener" {
  load_balancer_arn = aws_lb.lottery_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.lottery_service_tg.arn
  }
}

# Payment Service ALB
resource "aws_lb" "payment_service_alb" {
  name               = "amesa-payment-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "payment-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "payment_service_tg" {
  name     = "amesa-payment-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "payment-service"
  }
}

resource "aws_lb_listener" "payment_service_listener" {
  load_balancer_arn = aws_lb.payment_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.payment_service_tg.arn
  }
}

# Notification Service ALB
resource "aws_lb" "notification_service_alb" {
  name               = "amesa-notification-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "notification-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "notification_service_tg" {
  name     = "amesa-notification-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "notification-service"
  }
}

resource "aws_lb_listener" "notification_service_listener" {
  load_balancer_arn = aws_lb.notification_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.notification_service_tg.arn
  }
}

# Content Service ALB
resource "aws_lb" "content_service_alb" {
  name               = "amesa-content-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "content-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "content_service_tg" {
  name     = "amesa-content-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "content-service"
  }
}

resource "aws_lb_listener" "content_service_listener" {
  load_balancer_arn = aws_lb.content_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.content_service_tg.arn
  }
}

# Lottery Results Service ALB
resource "aws_lb" "lottery_results_service_alb" {
  name               = "amesa-lottery-results-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "lottery-results-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "lottery_results_service_tg" {
  name     = "amesa-lottery-results-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "lottery-results-service"
  }
}

resource "aws_lb_listener" "lottery_results_service_listener" {
  load_balancer_arn = aws_lb.lottery_results_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.lottery_results_service_tg.arn
  }
}

# Analytics Service ALB
resource "aws_lb" "analytics_service_alb" {
  name               = "amesa-analytics-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "analytics-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "analytics_service_tg" {
  name     = "amesa-analytics-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "analytics-service"
  }
}

resource "aws_lb_listener" "analytics_service_listener" {
  load_balancer_arn = aws_lb.analytics_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.analytics_service_tg.arn
  }
}

# Admin Service ALB
resource "aws_lb" "admin_service_alb" {
  name               = "amesa-admin-service-alb-${var.environment}"
  internal           = true
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = var.private_subnet_ids

  enable_deletion_protection = var.environment == "prod"

  tags = {
    Environment = var.environment
    Service     = "admin-service"
    Project     = "AmesaBackend"
  }
}

resource "aws_lb_target_group" "admin_service_tg" {
  name     = "amesa-admin-tg-${var.environment}"
  port     = 8080
  protocol = "HTTP"
  vpc_id   = var.vpc_id

  health_check {
    enabled             = true
    healthy_threshold   = 2
    unhealthy_threshold = 3
    timeout             = 5
    interval            = 30
    path                = "/health"
    matcher             = "200"
    protocol            = "HTTP"
  }

  deregistration_delay = 30

  tags = {
    Environment = var.environment
    Service     = "admin-service"
  }
}

resource "aws_lb_listener" "admin_service_listener" {
  load_balancer_arn = aws_lb.admin_service_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.admin_service_tg.arn
  }
}

# Security Group for ALBs
resource "aws_security_group" "alb_sg" {
  name        = "amesa-alb-sg-${var.environment}"
  description = "Security group for Amesa ALBs"
  vpc_id      = var.vpc_id

  ingress {
    description = "HTTP from API Gateway"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"] # Restrict to API Gateway IPs in production
  }

  ingress {
    description = "HTTPS from API Gateway"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"] # Restrict to API Gateway IPs in production
  }

  egress {
    description = "Allow all outbound"
    from_port   = 0
    to_port     = 0
    protocol     = "-1"
    cidr_blocks  = ["0.0.0.0/0"]
  }

  tags = {
    Name        = "amesa-alb-sg"
    Environment = var.environment
  }
}

