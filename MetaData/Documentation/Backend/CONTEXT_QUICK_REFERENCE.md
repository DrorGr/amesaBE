# Quick Reference for New Chat Sessions - Backend

## Essential Information
- **Project**: AmesaBE (Backend API for Amesa Lottery Platform)
- **Business Model**: Property lottery with 4Wins Model (community support)
- **Workspace**: AmesaBase-Monorepo at `C:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\`
- **This Repository**: BE/ (Backend) → https://github.com/DrorGr/amesaBE
- **Frontend Repository**: FE/ → https://github.com/DrorGr/amesaFE
- **Current Branch**: main
- **Architecture**: .NET 8.0 + ASP.NET Core + Aurora PostgreSQL
- **Deployment**: AWS (ECS Fargate + ECR + ALB)

## Repository Overview
- **amesaBE**: .NET backend API → Docker + ECS Fargate
- **amesaFE**: Angular frontend → S3 + CloudFront
- **amesaDevOps**: Infrastructure as Code (recommended)
- **Database**: Aurora PostgreSQL (production cluster)
- **Secrets**: GitHub repository secrets for AWS credentials
- **Configuration**: Environment-specific via GitHub Secrets
- **CLI Tools**: .NET CLI, AWS CLI, Docker, GitHub CLI

## Key Files to Check
### Backend (amesaBE):
- `AmesaBackend/Program.cs` - Application entry point and configuration
- `AmesaBackend/Controllers/` - API endpoint controllers
- `AmesaBackend/Services/` - Business logic services
- `AmesaBackend/Models/` - Entity models
- `AmesaBackend/Data/AmesaDbContext.cs` - Entity Framework context
- `AmesaBackend/Dockerfile` - Container configuration
- `.github/workflows/deploy.yml` - CI/CD pipeline
- `appsettings.json` - Configuration settings
- `database-schema.sql` - Database structure

## Common Commands
```bash
# Backend (amesaBE)
dotnet restore
dotnet build
dotnet test
dotnet run --project AmesaBackend
dotnet run --project AmesaBackend -- --seeder

# Database operations
dotnet ef migrations add MigrationName --project AmesaBackend
dotnet ef database update --project AmesaBackend

# Docker
docker build -t amesa-backend:local ./AmesaBackend
docker-compose up
docker-compose -f docker-compose.yml up

# AWS operations
aws ecs describe-services --cluster Amesa --services amesa-backend-service --region eu-north-1
aws ecr describe-images --repository-name amesabe --region eu-north-1
aws rds describe-db-clusters --db-cluster-identifier amesadbmain --region eu-north-1

# GitHub CLI
gh secret list
gh secret set SECRET_NAME --body "value"

# Git (all repos)
git status
git log --oneline -5
git checkout main
```

## Current Status
- **Working tree**: Clean, all changes committed
- **Last activity**: 2025-10-31 - Context files updated, OAuth integration plan created
- **Current focus**: Admin Panel fully operational on production
- **Environment**: Production operational with admin panel
- **Admin Panel**: ✅ LIVE on production with secure login
- **Latest Update**: Context files updated, production-only setup (2025-10-31)

## AWS Infrastructure
- **Backend**: ECS Fargate + ECR (Production operational ✅)
- **Database**: Aurora PostgreSQL Serverless v2 ✅
- **Load Balancer**: ALB for production ✅
- **Container Registry**: ECR (amesabe repository) ✅
- **Environments**: Production (Working ✅)
- **Secrets**: GitHub repository secrets + AWS Secrets Manager
- **Recent Update**: Context files updated, production-only setup (2025-10-31)

## API Endpoints

### Core Endpoints:
- **Health Check**: `GET /health`
- **Authentication**: `POST /api/v1/auth/*`
- **Houses**: `GET /api/v1/houses`
- **Translations**: `GET /api/v1/translations/{language}`
- **Lottery Results**: `GET /api/v1/lottery-results`

### Admin Panel:
- **Admin Login**: `/admin/login`
- **Admin Dashboard**: `/admin`
- **Content Management**: `/admin/houses`, `/admin/users`, `/admin/translations`
- **Database Switching**: Built-in database selector in admin panel

### Environment URLs:
- **Local Development**: 
  - API: http://localhost:5000
  - Admin: http://localhost:5000/admin
- **Production**: 
  - API: amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com
  - Admin: http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/admin ✅

## Deployment Strategy

### Automatic Deployment:
- **Main branch** → Push → Auto-deploy to production

### Docker Image Tags:
- **Production**: `prod-{sha}`, `latest`, `prod-latest`

## Tech Stack Details

### Backend:
- **.NET 8.0** with ASP.NET Core
- **Blazor Server** for admin panel UI
- **Entity Framework Core** for ORM
- **JWT** for authentication
- **SignalR** for real-time communication
- **Serilog** for structured logging
- **Swagger/OpenAPI** for API documentation
- **BCrypt** for password hashing
- **FluentValidation** for input validation (planned)

### Database:
- **Aurora PostgreSQL** Serverless v2 (production)
- **SQLite** for local development
- **EF Core Migrations** for schema management

### Infrastructure:
- **ECS Fargate** for container orchestration
- **ECR** for Docker image storage
- **Application Load Balancer** for traffic distribution
- **CloudWatch** for logs and monitoring
- **AWS Secrets Manager** for sensitive configuration

## When Starting New Chat
1. **Mention monorepo structure** - This is BE/ in AmesaBase-Monorepo
2. Share `BE/.cursorrules` - Backend context
3. Share `BE/CONTEXT_QUICK_REFERENCE.md` - This file
4. Share `BE/CURRENT_WORK.md` - Current status
5. Share `BE/API-Design.md` - API documentation
6. Reference `../MetaData/Documentation/` for cross-cutting docs
7. Reference `../MetaData/Reference/ENVIRONMENT_URLS_GRID.csv` for URLs
8. Mention current branch and recent changes
9. Describe what you need help with

## Monorepo Navigation
- **Backend work**: You're here in `BE/`
- **Frontend work**: Switch to `../FE/`
- **Documentation**: Check `../MetaData/Documentation/`
- **Scripts**: Check `../MetaData/Scripts/`
- **Configs**: Check `../MetaData/Configs/`

## Important Notes
- **Production deployments** require manual workflow_dispatch
- **Database credentials** are configured with correct passwords from AWS
- **Admin Panel** is fully functional with secure login and database switching
- **GitHub Secrets** must be configured for CI/CD
- **Health check endpoint** is critical for ECS task health
- **Connection strings** differ per environment with verified credentials
- **Docker images** are built and pushed to ECR on each deployment
- **Admin Panel** provides content management for all lottery data

