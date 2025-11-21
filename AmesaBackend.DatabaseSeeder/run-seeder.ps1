#!/usr/bin/env pwsh

param(
    [Parameter(Position=0)]
    [ValidateSet("dev", "development", "prod", "production")]
    [string]$Environment = "development"
)

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "AmesaBackend Database Seeder" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET 8 is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "ERROR: .NET 8 SDK is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Set environment
$env:ASPNETCORE_ENVIRONMENT = if ($Environment -in @("prod", "production")) { "Production" } else { "Development" }

Write-Host "Environment: $($env:ASPNETCORE_ENVIRONMENT)" -ForegroundColor Yellow
Write-Host ""

# Build project
Write-Host "Building seeder..." -ForegroundColor Blue
try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE"
    }
    Write-Host "Build successful!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Build failed - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Run seeder
Write-Host "Running database seeder..." -ForegroundColor Blue
Write-Host ""

try {
    dotnet run --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Database seeding completed successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "❌ Database seeding failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
} catch {
    Write-Host "ERROR: Seeder execution failed - $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Press any key to continue..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
