# Execute SQL Seeding Script via ECS Exec

param(
    [string]$Cluster = "Amesa",
    [string]$Region = "eu-north-1",
    [string]$ServiceName = "amesa-lottery-service",
    [string]$DbPassword = "u1fwn3s9"
)

$ErrorActionPreference = "Stop"

Write-Host "Executing SQL seeding script..." -ForegroundColor Cyan

# Get task
$taskArn = aws ecs list-tasks --cluster $Cluster --service-name $ServiceName --region $Region --query "taskArns[0]" --output text
$taskArn = $taskArn.Trim()

# Read SQL file
$sqlPath = Join-Path $PSScriptRoot "seed-all-data.sql"
$sqlContent = Get-Content $sqlPath -Raw -Encoding UTF8

# Create bash script that executes the SQL
$bashScript = @"
export PGPASSWORD='$DbPassword'
psql -h amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -p 5432 -U amesa_admin -d amesa_prod <<'PSQL_EOF'
$sqlContent
PSQL_EOF
"@

# Base64 encode
$scriptBytes = [System.Text.Encoding]::UTF8.GetBytes($bashScript)
$scriptBase64 = [Convert]::ToBase64String($scriptBytes)

# Execute
$env:Path += ";C:\Program Files\Amazon\SessionManagerPlugin\bin"
$command = "bash -c `"echo '$scriptBase64' | base64 -d | bash`""

Write-Host "Running SQL seeding..." -ForegroundColor Yellow
aws ecs execute-command `
    --cluster $Cluster `
    --task $taskArn `
    --container amesa-lottery-service-container `
    --interactive `
    --command $command `
    --region $Region

