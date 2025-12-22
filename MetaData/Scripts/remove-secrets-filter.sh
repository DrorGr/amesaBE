#!/bin/bash
# Script to remove OAuth secrets from git history

git filter-branch --force --index-filter '
    if git checkout f9b4263 -- AmesaBackend/appsettings.Development.json 2>/dev/null; then
        if [ -f AmesaBackend/appsettings.Development.json ]; then
            # Use sed to remove the secrets (simpler than JSON parsing in bash)
            sed -i "s/\"ClientId\": \"[^\"]*\"/\"ClientId\": \"\"/g" AmesaBackend/appsettings.Development.json
            sed -i "s/\"ClientSecret\": \"[^\"]*\"/\"ClientSecret\": \"\"/g" AmesaBackend/appsettings.Development.json
            git add AmesaBackend/appsettings.Development.json
        fi
    fi
' --prune-empty --tag-name-filter cat -- f9b4263..HEAD

