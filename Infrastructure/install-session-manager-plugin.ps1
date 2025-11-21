# Install AWS Session Manager Plugin for ECS Exec

Write-Host "Checking for AWS Session Manager Plugin..." -ForegroundColor Cyan

$pluginPath = "$env:ProgramFiles\Amazon\SessionManagerPlugin\bin\session-manager-plugin.exe"
if (Test-Path $pluginPath) {
    Write-Host "✅ Session Manager Plugin is already installed" -ForegroundColor Green
    exit 0
}

Write-Host "❌ Session Manager Plugin not found" -ForegroundColor Red
Write-Host ""
Write-Host "Installing AWS Session Manager Plugin..." -ForegroundColor Cyan
Write-Host ""

# Download and install
$downloadUrl = "https://s3.amazonaws.com/session-manager-downloads/plugin/latest/windows/SessionManagerPluginSetup.exe"
$installerPath = "$env:TEMP\SessionManagerPluginSetup.exe"

Write-Host "Downloading installer..." -ForegroundColor Yellow
try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
    Write-Host "✅ Download complete" -ForegroundColor Green
    
    Write-Host "Installing..." -ForegroundColor Yellow
    Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait -NoNewWindow
    
    Write-Host "✅ Installation complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Please restart your terminal/PowerShell session for changes to take effect." -ForegroundColor Yellow
} catch {
    Write-Host "❌ Installation failed: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Manual installation:" -ForegroundColor Cyan
    Write-Host "1. Download from: $downloadUrl" -ForegroundColor Gray
    Write-Host "2. Run the installer" -ForegroundColor Gray
    exit 1
}

