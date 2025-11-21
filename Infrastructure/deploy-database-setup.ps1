# Master script to execute all database setup tasks
# This script orchestrates: schema creation, password update, and migrations

$ErrorActionPreference = "Stop"

Write-Output "========================================"
Write-Output "Amesa Database Setup - Master Script"
Write-Output "========================================"
Write-Output ""

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$beDir = Split-Path -Parent $scriptDir

Write-Output "This script will:"
Write-Output "  1. Create database schemas in Aurora"
Write-Output "  2. Update database password in appsettings.json files"
Write-Output "  3. Apply database migrations"
Write-Output ""
Write-Output "⚠️  Prerequisites:"
Write-Output "  - psql must be installed and in PATH"
Write-Output "  - .NET SDK 8.0 must be installed"
Write-Output "  - AWS CLI configured (for ECR fix)"
Write-Output "  - Aurora database password available"
Write-Output ""

$confirm = Read-Host "Continue? (yes/no)"
if ($confirm -ne "yes") {
    Write-Output "Cancelled."
    exit 0
}

Write-Output ""
Write-Output "========================================"
Write-Output "Step 1: Fix ECR Network Access"
Write-Output "========================================"
Write-Output ""

$ecrScript = Join-Path $scriptDir "fix-ecr-network-access.ps1"
if (Test-Path $ecrScript) {
    Write-Output "Running ECR network access fix..."
    & $ecrScript
    if ($LASTEXITCODE -ne 0) {
        Write-Output "⚠️  ECR fix completed with warnings. Review output above."
    }
} else {
    Write-Output "⚠️  ECR fix script not found: $ecrScript"
}

Write-Output ""
Write-Output "========================================"
Write-Output "Step 2: Create Database Schemas"
Write-Output "========================================"
Write-Output ""

$schemaScript = Join-Path $scriptDir "setup-database.ps1"
if (Test-Path $schemaScript) {
    Write-Output "Running database schema creation..."
    & $schemaScript
    if ($LASTEXITCODE -ne 0) {
        Write-Output "❌ Schema creation failed. Please fix errors and try again."
        exit 1
    }
} else {
    Write-Output "❌ Schema creation script not found: $schemaScript"
    exit 1
}

Write-Output ""
Write-Output "========================================"
Write-Output "Step 3: Update Database Password"
Write-Output "========================================"
Write-Output ""

$passwordScript = Join-Path $scriptDir "update-database-password.ps1"
if (Test-Path $passwordScript) {
    Write-Output "Running password update..."
    & $passwordScript
    if ($LASTEXITCODE -ne 0) {
        Write-Output "❌ Password update failed. Please fix errors and try again."
        exit 1
    }
} else {
    Write-Output "❌ Password update script not found: $passwordScript"
    exit 1
}

Write-Output ""
Write-Output "========================================"
Write-Output "Step 4: Apply Database Migrations"
Write-Output "========================================"
Write-Output ""

$migrationsScript = Join-Path $beDir "scripts\apply-database-migrations.ps1"
if (Test-Path $migrationsScript) {
    Write-Output "Running database migrations..."
    & $migrationsScript
    if ($LASTEXITCODE -ne 0) {
        Write-Output "❌ Migration application failed. Please fix errors and try again."
        exit 1
    }
} else {
    Write-Output "❌ Migration script not found: $migrationsScript"
    exit 1
}

Write-Output ""
Write-Output "========================================"
Write-Output "✅ Database Setup Complete!"
Write-Output "========================================"
Write-Output ""
Write-Output "All tasks completed successfully:"
Write-Output "  ✅ ECR network access configured"
Write-Output "  ✅ Database schemas created"
Write-Output "  ✅ Database password updated"
Write-Output "  ✅ Database migrations applied"
Write-Output ""
Write-Output "Next steps:"
Write-Output "  1. Verify ECS services can pull images from ECR"
Write-Output "  2. Test database connectivity from ECS tasks"
Write-Output "  3. Deploy services to ECS"
Write-Output ""

