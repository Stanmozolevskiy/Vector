terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  # Uncomment and configure when ready to use remote state
  # backend "s3" {
  #   bucket = "vector-terraform-state"
  #   key    = "stage1/terraform.tfstate"
  #   region = "us-east-1"
  # }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "Vector"
      Environment = var.environment
      ManagedBy   = "Terraform"
    }
  }
}

# VPC Module
module "vpc" {
  source = "./modules/vpc"

  environment = var.environment
  vpc_cidr    = var.vpc_cidr
}

# RDS Module
module "database" {
  source = "./modules/rds"

  environment         = var.environment
  vpc_id              = module.vpc.vpc_id
  subnet_ids          = module.vpc.private_subnet_ids
  instance_class      = var.db_instance_class
  # engine_version will use default from module (15.7) or can be overridden
  db_name             = var.db_name
  db_username         = var.db_username
  db_password         = var.db_password
  allowed_cidr_blocks = [module.vpc.vpc_cidr_block]
}

# Redis Module
module "redis" {
  source = "./modules/redis"

  environment         = var.environment
  vpc_id              = module.vpc.vpc_id
  subnet_ids          = module.vpc.private_subnet_ids
  node_type           = var.redis_node_type
  allowed_cidr_blocks = [module.vpc.vpc_cidr_block]
}

# S3 Module
module "storage" {
  source = "./modules/s3"

  environment = var.environment
}

# ECR Module
module "ecr" {
  source = "./modules/ecr"

  environment = var.environment
}

# Application Load Balancer Module
module "alb" {
  source = "./modules/alb"

  environment      = var.environment
  vpc_id           = module.vpc.vpc_id
  public_subnet_ids = module.vpc.public_subnet_ids
}

# ECS Module
module "ecs" {
  source = "./modules/ecs"

  environment            = var.environment
  vpc_id                 = module.vpc.vpc_id
  s3_bucket_arn          = module.storage.bucket_arn
  rds_security_group_id  = module.database.security_group_id
  redis_security_group_id = module.redis.security_group_id
  alb_security_group_id   = module.alb.alb_security_group_id
  private_subnet_ids      = module.vpc.private_subnet_ids
  target_group_arn        = module.alb.target_group_arn
  aws_region              = var.aws_region
  ecr_repository_url = module.ecr.backend_repository_url
}

