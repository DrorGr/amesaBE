#Requires -Version 5.1
# Policy: NO SSM/DB unless -UseDatabase (generate SQL) or -Apply (generate + execute). Audit first (TranslationAuditTool default).
<#
.SYNOPSIS
  Generates seed-missing-translation-keys.sql from audit gaps; -Apply executes it on the database.

.EXAMPLE
  .\run-seed-missing-translation-keys.ps1
  .\run-seed-missing-translation-keys.ps1 -UseDatabase
  .\run-seed-missing-translation-keys.ps1 -Apply
#>
param(
    [switch]$Apply,
    [switch]$UseDatabase,
    [string]$Region = "eu-north-1",
    [string]$ParameterName = "/amesa/prod/ConnectionStrings/Content"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$toolProj = Join-Path $repoRoot "MetaData\Scripts\Database\TranslationAuditTool\TranslationAuditTool.csproj"

if (-not $Apply -and -not $UseDatabase) {
    Write-Host "Preview only (no DB/SSM)."
    Write-Host "  1. Run audit: dotnet run --project TranslationAuditTool (default: FE scrape + local files)"
    Write-Host "  2. Generate SQL: .\run-seed-missing-translation-keys.ps1 -UseDatabase"
    Write-Host "  3. Apply seed:   .\run-seed-missing-translation-keys.ps1 -Apply"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($env:AMESA_DB_CONNECTION_STRING)) {
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI required to fetch SSM connection string, or set AMESA_DB_CONNECTION_STRING."
    }
    $raw = aws ssm get-parameter --name $ParameterName --with-decryption --region $Region --query Parameter.Value --output text
    if ([string]::IsNullOrWhiteSpace($raw)) {
        throw "Empty connection string from $ParameterName"
    }
    $env:AMESA_DB_CONNECTION_STRING = $raw.Trim()
}

$dotnetArgs = @("--seed-only", "--use-db", $repoRoot)
if ($Apply) {
    $dotnetArgs = @("--apply-seed", "--seed-only", "--use-db", $repoRoot)
}

dotnet run --project $toolProj -- @dotnetArgs
if ($Apply) {
    Write-Host "Done. Flush Redis translations_* keys on Content service if cached."
}
