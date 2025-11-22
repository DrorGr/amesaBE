# PowerShell script to run lottery favorites migration
# Usage: .\run-migration.ps1

$env:PGPASSWORD = 'u1fwn3s9'
$host = 'amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com'
$user = 'amesa_admin'
$database = 'postgres'  # Change to your actual database name if different

Write-Host "Running lottery-favorites-migration.sql..." -ForegroundColor Green
psql -h $host -U $user -d $database -f lottery-favorites-migration.sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration completed successfully!" -ForegroundColor Green
    Write-Host "Running lottery-favorites-translations.sql..." -ForegroundColor Green
    psql -h $host -U $user -d $database -f lottery-favorites-translations.sql
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "All scripts completed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Translation script failed!" -ForegroundColor Red
    }
} else {
    Write-Host "Migration script failed!" -ForegroundColor Red
}


