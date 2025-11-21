# PowerShell script to verify database setup is complete
# This script checks all prerequisites and setup status

$ErrorActionPreference = "Stop"

Write-Output "========================================"
Write-Output "Database Setup Verification Script"
Write-Output "========================================"
Write-Output ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$beDir = Split-Path -Parent $scriptDir
$Region = "eu-north-1"
$AuroraHost = "amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com"

$allChecksPassed = $true

# Check 1: AWS CLI
Write-Output "Check 1: AWS CLI Installation"
$awsCli = Get-Command aws -ErrorAction SilentlyContinue
if ($awsCli) {
    $awsVersion = aws --version 2>&1
    Write-Output "  [OK] AWS CLI installed: $awsVersion"
} else {
    Write-Output "  [ERROR] AWS CLI not found. Please install: https://aws.amazon.com/cli/"
    $allChecksPassed = $false
}
Write-Output ""

# Check 2: PostgreSQL Client
Write-Output "Check 2: PostgreSQL Client (psql)"
$psql = Get-Command psql -ErrorAction SilentlyContinue
if ($psql) {
    $psqlVersion = psql --version 2>&1
    Write-Output "  [OK] psql installed: $psqlVersion"
} else {
    Write-Output "  [WARNING] psql not found. Required for schema creation."
    Write-Output "     Alternative: Use AWS RDS Query Editor"
}
Write-Output ""

# Check 3: .NET SDK
Write-Output "Check 3: .NET SDK"
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    $dotnetVersion = dotnet --version 2>&1
    Write-Output "  [OK] .NET SDK installed: $dotnetVersion"
} else {
    Write-Output "  [ERROR] .NET SDK not found. Please install .NET SDK 8.0"
    $allChecksPassed = $false
}
Write-Output ""

# Check 4: IAM Role
Write-Output "Check 4: ECS Task Execution Role"
try {
    $role = aws iam get-role --role-name ecsTaskExecutionRole --region $Region 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Output "  [OK] IAM role 'ecsTaskExecutionRole' exists"
        
        # Check for ECR policy
        $policies = aws iam list-role-policies --role-name ecsTaskExecutionRole --region $Region | ConvertFrom-Json
        if ($policies.PolicyNames -contains "ECR-Access-Policy") {
            Write-Output "  [OK] ECR permissions attached"
        } else {
            Write-Output "  [WARNING] ECR permissions not found. Run fix-ecr-network-access.ps1"
        }
    } else {
        Write-Output "  [WARNING] IAM role not found. Run fix-ecr-network-access.ps1"
    }
} catch {
    Write-Output "  [WARNING] Could not verify IAM role: $_"
}
Write-Output ""

# Check 5: Database Password in appsettings.json
Write-Output "Check 5: Database Password in appsettings.json"
$services = @(
    "AmesaBackend.Auth",
    "AmesaBackend.Payment",
    "AmesaBackend.Lottery",
    "AmesaBackend.Content",
    "AmesaBackend.Notification",
    "AmesaBackend.LotteryResults",
    "AmesaBackend.Analytics",
    "AmesaBackend.Admin"
)

$passwordUpdated = 0
$passwordNotUpdated = 0

foreach ($service in $services) {
    $appsettingsPath = Join-Path $beDir "$service\appsettings.json"
    if (Test-Path $appsettingsPath) {
        $content = Get-Content $appsettingsPath -Raw
        if ($content -match "Password=CHANGE_ME") {
            Write-Output "  [WARNING] ${service}: Password still set to CHANGE_ME"
            $passwordNotUpdated++
        } elseif ($content -match "Password=") {
            Write-Output "  [OK] ${service}: Password configured"
            $passwordUpdated++
        }
    }
}

if ($passwordNotUpdated -gt 0) {
    Write-Output "  [WARNING] $passwordNotUpdated service(s) still have CHANGE_ME password"
    Write-Output "     Run update-database-password.ps1 to fix"
} else {
    Write-Output "  [OK] All services have passwords configured"
}
Write-Output ""

