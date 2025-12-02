# Security Group for Bastion Host
resource "aws_security_group" "bastion" {
  name        = "${var.environment}-bastion-sg"
  description = "Security group for Bastion Host (SSH access)"
  vpc_id      = var.vpc_id

  # SSH access from specific IP addresses (your IP)
  ingress {
    description = "SSH from allowed IPs"
    from_port   = 22
    to_port     = 22
    protocol    = "tcp"
    cidr_blocks = var.allowed_ssh_cidr_blocks
  }

  # Allow all outbound traffic
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "${var.environment}-bastion-sg"
  }
}

# Security Group Rule: Allow Bastion to access RDS
resource "aws_security_group_rule" "bastion_to_rds" {
  type                     = "ingress"
  from_port                = 5432
  to_port                  = 5432
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.bastion.id
  security_group_id        = var.rds_security_group_id
  description              = "PostgreSQL access from Bastion Host"
}

# Security Group Rule: Allow Bastion to access Redis
resource "aws_security_group_rule" "bastion_to_redis" {
  type                     = "ingress"
  from_port                = 6379
  to_port                  = 6379
  protocol                 = "tcp"
  source_security_group_id = aws_security_group.bastion.id
  security_group_id        = var.redis_security_group_id
  description              = "Redis access from Bastion Host"
}

# IAM Role for Bastion Host (for SSM Session Manager - optional)
resource "aws_iam_role" "bastion" {
  name = "${var.environment}-bastion-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "ec2.amazonaws.com"
        }
      }
    ]
  })

  tags = {
    Name = "${var.environment}-bastion-role"
  }
}

# Attach SSM policy for Session Manager (SSH alternative)
resource "aws_iam_role_policy_attachment" "bastion_ssm" {
  role       = aws_iam_role.bastion.name
  policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
}

# IAM Instance Profile for Bastion
resource "aws_iam_instance_profile" "bastion" {
  name = "${var.environment}-bastion-profile"
  role = aws_iam_role.bastion.name

  tags = {
    Name = "${var.environment}-bastion-profile"
  }
}

# Get latest Amazon Linux 2023 AMI
data "aws_ami" "amazon_linux" {
  most_recent = true
  owners      = ["amazon"]

  filter {
    name   = "name"
    values = ["al2023-ami-*-x86_64"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }

  filter {
    name   = "root-device-type"
    values = ["ebs"]
  }
}

# Key Pair for SSH Access
resource "aws_key_pair" "bastion" {
  key_name   = "${var.environment}-bastion-key"
  public_key = var.ssh_public_key

  tags = {
    Name = "${var.environment}-bastion-key"
  }
}

# Bastion Host EC2 Instance
resource "aws_instance" "bastion" {
  ami                         = data.aws_ami.amazon_linux.id
  instance_type               = var.instance_type
  subnet_id                   = var.public_subnet_ids[0]
  vpc_security_group_ids      = [aws_security_group.bastion.id]
  iam_instance_profile        = aws_iam_instance_profile.bastion.name
  key_name                    = aws_key_pair.bastion.key_name
  associate_public_ip_address = true

  # User data to install PostgreSQL client
  user_data = <<-EOF
              #!/bin/bash
              yum update -y
              yum install -y postgresql15
              yum install -y redis
              EOF

  root_block_device {
    volume_type           = "gp3"
    volume_size           = 8
    delete_on_termination = true
    encrypted             = true
  }

  tags = {
    Name = "${var.environment}-bastion-host"
  }

  lifecycle {
    ignore_changes = [
      ami,
      user_data
    ]
  }
}

# Elastic IP for Bastion Host (optional - for static IP)
resource "aws_eip" "bastion" {
  count    = var.use_elastic_ip ? 1 : 0
  instance = aws_instance.bastion.id
  domain   = "vpc"

  tags = {
    Name = "${var.environment}-bastion-eip"
  }
}

