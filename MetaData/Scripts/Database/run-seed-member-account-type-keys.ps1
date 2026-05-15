#Requires -Version 5.1
# Policy: NO database access unless -Apply. Preview shows SQL path only.
<#
.SYNOPSIS
  Applies seed-member-account-type-keys.sql via Content DB connection from SSM.

.EXAMPLE
  .\run-seed-member-account-type-keys.ps1
  .\run-seed-member-account-type-keys.ps1 -Apply
#>
param(
    [switch]$Apply,
    [string]$Region = "eu-north-1",
    [string]$ParameterName = "/amesa/prod/ConnectionStrings/Content"
)

$ErrorActionPreference = "Stop"
$sqlFile = Join-Path $PSScriptRoot "seed-member-account-type-keys.sql"
if (-not (Test-Path $sqlFile)) {
    throw "SQL file not found: $sqlFile"
}

if (-not $Apply) {
    Write-Host "Preview only (no DB connection). SQL: $sqlFile"
    Write-Host "Re-run with -Apply to fetch SSM connection string and execute via psql."
    exit 0
}

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) { throw "AWS CLI required." }
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql required." }

$raw = aws ssm get-parameter --name $ParameterName --with-decryption --region $Region --query Parameter.Value --output text
$dict = @{}
foreach ($part in $raw -split ';') {
    if ([string]::IsNullOrWhiteSpace($part)) { continue }
    $eq = $part.IndexOf('=')
    if ($eq -lt 1) { continue }
    $dict[$part.Substring(0, $eq).Trim()] = $part.Substring($eq + 1).Trim()
}

$env:PGPASSWORD = $dict['Password']
try {
    & psql -h $dict['Host'] -p $(if ($dict['Port']) { $dict['Port'] } else { '5432' }) -U $dict['Username'] -d $dict['Database'] -v ON_ERROR_STOP=1 -f $sqlFile
    if ($LASTEXITCODE -ne 0) { throw "psql exited with code $LASTEXITCODE" }
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "Member account type keys seeded."
