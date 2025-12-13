# Quick Reference for New Chat Sessions - Frontend

## Essential Information
- **Project**: AmesaBase (Lottery Management System)
- **Business Model**: Property lotteries with 4Wins Model (community support)
- **Workspace**: AmesaBase-Monorepo at `C:\Users\dror0\Curser-Repos\AmesaBase-Monorepo\`
- **This Repository**: FE/ (Frontend) → https://github.com/DrorGr/amesaFE
- **Backend Repository**: BE/ → https://github.com/DrorGr/amesaBE
- **Current Branch**: main
- **Architecture**: Angular 20.2.1 + .NET 8.0 + Aurora PostgreSQL
- **Deployment**: AWS (S3 + CloudFront + ECS + ALB)

## Repository Overview
- **AmesaFE**: Angular frontend → S3 + CloudFront
- **AmesaBE**: .NET backend → Docker + ECS
- **AmesaDevOps**: Infrastructure as Code
- **Database**: Aurora PostgreSQL (production cluster)
- **Secrets**: GitHub repository secrets for AWS credentials
- **Configuration**: Single codebase with external environment config
- **CLI Tools**: AWS CLI, Angular CLI, GitHub CLI

## Key Files to Check
### Frontend (AmesaFE):
- `package.json` - Dependencies
- `angular.json` - Angular config
- `src/environments/` - Environment configs
- `.github/workflows/` - CI/CD

### Backend (AmesaBE):
- `Dockerfile` - Container config
- ECS task definitions
- .NET project files

### DevOps (AmesaDevOps):
- Terraform/CloudFormation templates
- Deployment scripts

## Common Commands
```bash
# Frontend (AmesaFE)
npm install
ng serve
ng build --configuration=production
ng build --configuration=development
ng build --configuration=test

# Backend (AmesaBE)
dotnet build
dotnet run
docker build -t amesa-backend .
docker build --build-arg ENV=production -t amesa-backend:local .

# DevOps (AmesaDevOps)
terraform apply
./deploy.sh [environment]
aws ssm get-parameter --name "/amesa/prod/database-url"

# GitHub CLI
gh secret list
gh secret set SECRET_NAME --body "value"

# Git (all repos)
git status
git log --oneline -5
```

## Current Status
- **Working tree**: Clean, all changes committed and pushed
- **Last activity**: 2025-10-31 - Context files updated, OAuth integration plan created
- **Backend Admin Panel**: ✅ Live on production
- **Current focus**: Production environment operational with admin panel
- **Environment**: Production fully operational with API + Admin Panel

## AWS Infrastructure
- **Frontend**: S3 + CloudFront (Production operational ✅)
- **Backend**: ECS + ALB (Production with API routing ✅)
- **Admin Panel**: Blazor Server deployed to production ✅
  - Production: http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/admin
- **Database**: Aurora PostgreSQL (Production cluster with proper authentication ✅)
- **Environments**: Production (Working with full API support + Admin Panel ✅)
- **Secrets**: GitHub repository secrets for production
- **Recent Update**: Context files updated, production-only setup (2025-10-31)

## When Starting New Chat
1. **Mention monorepo structure** - This is FE/ in AmesaBase-Monorepo
2. Share `FE/.cursorrules` - Frontend context
3. Share `FE/CONTEXT_QUICK_REFERENCE.md` - This file
4. Share `FE/CURRENT_WORK.md` - Current status
5. Reference `../MetaData/Documentation/` for cross-cutting docs
6. Reference `../MetaData/Reference/ENVIRONMENT_URLS_GRID.csv` for URLs
7. Mention current branch and recent changes
8. Describe what you need help with

## Monorepo Navigation
- **Frontend work**: You're here in `FE/`
- **Backend work**: Switch to `../BE/`
- **Documentation**: Check `../MetaData/Documentation/`
- **Scripts**: Check `../MetaData/Scripts/`
- **Configs**: Check `../MetaData/Configs/`
