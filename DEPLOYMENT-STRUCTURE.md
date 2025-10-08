# Deployment Structure & Scripts

## 🔐 **GitHub Secrets & CI/CD**

### How Secrets Work:
- **Secrets are encrypted** and stored in GitHub repository settings
- **Only CI/CD workflows** can access them using `${{ secrets.SECRET_NAME }}`
- **Values are never visible** in the repository or logs
- **Environment variables are injected** during build/deploy process

### Example:
```yaml
- name: Build application
  run: dotnet build
  env:
    DB_CONNECTION_STRING: ${{ secrets.PROD_DB_CONNECTION_STRING }}
    JWT_SECRET_KEY: ${{ secrets.PROD_JWT_SECRET_KEY }}
```

## 📁 **Repository Structure**

### **amesaFE** (Frontend Repository)
```
amesaFE/
├── src/
├── .github/workflows/deploy.yml    # CI/CD for frontend
├── scripts/                        # Local development scripts
└── README.md
```

### **amesaBE** (Backend Repository)
```
amesaBE/
├── AmesaBackend/
├── AmesaBackend.Tests/
├── .github/workflows/deploy.yml    # CI/CD for backend
├── scripts/                        # Local development scripts
│   ├── docker-compose.yml          # Production Docker setup
│   ├── docker-compose.dev.yml      # Development Docker setup
│   ├── deploy-template.sh          # Template deployment script
│   └── README.md
└── README.md
```

### **amesaDevOps** (Infrastructure Repository) - **Recommended**
```
amesaDevOps/
├── infrastructure/
│   ├── terraform/                  # Infrastructure as Code
│   ├── cloudformation/             # AWS CloudFormation
│   └── kubernetes/                 # K8s manifests
├── scripts/
│   ├── deploy-*.ps1                # Original deployment scripts
│   ├── aws-infrastructure.yaml     # AWS infrastructure
│   └── setup-secrets.*             # Secret management scripts
├── docs/
│   ├── DEPLOYMENT-GUIDE.md
│   └── INFRASTRUCTURE.md
└── .github/workflows/
    └── infrastructure.yml          # Deploy infrastructure
```

## 🚀 **Deployment Flow**

### **Automatic Deployment (via CI/CD)**
1. **Push to branch** → Triggers GitHub Actions
2. **Build application** → Uses environment-specific config
3. **Run tests** → Ensures quality
4. **Deploy to AWS** → Using GitHub Secrets for credentials
5. **Update infrastructure** → ECS, S3, CloudFront, etc.

### **Manual Deployment (local development)**
1. **Set environment variables** locally
2. **Run Docker Compose** → `docker-compose -f scripts/docker-compose.dev.yml up`
3. **Or run directly** → `dotnet run --project AmesaBackend`

## 🔧 **What to do with Original Scripts**

### **Option 1: Move to amesaDevOps** ⭐ **Recommended**
- Create `amesaDevOps` repository
- Move all deployment scripts there
- Keep infrastructure code separate from application code

### **Option 2: Keep Generic Scripts Only**
- Keep: `docker-compose.yml`, `database-schema.sql`, `nginx.conf`
- Remove: `deploy-*.ps1`, `aws-infrastructure.yaml`, `task-definition.json`

### **Option 3: Create Template Scripts**
- Replace sensitive values with environment variables
- Create template versions for reference

## 📋 **Required GitHub Secrets**

### **Frontend Secrets:**
```
DEV_API_URL, STAGE_API_URL, PROD_API_URL
DEV_S3_BUCKET, STAGE_S3_BUCKET, PROD_S3_BUCKET
DEV_CLOUDFRONT_ID, STAGE_CLOUDFRONT_ID, PROD_CLOUDFRONT_ID
AWS_ACCESS_KEY_ID, AWS_SECRET_ACCESS_KEY
```

### **Backend Secrets:**
```
DEV_ECS_CLUSTER, STAGE_ECS_CLUSTER, PROD_ECS_CLUSTER
DEV_ECS_SERVICE, STAGE_ECS_SERVICE, PROD_ECS_SERVICE
DEV_DB_CONNECTION_STRING, STAGE_DB_CONNECTION_STRING, PROD_DB_CONNECTION_STRING
DEV_JWT_SECRET_KEY, STAGE_JWT_SECRET_KEY, PROD_JWT_SECRET_KEY
DEV_STRIPE_SECRET_KEY, STAGE_STRIPE_SECRET_KEY, PROD_STRIPE_SECRET_KEY
# ... and many more environment-specific secrets
```

## ✅ **Benefits of This Structure**

1. **Security**: No secrets in repository code
2. **Automation**: CI/CD handles all deployments
3. **Environment Management**: Easy to manage dev/stage/prod
4. **Separation of Concerns**: Apps vs Infrastructure
5. **Team Collaboration**: Different permissions for different repos
6. **Maintainability**: Clear structure and documentation
