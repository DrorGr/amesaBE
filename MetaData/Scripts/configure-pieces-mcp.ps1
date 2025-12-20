# Configure Pieces MCP Server in Cursor Settings
# This script adds Pieces MCP server configuration to Cursor's settings.json

$cursorSettingsPath = Join-Path $env:APPDATA "Cursor\User\settings.json"
$mcpConfig = @{
    mcpServers = @{
        Pieces = @{
            url = "http://localhost:39300/model_context_protocol/2024-11-05/sse"
        }
    }
}

Write-Host "Configuring Pieces MCP Server in Cursor..." -ForegroundColor Cyan
Write-Host "Settings file: $cursorSettingsPath" -ForegroundColor Gray

# Check if settings file exists
if (-not (Test-Path $cursorSettingsPath)) {
    Write-Host "Creating new settings file..." -ForegroundColor Yellow
    $settingsDir = Split-Path $cursorSettingsPath -Parent
    if (-not (Test-Path $settingsDir)) {
        New-Item -ItemType Directory -Path $settingsDir -Force | Out-Null
    }
    $settings = @{}
} else {
    Write-Host "Reading existing settings file..." -ForegroundColor Green
    try {
        $settingsContent = Get-Content $cursorSettingsPath -Raw -ErrorAction Stop
        # Try with -AsHashtable first (PowerShell 6+), fallback to PSCustomObject
        try {
            $settings = $settingsContent | ConvertFrom-Json -AsHashtable -ErrorAction Stop
        } catch {
            # Fallback for older PowerShell versions
            $settingsObj = $settingsContent | ConvertFrom-Json -ErrorAction Stop
            $settings = @{}
            $settingsObj.PSObject.Properties | ForEach-Object {
                $settings[$_.Name] = $_.Value
            }
        }
    } catch {
        Write-Host "Error reading settings file: $_" -ForegroundColor Red
        Write-Host "Creating new settings object..." -ForegroundColor Yellow
        $settings = @{}
    }
}

# Add or update MCP servers configuration
if (-not $settings.ContainsKey("mcpServers")) {
    Write-Host "Adding mcpServers section..." -ForegroundColor Yellow
    $settings["mcpServers"] = @{}
}

# Add or update Pieces server
if ($settings["mcpServers"].ContainsKey("Pieces")) {
    Write-Host "Updating existing Pieces MCP server configuration..." -ForegroundColor Yellow
    $settings["mcpServers"]["Pieces"]["url"] = $mcpConfig.mcpServers.Pieces.url
} else {
    Write-Host "Adding Pieces MCP server configuration..." -ForegroundColor Green
    $settings["mcpServers"]["Pieces"] = $mcpConfig.mcpServers.Pieces
}

# Convert back to JSON with proper formatting
try {
    $jsonContent = $settings | ConvertTo-Json -Depth 10
    # Create backup
    if (Test-Path $cursorSettingsPath) {
        $backupPath = "$cursorSettingsPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        Copy-Item $cursorSettingsPath $backupPath
        Write-Host "Backup created: $backupPath" -ForegroundColor Gray
    }
    
    # Write updated settings
    $jsonContent | Set-Content $cursorSettingsPath -Encoding UTF8
    Write-Host "`n✅ Successfully configured Pieces MCP Server!" -ForegroundColor Green
    Write-Host "`nConfiguration added:" -ForegroundColor Cyan
    Write-Host ($mcpConfig | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "1. Restart Cursor IDE" -ForegroundColor White
    Write-Host "2. Open Cursor Settings → MCP section" -ForegroundColor White
    Write-Host "3. Verify green dot indicates server is running" -ForegroundColor White
    Write-Host "4. Switch to Agent Mode in chat" -ForegroundColor White
    Write-Host "5. Test with query: 'What was I working on yesterday?'" -ForegroundColor White
} catch {
    Write-Host "`n❌ Error writing settings file: $_" -ForegroundColor Red
    Write-Host "Please manually add the following to your Cursor settings.json:" -ForegroundColor Yellow
    Write-Host ($mcpConfig | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    exit 1
}
