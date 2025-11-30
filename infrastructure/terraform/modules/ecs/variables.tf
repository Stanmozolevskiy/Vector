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