# Check 6: Database Schemas (requires password)
Write-Output "Check 6: Database Schemas in Aurora"
Write-Output "  [INFO] Manual verification required:"
Write-Output "     Connect to Aurora and run:"
Write-Output "     SELECT schema_name FROM information_schema.schemata WHERE schema_name LIKE 'amesa_%';"
Write-Output ""

# Check 7: Migrations Directory
Write-Output "Check 7: Migration Files"
$servicesWithContext = @(
    @{Path="AmesaBackend.Auth"; Context="AuthDbContext"},
    @{Path="AmesaBackend.Content"; Context="ContentDbContext"},
    @{Path="AmesaBackend.Notification"; Context="NotificationDbContext"},
    @{Path="AmesaBackend.Payment"; Context="PaymentDbContext"},
    @{Path="AmesaBackend.Lottery"; Context="LotteryDbContext"},
    @{Path="AmesaBackend.LotteryResults"; Context="LotteryResultsDbContext"},
    @{Path="AmesaBackend.Analytics"; Context="AnalyticsDbContext"}
)

$migrationsFound = 0
$migrationsMissing = 0

foreach ($service in $servicesWithContext) {
    $migrationsPath = Join-Path $beDir "$($service.Path)\Migrations"
    if (Test-Path $migrationsPath) {
        $migrationFiles = Get-ChildItem $migrationsPath -Filter "*.cs" | Where-Object { $_.Name -notlike "*Designer*" }
        if ($migrationFiles.Count -gt 0) {
            Write-Output "  [OK] $($service.Path): $($migrationFiles.Count) migration(s) found"
            $migrationsFound++
        } else {
            Write-Output "  [WARNING] $($service.Path): Migrations directory exists but no migration files"
            $migrationsMissing++
        }
    } else {
        Write-Output "  [WARNING] $($service.Path): No Migrations directory"
        $migrationsMissing++
    }
}

if ($migrationsMissing -gt 0) {
    Write-Output "  [WARNING] $migrationsMissing service(s) missing migrations"
    Write-Output "     Run database-migrations.ps1 to create migrations"
} else {
    Write-Output "  [OK] All services have migrations"
}
Write-Output ""

# Check 8: ECS Cluster
Write-Output "Check 8: ECS Cluster"
try {
    $clusters = aws ecs list-clusters --region $Region | ConvertFrom-Json
    $amesaCluster = $clusters.clusterArns | Where-Object { $_ -like "*Amesa*" } | Select-Object -First 1
    if ($amesaCluster) {
        Write-Output "  [OK] ECS cluster found: $amesaCluster"
    } else {
        Write-Output "  [WARNING] ECS cluster 'Amesa' not found"
    }
} catch {
    Write-Output "  [WARNING] Could not verify ECS cluster: $_"
}
Write-Output ""

# Summary
Write-Output "========================================"
Write-Output "Verification Summary"
Write-Output "========================================"
Write-Output ""

if ($allChecksPassed -and $passwordNotUpdated -eq 0 -and $migrationsMissing -eq 0) {
    Write-Output "[SUCCESS] All checks passed! Ready to deploy."
} else {
    Write-Output "[WARNING] Some checks failed. Please review above and:"
    Write-Output ""
    if (-not $allChecksPassed) {
        Write-Output "  1. Install missing prerequisites (AWS CLI, .NET SDK)"
    }
    if ($passwordNotUpdated -gt 0) {
        Write-Output "  2. Run: .\update-database-password.ps1"
    }
    if ($migrationsMissing -gt 0) {
        Write-Output "  3. Run: ..\scripts\database-migrations.ps1"
    }
    Write-Output "  4. Run: .\setup-database.ps1 (create schemas)"
    Write-Output "  5. Run: ..\scripts\apply-database-migrations.ps1 (apply migrations)"
}
Write-Output ""

