variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "us-east-1"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be dev, staging, or prod."
  }
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}

variable "db_name" {
  description = "Database name"
  type        = string
  default     = "vector_db"
}

variable "db_username" {
  description = "Database master username"
  type        = string
  default     = "postgres"
  sensitive   = true
}

variable "db_password" {
  description = "Database master password"
  type        = string
  sensitive   = true
}

variable "redis_node_type" {
  description = "ElastiCache node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "bastion_ssh_public_key" {
  description = "SSH public key for bastion host access (e.g., contents of ~/.ssh/id_rsa.pub)"
  type        = string
}

variable "bastion_allowed_ssh_cidr_blocks" {
  description = "CIDR blocks allowed to SSH into bastion (use your IP for security)"
  type        = list(string)
  default     = ["0.0.0.0/0"]  # CHANGE THIS to your specific IP!
}

variable "bastion_instance_type" {
  description = "EC2 instance type for bastion host"
  type        = string
  default     = "t3.micro"
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

