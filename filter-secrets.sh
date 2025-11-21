#!/bin/sh
# Script to remove secrets from git history
git checkout f9b4263 -- AmesaBackend/appsettings.Development.json 2>/dev/null
if [ -f AmesaBackend/appsettings.Development.json ]; then
    # Use PowerShell to modify JSON (since we're on Windows)
    powershell -Command "$c=Get-Content 'AmesaBackend/appsettings.Development.json' -Raw|ConvertFrom-Json; $c.Authentication.Google.ClientId=''; $c.Authentication.Google.ClientSecret=''; $c|ConvertTo-Json -Depth 10|Set-Content 'AmesaBackend/appsettings.Development.json' -NoNewline"
    git add AmesaBackend/appsettings.Development.json
fi

