# Verify AWS Secrets Configuration for AmesaBackend
# This script checks that all required secrets are configured in AWS

param(
    [string]$Region = "eu-north-1",
    [string]$Cluster = "Amesa",
    [string]$Service = "amesa-auth-service"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Verifying AWS Secrets Configuration ===" -ForegroundColor Cyan
Write-Host ""

$allChecksPassed = $true

# 1. Check SSM Parameter for JWT SecretKey
Write-Host "1. Checking SSM Parameter Store for JWT SecretKey..." -ForegroundColor Yellow
$ssmParamPath = "/amesa/prod/JwtSettings/SecretKey"
try {
    $ssmParam = aws ssm get-parameter --region $Region --name $ssmParamPath --with-decryption --query "Parameter.Value" --output text 2>&1
    if ($LASTEXITCODE -eq 0 -and $ssmParam -and $ssmParam -ne "None") {
        $paramLength = $ssmParam.Length
        Write-Host "   ✅ SSM Parameter exists: $ssmParamPath" -ForegroundColor Green
        Write-Host "   ✅ Secret length: $paramLength characters" -ForegroundColor Green
        if ($paramLength -lt 32) {
            Write-Host "   ⚠️  WARNING: Secret is less than 32 characters (minimum recommended)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ❌ SSM Parameter NOT FOUND: $ssmParamPath" -ForegroundColor Red
        Write-Host "   Action: Create parameter using: aws ssm put-parameter --name $ssmParamPath --value 'YOUR_SECRET' --type SecureString" -ForegroundColor Yellow
        $allChecksPassed = $false
    }
} catch {
    Write-Host "   ❌ Error checking SSM Parameter: $_" -ForegroundColor Red
    $allChecksPassed = $false
}
Write-Host ""

# 2. Check Secrets Manager for Google OAuth
Write-Host "2. Checking AWS Secrets Manager for Google OAuth credentials..." -ForegroundColor Yellow
$googleSecretId = "amesa-google_people_API"
try {
    $googleSecret = aws secretsmanager get-secret-value --region $Region --secret-id $googleSecretId --query "SecretString" --output text 2>&1
    if ($LASTEXITCODE -eq 0 -and $googleSecret -and $googleSecret -ne "None") {
        try {
            $googleJson = $googleSecret | ConvertFrom-Json
            $hasClientId = $null -ne $googleJson.ClientId -and $googleJson.ClientId -ne ""
            $hasClientSecret = $null -ne $googleJson.ClientSecret -and $googleJson.ClientSecret -ne ""
            
            if ($hasClientId -and $hasClientSecret) {
                Write-Host "   ✅ Google OAuth secret exists: $googleSecretId" -ForegroundColor Green
                Write-Host "   ✅ Contains ClientId: Yes" -ForegroundColor Green
                Write-Host "   ✅ Contains ClientSecret: Yes" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  Google OAuth secret exists but missing required keys:" -ForegroundColor Yellow
                if (-not $hasClientId) { Write-Host "      ❌ Missing: ClientId" -ForegroundColor Red }
                if (-not $hasClientSecret) { Write-Host "      ❌ Missing: ClientSecret" -ForegroundColor Red }
                $allChecksPassed = $false
            }
        } catch {
            Write-Host "   ⚠️  Google OAuth secret exists but is not valid JSON" -ForegroundColor Yellow
            $allChecksPassed = $false
        }
    } else {
        Write-Host "   ❌ Google OAuth secret NOT FOUND: $googleSecretId" -ForegroundColor Red
        Write-Host "   Action: Create secret in AWS Secrets Manager with ClientId and ClientSecret keys" -ForegroundColor Yellow
        $allChecksPassed = $false
    }
} catch {
    Write-Host "   ❌ Error checking Google OAuth secret: $_" -ForegroundColor Red
    $allChecksPassed = $false
}
Write-Host ""

# 3. Check Secrets Manager for Meta OAuth
Write-Host "3. Checking AWS Secrets Manager for Meta OAuth credentials..." -ForegroundColor Yellow
$metaSecretId = "amesa-meta-facebook-api"
try {
    $metaSecret = aws secretsmanager get-secret-value --region $Region --secret-id $metaSecretId --query "SecretString" --output text 2>&1
    if ($LASTEXITCODE -eq 0 -and $metaSecret -and $metaSecret -ne "None") {
        try {
            $metaJson = $metaSecret | ConvertFrom-Json
            $hasAppId = $null -ne $metaJson.AppId -and $metaJson.AppId -ne ""
            $hasAppSecret = $null -ne $metaJson.AppSecret -and $metaJson.AppSecret -ne ""
            
            if ($hasAppId -and $hasAppSecret) {
                Write-Host "   ✅ Meta OAuth secret exists: $metaSecretId" -ForegroundColor Green
                Write-Host "   ✅ Contains AppId: Yes" -ForegroundColor Green
                Write-Host "   ✅ Contains AppSecret: Yes" -ForegroundColor Green
            } else {
                Write-Host "   ⚠️  Meta OAuth secret exists but missing required keys:" -ForegroundColor Yellow
                if (-not $hasAppId) { Write-Host "      ❌ Missing: AppId" -ForegroundColor Red }
                if (-not $hasAppSecret) { Write-Host "      ❌ Missing: AppSecret" -ForegroundColor Red }
                $allChecksPassed = $false
            }
        } catch {
            Write-Host "   ⚠️  Meta OAuth secret exists but is not valid JSON" -ForegroundColor Yellow
            $allChecksPassed = $false
        }
    } else {
        Write-Host "   ❌ Meta OAuth secret NOT FOUND: $metaSecretId" -ForegroundColor Red
        Write-Host "   Action: Create secret in AWS Secrets Manager with AppId and AppSecret keys" -ForegroundColor Yellow
        $allChecksPassed = $false
    }
} catch {
    Write-Host "   ❌ Error checking Meta OAuth secret: $_" -ForegroundColor Red
    $allChecksPassed = $false
}
Write-Host ""

# 4. Check ECS Task Definition for secrets configuration
Write-Host "4. Checking ECS Task Definition for secrets configuration..." -ForegroundColor Yellow
try {
    $tdArn = (aws ecs describe-services --region $Region --cluster $Cluster --services $Service --query "services[0].taskDefinition" --output text 2>&1)
    if ($LASTEXITCODE -eq 0 -and $tdArn -and $tdArn -ne "None") {
        $td = aws ecs describe-task-definition --region $Region --task-definition $tdArn --query "taskDefinition.containerDefinitions[0].secrets" | ConvertFrom-Json
        
        if ($td -and $td.Count -gt 0) {
            Write-Host "   ✅ Task Definition has secrets configured" -ForegroundColor Green
            
            $hasJwtSecret = $false
            $hasDbConnection = $false
            
            foreach ($secret in $td) {
                Write-Host "      - $($secret.name) -> $($secret.valueFrom)" -ForegroundColor Gray
                if ($secret.name -eq "JwtSettings__SecretKey") {
                    $hasJwtSecret = $true
                    if ($secret.valueFrom -eq $ssmParamPath) {
                        Write-Host "   ✅ JWT SecretKey correctly configured" -ForegroundColor Green
                    } else {
                        Write-Host "   ⚠️  JWT SecretKey path mismatch (expected: $ssmParamPath)" -ForegroundColor Yellow
                    }
                }
                if ($secret.name -eq "DB_CONNECTION_STRING") {
                    $hasDbConnection = $true
                    Write-Host "   ✅ Database connection string configured" -ForegroundColor Green
                }
            }
            
            if (-not $hasJwtSecret) {
                Write-Host "   ❌ JWT SecretKey NOT configured in task definition" -ForegroundColor Red
                Write-Host "   Action: Run BE/Infrastructure/add-jwt-secret-to-auth.ps1" -ForegroundColor Yellow
                $allChecksPassed = $false
            }
        } else {
            Write-Host "   ❌ Task Definition has NO secrets configured" -ForegroundColor Red
            Write-Host "   Action: Run BE/Infrastructure/add-jwt-secret-to-auth.ps1" -ForegroundColor Yellow
            $allChecksPassed = $false
        }
    } else {
        Write-Host "   ⚠️  Could not retrieve task definition (service may not exist yet)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Error checking task definition: $_" -ForegroundColor Yellow
}
Write-Host ""

# 5. Check ECS Task Execution Role permissions
Write-Host "5. Checking ECS Task Execution Role permissions..." -ForegroundColor Yellow
$executionRoleArn = "arn:aws:iam::129394705401:role/ecsTaskExecutionRole"
try {
    $rolePolicies = aws iam list-attached-role-policies --role-name ecsTaskExecutionRole --query "AttachedPolicies[].PolicyArn" --output text 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✅ Task Execution Role exists: $executionRoleArn" -ForegroundColor Green
        Write-Host "   Note: Verify role has these permissions:" -ForegroundColor Gray
        Write-Host "      - ssm:GetParameters (for SSM Parameter Store)" -ForegroundColor Gray
        Write-Host "      - secretsmanager:GetSecretValue (for Secrets Manager)" -ForegroundColor Gray
    } else {
        Write-Host "   ⚠️  Could not verify role permissions" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Error checking role: $_" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "=== Verification Summary ===" -ForegroundColor Cyan
if ($allChecksPassed) {
    Write-Host "✅ All critical checks PASSED" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Test OAuth flow in production environment" -ForegroundColor White
    Write-Host "2. Verify JWT tokens are generated correctly" -ForegroundColor White
    Write-Host "3. Monitor application logs for secret loading messages" -ForegroundColor White
} else {
    Write-Host "❌ Some checks FAILED - please fix issues above before deployment" -ForegroundColor Red
    Write-Host ""
    Write-Host "Required actions:" -ForegroundColor Yellow
    Write-Host "1. Create missing AWS secrets/parameters" -ForegroundColor White
    Write-Host "2. Update ECS Task Definition with secrets configuration" -ForegroundColor White
    Write-Host "3. Verify IAM role permissions" -ForegroundColor White
    exit 1
}

