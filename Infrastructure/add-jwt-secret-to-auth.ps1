param(
    [string]$Region = "eu-north-1",
    [string]$Cluster = "Amesa",
    [string]$Service = "amesa-auth-service"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Adding JWT SecretKey to Auth service ==="

$tdArn = (aws ecs describe-services --region $Region --cluster $Cluster --services $Service --query "services[0].taskDefinition" --output text)
if (-not $tdArn -or $tdArn -eq "None") {
    Write-Host "Service not found: $Service"
    exit 1
}

$td = aws ecs describe-task-definition --region $Region --task-definition $tdArn --query "taskDefinition" | ConvertFrom-Json

# Remove read-only fields
$td.PSObject.Properties.Remove('taskDefinitionArn')
$td.PSObject.Properties.Remove('revision')
$td.PSObject.Properties.Remove('status')
$td.PSObject.Properties.Remove('requiresAttributes')
$td.PSObject.Properties.Remove('compatibilities')
$td.PSObject.Properties.Remove('registeredAt')
$td.PSObject.Properties.Remove('registeredBy')
$td.PSObject.Properties.Remove('inferenceAccelerators')

# Ensure secrets array exists
if (-not $td.containerDefinitions[0].secrets) {
    $td.containerDefinitions[0] | Add-Member -Name secrets -MemberType NoteProperty -Value @()
}

# Remove existing JWT SecretKey if present, then add it
$td.containerDefinitions[0].secrets = @($td.containerDefinitions[0].secrets | Where-Object { $_.name -ne "JwtSettings__SecretKey" })
$td.containerDefinitions[0].secrets += @{ name = "JwtSettings__SecretKey"; valueFrom = "/amesa/prod/JwtSettings/SecretKey" }

# Build clean payload
$payload = [ordered]@{
    family                 = $td.family
    taskRoleArn            = $td.taskRoleArn
    executionRoleArn       = $td.executionRoleArn
    networkMode            = $td.networkMode
    containerDefinitions   = $td.containerDefinitions
    volumes                = $td.volumes
    placementConstraints   = $td.placementConstraints
    requiresCompatibilities= $td.requiresCompatibilities
    cpu                    = $td.cpu
    memory                 = $td.memory
}
if ($td.runtimePlatform) { $payload.runtimePlatform = $td.runtimePlatform }

$json = ($payload | ConvertTo-Json -Depth 100 -Compress)
$tmp = [System.IO.Path]::GetTempFileName()
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($tmp, $json, $utf8NoBom)

$newTdArn = (aws ecs register-task-definition --region $Region --cli-input-json file://$tmp --query "taskDefinition.taskDefinitionArn" --output text)
Remove-Item $tmp -Force -ErrorAction SilentlyContinue

Write-Host "Registered new task definition: $newTdArn"
aws ecs update-service --region $Region --cluster $Cluster --service $Service --task-definition $newTdArn --force-new-deployment | Out-Null
Write-Host "Deployment triggered for $Service"

