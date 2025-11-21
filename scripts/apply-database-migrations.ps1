<# 
PowerShell script to apply EF Core migrations for all microservices
Prerequisites: .NET SDK 8.0 must be installed and database schemas must exist

New:
- Supports non-interactive mode via -AutoYes
- Supports applying migrations against Aurora by passing connection parameters
  (-AuroraHost, -DbName, -DbUser, -DbPassword). The script will set
  ConnectionStrings__DefaultConnection per service with the appropriate SearchPath.
#>

[CmdletBinding()]
param(
    [switch]$AutoYes,
    [string]$AuroraHost,
    [string]$DbName,
    [string]$DbUser,
    [string]$DbPassword,
    [int]$Port = 5432
)

Write-Output "=== Apply Database Migrations for Microservices ==="
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

$schemaMap = @{
    "Auth"            = "amesa_auth"
    "Content"         = "amesa_content"
    "Notification"    = "amesa_notification"
    "Payment"         = "amesa_payment"
    "Lottery"         = "amesa_lottery"
    "LotteryResults"  = "amesa_lottery_results"
    "Analytics"       = "amesa_analytics"
}

$successCount = 0
$failCount = 0

Write-Output "⚠️  WARNING: This will apply migrations to the database."
Write-Output "   Ensure database schemas are created and password is updated."
Write-Output ""
if (-not $AutoYes) {
    $confirm = Read-Host "Continue? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Output "Cancelled."
        exit 0
    }
}

Write-Output ""

foreach ($service in $services) {
    $servicePath = Join-Path $beDir $service.Path
    
    Write-Output "----------------------------------------"
    Write-Output "Applying migrations for $($service.Name) Service..."
    Write-Output "Path: $servicePath"
    
    if (-not (Test-Path $servicePath)) {
        Write-Output "❌ Error: Service path not found: $servicePath"
        $failCount++
        continue
    }
    
    # Change to service directory
    Push-Location $servicePath
    
    try {
        # If Aurora parameters were supplied, construct and export per-service connection string
        $setDynamicConnection = $false
        if ($AuroraHost -and $DbName -and $DbUser -and $DbPassword) {
            $schema = $schemaMap[$service.Name]
            if (-not $schema) {
                Write-Output "❌ Error: No schema mapping found for service $($service.Name)"
                throw "Missing schema mapping"
            }
            $dynamicConnection = "Host=$AuroraHost;Port=$Port;Database=$DbName;Username=$DbUser;Password=$DbPassword;SearchPath=$schema;"
            # Export environment variable for EF Tools (used by CreateHostBuilder)
            $env:ConnectionStrings__DefaultConnection = $dynamicConnection
            $setDynamicConnection = $true
            Write-Output "Using dynamic connection string for $($service.Name) (SearchPath=$schema)"
        }

        # Check if migrations exist
        $migrationsDir = Join-Path $servicePath "Migrations"
        if (-not (Test-Path $migrationsDir)) {
            Write-Output "⚠️  No migrations directory found. Creating migrations first..."
            
            # Create migrations directory
            New-Item -ItemType Directory -Path $migrationsDir -Force | Out-Null
            
            # Create initial migration
            Write-Output "Creating initial migration..."
            $createResult = & dotnet ef migrations add InitialCreate --context $($service.Context) --output-dir Migrations 2>&1
            
            if ($LASTEXITCODE -ne 0) {
                Write-Output "❌ Error creating migration:"
                Write-Output $createResult
                $failCount++
                Pop-Location
                continue
            }
        }
        
        # Apply migrations
        Write-Output "Applying migrations to database..."
        $result = & dotnet ef database update --context $($service.Context) 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Output "✅ $($service.Name) Service migrations applied successfully"
            $successCount++
        } else {
            Write-Output "❌ Error applying migrations for $($service.Name) Service:"
            Write-Output $result
            $failCount++
        }
    } catch {
        Write-Output "❌ Error: $_"
        $failCount++
    } finally {
        if ($setDynamicConnection) {
            Remove-Item Env:\ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
        }
        Pop-Location
    }
    
    Write-Output ""
}

Write-Output "========================================"
Write-Output "Migration Application Summary:"
Write-Output "  ✅ Successful: $successCount"
Write-Output "  ❌ Failed: $failCount"
Write-Output ""

if ($failCount -eq 0) {
    Write-Output "✅ All database migrations applied successfully!"
} else {
    Write-Output "⚠️ Some migrations failed. Please review errors above."
    Write-Output ""
    Write-Output "Common issues:"
    Write-Output "  1. Database schemas not created - Run setup-database.ps1 first"
    Write-Output "  2. Incorrect password - Update password in appsettings.json"
    Write-Output "  3. Network connectivity - Verify Aurora endpoint is accessible"
    exit 1
}

