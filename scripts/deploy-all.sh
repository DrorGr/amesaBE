#!/bin/bash
# Complete Deployment Script for Amesa Microservices
# This script deploys infrastructure and prepares for service deployment

set -e

echo "=========================================="
echo "Amesa Microservices Deployment Script"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

# Check AWS CLI
if ! command -v aws &> /dev/null; then
    echo -e "${RED}❌ AWS CLI not found. Please install AWS CLI.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ AWS CLI found${NC}"

# Check Terraform
if ! command -v terraform &> /dev/null; then
    echo -e "${RED}❌ Terraform not found. Please install Terraform.${NC}"
    exit 1
fi
echo -e "${GREEN}✅ Terraform found${NC}"

# Check AWS credentials
echo -e "${YELLOW}Verifying AWS credentials...${NC}"
if aws sts get-caller-identity &> /dev/null; then
    AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
    AWS_USER=$(aws sts get-caller-identity --query Arn --output text)
    echo -e "${GREEN}✅ AWS credentials valid${NC}"
    echo -e "   Account: ${AWS_ACCOUNT}"
    echo -e "   User: ${AWS_USER}"
else
    echo -e "${RED}❌ AWS credentials not configured or invalid${NC}"
    exit 1
fi

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}⚠️  .NET SDK not found. Skipping database migrations.${NC}"
else
    echo -e "${GREEN}✅ .NET SDK found${NC}"
fi

echo ""
echo -e "${YELLOW}=========================================="
echo "Deployment Options:"
echo "==========================================${NC}"
echo "1. Deploy Infrastructure (Terraform)"
echo "2. Create Database Migrations"
echo "3. Build Docker Images (Local)"
echo "4. Full Deployment (Infrastructure + Migrations)"
echo "5. Exit"
echo ""
read -p "Select option (1-5): " option

case $option in
    1)
        echo -e "${YELLOW}Deploying infrastructure with Terraform...${NC}"
        cd Infrastructure/terraform
        
        echo -e "${YELLOW}Initializing Terraform...${NC}"
        terraform init
        
        echo -e "${YELLOW}Planning Terraform deployment...${NC}"
        terraform plan
        
        echo -e "${YELLOW}Do you want to apply these changes? (yes/no): ${NC}"
        read -p "" confirm
        if [ "$confirm" = "yes" ]; then
            terraform apply -auto-approve
            echo -e "${GREEN}✅ Infrastructure deployed successfully${NC}"
        else
            echo -e "${YELLOW}Deployment cancelled${NC}"
        fi
        ;;
    2)
        echo -e "${YELLOW}Creating database migrations...${NC}"
        if [ -f "scripts/database-migrations.sh" ]; then
            bash scripts/database-migrations.sh
            echo -e "${GREEN}✅ Migrations created${NC}"
        else
            echo -e "${RED}❌ Migration script not found${NC}"
        fi
        ;;
    3)
        echo -e "${YELLOW}Building Docker images locally...${NC}"
        SERVICES=("AmesaBackend.Auth" "AmesaBackend.Content" "AmesaBackend.Notification" "AmesaBackend.Payment" "AmesaBackend.Lottery" "AmesaBackend.LotteryResults" "AmesaBackend.Analytics" "AmesaBackend.Admin")
        
        for service in "${SERVICES[@]}"; do
            echo -e "${YELLOW}Building ${service}...${NC}"
            docker build -t ${service,,}:latest -f ${service}/Dockerfile .
            echo -e "${GREEN}✅ ${service} built${NC}"
        done
        ;;
    4)
        echo -e "${YELLOW}Starting full deployment...${NC}"
        
        # Deploy infrastructure
        echo -e "${YELLOW}Step 1: Deploying infrastructure...${NC}"
        cd Infrastructure/terraform
        terraform init
        terraform plan
        echo -e "${YELLOW}Do you want to apply infrastructure changes? (yes/no): ${NC}"
        read -p "" confirm
        if [ "$confirm" = "yes" ]; then
            terraform apply -auto-approve
            echo -e "${GREEN}✅ Infrastructure deployed${NC}"
        else
            echo -e "${RED}❌ Infrastructure deployment cancelled${NC}"
            exit 1
        fi
        
        cd ../..
        
        # Create migrations
        if command -v dotnet &> /dev/null; then
            echo -e "${YELLOW}Step 2: Creating database migrations...${NC}"
            if [ -f "scripts/database-migrations.sh" ]; then
                bash scripts/database-migrations.sh
                echo -e "${GREEN}✅ Migrations created${NC}"
            fi
        else
            echo -e "${YELLOW}⚠️  Skipping migrations (dotnet not found)${NC}"
        fi
        
        echo -e "${GREEN}✅ Full deployment preparation complete${NC}"
        echo -e "${YELLOW}Next steps:${NC}"
        echo "  1. Configure GitHub Secrets (AWS_ACCOUNT_ID, AWS_IAM_ROLE_NAME)"
        echo "  2. Push to main branch to trigger CI/CD"
        echo "  3. Monitor deployments in GitHub Actions"
        ;;
    5)
        echo -e "${YELLOW}Exiting...${NC}"
        exit 0
        ;;
    *)
        echo -e "${RED}Invalid option${NC}"
        exit 1
        ;;
esac

echo ""
echo -e "${GREEN}=========================================="
echo "Deployment script completed"
echo "==========================================${NC}"

