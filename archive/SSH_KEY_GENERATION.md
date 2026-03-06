# SSH Key Generation for Bastion Host

## Quick Start

### For Windows Users (PowerShell)

```powershell
# 1. Generate SSH key pair
ssh-keygen -t rsa -b 4096 -f $env:USERPROFILE\.ssh\dev-bastion-key -C "vector-bastion-dev"

# When prompted:
# - Enter passphrase (optional but recommended)
# - Confirm passphrase

# 2. View your public key (copy this for terraform.tfvars)
Get-Content $env:USERPROFILE\.ssh\dev-bastion-key.pub

# 3. Set correct permissions (Windows)
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r /grant:r "$env:USERNAME:R"
```

### For macOS/Linux Users

```bash
# 1. Generate SSH key pair
ssh-keygen -t rsa -b 4096 -f ~/.ssh/dev-bastion-key -C "vector-bastion-dev"

# When prompted:
# - Enter passphrase (optional but recommended)
# - Confirm passphrase

# 2. View your public key (copy this for terraform.tfvars)
cat ~/.ssh/dev-bastion-key.pub

# 3. Set correct permissions
chmod 400 ~/.ssh/dev-bastion-key
chmod 644 ~/.ssh/dev-bastion-key.pub
```

## Get Your Public IP

You need your public IP address to restrict SSH access to the bastion.

### Windows PowerShell
```powershell
(Invoke-WebRequest -Uri "https://api.ipify.org").Content
```

### macOS/Linux
```bash
curl https://api.ipify.org
```

### Alternative
Visit: https://whatismyipaddress.com

## Update terraform.tfvars

Add these to your `infrastructure/terraform/terraform.tfvars`:

```hcl
# Bastion Host Configuration
bastion_ssh_public_key = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQDxxxxx... [paste your public key here]"
bastion_allowed_ssh_cidr_blocks = ["YOUR.IP.ADDRESS.HERE/32"]
bastion_instance_type = "t3.micro"
```

**Example:**
```hcl
bastion_ssh_public_key = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQDa1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6A7B8C9D0E1F2G3H4I5J6K7L8M9N0O1P2Q3R4S5T6U7V8W9X0Y1Z2a3b4c5d6e7f8g9h0i1j2k3l4m5n6o7p8q9r0s1t2u3v4w5x6y7z8A9B0C1D2E3F4G5H6I7J8K9L0M1N2O3P4Q5R6S7T8U9V0W1X2Y3Z4a5b6c7d8e9f0g1h2i3j4k5l6m7n8o9p0q1r2s3t4u5v6w7x8y9z0== vector-bastion-dev"
bastion_allowed_ssh_cidr_blocks = ["203.0.113.45/32"]  # Example IP
bastion_instance_type = "t3.micro"
```

## Important Notes

### Security
- **Never commit your private key** (dev-bastion-key)
- **Never commit terraform.tfvars** (contains public key)
- **Always use a specific IP** instead of `0.0.0.0/0`
- **Use a passphrase** for your SSH key

### File Locations
- **Private Key:** `~/.ssh/dev-bastion-key` (keep this secret!)
- **Public Key:** `~/.ssh/dev-bastion-key.pub` (safe to share)
- **Terraform Config:** `infrastructure/terraform/terraform.tfvars` (gitignored)

### Key Management
- Generate separate keys for each environment (dev, staging, prod)
- Rotate keys every 90 days
- Store keys securely (password manager, AWS Secrets Manager)

## After Terraform Apply

### Get Connection Info
```powershell
cd infrastructure/terraform
terraform output bastion_public_ip
terraform output bastion_ssh_command
```

### Test SSH Connection
```powershell
# Windows
ssh -i $env:USERPROFILE\.ssh\dev-bastion-key ec2-user@BASTION_PUBLIC_IP

# macOS/Linux
ssh -i ~/.ssh/dev-bastion-key ec2-user@BASTION_PUBLIC_IP
```

You should see:
```
   ,     #_
   ~\_  ####_        Amazon Linux 2023
  ~~  \_#####\
  ~~     \###|
  ~~       \#/ ___
   ~~       V~' '->
    ~~~         /
      ~~._.   _/
         _/ _/
       _/m/'

[ec2-user@ip-10-0-x-x ~]$
```

## Troubleshooting

### Permission Denied (publickey)
**Solution:** Check file permissions
```powershell
# Windows
icacls "$env:USERPROFILE\.ssh\dev-bastion-key" /inheritance:r /grant:r "$env:USERNAME:R"

# macOS/Linux
chmod 400 ~/.ssh/dev-bastion-key
```

### Connection Timeout
**Solutions:**
1. Check your IP hasn't changed: `curl https://api.ipify.org`
2. Update terraform.tfvars with new IP
3. Run `terraform apply`
4. Check instance is running: `aws ec2 describe-instances --instance-ids INSTANCE_ID`

### Key Not Found
Make sure you're using the correct path:
```powershell
# Windows
ls $env:USERPROFILE\.ssh\dev-bastion-key

# macOS/Linux  
ls ~/.ssh/dev-bastion-key
```

