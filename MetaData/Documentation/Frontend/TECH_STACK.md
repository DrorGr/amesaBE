# Technology Stack

## Repository Architecture
- **AmesaFE**: Frontend repository (Angular)
- **AmesaBE**: Backend repository (.NET)
- **AmesaDevOps**: Infrastructure and deployment repository

## Frontend (AmesaFE)
- **Framework**: Angular 20.2.1 (latest version)
- **Language**: TypeScript 5.9.2
- **Styling**: Tailwind CSS 3.4.3
- **Build Tool**: Angular CLI 20.2.1
- **Testing**: Karma (configured in karma.conf.js)
- **Package Manager**: npm
- **Linting**: ESLint (eslint.config.js)
- **CLI Tools**: AWS CLI, Angular CLI, GitHub CLI
- **Key Features**: 
  - Standalone components architecture
  - Lazy loading with custom preloading strategy
  - Internationalization support
  - Dark/light theme switching
  - Real-time features with SignalR

## Backend (AmesaBE)
- **Framework**: .NET 8.0
- **Language**: C#
- **Testing**: .NET Test Framework
- **Containerization**: Docker
- **Deployment**: AWS ECS (Elastic Container Service)

## Database
- **Type**: Aurora PostgreSQL
- **Cloud Provider**: AWS
- **Managed Service**: Amazon Aurora

## AWS Infrastructure - Actual Implementation
- **Region**: eu-north-1
- **Frontend Hosting**: S3 static website hosting (4 buckets)
- **CDN**: 5 CloudFront distributions (dev/stage/prod + API + static)
- **Backend Hosting**: ECS Fargate cluster "Amesa" with 2 services
- **Load Balancing**: Application Load Balancer (ALB) for backend API
- **Database**: Aurora PostgreSQL Serverless v2 (4 instances across prod/stage)
- **Security**: Origin Access Control (OAC) for S3 origins

## CI/CD & DevOps
- **Version Control**: Git with GitHub CLI
- **CI/CD**: GitHub Actions workflows
- **Infrastructure**: Infrastructure as Code (AmesaDevOps repo)
- **Deployment**: Automated via GitHub Actions
- **Secrets Management**: GitHub repository secrets for AWS credentials and environment configs
- **Configuration Strategy**: Single codebase with external environment configuration

## Environments
- **Development (dev)**: 
  - Frontend: https://dev.amesa.com
  - Backend: https://dev-api.amesa.com
  - Database: Separate Aurora PostgreSQL cluster
- **Test**: Test environment with isolated database
- **Production (prod)**: 
  - Frontend: https://amesa.com
  - Backend: https://api.amesa.com
  - Database: Production Aurora PostgreSQL cluster

## Database Architecture
- **Production**: Dedicated Aurora PostgreSQL cluster
- **Test**: Separate Aurora PostgreSQL cluster for testing
- **Development**: Development Aurora PostgreSQL cluster
- **Isolation**: Complete environment separation with no cross-environment data sharing

## Key Configuration Files
### Frontend (AmesaFE)
- `angular.json` - Angular project configuration
- `tailwind.config.js` - Tailwind CSS configuration
- `tsconfig.json` - TypeScript configuration
- `eslint.config.js` - ESLint configuration
- `package.json` - Dependencies and scripts
- Environment files in `src/environments/`

### Backend (AmesaBE)
- Dockerfile - Container configuration
- .NET project files
- ECS task definitions

### DevOps (AmesaDevOps)
- AWS CloudFormation/Terraform templates
- Deployment scripts
- Infrastructure configurations

## Build & Deployment Process
1. **Frontend**: Angular builds with environment config → `dist/` → S3 → CloudFront
2. **Backend**: .NET builds → Docker image with environment variables → ECS deployment
3. **Database**: Aurora PostgreSQL managed service
4. **CI/CD**: GitHub Actions triggers deployments across environments with external config injection
5. **Configuration**: External environment configs injected at build/runtime
