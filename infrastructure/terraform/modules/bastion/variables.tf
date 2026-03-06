variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "vpc_id" {
  description = "VPC ID where the bastion will be deployed"
  type        = string
}

variable "public_subnet_ids" {
  description = "List of public subnet IDs for bastion host"
  type        = list(string)
}

variable "rds_security_group_id" {
  description = "Security group ID of the RDS instance"
  type        = string
}

variable "redis_security_group_id" {
  description = "Security group ID of the Redis instance"
  type        = string
}

variable "ssh_public_key" {
  description = "SSH public key for bastion host access"
  type        = string
}

variable "allowed_ssh_cidr_blocks" {
  description = "CIDR blocks allowed to SSH into bastion"
  type        = list(string)
  default     = ["0.0.0.0/0"]  # CHANGE THIS to your specific IP for security!
}

variable "instance_type" {
  description = "EC2 instance type for bastion host"
  type        = string
  default     = "t3.micro"
}

variable "use_elastic_ip" {
  description = "Whether to use an Elastic IP for the bastion host"
  type        = bool
  default     = true
}

