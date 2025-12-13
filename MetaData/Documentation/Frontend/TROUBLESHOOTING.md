# Troubleshooting Guide

## Recently Resolved Issues ✅

### CloudFront 404 Errors - RESOLVED 2025-10-09
**Problem**: 404 errors for index.html on stage and prod environments
**Root Cause**: CloudFront distributions missing `/browser` origin path configuration
**Solution**: 
```bash
# Update CloudFront distribution with correct origin path
aws cloudfront update-distribution --id [DISTRIBUTION_ID] --distribution-config file://config.json
# Invalidate cache to serve fresh content
aws cloudfront create-invalidation --distribution-id [DISTRIBUTION_ID] --paths "/*"
```
**Status**: ✅ RESOLVED - All environments now working

## Common Issues & Solutions

### Build Issues
**Problem**: Angular build fails
**Solution**: 
```bash
npm install
ng build --configuration=production
```

**Problem**: TypeScript compilation errors
**Solution**: Check `tsconfig.json` and ensure all imports are correct

### Deployment Issues
**Problem**: CloudFront not updating
**Solution**: Check CloudFront invalidation and distribution status

**Problem**: S3 deployment fails
**Solution**: Verify AWS credentials and bucket permissions

**Problem**: 404 errors for index.html
**Solution**: Verify CloudFront origin path configuration matches S3 bucket structure

### Development Issues
**Problem**: Angular serve not working
**Solution**:
```bash
ng serve --port 4200
```

**Problem**: Dependencies out of sync
**Solution**:
```bash
npm install
npm audit fix
```

## Environment-Specific Issues

### Development Environment
- Check `src/environments/environment.dev.ts`
- Verify local development server configuration

### Staging Environment
- Check `src/environments/environment.stage.ts`
- Verify staging deployment scripts

### Production Environment
- Check `src/environments/environment.prod.ts`
- Verify production deployment configuration

## Git Issues
**Problem**: Branch conflicts
**Solution**: 
```bash
git status
git pull origin Testing
git merge origin/main
```

## Performance Issues
- Check bundle size with `ng build --stats-json`
- Analyze with webpack-bundle-analyzer
- Review lazy loading implementation

## Debugging Tips
1. Check browser console for errors
2. Verify network requests in DevTools
3. Check Angular CLI output for build warnings
4. Review server logs for backend issues

## Useful Commands
```bash
# Check project status
git status
git log --oneline -10

# Build and serve
ng build
ng serve

# Test
ng test

# Lint
ng lint
```

## Contact Information
[Add any relevant contact info for team members or support]
