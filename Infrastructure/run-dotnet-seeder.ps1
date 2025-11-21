# Run .NET Database Seeder for Each Schema
# Uses the existing DatabaseSeeder.cs to populate data

param(
    [string]$Cluster = "Amesa",
    [string]$Region = "eu-north-1",
    [string]$ServiceName = "amesa-lottery-service",
    [string]$DbEndpoint = "amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com",
    [string]$DbName = "amesa_prod",
    [string]$DbUser = "amesa_admin",
    [string]$DbPassword = "u1fwn3s9",
    [int]$DbPort = 5432
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host ".NET Database Seeder via ECS Exec" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get running task
Write-Host "Finding running task..." -ForegroundColor Cyan
$taskArn = aws ecs list-tasks --cluster $Cluster --service-name $ServiceName --region $Region --query "taskArns[0]" --output text
$taskArn = $taskArn.Trim()

if ([string]::IsNullOrEmpty($taskArn) -or $taskArn -eq "None") {
    Write-Host "❌ No running tasks found" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Found task: $($taskArn.Split('/')[-1])" -ForegroundColor Green
Write-Host ""

# Get container name
$taskDetails = aws ecs describe-tasks --cluster $Cluster --tasks $taskArn --region $Region --query "tasks[0].containers[0].name" --output text 2>&1
$containerName = $taskDetails.Trim()

if ([string]::IsNullOrEmpty($containerName)) {
    $containerName = $ServiceName
}

Write-Host "Container: $containerName" -ForegroundColor White
Write-Host ""

# Create command to run .NET seeder
$seederCommand = @"
export DB_CONNECTION_STRING='Host=$DbEndpoint;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword;SSL Mode=Require;'
echo "Running .NET Database Seeder..."
echo "Connection: $DbName @ $DbEndpoint"
echo ""
if command -v dotnet &> /dev/null; then
    echo "✅ .NET found: `$(dotnet --version)"
    echo ""
    echo "Checking for seeder..."
    if [ -f "/app/AmesaBackend.dll" ]; then
        echo "Running seeder..."
        cd /app
        dotnet AmesaBackend.dll --seeder
    elif [ -f "/app/AmesaBackend" ]; then
        cd /app
        ./AmesaBackend --seeder
    else
        echo "⚠️  Seeder executable not found in /app"
        echo "Looking for it..."
        find / -name "AmesaBackend.dll" -o -name "AmesaBackend" 2>/dev/null | head -5
        echo ""
        echo "💡 You may need to:"
        echo "   1. Ensure the seeder code is in the container"
        echo "   2. Or use SQL scripts to seed data directly"
    fi
else
    echo "❌ .NET not found in container"
    echo "Installing .NET 8.0 SDK..."
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0
    export PATH="`$HOME/.dotnet:`$PATH"
    dotnet --version
fi
"@

Write-Host "Executing .NET seeder..." -ForegroundColor Cyan
Write-Host ""

$env:Path += ";C:\Program Files\Amazon\SessionManagerPlugin\bin"

# Base64 encode the command
$scriptBytes = [System.Text.Encoding]::UTF8.GetBytes($seederCommand)
$scriptBase64 = [Convert]::ToBase64String($scriptBytes)
$command = "bash -c `"echo '$scriptBase64' | base64 -d | bash`""

aws ecs execute-command `
    --cluster $Cluster `
    --task $taskArn `
    --container $containerName `
    --interactive `
    --command $command `
    --region $Region

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Seeder execution completed!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Check output above for details" -ForegroundColor Yellow
}

