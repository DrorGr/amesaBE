# Amesa Platform
## System Architecture

---

## 🏗️ Architecture Overview

Amesa is built on a **microservices architecture** deployed on AWS, ensuring scalability, reliability, and independent service deployment.

---

## 📐 System Architecture Layers

```
┌─────────────────────────────────────────────────────────┐
│                    CLIENT LAYER                          │
│  Angular 20.2.1 → CloudFront CDN → S3 Static Hosting   │
│  • Real-time updates (SignalR)                          │
│  • Responsive design (Tailwind CSS)                     │
│  • Multi-language support                               │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│               API GATEWAY LAYER                          │
│  CloudFront → ALB (Path-based Routing)                  │
│  • SSL/TLS termination                                  │
│  • Request routing to microservices                     │
│  • Health check monitoring                              │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│           MICROSERVICES LAYER (8 Services)               │
│  ECS Fargate → Docker Containers (.NET 8.0)            │
│                                                          │
│  🔐 Auth Service          → JWT, OAuth, ID verification │
│  🎰 Lottery Service       → Houses, tickets, draws      │
│  💳 Payment Service       → Stripe integration          │
│  📬 Notification Service  → Multi-channel notifications │
│  📝 Content Service       → Translations, content       │
│  🏆 Results Service       → Draw results, QR codes      │
│  📊 Analytics Service     → User analytics, tracking    │
│  ⚙️  Admin Service         → Blazor admin panel         │
└──────────────────────┬──────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────┐
│                  DATA LAYER                              │
│  Aurora PostgreSQL Serverless v2                        │
│  • 8 schema isolation (amesa_auth, amesa_lottery, etc.) │
│  • Connection pooling (100 max, 10 min)                 │
│  Redis Cache                                            │
│  • Distributed caching                                  │
│  • Rate limiting storage                                │
└─────────────────────────────────────────────────────────┘
```

---

## 🔌 Inter-Service Communication

### **HTTP/REST**
- Service-to-service calls via ALB
- Service-to-service authentication (API key via SSM)
- Retry policies with exponential backoff (Polly)
- Circuit breaker patterns for resilience

### **Event-Driven (AWS EventBridge)**
- Async event processing
- Decoupled service communication
- Event schemas for type safety

### **Real-Time (SignalR)**
- **LotteryHub** (`/ws/lottery`) → Real-time lottery updates
- **NotificationHub** (`/ws/notifications`) → Real-time notifications
- **AdminHub** (`/admin/hub`) → Admin real-time updates
- LongPolling transport (CloudFront compatible)

---

## 🛡️ Security Architecture

| Layer | Security Measures |
|-------|-------------------|
| **Authentication** | JWT Bearer tokens, OAuth (Google, Meta), BCrypt password hashing |
| **Authorization** | Role-based access control, service-to-service auth |
| **Secrets Management** | AWS Secrets Manager (OAuth, payment keys), SSM Parameter Store |
| **API Security** | Rate limiting (Redis), CORS, security headers, account lockout |
| **Data Security** | Encryption at rest (KMS), TLS/SSL in transit, parameterized queries |

---

## ☁️ AWS Infrastructure

| Service | Usage |
|---------|-------|
| **ECS Fargate** | Container orchestration (8 microservices) |
| **ECR** | Container registry (Docker images) |
| **ALB** | Application load balancer with path-based routing |
| **CloudFront** | CDN and API routing |
| **Aurora PostgreSQL** | Serverless v2 database (auto-scaling) |
| **S3** | Static hosting (frontend) + image storage |
| **Redis (ElastiCache)** | Distributed caching |
| **Secrets Manager** | Secure credential storage |
| **EventBridge** | Event-driven architecture |
| **CloudWatch** | Logging and monitoring |
| **Rekognition** | ID verification (AI/ML) |
| **SES/SNS** | Email and SMS notifications |

---

## 📊 Technology Stack

| Layer | Technology |
|-------|------------|
| **Frontend** | Angular 20.2.1, TypeScript 5.9.2, Tailwind CSS 3.4.3 |
| **Backend** | .NET 8.0, ASP.NET Core, Entity Framework Core 8.0 |
| **Database** | Aurora PostgreSQL Serverless v2, Redis |
| **Real-time** | SignalR (WebSocket/LongPolling) |
| **CI/CD** | GitHub Actions → ECR → ECS |
| **Infrastructure** | AWS (ECS, ALB, CloudFront, Aurora, S3, EventBridge) |

---

## 🚀 Scalability & Reliability

- **Horizontal Scaling**: Each microservice scales independently
- **Auto-scaling**: ECS auto-scaling + Aurora Serverless auto-scaling
- **High Availability**: Multi-AZ deployment, health checks, circuit breakers
- **Disaster Recovery**: Database backups, infrastructure as code

---

**Next Steps**: See Solutions & Tools (PDF 3) → User Flows (PDF 1)







