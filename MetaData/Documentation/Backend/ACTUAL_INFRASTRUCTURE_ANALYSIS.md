# 🔍 Actual AWS Infrastructure Analysis

## Infrastructure Discovered via AWS CLI

### **ECS Cluster: "Amesa"**
- **Region**: eu-north-1
- **Launch Type**: Fargate
- **Services**: 2 active services

#### **Service 1: amesa-backend-stage-service**
- **Purpose**: Handles both Development AND Staging traffic
- **Load Balancer**: amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com
- **Target Group**: amesa-backend-stage-tg
- **Container**: amesa-backend-stage:8080
- **Task Definition**: amesa-backend-staging:10
- **Status**: ACTIVE, 1 running task
- **Database**: amesadbmain-stage cluster

#### **Service 2: amesa-backend-service**
- **Purpose**: Production environment
- **Load Balancer**: amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com
- **Target Group**: amesa-backend-tg
- **Status**: ACTIVE, 1 running task
- **Database**: amesadbmain cluster

### **Load Balancers**
1. **Staging/Dev ALB**: amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com
2. **Production ALB**: amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com

### **Database Clusters**
1. **Staging/Dev**: amesadbmain-stage.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com
2. **Production**: amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com

### **ECR Repository**
- **Repository**: amesabe
- **URI**: 129394705401.dkr.ecr.eu-north-1.amazonaws.com/amesabe
- **Latest Image**: sha256:82d02faf6c03b6b9b36515aa53fa5356f1f3d4fbd6bb3733bb5e94779f578ef8 (latest)

## Admin Panel Access Strategy (Corrected)

### **Development Environment**
- **Frontend**: https://d2rmamd755wq7j.cloudfront.net/admin
- **Backend**: amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com
- **Database**: amesadbmain-stage
- **ECS Service**: amesa-backend-stage-service
- **Note**: ⚠️ **Shares infrastructure with staging**

### **Staging Environment**
- **Frontend**: https://d2ejqzjfslo5hs.cloudfront.net/admin
- **Backend**: amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com (same as dev)
- **Database**: amesadbmain-stage (same as dev)
- **ECS Service**: amesa-backend-stage-service (same as dev)
- **Note**: ⚠️ **Shares infrastructure with development**

### **Production Environment**
- **Frontend**: https://dpqbvdgnenckf.cloudfront.net/admin
- **Backend**: amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com
- **Database**: amesadbmain
- **ECS Service**: amesa-backend-service
- **Note**: ✅ **Completely isolated from dev/staging**

## GitHub Secrets Configuration (Corrected)

### **Development & Staging (Shared Infrastructure)**
```bash
# These will be used by the amesa-backend-stage-service
DEV_ECS_CLUSTER=Amesa
DEV_ECS_SERVICE=amesa-backend-stage-service
DEV_DB_CONNECTION_STRING=Host=amesadbmain-stage.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=postgres;Password=your-stage-password;Port=5432;
DEV_ADMIN_EMAIL=admin@amesa.com
DEV_ADMIN_PASSWORD=DevStageAdminPassword123!

# Note: STAGE_* secrets will point to the same infrastructure as DEV_*
STAGE_ECS_CLUSTER=Amesa
STAGE_ECS_SERVICE=amesa-backend-stage-service
STAGE_DB_CONNECTION_STRING=Host=amesadbmain-stage.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=postgres;Password=your-stage-password;Port=5432;
STAGE_ADMIN_EMAIL=admin@amesa.com
STAGE_ADMIN_PASSWORD=DevStageAdminPassword123!
```

### **Production (Separate Infrastructure)**
```bash
PROD_ECS_CLUSTER=Amesa
PROD_ECS_SERVICE=amesa-backend-service
PROD_DB_CONNECTION_STRING=Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Database=amesa_lottery;Username=dror;Password=your-prod-password;Port=5432;
PROD_ADMIN_EMAIL=admin@amesa.com
PROD_ADMIN_PASSWORD=ProdAdminPassword123!
```

## Deployment Strategy

### **Development Branch → amesa-backend-stage-service**
- **ECS Service**: amesa-backend-stage-service
- **Load Balancer**: amesa-backend-stage-alb
- **Database**: amesadbmain-stage
- **Admin Panel**: Accessible via both dev and stage frontend URLs

### **Staging Branch → amesa-backend-stage-service**
- **ECS Service**: amesa-backend-stage-service (same as dev)
- **Load Balancer**: amesa-backend-stage-alb (same as dev)
- **Database**: amesadbmain-stage (same as dev)
- **Admin Panel**: Accessible via both dev and stage frontend URLs

### **Main Branch → amesa-backend-service**
- **ECS Service**: amesa-backend-service
- **Load Balancer**: amesa-backend-alb
- **Database**: amesadbmain
- **Admin Panel**: Accessible via production frontend URL

## Important Notes

1. **Dev and Stage share the same backend service** - any deployment to either branch will affect both environments
2. **Production is completely isolated** - has its own service, load balancer, and database
3. **Admin panel will be accessible** at all three frontend URLs but will connect to the appropriate backend based on the frontend environment
4. **Database switching in admin panel** will work within the same cluster (dev/stage can switch between databases in amesadbmain-stage, prod can switch between databases in amesadbmain)

## Security Implications

1. **Dev/Stage Shared Access**: Admin panel changes in dev will be visible in staging immediately
2. **Production Isolation**: Production admin panel is completely separate and secure
3. **Database Isolation**: Production database is separate from dev/stage
4. **Load Balancer Isolation**: Production has its own load balancer

---

**Last Updated**: 2025-10-11
**Source**: AWS CLI investigation of actual infrastructure
