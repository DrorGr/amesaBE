# PowerShell script to open HTML files for PDF conversion
# Usage: .\convert-to-pdf.ps1

Write-Host "Opening investor pitch HTML files for PDF conversion..." -ForegroundColor Green
Write-Host ""
Write-Host "Instructions:" -ForegroundColor Yellow
Write-Host "1. Each file will open in your default browser" -ForegroundColor White
Write-Host "2. Press Ctrl+P to open Print dialog" -ForegroundColor White
Write-Host "3. Select 'Save as PDF' as destination" -ForegroundColor White
Write-Host "4. Ensure A4 paper size and background graphics enabled" -ForegroundColor White
Write-Host "5. Save each PDF" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to open the files..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# Get the current directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path

# Open each HTML file
$files = @(
    "investor-pitch-pdf1-user-flows.html",
    "investor-pitch-pdf2-architecture.html",
    "investor-pitch-pdf3-solutions-tools.html"
)

foreach ($file in $files) {
    $fullPath = Join-Path $scriptPath $file
    if (Test-Path $fullPath) {
        Write-Host "Opening: $file" -ForegroundColor Green
        Start-Process $fullPath
        Start-Sleep -Seconds 2
    } else {
        Write-Host "File not found: $file" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "All files opened! Follow the instructions above to convert to PDF." -ForegroundColor Green







