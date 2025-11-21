<# 
Generates SQL scripts for EF Core migrations for all microservices.
Outputs versioned, idempotent SQL you can run in RDS Query Editor v2.

Usage:
  powershell -ExecutionPolicy Bypass -File .\generate-migration-sql.ps1

Notes:
- Each script prepends "SET search_path TO <schema>;" to ensure correct schema.
- Scripts are written to BE\Infrastructure\sql\<service>-migrations.sql
#>

param()

$ErrorActionPreference = "Stop"

Write-Output "=== Generate EF Migration SQL for All Services ==="

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$beDir = Split-Path -Parent $scriptDir
$outDir = Join-Path $beDir "Infrastructure\sql"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$services = @(
    @{Name="Auth";           Path="AmesaBackend.Auth";           Context="AuthDbContext";             Schema="amesa_auth"},
    @{Name="Content";        Path="AmesaBackend.Content";        Context="ContentDbContext";          Schema="amesa_content"},
    @{Name="Notification";   Path="AmesaBackend.Notification";   Context="NotificationDbContext";     Schema="amesa_notification"},
    @{Name="Payment";        Path="AmesaBackend.Payment";        Context="PaymentDbContext";          Schema="amesa_payment"},
    @{Name="Lottery";        Path="AmesaBackend.Lottery";        Context="LotteryDbContext";          Schema="amesa_lottery"},
    @{Name="LotteryResults"; Path="AmesaBackend.LotteryResults"; Context="LotteryResultsDbContext";   Schema="amesa_lottery_results"},
    @{Name="Analytics";      Path="AmesaBackend.Analytics";      Context="AnalyticsDbContext";        Schema="amesa_analytics"}
)

foreach ($svc in $services) {
    $svcPath = Join-Path $beDir $svc.Path
    if (-not (Test-Path $svcPath)) {
        Write-Output "Skipping $($svc.Name): path not found $svcPath"
        continue
    }
    Push-Location $svcPath
    try {
        $outFile = Join-Path $outDir "$($svc.Name)-migrations.sql"
        Write-Output "Creating script for $($svc.Name) -> $outFile"
        # Generate idempotent (-i) SQL from initial to latest
        $tmp = Join-Path $env:TEMP "ef-$($svc.Name)-$(Get-Random).sql"
        dotnet ef migrations script -i --context $($svc.Context) -o $tmp | Out-Null

        # Prepend search_path to ensure the correct schema is targeted
        $header = @"
-- Amesa EF Core migration script for $($svc.Name)
-- $(Get-Date -Format o)
SET search_path TO $($svc.Schema);

"@
        $body = Get-Content -Raw $tmp
        $header + $body | Set-Content -NoNewline -Path $outFile -Encoding UTF8
        Remove-Item $tmp -ErrorAction SilentlyContinue
    } catch {
        Write-Output "Error generating script for $($svc.Name): $_"
    } finally {
        Pop-Location
    }
}

Write-Output ""
Write-Output "Done. SQL files written to: $outDir"
Write-Output "You can open each file and run it in RDS Query Editor v2."


