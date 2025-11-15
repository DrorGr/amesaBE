# GitHub Actions Workflows

## Build and Deploy Workflow

The `build-and-deploy.yml` workflow automates the Docker build and ECS deployment process, matching the local build script (`BE/Infrastructure/build-and-push-all-services.ps1`).

### Key Features

1. **Builds from BE directory**: All Docker builds use the BE directory as the build context (required because Dockerfiles reference `AmesaBackend.Shared`)
2. **Uses correct Dockerfile paths**: Each service uses the `-f` flag to specify its Dockerfile path
3. **Parallel builds**: Uses GitHub Actions matrix strategy to build all 8 services in parallel
4. **Tags images**: Pushes both `:latest` and commit SHA tags for versioning
5. **Auto-deploys**: Automatically triggers ECS service updates after successful builds

### Services Built

- `amesa-auth-service` → `AmesaBackend.Auth/Dockerfile`
- `amesa-content-service` → `AmesaBackend.Content/Dockerfile`
- `amesa-notification-service` → `AmesaBackend.Notification/Dockerfile`
- `amesa-payment-service` → `AmesaBackend.Payment/Dockerfile`
- `amesa-lottery-service` → `AmesaBackend.Lottery/Dockerfile`
- `amesa-lottery-results-service` → `AmesaBackend.LotteryResults/Dockerfile`
- `amesa-analytics-service` → `AmesaBackend.Analytics/Dockerfile`
- `amesa-admin-service` → `AmesaBackend.Admin/Dockerfile`

### Triggers

- **Push to main**: Automatically runs when code is pushed to the `main` branch (only if files in `AmesaBackend.**` change)
- **Manual trigger**: Can be manually triggered via GitHub Actions UI (`workflow_dispatch`)

### Required GitHub Secrets

The workflow requires these secrets to be configured in GitHub:

- `AWS_ACCESS_KEY_ID`: AWS access key with permissions for ECR and ECS
- `AWS_SECRET_ACCESS_KEY`: AWS secret access key

### Workflow Steps

1. **Checkout**: Gets the latest code
2. **Configure AWS**: Sets up AWS credentials using GitHub secrets
3. **Login to ECR**: Authenticates with Amazon ECR
4. **Build**: Builds Docker images for all services in parallel
5. **Push**: Pushes images to ECR with `:latest` and commit SHA tags
6. **Deploy**: Forces new ECS deployments for all services (only on main branch)

### Matching Local Build Process

This workflow matches the local PowerShell script:

**Local Script:**
```powershell
docker build -f $s.dockerfile -t $image .
docker push $image
```

**GitHub Workflow:**
```yaml
docker build -f ${{ matrix.dockerfile }} -t ${{ env.ECR_ROOT }}/${{ matrix.service }}:latest .
docker push ${{ env.ECR_ROOT }}/${{ matrix.service }}:latest
```

Both:
- Build from the BE directory (working-directory: `.`)
- Use `-f` flag to specify Dockerfile path
- Build context includes `AmesaBackend.Shared` (required for all services)
- Handle Admin service's special case (conflicting appsettings.json files are handled by the Dockerfile)

### Admin Service Special Handling

The Admin service Dockerfile includes cleanup of conflicting `appsettings.json` files from the Auth project reference:

```dockerfile
RUN dotnet publish "AmesaBackend.Admin/AmesaBackend.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false && \
    rm -f /app/publish/appsettings.json /app/publish/appsettings.*.json 2>/dev/null || true
```

This is already handled in the Dockerfile, so the workflow doesn't need special handling.

### Environment Variables

- `AWS_REGION`: `eu-north-1`
- `AWS_ACCOUNT_ID`: `129394705401`
- `ECR_ROOT`: `129394705401.dkr.ecr.eu-north-1.amazonaws.com`
- `ECS_CLUSTER`: `Amesa`

### Monitoring

After the workflow runs:
1. Check GitHub Actions tab for build status
2. Verify images in ECR: `aws ecr list-images --repository-name <service-name> --region eu-north-1`
3. Check ECS console to see services updating with new task definitions
4. Monitor ECS service events for deployment progress

