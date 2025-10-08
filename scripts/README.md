# Deployment Scripts

This directory contains deployment-related scripts and configurations.

## Local Development

### Using Docker Compose
```bash
# Start the application with dependencies
docker-compose -f docker-compose.dev.yml up

# Build and start in production mode
docker-compose up --build
```

### Manual Database Setup
```bash
# Create database (adjust connection string as needed)
psql -h localhost -U postgres -f create-database.sql

# Apply schema
psql -h localhost -U postgres -d amesa_lottery -f database-schema.sql
```

## Production Deployment

For production deployments, use the CI/CD pipeline configured in `.github/workflows/deploy.yml`.

### Environment Variables Required

Set these environment variables in your deployment environment:

```bash
# Database
DB_CONNECTION_STRING=Host=your-host;Database=amesa_lottery;Username=your-user;Password=your-password;Port=5432

# Redis
REDIS_CONNECTION_STRING=your-redis-host:6379

# JWT
JWT_SECRET_KEY=your-super-secret-key-at-least-32-characters-long

# Email
SMTP_SERVER=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password

# Payment
STRIPE_SECRET_KEY=sk_live_your_stripe_secret_key

# AWS
AWS_ACCESS_KEY_ID=your-aws-access-key
AWS_SECRET_ACCESS_KEY=your-aws-secret-key
AWS_REGION=us-east-1
```

## Scripts

- `deploy-template.sh` - Template deployment script
- `setup-database.sql` - Database setup scripts
- `docker-compose.yml` - Production Docker configuration
- `docker-compose.dev.yml` - Development Docker configuration

## Infrastructure

For AWS infrastructure deployment, see the separate `amesaDevOps` repository which contains:
- Terraform/CloudFormation templates
- ECS task definitions
- Load balancer configurations
- Security group settings
