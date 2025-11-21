# Database Seeding Options

Since your services are running on **Fargate** (not EC2), you have several options for seeding the databases:

## Option 1: ECS Exec (Recommended for Fargate)

Use AWS ECS Exec to run commands directly in your running Fargate containers.

### Prerequisites
- ECS Exec enabled on the service
- IAM permissions for `ecs:ExecuteCommand`
- Container has .NET SDK and PostgreSQL client tools

### Steps

1. **Enable ECS Exec** (if not already enabled):
```powershell
aws ecs update-service --cluster Amesa --service amesa-lottery-service --enable-execute-command --region eu-north-1
```

2. **Run the seeding script**:
```powershell
cd BE/Infrastructure
.\seed-databases-via-ecs-exec.ps1 -DbPassword "your-password"
```

3. **Or use interactive ECS Exec**:
```powershell
# Get running task
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text

# Execute interactive command
aws ecs execute-command --cluster Amesa --task $taskArn --container amesa-lottery-service --interactive --region eu-north-1
```

## Option 2: Create Temporary Seeding Task

Create a one-time ECS task specifically for seeding.

### Steps

1. **Create a seeding task definition** with:
   - Your seeder Docker image
   - Database connection secrets
   - Run-once configuration

2. **Run the task**:
```powershell
aws ecs run-task --cluster Amesa --task-definition amesa-database-seeder --region eu-north-1
```

3. **Monitor and clean up** after completion

## Option 3: Use EC2 Bastion Host

If you have an EC2 instance in the same VPC:

1. **SSH into EC2 instance**
2. **Run seeding script from there** (using the SSH script we created)

### Find EC2 Instances
```powershell
aws ec2 describe-instances --region eu-north-1 --filters "Name=instance-state-name,Values=running" --query "Reservations[*].Instances[*].{InstanceId:InstanceId,PublicIp:PublicIpAddress,Name:Tags[?Key=='Name'].Value|[0]}"
```

## Option 4: AWS RDS Query Editor

If you have access to AWS RDS Query Editor:

1. Connect to your Aurora cluster
2. Run SQL seeding scripts directly
3. Most manual but most direct

## Option 5: Local Development Database

For testing, use a local database:

1. **Use SQLite locally**:
```powershell
# Modify appsettings.Development.json
# Change connection string to: "Data Source=amesa.db"

cd BE/AmesaBackend
dotnet run -- --seeder
```

2. **Or use local PostgreSQL**:
```powershell
# Set connection string to local PostgreSQL
$env:DB_CONNECTION_STRING = "Host=localhost;Port=5432;Database=amesa_dev;Username=postgres;Password=postgres;"
dotnet run -- --seeder
```

## Recommended Approach

For **production seeding**, I recommend:

1. **Option 1 (ECS Exec)** - If your containers have .NET and database tools
2. **Option 2 (Temporary Task)** - If you need a dedicated seeding environment
3. **Option 3 (EC2 Bastion)** - If you have a bastion host set up

## What I Need From You

To proceed with any option, I need:

1. **Database Password** - For the Aurora cluster
2. **Preferred Method** - Which option you want to use
3. **Service Selection** - Which service to use (if using ECS Exec)
   - `amesa-lottery-service` (likely has .NET and DB access)
   - Or another service that has the tools needed

## Next Steps

Once you choose an option, I can:
- Create the specific scripts needed
- Help you run the seeding
- Verify the results

Which option would you like to use?

