# PowerShell script to create EF Core migrations for all microservices
# Prerequisites: .NET SDK 8.0 must be installed

Write-Output "=== Database Migration Script for Microservices ==="
Write-Output ""

$ErrorActionPreference = "Stop"

# Check if dotnet is available
$dotnetPath = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnetPath) {
    Write-Output "❌ Error: .NET SDK is not installed or not in PATH"
    Write-Output "Please install .NET SDK 8.0"
    exit 1
}

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$beDir = Split-Path -Parent $scriptDir

Write-Output "Working directory: $beDir"
Write-Output ""

# Services and their DbContext names
$services = @(
    @{Name="Auth"; Path="AmesaBackend.Auth"; Context="AuthDbContext"},
    @{Name="Content"; Path="AmesaBackend.Content"; Context="ContentDbContext"},
    @{Name="Notification"; Path="AmesaBackend.Notification"; Context="NotificationDbContext"},
    @{Name="Payment"; Path="AmesaBackend.Payment"; Context="PaymentDbContext"},
    @{Name="Lottery"; Path="AmesaBackend.Lottery"; Context="LotteryDbContext"},
    @{Name="LotteryResults"; Path="AmesaBackend.LotteryResults"; Context="LotteryResultsDbContext"},
    @{Name="Analytics"; Path="AmesaBackend.Analytics"; Context="AnalyticsDbContext"}
)

$successCount = 0
$failCount = 0

foreach ($service in $services) {
    $servicePath = Join-Path $beDir $service.Path
    $migrationsDir = Join-Path $servicePath "Migrations"
    
    Write-Output "----------------------------------------"
    Write-Output "Creating migration for $($service.Name) Service..."
    Write-Output "Path: $servicePath"
    
    if (-not (Test-Path $servicePath)) {
        Write-Output "❌ Error: Service path not found: $servicePath"
        $failCount++
        continue
    }
    
    # Change to service directory
    Push-Location $servicePath
    
    try {
        # Create migrations directory if it doesn't exist
        if (-not (Test-Path $migrationsDir)) {
            New-Item -ItemType Directory -Path $migrationsDir -Force | Out-Null
        }
        
        # Create migration
        Write-Output "Running: dotnet ef migrations add InitialCreate --context $($service.Context) --output-dir Migrations"
        $result = & dotnet ef migrations add InitialCreate --context $($service.Context) --output-dir Migrations 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Output "✅ $($service.Name) Service migration created successfully"
            $successCount++
        } else {
            Write-Output "❌ Error creating migration for $($service.Name) Service:"
            Write-Output $result
            $failCount++
        }
    } catch {
        Write-Output "❌ Error: $_"
        $failCount++
    } finally {
        Pop-Location
    }
    
    Write-Output ""
}

Write-Output "========================================"
Write-Output "Migration Summary:"
Write-Output "  ✅ Successful: $successCount"
Write-Output "  ❌ Failed: $failCount"
Write-Output ""

if ($failCount -eq 0) {
    Write-Output "✅ All database migrations created successfully!"
    Write-Output ""
    Write-Output "Next steps:"
    Write-Output "  1. Update database connection strings with actual password"
    Write-Output "  2. Run: dotnet ef database update --context <DbContext>"
    Write-Output "     (for each service)"
} else {
    Write-Output "⚠️ Some migrations failed. Please review errors above."
    exit 1
}

