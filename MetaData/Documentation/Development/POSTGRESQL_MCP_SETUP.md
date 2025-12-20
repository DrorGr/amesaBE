# PostgreSQL MCP Server Setup Guide

## Overview

This guide explains how to configure the PostgreSQL MCP (Model Context Protocol) server in Cursor to enable direct database connections. This allows Cursor agents to query your PostgreSQL database directly, inspect schemas, run queries, and understand database structure.

## Prerequisites

1. **PostgreSQL MCP Server**: Ensure you have a PostgreSQL MCP server installed
   - Common implementations: `@modelcontextprotocol/server-postgres` or similar
   - Installation typically via npm: `npm install -g @modelcontextprotocol/server-postgres`

2. **Database Connection Details**: You'll need:
   - Host (e.g., `amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com`)
   - Port (typically `5432` for PostgreSQL)
   - Database name (e.g., `amesa_lottery`, `amesa_auth`, etc.)
   - Username
   - Password
   - SSL mode (typically `Require` for AWS RDS)

## Database Connection Information

Based on your project configuration:

### Production Database (AWS Aurora PostgreSQL)
- **Host**: `amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com`
- **Port**: `5432`
- **Region**: `eu-north-1`
- **SSL Mode**: `Require`
- **Databases/Schemas Available**:
  - `amesa_auth` - Authentication service schema
  - `amesa_lottery` - Lottery service schema
  - `amesa_payment` - Payment service schema
  - `amesa_notification` - Notification service schema
  - `amesa_content` - Content service schema
  - `amesa_lottery_results` - Lottery results service schema
  - `amesa_analytics` - Analytics service schema
  - `amesa_admin` - Admin service schema
  - `public` - Shared/public schema

### Local Development Database
- **Host**: `localhost` (if running locally)
- **Port**: `5432`
- **SSL Mode**: `Prefer` or `Disable` (for local development)

## Cursor Configuration

### Step 1: Open Cursor Settings

1. Open Cursor IDE
2. Navigate to **Cursor Settings** (or **Preferences**)
3. Go to **MCP** section

### Step 2: Add PostgreSQL MCP Server

Click **"Add new global MCP server"** and use one of the following configuration formats:

#### Option 1: Connection String Format (Recommended)

```json
{
  "mcpServers": {
    "postgres": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres"
      ],
      "env": {
        "POSTGRES_CONNECTION_STRING": "postgresql://dror:aAXa406L6qdqfTU6o8vr@amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com:5432/amesa_lottery?sslmode=require"
      }
    }
  }
}
```

#### Option 2: Individual Parameters Format

```json
{
  "mcpServers": {
    "postgres": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres"
      ],
      "env": {
        "POSTGRES_HOST": "amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com",
        "POSTGRES_PORT": "5432",
        "POSTGRES_DATABASE": "amesa_lottery",
        "POSTGRES_USER": "dror",
        "POSTGRES_PASSWORD": "aAXa406L6qdqfTU6o8vr",
        "POSTGRES_SSL_MODE": "require"
      }
    }
  }
}
```

#### Option 3: Multiple Database Connections

If you want to connect to multiple databases, you can create separate MCP server entries:

```json
{
  "mcpServers": {
    "postgres-auth": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres"
      ],
      "env": {
        "POSTGRES_CONNECTION_STRING": "postgresql://dror:PASSWORD@amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com:5432/amesa_auth?sslmode=require"
      }
    },
    "postgres-lottery": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres"
      ],
      "env": {
        "POSTGRES_CONNECTION_STRING": "postgresql://dror:PASSWORD@amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com:5432/amesa_lottery?sslmode=require"
      }
    }
  }
}
```

### Step 3: Connection String Format

The PostgreSQL connection string format is:

```
postgresql://[username]:[password]@[host]:[port]/[database]?[parameters]
```

**Parameters:**
- `sslmode=require` - For AWS RDS (required)
- `sslmode=prefer` - For local development (optional)
- `sslmode=disable` - For local development without SSL

**Example:**
```
postgresql://dror:aAXa406L6qdqfTU6o8vr@amesadbmain1.cruuae28ob7m.eu-north-1.rds.amazonaws.com:5432/amesa_lottery?sslmode=require
```

### Step 4: Verify Connection

