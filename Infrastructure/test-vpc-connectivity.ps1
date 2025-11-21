# Test VPC Connectivity
# Task 4: Test connectivity from within VPC (requires bastion host or EC2 instance)

param(
    [string]$Region = "eu-north-1",
    [string]$BastionInstanceId = ""
)

$ErrorActionPreference = "Stop"

Write-Host "=== Task 4: Test VPC Connectivity ===" -ForegroundColor Cyan
Write-Host ""

if ([string]::IsNullOrEmpty($BastionInstanceId)) {
    Write-Host "No bastion instance provided. Checking for existing EC2 instances..." -ForegroundColor Yellow
    
    $instances = aws ec2 describe-instances `
        --region $Region `
        --filters "Name=instance-state-name,Values=running" `
        --query "Reservations[*].Instances[*].{InstanceId:InstanceId,Name:Tags[?Key=='Name'].Value|[0],PrivateIp:PrivateIpAddress,SubnetId:SubnetId}" `
        --output json | ConvertFrom-Json
    
    if ($instances.Count -gt 0) {
        Write-Host "`nFound EC2 instances:" -ForegroundColor Green
        $instances | ForEach-Object {
            Write-Host "  Instance ID: $($_.InstanceId)" -ForegroundColor Gray
            Write-Host "    Name: $($_.Name)" -ForegroundColor Gray
            Write-Host "    Private IP: $($_.PrivateIp)" -ForegroundColor Gray
            Write-Host "    Subnet: $($_.SubnetId)" -ForegroundColor Gray
            Write-Host ""
        }
        
        $BastionInstanceId = Read-Host "Enter instance ID to use for testing (or press Enter to skip)"
    } else {
        Write-Host "No running EC2 instances found." -ForegroundColor Yellow
        Write-Host ""
        Write-Host "To test connectivity, you need:" -ForegroundColor Cyan
        Write-Host "1. An EC2 instance in the same VPC" -ForegroundColor Gray
        Write-Host "2. Or use AWS Systems Manager Session Manager" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Alternative: Use AWS Systems Manager to run commands on ECS tasks" -ForegroundColor Yellow
        return
    }
}

if ([string]::IsNullOrEmpty($BastionInstanceId)) {
    Write-Host "Skipping connectivity test." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Manual test commands (run from EC2 in same VPC):" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "# Get ECS task IP" -ForegroundColor Gray
    Write-Host 'TASK_IP=$(aws ecs list-tasks --cluster Amesa --service-name amesa-auth-service --query "taskArns[0]" --output text | xargs -I {} aws ecs describe-tasks --cluster Amesa --tasks {} --query "tasks[0].attachments[0].details[?name==`"privateIPv4Address`"].value" --output text)' -ForegroundColor Gray
    Write-Host ""
    Write-Host "# Test health endpoint" -ForegroundColor Gray
    Write-Host "curl -v http://`$TASK_IP:8080/health" -ForegroundColor Gray
    Write-Host ""
    Write-Host "# Test from ALB subnet to ECS subnet" -ForegroundColor Gray
    Write-Host "telnet `$TASK_IP 8080" -ForegroundColor Gray
    return
}

Write-Host "Using bastion instance: $BastionInstanceId" -ForegroundColor Green

# Get ECS task IP
Write-Host "`nGetting ECS task IP..." -ForegroundColor Cyan
$taskArn = aws ecs list-tasks --region $Region --cluster Amesa --service-name amesa-auth-service --query "taskArns[0]" --output text
$taskIp = aws ecs describe-tasks --region $Region --cluster Amesa --tasks $taskArn --query "tasks[0].attachments[0].details[?name=='privateIPv4Address'].value" --output text

Write-Host "ECS Task IP: $taskIp" -ForegroundColor Yellow

# Test connectivity via SSM Session Manager
Write-Host "`nTesting connectivity via SSM Session Manager..." -ForegroundColor Cyan
Write-Host "This requires SSM agent on the instance and proper IAM roles." -ForegroundColor Yellow

$testCommand = @"
# Test connectivity to ECS task
echo "Testing connection to ECS task at $taskIp:8080"
curl -v --connect-timeout 5 http://$taskIp:8080/health
echo ""
echo "Testing TCP connection"
timeout 5 bash -c "</dev/tcp/$taskIp/8080" && echo "Port 8080 is open" || echo "Port 8080 is closed/unreachable"
"@

$testCommand | Out-File -FilePath "test_connectivity.sh" -Encoding ASCII

Write-Host "`nRun this command on the bastion instance:" -ForegroundColor Cyan
Write-Host "aws ssm send-command --instance-ids $BastionInstanceId --document-name AWS-RunShellScript --parameters 'commands=[`"bash test_connectivity.sh`"]'" -ForegroundColor Gray

Write-Host "`nOr connect via Session Manager and run:" -ForegroundColor Cyan
Write-Host "aws ssm start-session --target $BastionInstanceId" -ForegroundColor Gray
Write-Host "Then run: curl -v http://$taskIp:8080/health" -ForegroundColor Gray

Write-Host "`n=== Expected Results ===" -ForegroundColor Cyan
Write-Host "If connectivity works:" -ForegroundColor Green
Write-Host "  - curl should return HTTP 200 with 'Healthy' response" -ForegroundColor Gray
Write-Host "  - TCP connection should succeed" -ForegroundColor Gray
Write-Host ""
Write-Host "If connectivity fails:" -ForegroundColor Red
Write-Host "  - Check security group rules" -ForegroundColor Gray
Write-Host "  - Check route tables" -ForegroundColor Gray
Write-Host "  - Check network ACLs" -ForegroundColor Gray
Write-Host "  - Verify ECS task is listening on 0.0.0.0:8080" -ForegroundColor Gray

