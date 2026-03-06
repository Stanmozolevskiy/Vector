# SSM Permissions History - Why They Were Never Set

## Timeline

### December 2, 2025 - Bastion Host Added
- **Commit:** `470e226` - "Add SSH Bastion Host and deployment rules"
- **Commit:** `5709af9` - "Fix Terraform bastion deployment and add quick start guide"

The bastion host module was added to the infrastructure on **December 2, 2025**.

## What Was Configured

When the bastion host was created, the Terraform configuration included:

### ✅ What WAS Configured (For the EC2 Instance):

1. **IAM Role for Bastion Instance:**
   - Role: `dev-bastion-role`
   - Policy: `AmazonSSMManagedInstanceCore` (attached to the EC2 instance)
   - This allows the **EC2 instance itself** to communicate with SSM

2. **SSM Agent:**
   - Pre-installed on Amazon Linux 2023
   - Configured to register with SSM

### ❌ What WAS NOT Configured (For the IAM User):

The IAM user `Vector-Infrastructure` was **never given permissions** to:
- Start SSM sessions (`ssm:StartSession`)
- Describe instances (`ssm:DescribeInstanceInformation`)
- Terminate sessions (`ssm:TerminateSession`)

## Why This Happened

### The Issue:

The bastion module was designed with **two access methods**:

1. **SSH Access** (Primary method)
   - Uses SSH keys
   - Requires security group rules
   - This was the main focus

2. **SSM Session Manager** (Alternative method)
   - Mentioned as "optional" in the code
   - IAM role was set up for the **instance** (so it can register with SSM)
   - But **user permissions** were never added

### Root Cause:

The Terraform configuration only sets up:
- ✅ **Instance-side** SSM permissions (IAM role for EC2)
- ❌ **User-side** SSM permissions (IAM policy for users)

This is a common oversight because:
1. The bastion was primarily designed for SSH access
2. SSM was added as an "optional" feature
3. The documentation mentioned SSM but didn't include user permission setup
4. The IAM user `Vector-Infrastructure` was created separately (not via Terraform)

## When Did This Become a Problem?

**Today (December 7, 2025)** - When you tried to use SSM Session Manager for the first time.

The permissions were **never changed** - they were **never set up in the first place**.

## Why It Works Now (After Fix)

After adding the IAM policy to `Vector-Infrastructure`:
- ✅ User can call `ssm:StartSession`
- ✅ User can connect to the bastion via SSM
- ✅ No SSH keys or security group rules needed

## Summary

| Component | Status | When |
|-----------|--------|------|
| Bastion EC2 Instance IAM Role | ✅ Configured | Dec 2, 2025 |
| Bastion SSM Agent | ✅ Pre-installed | Dec 2, 2025 |
| User SSM Permissions | ❌ Never configured | - |
| User SSM Permissions | ✅ Fixed | Dec 7, 2025 |

**Answer:** The permissions were **never changed** - they were **never set up** when the bastion was created. This is a missing configuration, not a change.

---

**Last Updated:** December 7, 2025

