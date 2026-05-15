# Policy: scrape-only by default (no DB). Use -UseDatabase or -UseApi for live comparison.
# Seeding is not performed by this script.
param(
    [switch]$UseDatabase,
    [switch]$UseApi,
    [string]$ConnectionString = "",
    [string]$ApiBaseUrl = 'https://amesa-group.net/api/v1/translations',
    [string]$SqlExportPath = "",
    [string[]]$Languages = @('en', 'es', 'fr', 'pl', 'de', 'ru'),
    [string]$OutputDir = "$PSScriptRoot/Database/translation-audit"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')

function Get-TranslationKeysFromText {
    param([string]$Text)
    $keys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    $patterns = @(
        "translate\s*\(\s*['\`"]([a-zA-Z][a-zA-Z0-9_.]+)['\`"]",
        "translationService\.translate\s*\(\s*['\`"]([a-zA-Z][a-zA-Z0-9_.]+)['\`"]",
        "this\.translate\s*\(\s*['\`"]([a-zA-Z][a-zA-Z0-9_.]+)['\`"]",
        "Key\s*=\s*['\`"]([a-zA-Z][a-zA-Z0-9_.]+)['\`"]",
        "['\`"],\s*'([a-zA-Z][a-zA-Z0-9_.]+)',\s*'"
    )
    foreach ($p in $patterns) {
        [regex]::Matches($Text, $p) | ForEach-Object {
            [void]$keys.Add($_.Groups[1].Value)
        }
    }
    return $keys
}

Write-Host "Scraping codebase translation keys..."
$codeKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$extensions = @('*.ts', '*.html', '*.cs', '*.razor', '*.sql')
$searchRoots = @(
    (Join-Path $repoRoot 'FE/src'),
    (Join-Path $repoRoot 'AmesaBackend'),
    (Join-Path $repoRoot 'AmesaBackend.Admin'),
    (Join-Path $repoRoot 'MetaData/Scripts/Database')
)

$fileCount = 0
foreach ($root in $searchRoots) {
    if (-not (Test-Path $root)) { continue }
    Get-ChildItem -Path $root -Recurse -Include $extensions -File |
        Where-Object { $_.FullName -notmatch '\\node_modules\\|\\dist\\|\\bin\\|\\obj\\' } |
        ForEach-Object {
            $fileCount++
            $content = Get-Content -LiteralPath $_.FullName -Raw -ErrorAction SilentlyContinue
            if ($content) {
                Get-TranslationKeysFromText $content | ForEach-Object { [void]$codeKeys.Add($_) }
            }
        }
}

$lotteryKeysFile = Join-Path $repoRoot 'FE/src/shared/constants/lottery-translation-keys.ts'
if (Test-Path $lotteryKeysFile) {
    $lotteryContent = Get-Content -LiteralPath $lotteryKeysFile -Raw
    [regex]::Matches($lotteryContent, "['\`"]([a-zA-Z][a-zA-Z0-9_.]+)['\`"]") | ForEach-Object {
        $k = $_.Groups[1].Value
        if ($k -match '^[a-z]+\.[a-z]') { [void]$codeKeys.Add($k) }
    }
}

Write-Host "  Files scanned: $fileCount"
Write-Host "  Unique keys in code: $($codeKeys.Count)"

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
$codeKeys | Sort-Object | Set-Content (Join-Path $OutputDir 'codebase-keys.txt') -Encoding utf8

$dbKeysByLang = @{}
$allDbKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$referenceSource = "local audit files / seed SQL"

