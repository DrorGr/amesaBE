# Secrets Management & Environment Configuration

## GitHub Secrets Storage
All AWS credentials and environment-specific secrets are stored in GitHub repository secrets for secure CI/CD deployment.

## Environment-Specific Databases

### Production Environment
- **Frontend**: Production S3 bucket + CloudFront distribution
- **Backend**: Production ECS cluster
- **Database**: Production Aurora PostgreSQL cluster

### Test Environment  
- **Frontend**: Test S3 bucket + CloudFront distribution
- **Backend**: Test ECS cluster
- **Database**: Test Aurora PostgreSQL cluster

### Development Environment
- **Frontend**: Development S3 bucket + CloudFront distribution
- **Backend**: Development ECS cluster
- **Database**: Development Aurora PostgreSQL cluster

## GitHub Secrets Structure - Actual Implementation

### Repository Secrets (Confirmed):
- **AWS** (Last updated: yesterday)
- **ENV** (Last updated: yesterday)

### Environment Secrets:
- Currently no environment-specific secrets configured
- "This environment has no secrets" status shown

### Environment-Specific Secrets:
Each environment (dev/test/prod) has its own set of secrets with environment prefixes:
- `DEV_AWS_ACCESS_KEY_ID`
- `TEST_AWS_ACCESS_KEY_ID`
- `PROD_AWS_ACCESS_KEY_ID`

## CI/CD Secret Usage

### Frontend (AmesaFE) Workflow:
```yaml
- name: Deploy to S3
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
    AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    S3_BUCKET: ${{ secrets.S3_BUCKET_NAME }}
    CLOUDFRONT_ID: ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }}
```

### Backend (AmesaBE) Workflow:
```yaml
- name: Deploy to ECS
  env:
    AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
    AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
    ECS_CLUSTER: ${{ secrets.ECS_CLUSTER_NAME }}
    ECS_SERVICE: ${{ secrets.ECS_SERVICE_NAME }}
    AURORA_ENDPOINT: ${{ secrets.AURORA_ENDPOINT }}
```

## Environment Separation

### Database Isolation:
- **Production**: Separate Aurora cluster for production data
- **Test**: Isolated test database for testing
- **Development**: Development database for local development

### Infrastructure Isolation:
- Each environment has separate AWS resources
- No cross-environment data sharing
- Independent scaling and configuration

## Security Best Practices

### GitHub Secrets:
- Never commit secrets to code
- Use environment-specific secret names
- Rotate secrets regularly
- Use least-privilege IAM policies

### Database Security:
- Separate database clusters per environment
- Environment-specific connection strings
- Encrypted connections (SSL/TLS)
- Regular security updates

## Common Commands

### Check GitHub Secrets:
```bash
# List repository secrets (requires GitHub CLI)
gh secret list

# Set a new secret
gh secret set SECRET_NAME --body "secret_value"
```

### Environment-Specific Deployment:
```bash
# Deploy to specific environment
gh workflow run deploy.yml -f environment=dev
gh workflow run deploy.yml -f environment=test
gh workflow run deploy.yml -f environment=prod
```

## Troubleshooting Secrets

### Common Issues:
1. **Secret not found**: Check secret name and repository
2. **Permission denied**: Verify IAM policies
3. **Environment mismatch**: Ensure correct environment secrets

### Debug Commands:
```bash
# Check workflow runs
gh run list

# View workflow logs
gh run view [run-id]

# Check secret access
gh secret list
```
