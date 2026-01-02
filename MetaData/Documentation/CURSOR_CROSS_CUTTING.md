# AmesaBase Cross-Cutting Concerns

**Reference**: This file contains detailed cross-cutting concerns extracted from `.cursorrules` for performance optimization.

> **Note**: For complete details, see the original sections in `.cursorrules`. This is a reference document.

## Rate Limiting

- **Service**: `RateLimitService` (Redis-backed, circuit breaker protected)
- **Implementation**: Atomic increment-and-check pattern (prevents race conditions)
- **Cache Key Pattern**: `ratelimit:{key}` (Redis keys)
- **Fail-Open Behavior**: Default (allows requests if Redis fails), configurable to fail-closed
- **Circuit Breaker**: Protected by `CircuitBreakerService` (5 failures in 30 seconds threshold)

**Rate Limiting Rules**: See `.cursorrules` for complete table.

## Circuit Breaker Patterns

- **Service**: `CircuitBreakerService` (Polly v8.4.2)
- **Configuration**: 5 failures threshold, 30 seconds duration
- **Operations Protected**: RateLimit_Redis, AccountLockout_Redis

## Health Check Implementations

- **Generic Health Endpoint**: `/health` (all services)
- **Service-Specific**: `/health/notifications`, etc.

## Error Handling Patterns

- **Global Error Handler**: `ErrorHandlerMiddleware` (shared middleware)
- **Error Response Format**: `StandardApiResponse<object>` with `StandardErrorResponse`

## Caching Strategies

- **Redis Cache**: Distributed cache via `ICache` interface
- **Cache Key Patterns**: `houses_*`, `translation_{language}_{key}`, etc.
- **Services Using Redis**: Auth, Lottery, Content, Payment

## Security Headers

- **Middleware**: `SecurityHeadersMiddleware` (shared middleware)
- **Headers**: X-Frame-Options, X-Content-Type-Options, CSP, HSTS, etc.

## CORS Configuration

- Per-service CORS configuration in `Program.cs`
- Environment-specific origins (CloudFront URL in prod, localhost in dev)

## Request/Response Logging

- **Middleware**: `RequestResponseLoggingMiddleware`, `AMESASerilogScopedLoggingMiddleware`
- **PII Sanitization**: Passwords, tokens, sensitive data removed

## Retry Policies

- **HTTP Client Retries**: Polly-based, 3 retries, exponential backoff
- **EventBridge Retries**: 3 retries, tracked in Redis

## Secrets Management

- **AWS Secrets Manager**: OAuth secrets, notification secrets, payment secrets
- **AWS SSM Parameter Store**: Service-to-service auth API key, JWT secrets

## Database Connection Patterns

- **Connection Pooling**: MaxPoolSize 100, MinPoolSize 10, ConnectionLifetime 300s
- **EF Core Retry Policies**: 3 retries, 30s max delay
- **Transaction Management**: READ COMMITTED isolation level

## Monitoring & Observability

- **CloudWatch Logs**: `/ecs/{service-name}` log groups
- **CloudWatch Metrics**: Custom and auto metrics
- **Health Checks**: Generic and service-specific endpoints

## Validation Patterns

- **Data Annotations**: Primary validation method on DTOs
- **Custom Validators**: `PasswordValidatorService` with strength checks

## Background Services

See `.cursorrules` for complete list of background services across all microservices.

## Service Registration Patterns

- **Extension Method Pattern**: All service registration via extension methods
- **Naming Convention**: `Add{ServiceName}{Feature}()`
- **Service Lifetime**: Singleton (AWS clients), Scoped (business logic), Transient (default)

## Swagger/OpenAPI Configuration

- Configured via `AddAuthSwagger()` extension method
- Bearer token authentication scheme

## Logging Configuration Patterns

- **Framework**: Serilog with structured logging
- **Sinks**: Console, File (dev), CloudWatch (prod)

## Application Bootstrap Patterns

See `.cursorrules` for complete Program.cs structure and execution order.

---

**For complete details**: Refer to the original sections in `.cursorrules` or `BE/.cursorrules`.
