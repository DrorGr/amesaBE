# RDS Database Setup for Microservices

## Existing Aurora Cluster

**Cluster**: `amesadbmain`
- **Engine**: aurora-postgresql
- **Status**: available
- **Endpoint**: (check with AWS CLI)
- **Reader Endpoint**: (check with AWS CLI)
- **Port**: 5432 (default PostgreSQL)

## Database-per-Service Strategy

We have two options:

### Option 1: Use Existing Aurora with Separate Schemas (Recommended for Start)
- Create separate schemas in the existing Aurora cluster
- Each microservice uses its own schema
- Simpler to manage initially
- Lower cost

### Option 2: Create Separate RDS Instances (True Database-per-Service)
- Create 8 separate RDS instances (one per service)
- True isolation
- Higher cost
- More complex management

## Recommended Approach: Option 1 (Schemas)

Create schemas in existing Aurora cluster:

```sql
-- Connect to amesadbmain cluster
-- Create schemas for each service

CREATE SCHEMA IF NOT EXISTS amesa_auth;
CREATE SCHEMA IF NOT EXISTS amesa_payment;
CREATE SCHEMA IF NOT EXISTS amesa_lottery;
CREATE SCHEMA IF NOT EXISTS amesa_content;
CREATE SCHEMA IF NOT EXISTS amesa_notification;
CREATE SCHEMA IF NOT EXISTS amesa_lottery_results;
CREATE SCHEMA IF NOT EXISTS amesa_analytics;
-- Admin service may not need a separate schema
```

## Connection Strings

Update each service's `appsettings.json` with schema-specific connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=<aurora-endpoint>;Port=5432;Database=<cluster-db>;Username=<username>;Password=<password>;SearchPath=amesa_auth;"
  }
}
```

The `SearchPath` parameter ensures each service uses its own schema.

## Migration Strategy

1. Run migrations for each service
2. Each migration will create tables in the service's schema
3. Use EF Core's `HasDefaultSchema()` in DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("amesa_auth");
    // ... rest of configuration
}
```

## Future Migration to Separate Instances

When ready to migrate to true database-per-service:
1. Export data from each schema
2. Create new RDS instances
3. Import data
4. Update connection strings
5. Test and verify

