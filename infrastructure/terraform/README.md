# Vector Infrastructure - Terraform

This directory contains Terraform configuration for provisioning AWS infrastructure for the Vector platform.

## Prerequisites

1. Install [Terraform](https://www.terraform.io/downloads) (>= 1.0)
2. Configure AWS credentials:
   ```bash
   aws configure
   ```
   Or set environment variables:
   ```bash
   export AWS_ACCESS_KEY_ID=your_access_key
   export AWS_SECRET_ACCESS_KEY=your_secret_key
   export AWS_DEFAULT_REGION=us-east-1
   ```

## Structure

```
terraform/
├── main.tf              # Main configuration
├── variables.tf         # Variable definitions
├── outputs.tf           # Output values
└── modules/
    ├── vpc/            # VPC module
    ├── rds/            # RDS module
    ├── redis/          # ElastiCache module
    └── s3/             # S3 module
```

## Usage

### Initialize Terraform

```bash
cd infrastructure/terraform
terraform init
```

### Plan Changes

```bash
terraform plan -var="db_password=your_secure_password" -var="environment=dev"
```

### Apply Configuration

```bash
terraform apply -var="db_password=your_secure_password" -var="environment=dev"
```

### Using terraform.tfvars

Create a `terraform.tfvars` file (do not commit this file):

```hcl
aws_region      = "us-east-1"
environment     = "dev"
db_password     = "your_secure_password"
db_instance_class = "db.t3.micro"
redis_node_type = "cache.t3.micro"
```

Then run:

```bash
terraform plan
terraform apply
```

### Destroy Infrastructure

```bash
terraform destroy -var="db_password=your_secure_password" -var="environment=dev"
```

## Variables

See `variables.tf` for all available variables.

### Required Variables

- `db_password`: Database master password (sensitive)

### Optional Variables

- `aws_region`: AWS region (default: "us-east-1")
- `environment`: Environment name (default: "dev")
- `vpc_cidr`: VPC CIDR block (default: "10.0.0.0/16")
- `db_instance_class`: RDS instance class (default: "db.t3.micro")
- `redis_node_type`: ElastiCache node type (default: "cache.t3.micro")

## Outputs

After applying, you'll get outputs for:
- VPC ID and subnet IDs
- RDS endpoint and port
- Redis endpoint and port
- S3 bucket name and ARN

View outputs:
```bash
terraform output
```

## Remote State (Optional)

To use remote state with S3, uncomment the backend configuration in `main.tf` and create the S3 bucket first:

```bash
aws s3 mb s3://vector-terraform-state
aws s3api put-bucket-versioning \
  --bucket vector-terraform-state \
  --versioning-configuration Status=Enabled
```

## Security Notes

- Never commit `terraform.tfvars` files with sensitive data
- Use AWS Secrets Manager or Parameter Store for production passwords
- Enable versioning on the S3 state bucket
- Use IAM roles with least privilege for Terraform execution

