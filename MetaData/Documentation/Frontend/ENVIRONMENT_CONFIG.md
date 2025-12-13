# Environment Configuration Strategy

## Configuration Philosophy
**Single Codebase, External Configuration**: Use the same code for all environments (dev/test/prod) with external environment-specific configuration injected at runtime.

## CLI Tools Available
- **AWS CLI**: AWS resource management and deployment
- **Angular CLI**: Frontend development and building
- **GitHub CLI**: Repository management and secrets

## External Configuration Sources

### Frontend (AmesaFE)
- **Environment Files**: `src/environments/` directory
- **Runtime Configuration**: Environment variables injected during build
- **Build-time Configuration**: Different configs per environment build

### Backend (AmesaBE)
- **Environment Variables**: Injected via ECS task definitions
- **Configuration Files**: External config files mounted to containers
- **AWS Parameter Store**: Runtime configuration from AWS
- **Secrets Manager**: Secure configuration values

### DevOps (AmesaDevOps)
- **Terraform Variables**: Environment-specific terraform.tfvars
- **CloudFormation Parameters**: Environment-specific parameter files
- **GitHub Secrets**: Environment-specific secret values

## Configuration Management

### Frontend Configuration
```typescript
// src/environments/environment.ts (base)
export const environment = {
  production: false,
  apiUrl: process.env['API_URL'] || 'http://localhost:3000',
  databaseUrl: process.env['DATABASE_URL'] || 'localhost:5432'
};

// src/environments/environment.prod.ts
export const environment = {
  production: true,
  apiUrl: process.env['API_URL'],
  databaseUrl: process.env['DATABASE_URL']
};
```

### Backend Configuration
```csharp
// appsettings.json (base configuration)
{
  "ConnectionStrings": {
    "DefaultConnection": "${DATABASE_CONNECTION_STRING}"
  },
  "AWS": {
    "Region": "${AWS_REGION}",
    "S3Bucket": "${S3_BUCKET_NAME}"
  }
}
```

### ECS Task Definition
```json
{
  "environment": [
    {
      "name": "DATABASE_CONNECTION_STRING",
      "value": "${AURORA_ENDPOINT}"
    },
    {
      "name": "AWS_REGION",
      "value": "${AWS_REGION}"
    }
  ]
}
```

## Environment-Specific Deployment

### Development
```bash
# Frontend
ng build --configuration=development
# Uses environment.dev.ts

# Backend
docker build --build-arg ENV=dev -t amesa-backend:dev .
# Injects dev environment variables
```

### Test
```bash
# Frontend
ng build --configuration=test
# Uses environment.test.ts

# Backend
docker build --build-arg ENV=test -t amesa-backend:test .
# Injects test environment variables
```

### Production
```bash
# Frontend
ng build --configuration=production
# Uses environment.prod.ts

# Backend
docker build --build-arg ENV=prod -t amesa-backend:prod .
# Injects production environment variables
```

## CLI Commands for Configuration

### AWS CLI
```bash
# Get environment-specific parameters
aws ssm get-parameter --name "/amesa/dev/database-url"
aws ssm get-parameter --name "/amesa/prod/database-url"

# Update configuration
aws ssm put-parameter --name "/amesa/dev/api-url" --value "https://dev-api.amesa.com"
```

### Angular CLI
```bash
# Build with environment-specific config
ng build --configuration=development
ng build --configuration=test
ng build --configuration=production

# Serve with environment config
ng serve --configuration=development
```

### GitHub CLI
```bash
# Set environment-specific secrets
gh secret set DEV_DATABASE_URL --body "dev-db-url"
gh secret set PROD_DATABASE_URL --body "prod-db-url"

# List secrets by environment
gh secret list
```

## Configuration Injection Points

### Build Time (Frontend)
- Angular build process injects environment variables
- Different build configurations per environment
- Static configuration baked into build artifacts

### Runtime (Backend)
- ECS task definitions inject environment variables
- AWS Parameter Store provides runtime configuration
- Secrets Manager provides secure configuration values

### Deployment Time
- GitHub Actions workflows inject environment-specific values
- Terraform/CloudFormation applies environment-specific parameters
- Docker builds with environment-specific build args

## Benefits of External Configuration
- **Single Codebase**: Same code runs in all environments
- **Environment Isolation**: No environment-specific code paths
- **Security**: Sensitive configs not in code
- **Flexibility**: Easy to change configs without code changes
- **Consistency**: Same deployment process across environments

## Configuration Files Structure
```
AmesaFE/
├── src/environments/
│   ├── environment.ts          # Base config
│   ├── environment.dev.ts      # Development config
│   ├── environment.test.ts     # Test config
│   └── environment.prod.ts     # Production config

AmesaBE/
├── appsettings.json            # Base config
├── appsettings.Development.json
├── appsettings.Test.json
└── appsettings.Production.json

AmesaDevOps/
├── environments/
│   ├── dev/
│   │   └── terraform.tfvars
│   ├── test/
│   │   └── terraform.tfvars
│   └── prod/
│       └── terraform.tfvars
```
