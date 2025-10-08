# Local Deployment Script for Amesa Backend (PowerShell)
# This script builds and runs the backend locally using Docker

param(
    [Parameter(Position=0)]
    [string]$Command = "help",
    
    [Parameter(Position=1)]
    [string]$Environment = "production",
    
    [Parameter(Position=2)]
    [string]$Service = "api"
)

# Configuration
$ComposeFile = "docker-compose.yml"
$DevComposeFile = "docker-compose.dev.yml"
$EnvFile = ".env"

# Function to print colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Header {
    param([string]$Message)
    Write-Host "[HEADER] $Message" -ForegroundColor Blue
}

# Function to check if Docker is running
function Test-Docker {
    Write-Status "Checking Docker status..."
    
    try {
        docker info | Out-Null
        Write-Status "Docker is running."
        return $true
    }
    catch {
        Write-Error "Docker is not running. Please start Docker Desktop and try again."
        return $false
    }
}

# Function to check if .env file exists
function Test-EnvFile {
    if (-not (Test-Path $EnvFile)) {
        Write-Warning ".env file not found. Creating from template..."
        
        if (Test-Path "env.example") {
            Copy-Item "env.example" $EnvFile
            Write-Status ".env file created from template. Please edit it with your configuration."
        }
        else {
            Write-Error "env.example file not found. Please create a .env file manually."
            exit 1
        }
    }
    else {
        Write-Status ".env file found."
    }
}

# Function to build and start services
function Start-Services {
    param([string]$Environment)
    
    Write-Header "Starting Amesa Backend in $Environment mode..."
    
    if ($Environment -eq "development") {
        Write-Status "Starting development environment..."
        docker-compose -f $DevComposeFile up -d --build
    }
    else {
        Write-Status "Starting production environment..."
        docker-compose -f $ComposeFile up -d --build
    }
    
    Write-Status "Services started successfully!"
}

# Function to stop services
function Stop-Services {
    param([string]$Environment)
    
    Write-Header "Stopping Amesa Backend services..."
    
    if ($Environment -eq "development") {
        docker-compose -f $DevComposeFile down
    }
    else {
        docker-compose -f $ComposeFile down
    }
    
    Write-Status "Services stopped successfully!"
}

# Function to show logs
function Show-Logs {
    param([string]$Environment, [string]$Service)
    
    Write-Header "Showing logs for $Service service..."
    
    if ($Environment -eq "development") {
        docker-compose -f $DevComposeFile logs -f $Service
    }
    else {
        docker-compose -f $ComposeFile logs -f $Service
    }
}

# Function to show service status
function Show-Status {
    param([string]$Environment)
    
    Write-Header "Service Status:"
    
    if ($Environment -eq "development") {
        docker-compose -f $DevComposeFile ps
    }
    else {
        docker-compose -f $ComposeFile ps
    }
}

# Function to run database migrations
function Invoke-Migrations {
    param([string]$Environment)
    
    Write-Header "Running database migrations..."
    
    if ($Environment -eq "development") {
        docker-compose -f $DevComposeFile exec api-dev dotnet ef database update
    }
    else {
        docker-compose -f $ComposeFile exec api dotnet ef database update
    }
    
    Write-Status "Database migrations completed!"
}

# Function to clean up
function Remove-DockerResources {
    Write-Header "Cleaning up Docker resources..."
    
    # Stop all services
    try { docker-compose -f $ComposeFile down } catch { }
    try { docker-compose -f $DevComposeFile down } catch { }
    
    # Remove unused images
    docker image prune -f
    
    # Remove unused volumes (be careful with this)
    $response = Read-Host "Do you want to remove unused volumes? This will delete all data! (y/N)"
    if ($response -eq "y" -or $response -eq "Y") {
        docker volume prune -f
        Write-Warning "Unused volumes removed!"
    }
    
    Write-Status "Cleanup completed!"
}

# Function to show help
function Show-Help {
    Write-Host "Amesa Backend Local Deployment Script (PowerShell)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\deploy-local.ps1 [COMMAND] [ENVIRONMENT] [SERVICE]" -ForegroundColor White
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Yellow
    Write-Host "  start [dev|prod]     Start services (default: prod)"
    Write-Host "  stop [dev|prod]      Stop services (default: prod)"
    Write-Host "  restart [dev|prod]   Restart services (default: prod)"
    Write-Host "  logs [dev|prod] [service]  Show logs (default: prod, api)"
    Write-Host "  status [dev|prod]    Show service status (default: prod)"
    Write-Host "  migrate [dev|prod]   Run database migrations (default: prod)"
    Write-Host "  cleanup              Clean up Docker resources"
    Write-Host "  help                 Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\deploy-local.ps1 start dev         Start development environment"
    Write-Host "  .\deploy-local.ps1 logs prod api     Show API logs in production mode"
    Write-Host "  .\deploy-local.ps1 migrate dev       Run migrations in development"
    Write-Host "  .\deploy-local.ps1 cleanup           Clean up Docker resources"
}

# Main script logic
switch ($Command.ToLower()) {
    "start" {
        if (-not (Test-Docker)) { exit 1 }
        Test-EnvFile
        Start-Services $Environment
        Show-Status $Environment
    }
    "stop" {
        Stop-Services $Environment
    }
    "restart" {
        Stop-Services $Environment
        Start-Sleep -Seconds 2
        Start-Services $Environment
        Show-Status $Environment
    }
    "logs" {
        Show-Logs $Environment $Service
    }
    "status" {
        Show-Status $Environment
    }
    "migrate" {
        Invoke-Migrations $Environment
    }
    "cleanup" {
        Remove-DockerResources
    }
    "help" {
        Show-Help
    }
    default {
        Write-Error "Unknown command: $Command"
        Show-Help
        exit 1
    }
}

