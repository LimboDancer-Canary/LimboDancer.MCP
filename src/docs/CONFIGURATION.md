# LimboDancer MCP Configuration Guide

## Required Configuration

### Storage (PostgreSQL)
```json
{
  "Storage": {
    "ConnectionString": "Host=localhost;Port=5432;Database=limbodancer_dev;Username=postgres;Password=postgres",
    "ApplyMigrationsAtStartup": false
  }
}
```
Alternative key (legacy): `Persistence:ConnectionString`

### Vector Search (Azure AI Search)
```json
{
  "Vector": {
    "Endpoint": "https://<your-service>.search.windows.net",
    "ApiKey": "<admin-key>",
    "IndexName": "ldm-memory",
    "VectorDimensions": 1536
  }
}
```
Alternative keys (legacy): `Search:Endpoint`, `Search:ApiKey`, `Search:Index`

### Graph Database (Cosmos Gremlin)
```json
{
  "CosmosGremlin": {
    "Host": "<account>.gremlin.cosmos.azure.com",
    "Port": 443,
    "EnableSsl": true,
    "Database": "limbodancer",
    "Graph": "history_memory_graph",
    "AuthKey": "<primary-key>",
    "ConnectionPoolSize": 8,
    "IsCosmos": true
  }
}
```

### Tenancy
```json
{
  "Tenancy": {
    "DefaultTenantId": "00000000-0000-0000-0000-000000000000",
    "DefaultPackage": "default",
    "DefaultChannel": "dev"
  }
}
```

### Authentication (McpServer.Http)
```json
{
  "Authentication": {
    "Jwt": {
      "Authority": "https://login.microsoftonline.com/<TENANT_ID>/v2.0",
      "Audience": "<API_CLIENT_ID>"
    }
  }
}
```

### Ontology API (BlazorConsole)
```json
{
  "OntologyApi": {
    "BaseUrl": "http://localhost:5179",
    "TenantHeaderName": "X-Tenant-Id",
    "TimeoutSeconds": 10
  }
}
```

## Environment-Specific Settings

### Development
- Set `ASPNETCORE_ENVIRONMENT=Development`
- Migrations can be auto-applied via `Storage:ApplyMigrationsAtStartup=true`
- Default tenant from config is used when no header present

### Production
- Ensure all connection strings use secure credentials
- Disable auto-migrations
- Configure proper CORS origins
- Use managed identities where possible

## CLI Configuration

The CLI reads from `appsettings.json` and `appsettings.Development.json` in the working directory.

Required for CLI operations:
- Storage connection for EF migrations
- Vector search credentials for index operations
- Graph database for Gremlin operations
- Tenancy defaults for development

## Running Migrations

```bash
# From CLI
ldm db migrate

# From .NET CLI
dotnet ef database update -p LimboDancer.MCP.Storage -s LimboDancer.MCP.McpServer
```