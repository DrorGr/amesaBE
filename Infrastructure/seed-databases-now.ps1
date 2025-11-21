# Quick Database Seeding Script
# Creates schemas and prepares for data seeding

param(
    [string]$DbPassword = "u1fwn3s9"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Database Schema Seeding" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get task
Write-Host "Finding running task..." -ForegroundColor Cyan
$taskArn = aws ecs list-tasks --cluster Amesa --service-name amesa-lottery-service --region eu-north-1 --query "taskArns[0]" --output text
$taskArn = $taskArn.Trim()

if ([string]::IsNullOrEmpty($taskArn) -or $taskArn -eq "None") {
    Write-Host "❌ No running tasks found" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found task: $($taskArn.Split('/')[-1])" -ForegroundColor Green
Write-Host ""

# Create a simple seeding command
$seedCommand = @"
export PGPASSWORD='$DbPassword'
SCHEMAS=("amesa_auth" "amesa_content" "amesa_lottery" "amesa_lottery_results" "amesa_payment" "amesa_notification" "amesa_analytics")
DB_HOST='amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
DB_NAME='amesa_prod'
DB_USER='amesa_admin'
DB_PORT=5432

echo "Testing database connection..."
psql -h `$DB_HOST -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -c "SELECT 1;" || {
    echo "Installing PostgreSQL client..."
    apk add --no-cache postgresql-client 2>/dev/null || apt-get update && apt-get install -y postgresql-client 2>/dev/null || yum install -y postgresql 2>/dev/null
}

echo "Creating schemas..."
for schema in "`${SCHEMAS[@]}"; do
    echo "Processing: `$schema"
    psql -h `$DB_HOST -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -c "CREATE SCHEMA IF NOT EXISTS `$schema;"
    COUNT=`$(psql -h `$DB_HOST -p `$DB_PORT -U `$DB_USER -d `$DB_NAME -tAc "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='`$schema';" 2>/dev/null || echo "0")
    echo "  Tables in `$schema: `$COUNT"
done
echo "Done!"
"@

Write-Host "Executing seeding command..." -ForegroundColor Cyan
Write-Host ""

# Execute via ECS Exec
aws ecs execute-command `
    --cluster Amesa `
    --task $taskArn `
    --container amesa-lottery-service-container `
    --interactive `
    --command "bash -c `"$seedCommand`"" `
    --region eu-north-1

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Seeding completed!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Check output above for any errors" -ForegroundColor Yellow
}

