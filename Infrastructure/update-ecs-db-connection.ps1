param(
    [string]$Region = "eu-north-1",
    [string]$Cluster = "Amesa",
    [string]$WriterEndpoint = "amesa-prod-instance-1.cruuae28ob7m.eu-north-1.rds.amazonaws.com",
    [string]$Database = "amesa_prod",
    [string]$DbUser = "amesa_admin",
    [string]$DbPassword = "u1fwn3s9"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Updating ECS services with Aurora connection strings ==="
Write-Host "Region: $Region | Cluster: $Cluster | Writer: $WriterEndpoint"

$services = @(
    @{ Service="amesa-auth-service";            Schema="amesa_auth";             Param="/amesa/prod/ConnectionStrings/Auth" },
    @{ Service="amesa-content-service";         Schema="amesa_content";          Param="/amesa/prod/ConnectionStrings/Content" },
    @{ Service="amesa-notification-service";    Schema="amesa_notification";     Param="/amesa/prod/ConnectionStrings/Notification" },
    @{ Service="amesa-payment-service";         Schema="amesa_payment";          Param="/amesa/prod/ConnectionStrings/Payment" },
    @{ Service="amesa-lottery-service";         Schema="amesa_lottery";          Param="/amesa/prod/ConnectionStrings/Lottery" },
    @{ Service="amesa-lottery-results-service"; Schema="amesa_lottery_results";  Param="/amesa/prod/ConnectionStrings/LotteryResults" },
    @{ Service="amesa-analytics-service";       Schema="amesa_analytics";        Param="/amesa/prod/ConnectionStrings/Analytics" }
)

function Ensure-Parameter([string]$name, [string]$schema) {
    $value = "Host=$WriterEndpoint;Port=5432;Database=$Database;Username=$DbUser;Password=$DbPassword;SearchPath=$schema;"
    Write-Host "Putting SSM parameter $name"
    aws ssm put-parameter --region $Region --name $name --type SecureString --overwrite --value $value | Out-Null
}

function Update-Service([string]$service, [string]$param) {
    Write-Host "`n-- Service: $service --"
    $tdArn = (aws ecs describe-services --region $Region --cluster $Cluster --services $service --query "services[0].taskDefinition" --output text)
    if (-not $tdArn -or $tdArn -eq "None") {
        Write-Host "Service not found or no task definition: $service"
        return
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

    # Ensure secrets on the first container
    if (-not $td.containerDefinitions[0].secrets) {
        $td.containerDefinitions[0] | Add-Member -Name secrets -MemberType NoteProperty -Value @()
    }
    # Remove any existing with same name
    $td.containerDefinitions[0].secrets = @($td.containerDefinitions[0].secrets | Where-Object { $_.name -ne "ConnectionStrings__DefaultConnection" })
    $td.containerDefinitions[0].secrets += @{ name = "ConnectionStrings__DefaultConnection"; valueFrom = $param }

    # Build a clean registration payload with only allowed fields
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
    $sw = New-Object System.IO.StreamWriter($tmp, $false, $utf8NoBom)
    $sw.Write($json)
    $sw.Close()

    $newTdArn = (aws ecs register-task-definition --region $Region --cli-input-json file://$tmp --query "taskDefinition.taskDefinitionArn" --output text 2>$null)
    Remove-Item $tmp -Force -ErrorAction SilentlyContinue

    Write-Host "Registered new task definition: $newTdArn"
    aws ecs update-service --region $Region --cluster $Cluster --service $service --task-definition $newTdArn --force-new-deployment | Out-Null
    Write-Host "Deployment triggered for $service"
}

# 1) Parameters
foreach ($s in $services) {
    Ensure-Parameter -name $s.Param -schema $s.Schema
}

# 2) Services
foreach ($s in $services) {
    Update-Service -service $s.Service -param $s.Param
}

Write-Host "`nAll done. Monitor deployments in ECS console."


