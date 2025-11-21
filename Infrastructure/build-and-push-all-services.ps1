# CONFIG
$Region   = "eu-north-1"
$Account  = "129394705401"
$Cluster  = "Amesa"
$EcrRoot  = "$Account.dkr.ecr.$Region.amazonaws.com"

# Service -> {repo, dockerfilePath, ecsService}
# Dockerfiles expect build context from BE directory
$services = @(
  @{ repo="amesa-auth-service";            dockerfile="AmesaBackend.Auth/Dockerfile";           ecs="amesa-auth-service" },
  @{ repo="amesa-content-service";         dockerfile="AmesaBackend.Content/Dockerfile";        ecs="amesa-content-service" },
  @{ repo="amesa-notification-service";    dockerfile="AmesaBackend.Notification/Dockerfile";   ecs="amesa-notification-service" },
  @{ repo="amesa-payment-service";         dockerfile="AmesaBackend.Payment/Dockerfile";        ecs="amesa-payment-service" },
  @{ repo="amesa-lottery-service";         dockerfile="AmesaBackend.Lottery/Dockerfile";        ecs="amesa-lottery-service" },
  @{ repo="amesa-lottery-results-service"; dockerfile="AmesaBackend.LotteryResults/Dockerfile"; ecs="amesa-lottery-results-service" },
  @{ repo="amesa-analytics-service";       dockerfile="AmesaBackend.Analytics/Dockerfile";      ecs="amesa-analytics-service" },
  @{ repo="amesa-admin-service";           dockerfile="AmesaBackend.Admin/Dockerfile";          ecs="amesa-admin-service" }
)

# 1) Login to ECR
Write-Host "Logging into ECR..." -ForegroundColor Cyan
aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin $EcrRoot
if ($LASTEXITCODE -ne 0) { throw "ECR login failed" }

# 2) Build + push latest for each repo
# Build context must be BE directory (Dockerfiles reference AmesaBackend.Shared)
$env:DOCKER_BUILDKIT = "1"
$beDir = Join-Path $PSScriptRoot ".."
Push-Location $beDir
try {
  foreach ($s in $services) {
    if (-not (Test-Path $s.dockerfile)) { 
      Write-Warning "Skip (Dockerfile not found): $($s.dockerfile)" 
      continue 
    }

    $image = "$EcrRoot/$($s.repo):latest"
    Write-Host "`n=== Building $($s.repo) using $($s.dockerfile) ===" -ForegroundColor Cyan
    docker build -f $s.dockerfile -t $image .
    if ($LASTEXITCODE -ne 0) { 
      Write-Error "Build failed: $($s.repo)"
      continue
    }

    Write-Host "Pushing $image" -ForegroundColor Cyan
    docker push $image
    if ($LASTEXITCODE -ne 0) { 
      Write-Error "Push failed: $($s.repo)"
      continue
    }
    Write-Host "✅ $($s.repo) pushed successfully" -ForegroundColor Green
  }
} finally {
  Pop-Location
}

# 3) Redeploy ECS services to pick up :latest
Write-Host "`n=== Redeploying ECS services ===" -ForegroundColor Cyan
foreach ($s in $services) {
  Write-Host "Forcing new deployment: $($s.ecs)" -ForegroundColor Green
  aws ecs update-service --region $Region --cluster $Cluster --service $($s.ecs) --force-new-deployment | Out-Null
}
Write-Host "`n✅ All done. Watch ECS until each shows 1/1 tasks running." -ForegroundColor Green

