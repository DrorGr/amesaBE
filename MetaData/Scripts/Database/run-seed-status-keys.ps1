#Requires -Version 5.1
# Policy: NO database access unless -Apply. Preview shows SQL path only.
<#
.SYNOPSIS
  Applies seed-status-translation-keys.sql to Aurora PostgreSQL (Content schema).

.NOTES
  Prerequisites when using -Apply: AWS CLI, psql, network path to Aurora.

.EXAMPLE
  .\run-seed-status-keys.ps1
  .\run-seed-status-keys.ps1 -Apply -Region eu-north-1
#>
param(
    [switch]$Apply,
    [string]$Region = "eu-north-1",
    [string]$ParameterName = "/amesa/prod/ConnectionStrings/Content"
)

$ErrorActionPreference = "Stop"

function Get-RepoRoot {
    $here = $PSScriptRoot
    if (-not $here) { $here = Split-Path -Parent $MyInvocation.MyCommand.Path }
    return (Resolve-Path (Join-Path $here "..\..\..")).Path
}

$sqlFile = Join-Path (Get-RepoRoot) "MetaData\Scripts\Database\seed-status-translation-keys.sql"
if (-not (Test-Path $sqlFile)) {
    throw "SQL file not found: $sqlFile"
}

if (-not $Apply) {
    Write-Host "Preview only (no DB connection). SQL: $sqlFile"
    Write-Host "Re-run with -Apply to fetch SSM connection string and execute via psql."
    exit 0
}

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    throw "AWS CLI (aws) not found on PATH."
}
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql not found. Install PostgreSQL client tools and ensure psql is on PATH."
}

$null = aws ssm get-parameter --name $ParameterName --region $Region --query Parameter.Name --output text

$raw = aws ssm get-parameter --name $ParameterName --with-decryption --region $Region --query Parameter.Value --output text
if ([string]::IsNullOrWhiteSpace($raw)) {
    throw "Empty connection string from SSM parameter $ParameterName"
}

function Parse-DotNetConnectionString([string]$s) {
    $dict = @{}
    foreach ($part in $s -split ';') {
        if ([string]::IsNullOrWhiteSpace($part)) { continue }
        $eq = $part.IndexOf('=')
        if ($eq -lt 1) { continue }
        $k = $part.Substring(0, $eq).Trim()
        $v = $part.Substring($eq + 1).Trim()
        $dict[$k] = $v
    }
    return $dict
}

$cs = Parse-DotNetConnectionString $raw
$dbHost = $cs['Host']
$dbPort = if ($cs['Port']) { $cs['Port'] } else { '5432' }
$dbName = $cs['Database']
$dbUser = $cs['Username']
$dbPass = $cs['Password']

if (-not $dbHost -or -not $dbName -or -not $dbUser) {
    throw "Could not parse Host, Database, or Username from SSM connection string."
}

$env:PGPASSWORD = $dbPass
try {
    & psql -h $dbHost -p $dbPort -U $dbUser -d $dbName -v ON_ERROR_STOP=1 -f $sqlFile
    if ($LASTEXITCODE -ne 0) {
        throw "psql exited with code $LASTEXITCODE"
    }
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

Write-Host "Seed finished. If translations are cached in Redis, flush Content keys matching translations_* ."
