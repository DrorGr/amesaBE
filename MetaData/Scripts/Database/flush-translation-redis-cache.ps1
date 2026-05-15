#Requires -Version 5.1
# Policy: NO Redis access unless -Apply (deletes translation cache keys).
<#
.SYNOPSIS
  Flushes Content service translation cache keys in Redis (amesa-content:translations_* and languages_list).

.EXAMPLE
  .\flush-translation-redis-cache.ps1
  .\flush-translation-redis-cache.ps1 -Apply
#>
param(
    [switch]$Apply,
    [string]$Region = "eu-north-1",
    [string]$RedisParameter = "/amesa/prod/ConnectionStrings/Redis"
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$toolProj = Join-Path $repoRoot "MetaData\Scripts\Database\TranslationAuditTool\TranslationAuditTool.csproj"

if (-not $Apply) {
    Write-Host "Preview only (no Redis connection)."
    Write-Host "Re-run with -Apply to fetch SSM Redis connection and delete translations_* / languages_list keys."
    exit 0
}

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    throw "AWS CLI required."
}

$redis = aws ssm get-parameter --name $RedisParameter --with-decryption --region $Region --query Parameter.Value --output text
if ([string]::IsNullOrWhiteSpace($redis)) {
    throw "Empty Redis connection from $RedisParameter"
}

$env:AMESA_REDIS_CONNECTION_STRING = $redis.Trim()
dotnet run --project $toolProj -- --flush-redis
