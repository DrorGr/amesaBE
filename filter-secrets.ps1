$file = "AmesaBackend/appsettings.Development.json"
if (Test-Path $file) {
    $content = Get-Content $file -Raw | ConvertFrom-Json
    $content.Authentication.Google.ClientId = ""
    $content.Authentication.Google.ClientSecret = ""
    $content | ConvertTo-Json -Depth 10 | Set-Content $file -NoNewline
    git add $file
}

