# Repository Structure & Architecture

## Multi-Repository Setup

### 1. **AmesaFE** (Frontend Repository)
**Purpose**: Angular frontend application
**Location**: GitHub repository
**Current Branch**: Testing

#### Structure:
```
AmesaFE/
├── src/
│   ├── components/         # UI components (house-card, house-grid, etc.)
│   ├── services/          # Angular services
│   ├── models/            # TypeScript models
│   ├── guards/            # Route guards
│   ├── environments/      # Environment configurations
│   └── assets/            # Static assets
├── dist/                  # Built application
├── package.json           # Dependencies
├── angular.json           # Angular configuration
├── tailwind.config.js     # Tailwind CSS config
└── .github/workflows/     # CI/CD workflows
```

#### Deployment:
- **Build**: Angular CLI builds to `dist/`
- **Hosting**: AWS S3 static website hosting
- **CDN**: CloudFront distribution
- **CI/CD**: GitHub Actions workflow

### 2. **AmesaBE** (Backend Repository)
**Purpose**: .NET backend API
**Architecture**: Containerized microservices

#### Structure:
```
AmesaBE/
├── AmesaBackend/          # Main .NET project
├── AmesaBackend.Tests/    # Test project
├── Dockerfile             # Container configuration
├── docker-compose.yml     # Local development
└── ECS task definitions   # AWS deployment configs
```

#### Deployment:
- **Containerization**: Docker images
- **Hosting**: AWS ECS (Elastic Container Service)
- **Load Balancing**: Application Load Balancer (ALB)
- **Scaling**: Multiple ECS instances

### 3. **AmesaDevOps** (Infrastructure Repository)
**Purpose**: Infrastructure as Code and deployment automation

#### Structure:
```
AmesaDevOps/
├── terraform/             # Infrastructure definitions
├── cloudformation/        # AWS CloudFormation templates
├── scripts/               # Deployment scripts
├── environments/          # Environment-specific configs
│   ├── dev/
│   ├── stage/
│   └── prod/
└── monitoring/            # Monitoring and logging configs
```

## AWS Architecture

### Frontend Flow:
```
GitHub (AmesaFE) → GitHub Actions → S3 Bucket → CloudFront → Users
```

### Backend Flow:
```
GitHub (AmesaBE) → GitHub Actions → ECR → ECS → ALB → Users
```

### Database:
```
Aurora PostgreSQL (Multi-AZ) ← ECS Backend Services
```

## Environment Separation

### Development:
- **Frontend**: `d2rmamd755wq7j.cloudfront.net` (amesa-frontend-dev S3 + CloudFront E2XBDFAUZJT159)
- **Backend**: ECS service `amesa-backend-stage-service` (Fargate)
- **Database**: `amesadbmain-stage` + `amesadbmain1-stage` (Aurora PostgreSQL Serverless v2)
- **Secrets**: GitHub repository secrets (AWS, ENV)

### Staging:
- **Frontend**: `d2ejqzjfslo5hs.cloudfront.net` (amesa-frontend-stage S3 + CloudFront E1D7XQHFF1469W)
- **Backend**: ECS service `amesa-backend-stage-service` (Fargate)
- **Database**: `amesadbmain-stage` + `amesadbmain1-stage` (Aurora PostgreSQL Serverless v2)
- **Secrets**: GitHub repository secrets (AWS, ENV)

### Production:
- **Frontend**: `dpqbvdgnenckf.cloudfront.net` (amesa-frontend-prod S3 + CloudFront E3GU3QXUR43ZOH)
- **Backend**: ECS service `amesa-backend-service` (Fargate)
- **Database**: `amesadbmain` + `amesadbmain1` (Aurora PostgreSQL Serverless v2)
- **API**: `dgfh2mwcqhlbw.cloudfront.net` (Backend API via ALB + CloudFront EJY57DPCK1LYS)
- **Secrets**: GitHub repository secrets (AWS, ENV)

## Database Isolation
- **Complete Separation**: Each environment has its own Aurora PostgreSQL cluster
- **No Cross-Environment Access**: Production, test, and development databases are completely isolated
- **Independent Scaling**: Each database cluster can be scaled independently
- **Environment-Specific Configurations**: Different connection strings and credentials per environment

## CI/CD Pipeline

### Frontend (AmesaFE):
1. Code push to GitHub
2. GitHub Actions triggers
3. Build Angular application
4. Deploy to S3
5. Invalidate CloudFront cache

### Backend (AmesaBE):
1. Code push to GitHub
2. GitHub Actions triggers
3. Build .NET application
4. Create Docker image
5. Push to ECR
6. Deploy to ECS
7. Update ALB target groups

## Key Commands

### Frontend (AmesaFE):
```bash
# Local development
npm install
ng serve

# Build
ng build --configuration=production

# Environment-specific builds
ng build --configuration=development
ng build --configuration=test
ng build --configuration=production

# Deploy (via GitHub Actions)
git push origin main
```

### Backend (AmesaBE):
```bash
# Local development
dotnet run

# Docker build
docker build -t amesa-backend .

# Environment-specific Docker builds
docker build --build-arg ENV=dev -t amesa-backend:dev .
docker build --build-arg ENV=test -t amesa-backend:test .
docker build --build-arg ENV=prod -t amesa-backend:prod .

# Deploy (via GitHub Actions)
git push origin main
```

### DevOps (AmesaDevOps):
```bash
# Infrastructure deployment
terraform apply

# Environment-specific deployment
./deploy.sh dev
./deploy.sh test
./deploy.sh prod

# GitHub secrets management
gh secret list
gh secret set SECRET_NAME --body "secret_value"

# AWS CLI operations
aws ssm get-parameter --name "/amesa/dev/database-url"
aws ssm put-parameter --name "/amesa/prod/api-url" --value "https://api.amesa.com"
```
