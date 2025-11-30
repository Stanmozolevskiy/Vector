# ECS Task Definition for Backend API
resource "aws_ecs_task_definition" "backend" {
  family                   = "${var.environment}-vector-backend"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.environment == "dev" ? "256" : "512"
  memory                   = var.environment == "dev" ? "512" : "1024"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([
    {
      name  = "vector-backend"
      image = "${var.ecr_repository_url}:latest"

      portMappings = [
        {
          containerPort = 80
          protocol      = "tcp"
        }
      ]

      environment = concat(
        [
          {
            name  = "ASPNETCORE_ENVIRONMENT"
            value = var.environment == "dev" ? "Development" : var.environment == "staging" ? "Staging" : "Production"
          },
          {
            name  = "ASPNETCORE_URLS"
            value = "http://+:80"
          }
        ],
        var.db_connection_string != "" ? [
          {
            name  = "ConnectionStrings__DefaultConnection"
            value = var.db_connection_string
          }
        ] : [],
        var.redis_connection_string != "" ? [
          {
            name  = "ConnectionStrings__Redis"
            value = var.redis_connection_string
          }
        ] : [],
        var.sendgrid_api_key != "" ? [
          {
            name  = "SendGrid__ApiKey"
            value = var.sendgrid_api_key
          }
        ] : [],
        var.sendgrid_from_email != "" ? [
          {
            name  = "SendGrid__FromEmail"
            value = var.sendgrid_from_email
          }
        ] : [],
        var.sendgrid_from_name != "" ? [
          {
            name  = "SendGrid__FromName"
            value = var.sendgrid_from_name
          }
        ] : []
      )

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.ecs.name
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs"
        }
      }

      healthCheck = {
        command     = ["CMD-SHELL", "curl -f http://localhost:80/api/health || exit 1"]
        interval    = 30
        timeout     = 5
        retries     = 3
        startPeriod = 60
      }
    }
  ])

  tags = {
    Name = "${var.environment}-vector-backend-task"
  }
}

# ECS Task Definition for Frontend
resource "aws_ecs_task_definition" "frontend" {
  family                   = "${var.environment}-vector-frontend"
  network_mode             = "awsvpc"
  requires_compatibilities = ["FARGATE"]
  cpu                      = var.environment == "dev" ? "256" : "256"
  memory                   = var.environment == "dev" ? "512" : "512"
  execution_role_arn       = aws_iam_role.ecs_task_execution.arn
  task_role_arn            = aws_iam_role.ecs_task.arn

  container_definitions = jsonencode([
    {
      name  = "vector-frontend"
      image = "${var.frontend_ecr_repository_url}:latest"

      portMappings = [
        {
          containerPort = 80
          protocol      = "tcp"
        }
      ]

      environment = [
        {
          name  = "VITE_API_URL"
          value = var.backend_api_url
        }
      ]

      logConfiguration = {
        logDriver = "awslogs"
        options = {
          "awslogs-group"         = aws_cloudwatch_log_group.ecs.name
          "awslogs-region"        = var.aws_region
          "awslogs-stream-prefix" = "ecs-frontend"
        }
      }

      healthCheck = {
        command     = ["CMD-SHELL", "curl -f http://localhost:80/health || exit 1"]
        interval    = 30
        timeout     = 5
        retries     = 3
        startPeriod = 60
      }
    }
  ])

  tags = {
    Name = "${var.environment}-vector-frontend-task"
  }
}

# ECS Service for Frontend
resource "aws_ecs_service" "frontend" {
  name            = "${var.environment}-vector-frontend-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.frontend.arn
  desired_count   = var.environment == "dev" ? 1 : 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.private_subnet_ids
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = var.frontend_target_group_arn
    container_name   = "vector-frontend"
    container_port   = 80
  }

  tags = {
    Name = "${var.environment}-vector-frontend-service"
  }
}

# ECS Service for Backend API
resource "aws_ecs_service" "backend" {
  name            = "${var.environment}-vector-backend-service"
  cluster         = aws_ecs_cluster.main.id
  task_definition = aws_ecs_task_definition.backend.arn
  desired_count   = var.environment == "dev" ? 1 : 2
  launch_type     = "FARGATE"

  network_configuration {
    subnets          = var.private_subnet_ids
    security_groups  = [aws_security_group.ecs_tasks.id]
    assign_public_ip = false
  }

  load_balancer {
    target_group_arn = var.target_group_arn
    container_name   = "vector-backend"
    container_port   = 80
  }


  depends_on = [aws_iam_role_policy.ecs_task_s3]

  tags = {
    Name = "${var.environment}-vector-backend-service"
  }
}

