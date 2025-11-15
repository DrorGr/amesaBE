# Start Local Backend with Production Database

**Note**: This configuration connects your local backend to the production PostgreSQL database.

## ⚠️ Important Warnings

1. **Be Careful**: You're connecting to PRODUCTION database
2. **Read-Only Recommended**: Consider using read-only user if available
3. **Test Data Only**: Only create test users, don't modify production data
4. **Network Access**: Ensure your IP is allowed in RDS security group

## Configuration

The `appsettings.Development.json` is configured to use production database.

**Connection String Format**:
```
Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=amesadb;Username=amesa_admin;Password=YOUR_PASSWORD;SSL Mode=Require;
```

## Steps

1. **Update Password**: Replace `YOUR_PASSWORD_HERE` in `appsettings.Development.json` with actual database password

2. **Start Backend**:
   ```bash
   cd BE/AmesaBackend
   dotnet run
   ```

3. **Verify Connection**:
   - Check logs for: "Using PostgreSQL (connection from environment)"
   - No database connection errors
   - Health check works: `http://localhost:5000/health`

## Alternative: Use Environment Variable

Instead of putting password in file, use environment variable:

```bash
# Windows PowerShell
$env:DB_CONNECTION_STRING="Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=amesadb;Username=amesa_admin;Password=YOUR_PASSWORD;SSL Mode=Require;"
cd BE/AmesaBackend
dotnet run

# Windows CMD
set DB_CONNECTION_STRING=Host=amesadbmain.cluster-cruuae28ob7m.eu-north-1.rds.amazonaws.com;Port=5432;Database=amesadb;Username=amesa_admin;Password=YOUR_PASSWORD;SSL Mode=Require;
cd BE\AmesaBackend
dotnet run
```

## Security Note

Never commit the actual password to git! Use environment variables or secure secret management.

