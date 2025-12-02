output "vpc_id" {
  description = "VPC ID"
  value       = module.vpc.vpc_id
}

output "vpc_cidr_block" {
  description = "VPC CIDR block"
  value       = module.vpc.vpc_cidr_block
}

output "public_subnet_ids" {
  description = "Public subnet IDs"
  value       = module.vpc.public_subnet_ids
}

output "private_subnet_ids" {
  description = "Private subnet IDs"
  value       = module.vpc.private_subnet_ids
}

output "database_endpoint" {
  description = "RDS instance endpoint"
  value       = module.database.endpoint
  sensitive   = true
}

output "database_port" {
  description = "RDS instance port"
  value       = module.database.port
}

output "redis_endpoint" {
  description = "ElastiCache Redis endpoint"
  value       = module.redis.endpoint
}

output "redis_port" {
  description = "ElastiCache Redis port"
  value       = module.redis.port
}

output "s3_bucket_name" {
  description = "S3 bucket name for user uploads"
  value       = module.storage.bucket_name
}

output "s3_bucket_arn" {
  description = "S3 bucket ARN"
  value       = module.storage.bucket_arn
}

output "alb_dns_name" {
  description = "Application Load Balancer DNS name"
  value       = module.alb.alb_dns_name
}

output "alb_arn" {
  description = "Application Load Balancer ARN"
  value       = module.alb.alb_arn
}

output "ecs_cluster_name" {
  description = "ECS cluster name"
  value       = module.ecs.cluster_name
}

output "ecs_cluster_arn" {
  description = "ECS cluster ARN"
  value       = module.ecs.cluster_arn
}

output "target_group_arn" {
  description = "Target group ARN for backend"
  value       = module.alb.target_group_arn
}

# Bastion Host Outputs
output "bastion_public_ip" {
  description = "Public IP address of the bastion host"
  value       = module.bastion.bastion_public_ip
}

output "bastion_ssh_command" {
  description = "SSH command to connect to bastion host"
  value       = module.bastion.ssh_command
}

output "bastion_ssm_command" {
  description = "SSM Session Manager command to connect to bastion"
  value       = module.bastion.ssm_command
}

output "bastion_instance_id" {
  description = "Instance ID of the bastion host"
  value       = module.bastion.bastion_instance_id
}

# Database Connection via Bastion
output "db_connection_via_bastion" {
  description = "Instructions to connect to database via bastion"
  value = <<-EOT
    To connect to PostgreSQL via bastion:
    1. SSH into bastion: ${module.bastion.ssh_command}
    2. From bastion, connect to PostgreSQL: psql -h ${split(":", module.database.endpoint)[0]} -U ${var.db_username} -d ${var.db_name}
    
    Or use SSH tunnel with pgAdmin:
    ssh -i ~/.ssh/dev-bastion-key -L 5433:${split(":", module.database.endpoint)[0]}:5432 ec2-user@${module.bastion.bastion_public_ip}
    Then connect pgAdmin to: localhost:5433
  EOT
  sensitive = true
}

