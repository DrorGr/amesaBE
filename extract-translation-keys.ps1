#!/usr/bin/env pwsh

# Extract all translation keys from the frontend
Write-Host "Extracting translation keys from frontend..." -ForegroundColor Blue

$keys = @()

# Search for translate('key') patterns
$files = Get-ChildItem -Path "FE/src" -Recurse -Include "*.ts", "*.html" -ErrorAction SilentlyContinue

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($content) {
        # Match translate('key') or translate("key") patterns
        $pattern = "translate\([`'`"]([^`'`"]+)[`'`"]\)"
        $matches = [regex]::Matches($content, $pattern)
        foreach ($match in $matches) {
            $key = $match.Groups[1].Value
            if ($key -and $key -notin $keys) {
                $keys += $key
            }
        }
    }
}

# Sort keys
$keys = $keys | Sort-Object

Write-Host "Found $($keys.Count) unique translation keys:" -ForegroundColor Green
$keys | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }

# Save to file
$keys | Out-File -FilePath "BE/translation-keys.txt" -Encoding UTF8
Write-Host "`nKeys saved to BE/translation-keys.txt" -ForegroundColor Green
