# Task Breakdown - Independent Scripts

## Overview
All tasks have been divided into separate, independent scripts that can be run individually to avoid connection issues.

## Task 1: Remove Secrets from Git History
**Script**: `remove-secrets-from-history.ps1`

**Options**:
1. **GitHub Allow URL** (Simplest - Recommended)
   ```powershell
   .\remove-secrets-from-history.ps1 -UseGitHubAllow
   ```
   Then visit: https://github.com/DrorGr/amesaBE/security/secret-scanning/unblock-secret/35e54lwedqCeb8jRqmdRR1bWszL

2. **Manual History Rewrite** (If git-filter-repo not available)
   - Script will attempt manual rebase approach
   - Amends the commit to remove secrets

3. **git-filter-repo** (If installed)
   - Fully rewrites history
   - Requires: `pip install git-filter-repo`

**Run**: `cd BE\Infrastructure; .\remove-secrets-from-history.ps1`

---

## Task 2: Investigate Route Tables
**Script**: `investigate-route-tables.ps1`

**What it does**:
- Lists ALB subnets and their route tables
- Lists ECS subnets and their route tables
- Shows all routes in each route table
- Identifies potential routing issues

**Run**: `cd BE\Infrastructure; .\investigate-route-tables.ps1`

**Output**: Route table analysis with recommendations

---

## Task 3: Fix Security Groups
**Script**: `fix-security-groups.ps1`

**What it does**:
- Option 1: Add explicit rule to current shared security group
- Option 2: Create separate security groups for ALB and ECS
  - ALB SG: Allows HTTP/HTTPS from internet
  - ECS SG: Allows port 8080 from ALB SG only

**Run**: `cd BE\Infrastructure; .\fix-security-groups.ps1`

**Note**: After creating separate groups, you'll need to:
1. Update ALB to use new ALB security group
2. Update ECS services to use new ECS security group

---

## Task 4: Test VPC Connectivity
**Script**: `test-vpc-connectivity.ps1`

**What it does**:
- Lists available EC2 instances in the VPC
- Provides commands to test connectivity from bastion host
- Tests connection to ECS task IP on port 8080

**Run**: `cd BE\Infrastructure; .\test-vpc-connectivity.ps1`

**Requirements**:
- EC2 instance in same VPC (or use SSM Session Manager)
- SSM agent installed on instance
- Proper IAM roles for SSM

**Alternative**: Manual testing commands provided in script output

---

## Execution Order (Recommended)

1. **First**: Remove secrets from git history
   ```powershell
   cd BE\Infrastructure
   .\remove-secrets-from-history.ps1 -UseGitHubAllow
   ```

2. **Then**: Investigate route tables
   ```powershell
   .\investigate-route-tables.ps1
   ```

3. **Based on findings**: Fix security groups
   ```powershell
   .\fix-security-groups.ps1
   ```

4. **Finally**: Test connectivity
   ```powershell
   .\test-vpc-connectivity.ps1
   ```

---

## Notes

- All scripts are independent and can be run in any order
- Each script provides clear output and next steps
- Scripts handle errors gracefully
- No script depends on another completing first

