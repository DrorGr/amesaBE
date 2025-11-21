import json
import sys

file_path = "AmesaBackend/appsettings.Development.json"

try:
    with open(file_path, 'r', encoding='utf-8') as f:
        content = json.load(f)
    
    # Remove secrets
    if 'Authentication' in content and 'Google' in content['Authentication']:
        content['Authentication']['Google']['ClientId'] = ""
        content['Authentication']['Google']['ClientSecret'] = ""
    
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(content, f, indent=2, ensure_ascii=False)
    
    sys.exit(0)
except Exception as e:
    print(f"Error: {e}", file=sys.stderr)
    sys.exit(1)
