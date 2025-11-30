# Prerequisites Installation Guide

## Docker Desktop Status

Docker Desktop appears to be installed but may need to be fully started. Please:

1. **Start Docker Desktop** (if not running)
   - Open Docker Desktop from Start Menu
   - Wait for it to fully start (whale icon in system tray should be steady)

2. **Verify Docker is running:**
   ```powershell
   docker ps
   ```

3. **Test Docker Compose:**
   ```powershell
   cd docker
   docker compose config
   docker compose up -d postgres redis
   docker compose ps
   ```

## Branch Protection Configuration

**GitHub CLI is not installed**, so branch protection must be configured manually through the GitHub web interface.

### Option 1: Manual Setup (Recommended)
1. Go to: https://github.com/Stanmozolevskiy/Vector/settings/branches
2. Follow instructions in `.github/BRANCH_PROTECTION_SETUP.md`

### Option 2: Install GitHub CLI (Optional)
If you want to configure via CLI:
```powershell
# Install GitHub CLI
winget install --id GitHub.cli
# Or download from: https://cli.github.com/

# Authenticate
gh auth login

# Then we can configure branch protection via CLI
```

## AWS CLI Installation

### Option 1: Manual Installer (Recommended - No Admin Required)
1. Download: https://awscli.amazonaws.com/AWSCLIV2.msi
2. Run the installer
3. Verify:
   ```powershell
   aws --version
   ```

### Option 2: Chocolatey (Requires Admin)
Run PowerShell **as Administrator**:
```powershell
choco install awscli -y
```

### Option 3: MSI Installer via PowerShell (No Admin for User Install)
```powershell
# Download installer
Invoke-WebRequest -Uri "https://awscli.amazonaws.com/AWSCLIV2.msi" -OutFile "$env:TEMP\AWSCLIV2.msi"

# Install for current user
Start-Process msiexec.exe -ArgumentList "/i `"$env:TEMP\AWSCLIV2.msi`" /quiet /norestart" -Wait

# Add to PATH (if not auto-added)
$env:Path += ";C:\Program Files\Amazon\AWSCLIV2"
```

## Terraform Installation

### Option 1: Manual Installer (Recommended)
1. Download: https://developer.hashicorp.com/terraform/downloads
2. Extract to `C:\terraform` (or any folder)
3. Add to PATH:
   - System Properties → Environment Variables
   - Edit "Path" → Add `C:\terraform`
4. Verify:
   ```powershell
   terraform --version
   ```

### Option 2: Chocolatey (Requires Admin)
Run PowerShell **as Administrator**:
```powershell
choco install terraform -y
```

### Option 3: Chocolatey (User Install - Alternative Location)
```powershell
# Install to user directory (no admin needed)
choco install terraform --params="'/InstallDir:C:\Users\$env:USERNAME\terraform'"
```

### Option 4: Direct Download (No Admin)
```powershell
# Create directory
New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\terraform"

# Download (get latest version from https://developer.hashicorp.com/terraform/downloads)
$terraformUrl = "https://releases.hashicorp.com/terraform/1.6.6/terraform_1.6.6_windows_amd64.zip"
Invoke-WebRequest -Uri $terraformUrl -OutFile "$env:TEMP\terraform.zip"

# Extract
Expand-Archive -Path "$env:TEMP\terraform.zip" -DestinationPath "$env:USERPROFILE\terraform" -Force

# Add to PATH for current session
$env:Path += ";$env:USERPROFILE\terraform"

# Add to PATH permanently (user-level)
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";$env:USERPROFILE\terraform", "User")
```

## Quick Install Script (Run as Administrator)

Save this as `install-prerequisites.ps1` and run as Administrator:

```powershell
# Install AWS CLI
Write-Host "Installing AWS CLI..."
choco install awscli -y

# Install Terraform
Write-Host "Installing Terraform..."
choco install terraform -y

# Install GitHub CLI (optional)
Write-Host "Installing GitHub CLI..."
choco install gh -y

# Refresh environment
Write-Host "Refreshing environment variables..."
$env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path","User")

# Verify installations
Write-Host "`nVerifying installations..."
aws --version
terraform --version
gh --version
```

## After Installation

### Configure AWS CLI
```powershell
aws configure
# Enter your AWS Access Key ID
# Enter your AWS Secret Access Key
# Default region: us-east-1
# Default output: json

# Verify connection
aws sts get-caller-identity
```

### Initialize Terraform
```powershell
cd infrastructure/terraform

# Create terraform.tfvars (DO NOT COMMIT)
# See infrastructure/terraform/SETUP_GUIDE.md for template

terraform init
terraform validate
terraform plan
```

## Troubleshooting

### Docker Issues
- Ensure Docker Desktop is fully started
- Restart Docker Desktop if needed
- Check Windows WSL 2 is enabled (if using WSL backend)

### Chocolatey Permission Issues
- Run PowerShell as Administrator
- Or use manual installers (no admin needed)

### PATH Issues
- Restart terminal after adding to PATH
- Or use `refreshenv` command in Chocolatey

