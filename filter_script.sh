#!/bin/sh
git checkout-index --force --all
if [ -f "AmesaBackend/appsettings.Development.json" ]; then
    # Use PowerShell to modify JSON (we'll handle this differently)
    echo "File exists, will be modified"
fi
