$password = 'u1fwn3s9'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$beDir = Split-Path -Parent $scriptDir

$services = @(
    'AmesaBackend.Auth',
    'AmesaBackend.Payment',
    'AmesaBackend.Lottery',
    'AmesaBackend.Content',
    'AmesaBackend.Notification',
    'AmesaBackend.LotteryResults',
    'AmesaBackend.Analytics',
    'AmesaBackend.Admin'
)

$updatedCount = 0

foreach ($service in $services) {
    $appsettingsPath = Join-Path $beDir "$service\appsettings.json"
    if (Test-Path $appsettingsPath) {
        $content = Get-Content $appsettingsPath -Raw -Encoding UTF8
        if ($content -match 'Password=CHANGE_ME') {
            $content = $content -replace [regex]::Escape('Password=CHANGE_ME'), "Password=$password"
            [System.IO.File]::WriteAllText($appsettingsPath, $content, [System.Text.Encoding]::UTF8)
            Write-Output "[OK] Updated password in $service"
            $updatedCount++
        }
    }
}

Write-Output ""
Write-Output "Updated $updatedCount service(s)"

