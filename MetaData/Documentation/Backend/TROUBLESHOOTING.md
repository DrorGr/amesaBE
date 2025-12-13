# Troubleshooting Guide - AmesaBE

This guide covers common issues and their solutions for the AmesaBE backend.

---

## Table of Contents
1. [Deployment Issues](#deployment-issues)
2. [Database Connection Issues](#database-connection-issues)
3. [ECS & Docker Issues](#ecs--docker-issues)
4. [API & Application Issues](#api--application-issues)
5. [Authentication Issues](#authentication-issues)
6. [GitHub Actions Issues](#github-actions-issues)
7. [Local Development Issues](#local-development-issues)

---

## Deployment Issues

### Issue: GitHub Actions Workflow Fails

**Symptoms:**
- Workflow shows red X in GitHub Actions
- Deployment not completing

**Common Causes & Solutions:**

1. **Missing GitHub Secrets**
   ```bash
   # Check if all secrets are configured
   gh secret list
   
   # Add missing secret
   gh secret set SECRET_NAME --body "value"
   ```

2. **AWS Credentials Invalid**
   ```bash
   # Test AWS credentials locally
   aws sts get-caller-identity --region eu-north-1
   
   # Update credentials in GitHub Secrets
   gh secret set AWS_ACCESS_KEY_ID
   gh secret set AWS_SECRET_ACCESS_KEY
   ```

3. **Build or Test Failures**
   ```bash
   # Run locally to identify issue
   dotnet build
   dotnet test
   
   # Check specific error in GitHub Actions logs
   ```

---

### Issue: ECS Service Not Updating

**Symptoms:**
- Deployment succeeds but service shows old image
- Tasks not restarting

**Solutions:**

1. **Force New Deployment**
   ```bash
   aws ecs update-service \
     --cluster Amesa \
     --service amesa-backend-service \
     --force-new-deployment \
     --region eu-north-1
   ```

2. **Check Task Definition**
   ```bash
   # Get current task definition
   aws ecs describe-services \
     --cluster Amesa \
     --services amesa-backend-service \
     --region eu-north-1
   
   # Verify image tag is correct
   aws ecs describe-task-definition \
     --task-definition amesa-backend-task
   ```

3. **Verify ECR Image Exists**
   ```bash
   aws ecr describe-images \
     --repository-name amesabe \
     --region eu-north-1
   ```

---

## Database Connection Issues

### Issue: Cannot Connect to Aurora PostgreSQL

**Symptoms:**
- Application logs show connection timeout
- Health check fails with database error
- Error: "An error occurred using the connection to database"

**Solutions:**

1. **Verify Connection String**
   ```csharp
   // Check appsettings.json or environment variables
   // Format should be:
   "Host=amesadbmain.cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=postgres;Password=xxx;Port=5432"
   ```

2. **Check Database Cluster Status**
   ```bash
   aws rds describe-db-clusters \
     --db-cluster-identifier amesadbmain \
     --region eu-north-1
   ```

3. **Verify Security Groups**
   - Ensure ECS security group can access RDS security group
   - Port 5432 must be open between ECS and RDS
   - Check VPC and subnet configurations

4. **Test Connection**
   ```bash
   # From within ECS task or EC2 in same VPC
   psql -h amesadbmain.cruuae28ob7m.eu-north-1.rds.amazonaws.com \
        -U postgres \
        -d amesa_lottery \
        -p 5432
   ```

---

### Issue: Authentication Failed - Database

**Symptoms:**
- Error: "Routine: auth_failed"
- Error: "password authentication failed for user"

**Solutions:**

1. **Reset Database Password**
   ```bash
   aws rds modify-db-cluster \
     --db-cluster-identifier amesadbmain \
     --master-user-password NewPassword \
     --apply-immediately \
     --region eu-north-1
   ```

2. **Update Connection String in Secrets**
   - Update GitHub Secrets with new password
   - Update AWS Secrets Manager if used
   - Redeploy application

3. **Verify Username**
   - Default username is usually `postgres`
   - Check if username was changed during setup

---

### Issue: Migration Failures

**Symptoms:**
- Error during database migration
- Schema not updating

**Solutions:**

1. **Manual Migration**
   ```bash
   # Apply migrations manually
   dotnet ef database update --project AmesaBackend
   
   # Or specific migration
   dotnet ef database update MigrationName --project AmesaBackend
   ```

2. **Generate SQL Script**
   ```bash
   # Generate script to review changes
   dotnet ef migrations script --project AmesaBackend --output migration.sql
   
   # Apply manually via psql
   psql -h HOST -U postgres -d amesa_lottery -f migration.sql
   ```

3. **Rollback Migration**
   ```bash
   # Rollback to previous migration
   dotnet ef database update PreviousMigrationName --project AmesaBackend
   ```

---

## ECS & Docker Issues

### Issue: ECS Tasks Failing Health Checks

**Symptoms:**
- Tasks start but then stop
- "Task failed ELB health checks" in events

**Solutions:**

1. **Verify Health Check Endpoint**
   ```bash
   # Test health endpoint locally
   curl http://localhost:8080/health
   
   # Should return 200 OK with "Healthy" response
   ```

2. **Check Health Check Configuration**
   - ECS task definition health check path: `/health`
   - Interval: 30 seconds
   - Timeout: 5 seconds
   - Healthy threshold: 2
   - Unhealthy threshold: 3

3. **Review CloudWatch Logs**
   ```bash
   aws logs tail /ecs/amesa-backend --follow --region eu-north-1
   ```

4. **Increase Health Check Grace Period**
   - Give application more time to start
   - Default: 60 seconds, try 120 seconds

---

### Issue: Docker Build Fails

**Symptoms:**
- Error during `docker build` step
- GitHub Actions Docker build fails

**Common Solutions:**

1. **Missing Files**
   - Ensure Dockerfile is in `AmesaBackend/` directory
   - Check `.dockerignore` isn't excluding necessary files

2. **Base Image Issues**
   ```dockerfile
   # Ensure using correct .NET 8.0 images
   FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
   FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
   ```

3. **Build Context**
   ```bash
   # Build from correct directory
   docker build -t amesa-backend:dev ./AmesaBackend
   
   # Not from root of repo
   ```

4. **Dependency Restore Failures**
   ```bash
   # Restore locally first to identify issue
   dotnet restore AmesaBackend/AmesaBackend.csproj
   ```

---

### Issue: Container Out of Memory

**Symptoms:**
- Container exits with status 137
- ECS task stopped (OutOfMemoryError)

**Solutions:**

1. **Increase Task Memory**
   - Edit ECS task definition
   - Increase memory allocation (e.g., 1024 → 2048 MB)

2. **Optimize Application**
   - Review memory-intensive operations
   - Implement proper disposal of resources
   - Use memory profiling tools

3. **Add Memory Limits in Docker**
   ```bash
   docker run --memory="2g" amesa-backend:dev
   ```

---

## API & Application Issues

### Issue: API Returns 500 Internal Server Error

**Symptoms:**
- API endpoints returning 500 errors
- Application crashes or exceptions

**Solutions:**

1. **Check CloudWatch Logs**
   ```bash
   aws logs tail /ecs/amesa-backend --follow --region eu-north-1
   ```

2. **Enable Detailed Logging**
   ```json
   // In appsettings.json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.AspNetCore": "Debug"
       }
     }
   }
   ```

3. **Test Locally**
   ```bash
   dotnet run --project AmesaBackend
   # Make API request to identify issue
   ```

4. **Check Database Connection**
   - Often cause of 500 errors
   - Verify connection string and database availability

---

### Issue: SignalR Connection Failures

**Symptoms:**
- WebSocket connections not establishing
- Real-time updates not working

**Solutions:**

1. **Enable WebSocket in ALB**
   - Ensure ALB target group has WebSocket support enabled
   - Check ALB settings in AWS Console

2. **Check CORS Configuration**
   ```csharp
   // In Program.cs
   builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowFrontend",
           builder => builder
               .WithOrigins("https://yourfrontend.cloudfront.net")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
   });
   ```

3. **Test SignalR Connection**
   ```javascript
   // From frontend
   const connection = new signalR.HubConnectionBuilder()
       .withUrl("http://your-alb/lotteryHub")
       .build();
   
   connection.start().catch(err => console.error(err));
   ```

---

### Issue: API Endpoints Not Found (404)

**Symptoms:**
- All API requests return 404
- Swagger UI not accessible

**Solutions:**

1. **Verify Base URL**
   - Correct: `http://alb-address/api/v1/houses`
   - Incorrect: `http://alb-address/houses`

2. **Check Route Configuration**
   ```csharp
   // Controllers should have [ApiController] and [Route] attributes
   [ApiController]
   [Route("api/v1/[controller]")]
   public class HousesController : ControllerBase
   ```

3. **Verify Application is Running**
   ```bash
   # Check health endpoint first
   curl http://your-alb/health
   ```

---

## Authentication Issues

### Issue: JWT Token Validation Fails

**Symptoms:**
- 401 Unauthorized on protected endpoints
- "Token validation failed" in logs

**Solutions:**

1. **Verify JWT Configuration**
   ```csharp
   // Check appsettings.json
   {
     "Jwt": {
       "SecretKey": "your-secret-key-at-least-32-characters-long",
       "Issuer": "AmesaLottery",
       "Audience": "AmesaLotteryUsers",
       "ExpirationMinutes": 15
     }
   }
   ```

2. **Check Token Format**
   - Header should include: `Authorization: Bearer <token>`
   - Token should be valid JWT format

3. **Verify Secret Key**
   - Ensure same secret key is used for signing and validation
   - Check environment-specific secrets in GitHub

4. **Test Token Generation**
   ```bash
   # Login endpoint should return valid token
   curl -X POST http://your-alb/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email":"test@example.com","password":"password"}'
   ```

---

## GitHub Actions Issues

### Issue: AWS ECR Login Fails

**Symptoms:**
- Error: "Unable to locate credentials"
- ECR push step fails

**Solutions:**

1. **Verify AWS Credentials**
   ```yaml
   # In workflow file
   - name: Configure AWS credentials
     uses: aws-actions/configure-aws-credentials@v4
     with:
       aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
       aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
       aws-region: eu-north-1
   ```

2. **Check IAM Permissions**
   - User needs ECR push permissions
   - User needs ECS update permissions

3. **Test Credentials Locally**
   ```bash
   aws ecr get-login-password --region eu-north-1
   ```

---

### Issue: Workflow Not Triggering

**Symptoms:**
- Push to branch doesn't trigger workflow
- Workflow not appearing in Actions tab

**Solutions:**

1. **Verify Branch Name**
   ```yaml
   # Check workflow trigger configuration
   on:
     push:
       branches: [dev, stage]  # Must match your branch names exactly
   ```

2. **Check Workflow File Location**
   - Must be in `.github/workflows/` directory
   - File must have `.yml` or `.yaml` extension

3. **Validate YAML Syntax**
   ```bash
   # Use online YAML validator or
   yamllint .github/workflows/deploy.yml
   ```

---

## Local Development Issues

### Issue: Cannot Run Application Locally

**Symptoms:**
- Error when running `dotnet run`
- Application fails to start

**Solutions:**

1. **Install Dependencies**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Check .NET Version**
   ```bash
   dotnet --version
   # Should be 8.0.x or higher
   ```

3. **Use Development Configuration**
   ```bash
   # Application uses SQLite in development
   dotnet run --project AmesaBackend --environment Development
   ```

4. **Database Setup**
   ```bash
   # Create and seed database
   dotnet ef database update --project AmesaBackend
   dotnet run --project AmesaBackend -- --seeder
   ```

---

### Issue: Port Already in Use

**Symptoms:**
- Error: "Address already in use"
- Cannot bind to port 5000 or 8080

**Solutions:**

1. **Find Process Using Port**
   ```bash
   # Windows
   netstat -ano | findstr :8080
   taskkill /PID <PID> /F
   
   # Linux/Mac
   lsof -i :8080
   kill -9 <PID>
   ```

2. **Use Different Port**
   ```bash
   dotnet run --project AmesaBackend --urls="http://localhost:5001"
   ```

---

### Issue: Environment Variables Not Loading

**Symptoms:**
- Configuration values are null or default
- Cannot connect to services

**Solutions:**

1. **Use User Secrets (Development)**
   ```bash
   # Initialize user secrets
   dotnet user-secrets init --project AmesaBackend
   
   # Set secret
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string" --project AmesaBackend
   ```

2. **Create appsettings.Development.json**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=amesa.db"
     },
     "Jwt": {
       "SecretKey": "development-secret-key-at-least-32-characters-long"
     }
   }
   ```

3. **Set Environment Variables**
   ```bash
   # Windows PowerShell
   $env:ConnectionStrings__DefaultConnection="your-connection-string"
   
   # Linux/Mac
   export ConnectionStrings__DefaultConnection="your-connection-string"
   ```

---

## Monitoring & Debugging

### Useful Commands

**Check ECS Service Status:**
```bash
aws ecs describe-services \
  --cluster Amesa \
  --services amesa-backend-service \
  --region eu-north-1
```

**View Application Logs:**
```bash
aws logs tail /ecs/amesa-backend --follow --region eu-north-1
```

**List Running Tasks:**
```bash
aws ecs list-tasks --cluster Amesa --region eu-north-1
```

**Test API Health:**
```bash
curl http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/health
```

**Check Database Cluster:**
```bash
aws rds describe-db-clusters \
  --db-cluster-identifier amesadbmain \
  --region eu-north-1
```

**View ECR Images:**
```bash
aws ecr describe-images \
  --repository-name amesabe \
  --region eu-north-1
```

---

## Getting Help

If you've tried the solutions above and still have issues:

1. **Check CloudWatch Logs** - Most issues leave traces in logs
2. **Review GitHub Actions Logs** - Detailed deployment information
3. **Test Locally First** - Easier to debug than in AWS
4. **Verify AWS Resource Status** - Ensure all services are running
5. **Check Context Files** - Review recent changes in documentation

### Support Resources:
- **AWS Documentation**: https://docs.aws.amazon.com
- **.NET Documentation**: https://docs.microsoft.com/dotnet
- **GitHub Actions**: https://docs.github.com/actions
- **Repository Issues**: https://github.com/DrorGr/amesaBE/issues

---

**Last Updated**: 2025-10-10

