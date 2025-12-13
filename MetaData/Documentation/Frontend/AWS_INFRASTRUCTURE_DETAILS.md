# AWS Infrastructure Details - Live Configuration

## Overview
- **Region**: eu-north-1
- **Account**: 129394705401
- **Last Updated**: October 9, 2025
- **Status**: ALL ENVIRONMENTS OPERATIONAL ✅

## CloudFront Distributions (5 Active)

### Frontend Distributions
1. **Development Frontend** ✅
   - **ID**: E2XBDFAUZJTI59
   - **Domain**: `d2rmamd755wq7j.cloudfront.net`
   - **Origin**: `amesa-frontend-dev.s3.amazonaws.com`
   - **Origin Path**: `/browser`
   - **Status**: Enabled & Working (200 OK)
   - **Content Size**: 6,484 bytes
   - **Last Modified**: October 9, 2025, 12:54 PM GMT+3

2. **Production Frontend** ✅
   - **ID**: E3GU3QXUR43ZOH
   - **Domain**: `dpqbvdgnenckf.cloudfront.net`
   - **Origin**: `amesa-frontend-prod.s3-website.eu-north-1.amazonaws.com`
   - **Origin Path**: `/browser` (Fixed 2025-10-09)
   - **Status**: Enabled & Working (200 OK)
   - **Content Size**: 20,990 bytes
   - **Last Modified**: October 9, 2025, 12:54 PM GMT+3

3. **Staging Frontend** ✅
   - **ID**: E1D7XQHFF1469W
   - **Domain**: `d2ejqzjfslo5hs.cloudfront.net`
   - **Origin**: `amesa-frontend-stage.s3-website.eu-north-1.amazonaws.com`
   - **Origin Path**: `/browser` (Fixed 2025-10-09)
   - **Status**: Enabled & Working (200 OK)
   - **Content Size**: 20,990 bytes
   - **Last Modified**: October 9, 2025, 12:54 PM GMT+3

4. **Static Assets with OAC**
   - **ID**: E12EBI6VC526PS
   - **Domain**: `d3bkt41uo2lxir.cloudfront.net`
   - **Origin**: `amesa-frontend-static.s3.amazonaws.com`
   - **Status**: Enabled
   - **Security**: Origin Access Control (OAC)
   - **Last Modified**: October 7, 2025, 05:25 PM GMT+3

### Backend API Distribution
5. **Backend API**
   - **ID**: EJY57DPCK1LYS
   - **Domain**: `dgfh2mwcqhlbw.cloudfront.net`
   - **Origin**: `amesa-backend-alb-50` (Application Load Balancer)
   - **Status**: Enabled
   - **Last Modified**: October 7, 2025, 08:51 PM GMT+3

## ECS Cluster - "Amesa"

### Cluster Details
- **ARN**: `arn:aws:ecs:eu-north-1:129394705401:cluster/Amesa`
- **Status**: Active
- **Region**: eu-north-1
- **Services**: 2 active
- **Tasks**: 2 running
- **Launch Type**: Fargate (serverless)

### ECS Services
1. **amesa-backend-service** (Production)
   - **Task Definition**: `amesa-backend:1`
   - **Status**: Active
   - **Scheduling Strategy**: REPLICA
   - **Launch Type**: FARGATE
   - **Tasks**: 1/1 running
   - **Created**: 2 days ago
   - **Last Deployment**: In progress

2. **amesa-backend-stage-service** (Staging)
   - **Task Definition**: `amesa-backend-staging:4`
   - **Status**: Active
   - **Scheduling Strategy**: REPLICA
   - **Launch Type**: FARGATE
   - **Tasks**: 1/1 running
   - **Created**: 17 hours ago
   - **Last Deployment**: In progress

## Aurora PostgreSQL Databases

### Production Environment
1. **amesadbmain** (Regional Cluster)
   - **Status**: Available
   - **Role**: Regional cluster
   - **Engine**: Aurora PostgreSQL
   - **Region**: eu-north-1
   - **Size**: 1 instance

2. **amesadbmain1** (Writer Instance)
   - **Status**: Available
   - **Role**: Writer instance
   - **Engine**: Aurora PostgreSQL
   - **Region**: eu-north-1c
   - **Size**: Serverless v2 (0.5 - 128 ACU)
   - **CPU Usage**: 0.41%
   - **VPC**: vpc-0faee...
   - **Multi-AZ**: No

### Staging Environment
3. **amesadbmain-stage** (Regional Cluster)
   - **Status**: Available
   - **Role**: Regional cluster
   - **Engine**: Aurora PostgreSQL
   - **Region**: eu-north-1
   - **Size**: 1 instance

4. **amesadbmain1-stage** (Writer Instance)
   - **Status**: Available
   - **Role**: Writer instance
   - **Engine**: Aurora PostgreSQL
   - **Region**: eu-north-1c
   - **Size**: Serverless v2 (0.5 - 128 ACU)
   - **CPU Usage**: 0.40%
   - **Current Activity**: 2 Connections
   - **VPC**: vpc-0faee...
   - **Multi-AZ**: No

## S3 Buckets (Inferred from CloudFront Origins)
- **amesa-frontend-dev**: Development frontend hosting
- **amesa-frontend-prod**: Production frontend hosting
- **amesa-frontend-stage**: Staging frontend hosting
- **amesa-frontend-static**: Static assets with OAC

## Application Load Balancer
- **Name**: amesa-backend-alb-50
- **Purpose**: Backend API load balancing
- **Integration**: CloudFront distribution EJY57DPCK1LYS

## Recent Infrastructure Updates - 2025-10-09 ✅

### CloudFront Configuration Fixes
**Issue Resolved**: 404 errors for index.html on stage and prod environments
**Root Cause**: Missing `/browser` origin path configuration
**Solution Applied**:
- Updated Staging Distribution (E1D7XQHFF1469W): Added `OriginPath: "/browser"`
- Updated Production Distribution (E3GU3QXUR43ZOH): Added `OriginPath: "/browser"`
- Invalidated all CloudFront caches to serve fresh content
- Verified all environments now return 200 OK responses

### Current Status
- **All Frontend Environments**: Operational and serving content
- **S3 Buckets**: All populated with latest Angular builds
- **CloudFront Distributions**: All properly configured with correct origin paths
- **Cache Status**: Fresh content being served after invalidation

## Key Infrastructure Features
- **Serverless Architecture**: ECS Fargate + Aurora Serverless v2
- **Multi-Environment**: Separate prod/stage/dev environments
- **CDN Distribution**: 5 CloudFront distributions for global performance
- **Security**: Origin Access Control (OAC) for S3 origins
- **Monitoring**: CloudWatch Container Insights enabled
- **Auto-scaling**: Aurora Serverless v2 (0.5 - 128 ACU range)
- **Origin Path Configuration**: Properly configured for Angular build structure
