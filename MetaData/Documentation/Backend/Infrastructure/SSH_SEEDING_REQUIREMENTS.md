# SSH Database Seeding Requirements

## What I Need From You

To seed the databases via SSH, I need the following information:

### 1. SSH Connection Details

- **EC2 Instance IP or Hostname**
  - Example: `54.123.45.67` or `ec2-54-123-45-67.eu-north-1.compute.amazonaws.com`
  - You can find this in AWS Console → EC2 → Instances

- **SSH Username**
  - `ec2-user` (for Amazon Linux)
  - `ubuntu` (for Ubuntu)
  - `admin` (for Debian)
  - Or your custom username

- **SSH Private Key File Path**
  - Path to your `.pem` file
  - Example: `C:\Users\YourName\.ssh\amesa-key.pem`
  - **Note**: On Linux/Mac, ensure key has correct permissions: `chmod 400 key.pem`

- **SSH Port** (optional, default: 22)

### 2. Database Connection Details

I can use these defaults (from your infrastructure), but please confirm:

- **Database Endpoint**: `amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com`
- **Database Name**: `amesa_prod`
- **Database Username**: `amesa_admin`
- **Database Password**: ⚠️ **You'll need to provide this**
- **Database Port**: `5432` (default PostgreSQL)

### 3. Seeding Options

- **Which schemas to seed?**
  - All schemas (default)
  - Specific schemas only
  - Options: `amesa_auth`, `amesa_content`, `amesa_lottery`, `amesa_lottery_results`, `amesa_payment`, `amesa_notification`, `amesa_analytics`

- **Existing data handling:**
  - `-ClearExisting` - Clear all existing data before seeding (⚠️ DESTRUCTIVE)
  - `-SkipIfExists` - Skip schemas that already have data
  - Default: Add new data to existing tables

## Quick Start

Once you provide the SSH details, run:

```powershell
cd BE/Infrastructure

# Basic usage
.\seed-databases-via-ssh.ps1 `
    -SshHost "54.123.45.67" `
    -SshUser "ec2-user" `
    -SshKeyPath "C:\path\to\your-key.pem" `
    -DbPassword "your-database-password"

# Seed specific schemas only
.\seed-databases-via-ssh.ps1 `
    -SshHost "54.123.45.67" `
    -SshUser "ec2-user" `
    -SshKeyPath "C:\path\to\your-key.pem" `
    -DbPassword "your-database-password" `
    -SchemasToSeed @("amesa_auth", "amesa_lottery")

# Clear existing data before seeding
.\seed-databases-via-ssh.ps1 `
    -SshHost "54.123.45.67" `
    -SshUser "ec2-user" `
    -SshKeyPath "C:\path\to\your-key.pem" `
    -DbPassword "your-database-password" `
    -ClearExisting
```

## What the Script Does

1. **Tests SSH Connection** - Verifies you can connect to the EC2 instance
2. **Checks .NET Installation** - Installs .NET 8.0 SDK if not present
3. **Creates Seeding Script** - Generates a bash script with all seeding logic
4. **Uploads Script** - Copies script to EC2 instance via SCP
5. **Executes Seeding** - Runs the seeding script on the remote host
6. **Seeds Each Schema** - Processes each schema in sequence
7. **Reports Results** - Shows success/failure for each schema

## Prerequisites on EC2 Instance

The script will automatically install these if missing:
- ✅ .NET 8.0 SDK
- ✅ PostgreSQL client tools (psql)

The EC2 instance needs:
- ✅ Internet access (to download .NET)
- ✅ Network access to RDS (security group configured)
- ✅ SSH access from your machine

## Security Notes

⚠️ **Important Security Considerations:**

1. **SSH Key Security**
   - Never commit SSH keys to git
   - Use proper file permissions (chmod 400 on Linux/Mac)
   - Consider using AWS Systems Manager Session Manager instead

2. **Password Handling**
   - Script will prompt for password if not provided
   - Consider using AWS Secrets Manager for production
   - Password is passed securely via SSH

3. **Network Security**
   - Ensure EC2 security group allows SSH from your IP only
   - Use VPN or bastion host for additional security
   - Consider using AWS Systems Manager Session Manager (no SSH keys needed)

## Alternative: AWS Systems Manager Session Manager

If you prefer not to use SSH keys, you can use AWS SSM Session Manager:

```powershell
# Connect via SSM (requires SSM agent on EC2)
aws ssm start-session --target i-1234567890abcdef0

# Then run seeding commands manually
```

## Troubleshooting

### SSH Connection Fails
- Check EC2 instance is running
- Verify security group allows SSH (port 22) from your IP
- Check SSH key permissions: `chmod 400 key.pem` (Linux/Mac)
- Verify username is correct for your AMI

### Database Connection Fails
- Check RDS security group allows connections from EC2 security group
- Verify database endpoint is correct
- Check database credentials
- Ensure EC2 instance is in same VPC or has network path to RDS

### .NET Installation Fails
- Check EC2 instance has internet access
- Verify NAT Gateway or Internet Gateway is configured
- Check security group allows outbound HTTPS (443)

## Next Steps

1. **Provide SSH Details** - Give me the EC2 instance IP, username, and key path
2. **Confirm Database Details** - Verify or provide database connection info
3. **Choose Seeding Options** - Which schemas and data handling strategy
4. **Run the Script** - Execute the seeding script

Once you provide the information, I'll help you run it!

