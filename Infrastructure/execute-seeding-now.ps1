# Execute Database Seeding via ECS Exec
$ErrorActionPreference = "Stop"

$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text
$taskArn = $taskArn.Trim()

Write-Host "Task: $($taskArn.Split('/')[-1])" -ForegroundColor Green

$env:Path += ";C:\Program Files\Amazon\SessionManagerPlugin\bin"

# Read SQL file
$sqlPath = Join-Path $PSScriptRoot "complete-seed-all-data.sql"
$sqlContent = Get-Content $sqlPath -Raw -Encoding UTF8

# Create a bash script that will be executed
$bashScript = @"
export PGPASSWORD='u1fwn3s9'
which psql || apk add --no-cache postgresql-client
psql -h amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com -p 5432 -U amesa_admin -d amesa_prod <<'SQL_EOF'
$sqlContent
SQL_EOF
"@

# Write to temp file and base64 encode
$tempFile = [System.IO.Path]::GetTempFileName()
$bashScript | Out-File -FilePath $tempFile -Encoding UTF8 -NoNewline

$scriptBytes = [System.IO.File]::ReadAllBytes($tempFile)
$scriptBase64 = [Convert]::ToBase64String($scriptBytes)

# Execute via ECS
Write-Host "Executing seeding script..." -ForegroundColor Yellow

$command = "bash -c `"echo '$scriptBase64' | base64 -d | bash`""

& aws ecs execute-command `
    --cluster Amesa `
    --task $taskArn `
    --container amesa-lottery-service-container `
    --interactive `
    --command $command `
    --region eu-north-1

Remove-Item $tempFile -ErrorAction SilentlyContinue

