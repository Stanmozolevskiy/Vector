variable "environment" {
  description = "Environment name"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID"
  type        = string
}

variable "s3_bucket_arn" {
  description = "S3 bucket ARN for user uploads"
  type        = string
}

variable "rds_security_group_id" {
  description = "RDS security group ID"
  type        = string
}

variable "redis_security_group_id" {
  description = "Redis security group ID"
  type        = string
}

variable "alb_security_group_id" {
  description = "Application Load Balancer security group ID"
  type        = string
}

variable "private_subnet_ids" {
  description = "Private subnet IDs for ECS tasks"
  type        = list(string)
}

variable "target_group_arn" {
  description = "Target group ARN for load balancer"
  type        = string
}

variable "ecr_repository_url" {
  description = "ECR repository URL for container image"
  type        = string
  default     = ""
}

variable "db_connection_secret_arn" {
  description = "ARN of Secrets Manager secret for database connection"
  type        = string
  default     = ""
}

variable "redis_connection_secret_arn" {
  description = "ARN of Secrets Manager secret for Redis connection"
  type        = string
  default     = ""
}

variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "us-east-1"
}

variable "db_connection_string" {
  description = "Database connection string"
  type        = string
  default     = ""
}

variable "redis_connection_string" {
  description = "Redis connection string"
  type        = string
  default     = ""
}

variable "frontend_ecr_repository_url" {
  description = "ECR repository URL for frontend container image"
  type        = string
  default     = ""
}

variable "frontend_target_group_arn" {
  description = "Target group ARN for frontend load balancer"
  type        = string
  default     = ""
}

variable "backend_api_url" {
  description = "Backend API URL for frontend to connect to"
  type        = string
  default     = ""
}

variable "frontend_url" {
  description = "Frontend URL for email verification links"
  type        = string
  default     = ""
}

variable "sendgrid_api_key" {
  description = "SendGrid API key for email service"
  type        = string
  default     = ""
  sensitive   = true
}

variable "sendgrid_from_email" {
  description = "SendGrid verified sender email address"
  type        = string
  default     = ""
  sensitive   = true
}

variable "sendgrid_from_name" {
  description = "SendGrid sender display name"
  type        = string
  default     = "Vector"
}