if ($SqlExportPath -and (Test-Path $SqlExportPath)) {
    Write-Host "Loading keys from SQL export: $SqlExportPath"
    $referenceSource = "SQL export: $SqlExportPath"
    $sql = Get-Content -LiteralPath $SqlExportPath -Raw
    [regex]::Matches($sql, "',\s*'([a-zA-Z][a-zA-Z0-9_.]+)',\s*'") | ForEach-Object {
        [void]$allDbKeys.Add($_.Groups[1].Value)
    }
}
elseif ($UseApi) {
    $referenceSource = "API $ApiBaseUrl"
    foreach ($lang in $Languages) {
        $url = "$($ApiBaseUrl.TrimEnd('/'))/$lang"
        Write-Host "  GET $url"
        $resp = Invoke-RestMethod -Uri $url -Method Get
        $langKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $resp.PSObject.Properties | ForEach-Object {
            [void]$langKeys.Add($_.Name)
            [void]$allDbKeys.Add($_.Name)
        }
        $dbKeysByLang[$lang] = $langKeys
        $langKeys | Sort-Object | Set-Content (Join-Path $OutputDir "api-keys-$lang.txt") -Encoding utf8
        Write-Host "  API keys ($lang): $($langKeys.Count)"
    }
}
elseif ($UseDatabase) {
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        $ConnectionString = $env:AMESA_DB_CONNECTION_STRING
    }
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        throw "UseDatabase requires AMESA_DB_CONNECTION_STRING or -ConnectionString."
    }
    $referenceSource = "database"
    Write-Host "Querying database..."
    $npgsqlPath = Join-Path $repoRoot 'Infrastructure/sql/QueryTool/bin/Debug/net8.0/Npgsql.dll'
    if (-not (Test-Path $npgsqlPath)) {
        throw "Npgsql.dll not found at $npgsqlPath. Build QueryTool or use TranslationAuditTool with --use-db."
    }
    Add-Type -Path $npgsqlPath
    $npgsql = [Npgsql.NpgsqlConnection]::new($ConnectionString)
    $npgsql.Open()
    try {
        foreach ($lang in $Languages) {
            $cmd = $npgsql.CreateCommand()
            $cmd.CommandText = 'SELECT DISTINCT "Key" FROM amesa_content.translations WHERE "LanguageCode" = @lang AND "IsActive" = true ORDER BY "Key"'
            $null = $cmd.Parameters.AddWithValue('lang', $lang)
            $reader = $cmd.ExecuteReader()
            $langKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            while ($reader.Read()) {
                $k = $reader.GetString(0)
                [void]$langKeys.Add($k)
                [void]$allDbKeys.Add($k)
            }
            $reader.Close()
            $dbKeysByLang[$lang] = $langKeys
            $langKeys | Sort-Object | Set-Content (Join-Path $OutputDir "db-keys-$lang.txt") -Encoding utf8
            Write-Host "  DB keys ($lang): $($langKeys.Count)"
        }
    }
    finally {
        $npgsql.Close()
    }
}
else {
    $loadedLocal = $false
    foreach ($lang in $Languages) {
        $path = Join-Path $OutputDir "db-keys-$lang.txt"
        if (-not (Test-Path $path)) { continue }
        $langKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        Get-Content -LiteralPath $path | ForEach-Object {
            $k = $_.Trim()
            if ($k) {
                [void]$langKeys.Add($k)
                [void]$allDbKeys.Add($k)
            }
        }
        $dbKeysByLang[$lang] = $langKeys
        $loadedLocal = $true
        Write-Host "  Local db-keys-$lang.txt: $($langKeys.Count)"
    }

    if (-not $loadedLocal) {
        Write-Host "No local audit files. Parsing seed SQL files as reference..."
        $seedFiles = @(
            'complete-507-translations.sql',
            'seed-payment-sandbox-translation-keys.sql',
            'seed-status-translation-keys.sql',
            'lottery-favorites-translations.sql',
            'add-missing-translation-keys.sql'
        )
        foreach ($sf in $seedFiles) {
            $path = Join-Path $repoRoot "MetaData/Scripts/Database/$sf"
            if (Test-Path $path) {
                $sql = Get-Content -LiteralPath $path -Raw
                [regex]::Matches($sql, "',\s*'([a-zA-Z][a-zA-Z0-9_.]+)',\s*'") | ForEach-Object {
                    [void]$allDbKeys.Add($_.Groups[1].Value)
                }
            }
        }
        Write-Host "  Keys from seed SQL files: $($allDbKeys.Count)"
    }
    else {
        $referenceSource = "translation-audit/db-keys-*.txt"
    }
}

$allDbKeys | Sort-Object | Set-Content (Join-Path $OutputDir 'db-keys-union.txt') -Encoding utf8

$missingInDb = $codeKeys | Where-Object { -not $allDbKeys.Contains($_) } | Sort-Object
$orphanInDb = $allDbKeys | Where-Object { -not $codeKeys.Contains($_) } | Sort-Object

$missingInDb | Set-Content (Join-Path $OutputDir 'missing-in-db.txt') -Encoding utf8
$orphanInDb | Set-Content (Join-Path $OutputDir 'orphan-in-db.txt') -Encoding utf8

$langGaps = @()
if ($dbKeysByLang.Count -gt 0) {
    $enKeys = $dbKeysByLang['en']
    if (-not $enKeys) { $enKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal) }
    foreach ($lang in $Languages) {
        if ($lang -eq 'en') { continue }
        $langSet = $dbKeysByLang[$lang]
        if (-not $langSet) { continue }
        foreach ($key in $enKeys) {
            if (-not $langSet.Contains($key)) {
                $langGaps += [pscustomobject]@{ Language = $lang; Key = $key }
            }
        }
    }
    $langGaps | Export-Csv (Join-Path $OutputDir 'missing-per-language.csv') -NoTypeInformation -Encoding utf8
}

$report = @"
# Translation Key Audit
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Reference: $referenceSource

## Summary
| Metric | Count |
|--------|------:|
| Keys used in codebase | $($codeKeys.Count) |
| Keys in reference | $($allDbKeys.Count) |
| **Missing in reference** (used in code, not in reference) | $($missingInDb.Count) |
| Orphan in reference (not found in code scrape) | $($orphanInDb.Count) |
| Cross-language gaps (en key missing in other lang) | $($langGaps.Count) |

## Missing in reference (first 50)
$(
    if ($missingInDb.Count -eq 0) { '(none)' }
    else { ($missingInDb | Select-Object -First 50 | ForEach-Object { "- $_" }) -join "`n" }
)

## Usage
- Default: local files / seed SQL only (no DB).
- Live API:  -UseApi
- Live DB:   -UseDatabase (set AMESA_DB_CONNECTION_STRING)

## Files
- codebase-keys.txt
- db-keys-union.txt
- missing-in-db.txt
- orphan-in-db.txt
"@

$report | Set-Content (Join-Path $OutputDir 'REPORT.md') -Encoding utf8
Write-Host ""
Write-Host $report
Write-Host ""
Write-Host "Output: $OutputDir"
