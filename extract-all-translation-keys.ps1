# PowerShell script to extract all translation keys from the frontend

$translationKeys = @()

# Get all TypeScript files in the frontend
$tsFiles = Get-ChildItem -Path "FE/src" -Recurse -Filter "*.ts"

foreach ($file in $tsFiles) {
    $content = Get-Content $file.FullName -Raw
    
    # Extract translation keys using regex
    $matches = [regex]::Matches($content, "translate\(['\`"]([^'\`"]+)['\`"]\)")
    
    foreach ($match in $matches) {
        $key = $match.Groups[1].Value
        if ($key -and $key.Trim() -ne "") {
            $translationKeys += $key.Trim()
        }
    }
}

# Remove duplicates and sort
$uniqueKeys = $translationKeys | Sort-Object | Get-Unique

# Output results
Write-Host "Found $($uniqueKeys.Count) unique translation keys:"
Write-Host "================================"

$uniqueKeys | ForEach-Object { Write-Host $_ }

# Save to file
$uniqueKeys | Out-File -FilePath "BE/all-translation-keys.txt" -Encoding UTF8

Write-Host ""
Write-Host "Keys saved to BE/all-translation-keys.txt"


