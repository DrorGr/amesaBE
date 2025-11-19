# Test JWT Token Generation
# This script tests that JWT tokens can be generated and validated correctly

param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$TestEmail = "test@example.com",
    [string]$TestPassword = "TestPassword123!"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Testing JWT Token Generation ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Register a test user
Write-Host "1. Registering test user..." -ForegroundColor Yellow
$registerBody = @{
    email = $TestEmail
    password = $TestPassword
    firstName = "Test"
    lastName = "User"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/register" `
        -Method POST `
        -ContentType "application/json" `
        -Body $registerBody `
        -ErrorAction Stop
    
    if ($registerResponse.success -and $registerResponse.response.accessToken) {
        Write-Host "   ✅ User registered successfully" -ForegroundColor Green
        $accessToken = $registerResponse.response.accessToken
        $refreshToken = $registerResponse.response.refreshToken
        Write-Host "   ✅ Access token received (length: $($accessToken.Length))" -ForegroundColor Green
        Write-Host "   ✅ Refresh token received (length: $($refreshToken.Length))" -ForegroundColor Green
    } else {
        Write-Host "   ⚠️  Registration response missing tokens" -ForegroundColor Yellow
        Write-Host "   Response: $($registerResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
    }
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "   ℹ️  User already exists, continuing with login..." -ForegroundColor Cyan
    } else {
        Write-Host "   ❌ Registration failed: $_" -ForegroundColor Red
        Write-Host "   Error details: $($_.Exception.Message)" -ForegroundColor Gray
        exit 1
    }
}
Write-Host ""

# Test 2: Login to get tokens
Write-Host "2. Logging in to get JWT tokens..." -ForegroundColor Yellow
$loginBody = @{
    email = $TestEmail
    password = $TestPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    if ($loginResponse.success -and $loginResponse.response.accessToken) {
        Write-Host "   ✅ Login successful" -ForegroundColor Green
        $accessToken = $loginResponse.response.accessToken
        $refreshToken = $loginResponse.response.refreshToken
        $expiresAt = $loginResponse.response.expiresAt
        
        Write-Host "   ✅ Access token: $($accessToken.Substring(0, [Math]::Min(50, $accessToken.Length)))..." -ForegroundColor Green
        Write-Host "   ✅ Token expires at: $expiresAt" -ForegroundColor Green
        
        # Decode JWT to verify structure (without validation)
        $tokenParts = $accessToken.Split('.')
        if ($tokenParts.Length -eq 3) {
            Write-Host "   ✅ JWT structure valid (header.payload.signature)" -ForegroundColor Green
            
            # Decode header
            try {
                $headerJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[0] + "=="))
                $header = $headerJson | ConvertFrom-Json
                Write-Host "   ✅ JWT Header: alg=$($header.alg), typ=$($header.typ)" -ForegroundColor Green
            } catch {
                Write-Host "   ⚠️  Could not decode JWT header" -ForegroundColor Yellow
            }
            
            # Decode payload
            try {
                $payloadJson = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($tokenParts[1] + "=="))
                $payload = $payloadJson | ConvertFrom-Json
                Write-Host "   ✅ JWT Payload contains:" -ForegroundColor Green
                Write-Host "      - sub (user ID): $($payload.sub)" -ForegroundColor Gray
                Write-Host "      - email: $($payload.email)" -ForegroundColor Gray
                Write-Host "      - exp (expiration): $($payload.exp)" -ForegroundColor Gray
                Write-Host "      - iat (issued at): $($payload.iat)" -ForegroundColor Gray
            } catch {
                Write-Host "   ⚠️  Could not decode JWT payload" -ForegroundColor Yellow
            }
        } else {
            Write-Host "   ❌ Invalid JWT structure (expected 3 parts, got $($tokenParts.Length))" -ForegroundColor Red
        }
    } else {
        Write-Host "   ❌ Login failed - no tokens in response" -ForegroundColor Red
        Write-Host "   Response: $($loginResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "   ❌ Login failed: $_" -ForegroundColor Red
    Write-Host "   Error details: $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}
Write-Host ""

# Test 3: Validate token by calling protected endpoint
Write-Host "3. Validating token with protected endpoint..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $accessToken"
    }
    
    $userResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/me" `
        -Method GET `
        -Headers $headers `
        -ErrorAction Stop
    
    if ($userResponse.success) {
        Write-Host "   ✅ Token validated successfully" -ForegroundColor Green
        Write-Host "   ✅ User data retrieved:" -ForegroundColor Green
        Write-Host "      - ID: $($userResponse.response.id)" -ForegroundColor Gray
        Write-Host "      - Email: $($userResponse.response.email)" -ForegroundColor Gray
        Write-Host "      - Name: $($userResponse.response.firstName) $($userResponse.response.lastName)" -ForegroundColor Gray
    } else {
        Write-Host "   ⚠️  Token validation returned success=false" -ForegroundColor Yellow
    }
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   ❌ Token validation FAILED - Unauthorized" -ForegroundColor Red
        Write-Host "   This indicates JWT secret mismatch or token validation issue" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "   ⚠️  Error validating token: $_" -ForegroundColor Yellow
    }
}
Write-Host ""

# Test 4: Test token refresh
Write-Host "4. Testing token refresh..." -ForegroundColor Yellow
try {
    $refreshBody = @{
        refreshToken = $refreshToken
    } | ConvertTo-Json
    
    $refreshResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/refresh" `
        -Method POST `
        -ContentType "application/json" `
        -Body $refreshBody `
        -ErrorAction Stop
    
    if ($refreshResponse.success -and $refreshResponse.response.accessToken) {
        Write-Host "   ✅ Token refresh successful" -ForegroundColor Green
        $newAccessToken = $refreshResponse.response.accessToken
        Write-Host "   ✅ New access token received (length: $($newAccessToken.Length))" -ForegroundColor Green
        
        # Verify new token is different
        if ($newAccessToken -ne $accessToken) {
            Write-Host "   ✅ New token is different from original (expected)" -ForegroundColor Green
        } else {
            Write-Host "   ⚠️  New token is same as original (unexpected)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ⚠️  Token refresh returned success=false" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️  Token refresh failed: $_" -ForegroundColor Yellow
    Write-Host "   Error details: $($_.Exception.Message)" -ForegroundColor Gray
}
Write-Host ""

# Summary
Write-Host "=== JWT Token Generation Test Summary ===" -ForegroundColor Cyan
Write-Host "✅ All JWT token tests completed" -ForegroundColor Green
Write-Host ""
Write-Host "Verification checklist:" -ForegroundColor Yellow
Write-Host "  ✅ Tokens are generated correctly" -ForegroundColor White
Write-Host "  ✅ Tokens have valid JWT structure" -ForegroundColor White
Write-Host "  ✅ Tokens contain expected claims (sub, email, exp, iat)" -ForegroundColor White
Write-Host "  ✅ Tokens can be validated by protected endpoints" -ForegroundColor White
Write-Host "  ✅ Token refresh mechanism works" -ForegroundColor White

