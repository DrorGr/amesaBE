#!/bin/bash

# Local Deployment Script for Amesa Backend
# This script builds and runs the backend locally using Docker

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo -e "${BLUE}[HEADER]${NC} $1"
}

# Configuration
COMPOSE_FILE="docker-compose.yml"
DEV_COMPOSE_FILE="docker-compose.dev.yml"
ENV_FILE=".env"

# Function to check if Docker is running
check_docker() {
    print_status "Checking Docker status..."
    
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker Desktop and try again."
        exit 1
    fi
    
    print_status "Docker is running."
}

# Function to check if .env file exists
check_env_file() {
    if [ ! -f "$ENV_FILE" ]; then
        print_warning ".env file not found. Creating from template..."
        
        if [ -f "env.example" ]; then
            cp env.example .env
            print_status ".env file created from template. Please edit it with your configuration."
        else
            print_error "env.example file not found. Please create a .env file manually."
            exit 1
        fi
    else
        print_status ".env file found."
    fi
}

# Function to build and start services
start_services() {
    local environment=${1:-production}
    
    print_header "Starting Amesa Backend in $environment mode..."
    
    if [ "$environment" = "development" ]; then
        print_status "Starting development environment..."
        docker-compose -f $DEV_COMPOSE_FILE up -d --build
    else
        print_status "Starting production environment..."
        docker-compose -f $COMPOSE_FILE up -d --build
    fi
    
    print_status "Services started successfully!"
}

# Function to stop services
stop_services() {
    local environment=${1:-production}
    
    print_header "Stopping Amesa Backend services..."
    
    if [ "$environment" = "development" ]; then
        docker-compose -f $DEV_COMPOSE_FILE down
    else
        docker-compose -f $COMPOSE_FILE down
    fi
    
    print_status "Services stopped successfully!"
}

# Function to show logs
show_logs() {
    local environment=${1:-production}
    local service=${2:-api}
    
    print_header "Showing logs for $service service..."
    
    if [ "$environment" = "development" ]; then
        docker-compose -f $DEV_COMPOSE_FILE logs -f $service
    else
        docker-compose -f $COMPOSE_FILE logs -f $service
    fi
}

# Function to show service status
show_status() {
    local environment=${1:-production}
    
    print_header "Service Status:"
    
    if [ "$environment" = "development" ]; then
        docker-compose -f $DEV_COMPOSE_FILE ps
    else
        docker-compose -f $COMPOSE_FILE ps
    fi
}

# Function to run database migrations
run_migrations() {
    local environment=${1:-production}
    
    print_header "Running database migrations..."
    
    if [ "$environment" = "development" ]; then
        docker-compose -f $DEV_COMPOSE_FILE exec api-dev dotnet ef database update
    else
        docker-compose -f $COMPOSE_FILE exec api dotnet ef database update
    fi
    
    print_status "Database migrations completed!"
}

# Function to clean up
cleanup() {
    print_header "Cleaning up Docker resources..."
    
    # Stop all services
    docker-compose -f $COMPOSE_FILE down 2>/dev/null || true
    docker-compose -f $DEV_COMPOSE_FILE down 2>/dev/null || true
    
    # Remove unused images
    docker image prune -f
    
    # Remove unused volumes (be careful with this)
    read -p "Do you want to remove unused volumes? This will delete all data! (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        docker volume prune -f
        print_warning "Unused volumes removed!"
    fi
    
    print_status "Cleanup completed!"
}

# Function to show help
show_help() {
    echo "Amesa Backend Local Deployment Script"
    echo ""
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  start [dev|prod]     Start services (default: prod)"
    echo "  stop [dev|prod]      Stop services (default: prod)"
    echo "  restart [dev|prod]   Restart services (default: prod)"
    echo "  logs [dev|prod] [service]  Show logs (default: prod, api)"
    echo "  status [dev|prod]    Show service status (default: prod)"
    echo "  migrate [dev|prod]   Run database migrations (default: prod)"
    echo "  cleanup              Clean up Docker resources"
    echo "  help                 Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 start dev         Start development environment"
    echo "  $0 logs prod api     Show API logs in production mode"
    echo "  $0 migrate dev       Run migrations in development"
    echo "  $0 cleanup           Clean up Docker resources"
}

# Main script logic
main() {
    local command=${1:-help}
    local environment=${2:-production}
    
    case $command in
        start)
            check_docker
            check_env_file
            start_services $environment
            show_status $environment
            ;;
        stop)
            stop_services $environment
            ;;
        restart)
            stop_services $environment
            sleep 2
            start_services $environment
            show_status $environment
            ;;
        logs)
            show_logs $environment $3
            ;;
        status)
            show_status $environment
            ;;
        migrate)
            run_migrations $environment
            ;;
        cleanup)
            cleanup
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            print_error "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"

