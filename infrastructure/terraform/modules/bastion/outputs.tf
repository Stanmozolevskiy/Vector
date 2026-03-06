output "bastion_public_ip" {
  description = "Public IP address of the bastion host"
  value       = var.use_elastic_ip && length(aws_eip.bastion) > 0 ? aws_eip.bastion[0].public_ip : aws_instance.bastion.public_ip
}

output "bastion_instance_id" {
  description = "Instance ID of the bastion host"
  value       = aws_instance.bastion.id
}

output "bastion_security_group_id" {
  description = "Security group ID of the bastion host"
  value       = aws_security_group.bastion.id
}

output "ssh_command" {
  description = "SSH command to connect to bastion"
  value       = "ssh -i ~/.ssh/${var.environment}-bastion-key.pem ec2-user@${var.use_elastic_ip && length(aws_eip.bastion) > 0 ? aws_eip.bastion[0].public_ip : aws_instance.bastion.public_ip}"
}

output "ssm_command" {
  description = "AWS Systems Manager Session Manager command (SSH alternative)"
  value       = "aws ssm start-session --target ${aws_instance.bastion.id} --region ${data.aws_region.current.name}"
}

data "aws_region" "current" {}

