# Lottery Favorites & Entry Management - Integration Test Script
# Tests all Phase 1 endpoints after migration

$baseUrl = "http://localhost:5000/api/v1"
$token = "" # Will be set after login

Write-Host "🧪 Lottery Favorites Integration Tests" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Login to get JWT token
Write-Host "1️⃣ Testing Authentication..." -ForegroundColor Yellow
$loginBody = @{
    email = "test@example.com"  # Replace with actual test user
    password = "TestPassword123!"  # Replace with actual password
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "✅ Login successful" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Please ensure backend is running and test user exists" -ForegroundColor Yellow
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""
Write-Host "2️⃣ Testing Favorites Endpoints..." -ForegroundColor Yellow

# Test GET /api/v1/houses/favorites
Write-Host "   Testing GET /api/v1/houses/favorites..." -ForegroundColor Gray
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/houses/favorites" -Method Get -Headers $headers
    Write-Host "   ✅ GET /houses/favorites - Success" -ForegroundColor Green
    Write-Host "      Found $($response.items.Count) favorite houses" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ GET /houses/favorites - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test GET /api/v1/houses/recommendations
Write-Host "   Testing GET /api/v1/houses/recommendations..." -ForegroundColor Gray
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/houses/recommendations?limit=5" -Method Get -Headers $headers
    Write-Host "   ✅ GET /houses/recommendations - Success" -ForegroundColor Green
    Write-Host "      Found $($response.items.Count) recommendations" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ GET /houses/recommendations - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test POST /api/v1/houses/{id}/favorite (need a house ID)
Write-Host "   Testing POST /api/v1/houses/{id}/favorite..." -ForegroundColor Gray
$testHouseId = "00000000-0000-0000-0000-000000000001"  # Replace with actual house ID
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/houses/$testHouseId/favorite" -Method Post -Headers $headers
    Write-Host "   ✅ POST /houses/{id}/favorite - Success" -ForegroundColor Green
    Write-Host "      Message: $($response.message)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ POST /houses/{id}/favorite - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "3️⃣ Testing Entry Management Endpoints..." -ForegroundColor Yellow

# Test GET /api/v1/tickets/active
Write-Host "   Testing GET /api/v1/tickets/active..." -ForegroundColor Gray
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/tickets/active" -Method Get -Headers $headers
    Write-Host "   ✅ GET /tickets/active - Success" -ForegroundColor Green
    Write-Host "      Found $($response.items.Count) active entries" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ GET /tickets/active - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test GET /api/v1/tickets/history
Write-Host "   Testing GET /api/v1/tickets/history..." -ForegroundColor Gray
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/tickets/history?page=1&pageSize=10" -Method Get -Headers $headers
    Write-Host "   ✅ GET /tickets/history - Success" -ForegroundColor Green
    Write-Host "      Total: $($response.totalCount), Page: $($response.page), Items: $($response.items.Count)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ GET /tickets/history - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test GET /api/v1/tickets/analytics
Write-Host "   Testing GET /api/v1/tickets/analytics..." -ForegroundColor Gray
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/tickets/analytics" -Method Get -Headers $headers
    Write-Host "   ✅ GET /tickets/analytics - Success" -ForegroundColor Green
    Write-Host "      Total Entries: $($response.totalEntries)" -ForegroundColor Gray
    Write-Host "      Active Entries: $($response.activeEntries)" -ForegroundColor Gray
    Write-Host "      Total Wins: $($response.totalWins)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ GET /tickets/analytics - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test POST /api/v1/tickets/quick-entry
Write-Host "   Testing POST /api/v1/tickets/quick-entry..." -ForegroundColor Gray
$quickEntryBody = @{
    houseId = "00000000-0000-0000-0000-000000000001"  # Replace with actual house ID
    quantity = 2
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/tickets/quick-entry" -Method Post -Body $quickEntryBody -Headers $headers
    Write-Host "   ✅ POST /tickets/quick-entry - Success" -ForegroundColor Green
    Write-Host "      Tickets Created: $($response.ticketsCreated)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ POST /tickets/quick-entry - Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "      Note: This may fail if payment integration is not complete (expected for Phase 1)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "4️⃣ Testing Auth Endpoint Enhancement..." -ForegroundColor Yellow

# Test GET /api/v1/auth/me (should include lottery data)
Write-Host "   Testing GET /api/v1/auth/me..." -ForegroundColor Gray
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/auth/me" -Method Get -Headers $headers
    Write-Host "   ✅ GET /auth/me - Success" -ForegroundColor Green
    if ($response.lotteryData) {
        Write-Host "      Lottery Data Present: ✅" -ForegroundColor Green
        Write-Host "      Favorite Houses: $($response.lotteryData.favoriteHouseIds.Count)" -ForegroundColor Gray
    } else {
        Write-Host "      ⚠️  Lottery Data not found in response" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ GET /auth/me - Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "✅ Integration Tests Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Review any failed tests above" -ForegroundColor Gray
Write-Host "2. Verify database migration was successful" -ForegroundColor Gray
Write-Host "3. Test frontend integration" -ForegroundColor Gray
Write-Host "4. Check API contract compliance" -ForegroundColor Gray