1. Click the **refresh icon** in the MCP server window
2. Verify **green dot** indicates server is running
3. Status should show "running" (not "start" or "pause")

**Troubleshooting:**
- If status shows "start" or "pause", click to restart the server
- Check terminal outputs for error messages
- Verify database credentials are correct
- Ensure network access to database (AWS security groups, firewall rules)

## Security Considerations

### ⚠️ Important Security Notes

1. **Credentials in Configuration**: 
   - MCP server configuration files may contain sensitive credentials
   - Consider using environment variables or secrets management
   - Do not commit configuration files with passwords to version control

2. **Environment Variables Alternative**:
   ```json
   {
     "mcpServers": {
       "postgres": {
         "command": "npx",
         "args": [
           "-y",
           "@modelcontextprotocol/server-postgres"
         ],
         "env": {
           "POSTGRES_CONNECTION_STRING": "${POSTGRES_CONNECTION_STRING}"
         }
       }
     }
   }
   ```
   Then set `POSTGRES_CONNECTION_STRING` in your environment.

3. **Read-Only Access**: Consider creating a read-only database user for MCP connections:
   ```sql
   CREATE USER mcp_readonly WITH PASSWORD 'secure_password';
   GRANT CONNECT ON DATABASE amesa_lottery TO mcp_readonly;
   GRANT USAGE ON SCHEMA amesa_lottery TO mcp_readonly;
   GRANT SELECT ON ALL TABLES IN SCHEMA amesa_lottery TO mcp_readonly;
   ```

4. **Network Security**: 
   - Ensure AWS security groups allow connections from your IP
   - Use VPC endpoints or VPN for production access
   - Consider IP whitelisting for additional security

## Usage

Once configured, Cursor agents can:

1. **Query Database Schema**:
   - "Show me the structure of the users table"
   - "What columns are in the lottery_tickets table?"
   - "List all tables in the amesa_auth schema"

2. **Run Queries**:
   - "How many users are in the database?"
   - "Show me recent lottery tickets"
   - "What's the structure of the payment transactions table?"

3. **Understand Relationships**:
   - "What foreign keys exist in the lottery schema?"
   - "Show me the relationship between houses and tickets"

4. **Database Analysis**:
   - "What indexes exist on the users table?"
   - "Show me the database schema for the payment service"

## Troubleshooting

### Connection Errors

**Error: "Connection refused"**
- Verify database host and port are correct
- Check AWS security groups allow your IP
- Verify database is running and accessible

**Error: "Authentication failed"**
- Verify username and password are correct
- Check user has proper permissions
- Verify database name is correct

**Error: "SSL required"**
- Ensure `sslmode=require` is set for AWS RDS
- Check SSL certificate configuration
- For local development, try `sslmode=prefer` or `sslmode=disable`

### MCP Server Not Starting

**Error: "Command not found: npx"**
- Install Node.js and npm
- Verify npx is available in PATH
- Try using full path to npx

**Error: "Package not found"**
- Verify `@modelcontextprotocol/server-postgres` is installed
- Try installing globally: `npm install -g @modelcontextprotocol/server-postgres`
- Check npm registry access

### Performance Issues

**Slow Queries**:
- Use specific database connections (not connecting to all databases)
- Limit query results
- Use read replicas for read-only queries

## Alternative MCP Server Implementations

If the standard PostgreSQL MCP server doesn't work, consider:

1. **Custom MCP Server**: Create a custom MCP server using the MCP SDK
2. **Different Package**: Try alternative PostgreSQL MCP implementations
3. **Direct Database Tools**: Use database tools like pgAdmin, DBeaver, or psql

## Configuration File Location

Cursor MCP configuration is typically stored in:
- **Windows**: `%APPDATA%\Cursor\User\globalStorage\mcp.json` or Cursor settings
- **macOS**: `~/Library/Application Support/Cursor/User/globalStorage/mcp.json` or Cursor settings
- **Linux**: `~/.config/Cursor/User/globalStorage/mcp.json` or Cursor settings

## References

- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)
- [PostgreSQL Connection Strings](https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING)
- [AWS RDS PostgreSQL Connection](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_ConnectToPostgreSQLInstance.html)

---

**Last Updated**: 2025-01-25
**Configuration Version**: 1.0.0
