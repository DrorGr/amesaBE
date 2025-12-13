# AmesaBase Environment URLs Reference

## 🌐 **Environment Overview**

| Environment | Frontend URL | Backend API | CloudFront ID | Status |
|-------------|--------------|-------------|---------------|--------|
| **Development** | https://d2rmamd755wq7j.cloudfront.net | amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com | E2XBDFAUZJTI59 | ✅ Active |
| **Staging** | https://d2ejqzjfslo5hs.cloudfront.net | amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com | E1D7XQHFF1469W | ✅ Active |
| **Production** | https://dpqbvdgnenckf.cloudfront.net | amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com | E3GU3QXUR43ZOH | ✅ Active |

---

## 🔗 **Direct API Endpoints**

| Environment | Health Check | Houses API | Translations API |
|-------------|--------------|------------|------------------|
| **Development** | http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/health | http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/api/v1/houses | http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/api/v1/translations/en |
| **Staging** | http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/health | http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/api/v1/houses | http://amesa-backend-stage-alb-467028641.eu-north-1.elb.amazonaws.com/api/v1/translations/en |
| **Production** | http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/health | http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/houses | http://amesa-backend-alb-509078867.eu-north-1.elb.amazonaws.com/api/v1/translations/en |

---

## 🌍 **CloudFront API Endpoints** (Recommended)

| Environment | Health Check | Houses API | Translations API |
|-------------|--------------|------------|------------------|
| **Development** | https://d2rmamd755wq7j.cloudfront.net/health | https://d2rmamd755wq7j.cloudfront.net/api/v1/houses | https://d2rmamd755wq7j.cloudfront.net/api/v1/translations/en |
| **Staging** | https://d2ejqzjfslo5hs.cloudfront.net/health | https://d2ejqzjfslo5hs.cloudfront.net/api/v1/houses | https://d2ejqzjfslo5hs.cloudfront.net/api/v1/translations/en |
| **Production** | https://dpqbvdgnenckf.cloudfront.net/health | https://dpqbvdgnenckf.cloudfront.net/api/v1/houses | https://dpqbvdgnenckf.cloudfront.net/api/v1/translations/en |

---

## 🗄️ **Database & Infrastructure**

| Environment | Database Cluster | Database Instance | ALB Name |
|-------------|------------------|-------------------|----------|
| **Development** | amesadbmain-stage | amesadbmain1-stage | amesa-backend-stage-alb |
| **Staging** | amesadbmain-stage | amesadbmain1-stage | amesa-backend-stage-alb |
| **Production** | amesadbmain | amesadbmain1 | amesa-backend-alb |

---

## 📦 **S3 Buckets**

| Environment | S3 Bucket | Website Endpoint |
|-------------|-----------|------------------|
| **Development** | amesa-frontend-dev | amesa-frontend-dev.s3-website.eu-north-1.amazonaws.com |
| **Staging** | amesa-frontend-stage | amesa-frontend-stage.s3-website.eu-north-1.amazonaws.com |
| **Production** | amesa-frontend-prod | amesa-frontend-prod.s3-website.eu-north-1.amazonaws.com |

---

## 🔧 **AWS Resources Summary**

| Resource Type | Development | Staging | Production |
|---------------|-------------|---------|------------|
| **CloudFront Distribution** | E2XBDFAUZJTI59 | E1D7XQHFF1469W | E3GU3QXUR43ZOH |
| **S3 Bucket** | amesa-frontend-dev | amesa-frontend-stage | amesa-frontend-prod |
| **Backend ALB** | amesa-backend-stage-alb-467028641 | amesa-backend-stage-alb-467028641 | amesa-backend-alb-509078867 |
| **ECS Service** | amesa-backend-service | amesa-backend-stage-service | amesa-backend-service |
| **Database** | amesadbmain-stage | amesadbmain-stage | amesadbmain |

---

## 🚀 **Quick Access Links**

### **Frontend Applications:**
- **Development**: https://d2rmamd755wq7j.cloudfront.net
- **Staging**: https://d2ejqzjfslo5hs.cloudfront.net
- **Production**: https://dpqbvdgnenckf.cloudfront.net

### **API Health Checks:**
- **Development**: https://d2rmamd755wq7j.cloudfront.net/health
- **Staging**: https://d2ejqzjfslo5hs.cloudfront.net/health
- **Production**: https://dpqbvdgnenckf.cloudfront.net/health

---

## 📝 **Notes**

- **Development & Staging** share the same backend infrastructure
- **Production** has its own separate backend infrastructure
- All environments use **CloudFront API routing** for frontend API calls
- **AWS Region**: eu-north-1
- **Account ID**: 129394705401

---

**Last Updated**: 2025-10-10  
**Status**: All environments operational ✅
