# AmesaBase Architecture Overview

**Reference**: This file contains detailed architecture information extracted from `.cursorrules` for performance optimization.

## System Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │───▶│     Backend      │───▶│    Database     │
│                 │    │                  │    │                 │
│ Angular 20.2.1  │    │ .NET 8.0         │    │ Aurora PostgreSQL│
│ S3 + CloudFront │    │ ECS Fargate      │    │ Serverless v2   │
└─────────────────┘    │ Docker + ALB     │    │ (8 Schemas)     │
                       │ SignalR Hubs     │    └─────────────────┘
                       │ 8 Microservices  │
                       │ EventBridge      │
                       └──────────────────┘
```

## Microservices Architecture

All 8 microservices are independently deployable:
- Each service has its own Dockerfile and ECS task definition
- Each service uses its own PostgreSQL schema
- Services communicate via:
  - HTTP/REST (via ALB path-based routing)
  - EventBridge (event-driven architecture)
  - Service-to-service authentication (API key)
- Shared library (`AmesaBackend.Shared`) provides common functionality
- Redis caching (where required) for performance

## Request Flow

```
Client → CloudFront /api/* → ALB (path-based routing) → ECS Task (microservice) → .NET API → Aurora PostgreSQL (schema-specific)
                                              → SignalR Hub → LongPolling Connection
                                              → EventBridge → Other Services
```

## Inter-Service Communication

- **HTTP/REST**: Services call each other via ALB using service-to-service authentication
- **EventBridge**: Event-driven communication for async operations
  - Event bus: `amesa-event-bus`
  - Event publisher: `EventBridgePublisher` in Shared library
  - Event schemas: Defined in `AmesaBackend.Shared/Events/EventSchemas.cs`
- **Service-to-Service Auth**: API key authentication via `ServiceToServiceAuthMiddleware`
  - API key stored in: `/amesa/prod/ServiceAuth/ApiKey` (SSM Parameter Store)
