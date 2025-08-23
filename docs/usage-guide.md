# LimboDancer.MCP Usage Guide

## Quick Start

### Installation

1. Build the project:
```bash
dotnet build src/LimboDancer.MCP.sln
```

2. Run database migrations:
```bash
ldm db migrate --connection "Host=localhost;Database=limbodancer;Username=postgres;Password=postgres"
```

3. Initialize vector index:
```bash
ldm vector init --endpoint https://your-search.search.windows.net --api-key YOUR_KEY
```

## Running the MCP Server

### Stdio Mode (for CLI/IDE Integration)

The stdio mode is designed for integration with command-line tools and IDEs that support the Model Context Protocol.

#### Basic Usage

```bash
# Run with default tenant from config
ldm serve --stdio

# Run with specific tenant
ldm serve --stdio --tenant "00000000-0000-0000-0000-000000000000"

# Run with verbose logging (logs to stderr)
ldm serve --stdio --verbose
```

#### Interactive Session Example

```bash
# Start the server
ldm serve --stdio --tenant "test-tenant"

# Send initialize request
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}

# Expected response:
{"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2024-11-01","capabilities":{"tools":{}},"serverInfo":{"name":"LimboDancer.MCP","version":"1.0.0"}}}

# List available tools
{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}

# Execute a tool
{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"history_get","arguments":{"sessionId":"123e4567-e89b-12d3-a456-426614174000","limit":10}}}

# Send shutdown notification
{"jsonrpc":"2.0","method":"shutdown"}
```

#### Piping Commands

```bash
# Single command execution
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | ldm serve --stdio

# Multiple commands from file
cat commands.jsonl | ldm serve --stdio

# With jq for pretty output
echo '{"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}' | ldm serve --stdio | jq .
```

### HTTP Mode (for Web Integration)

The HTTP mode provides a RESTful API for web applications and services.

#### Starting the Server

```bash
# Run with default settings
ldm serve

# Run on specific port
ldm serve --urls "http://localhost:5000"

# Run with specific environment
ASPNETCORE_ENVIRONMENT=Production ldm serve
```

#### Authentication Setup

1. Get an access token from Azure AD:
```bash
# Using Azure CLI
TOKEN=$(az account get-access-token --resource api://your-app-id --query accessToken -o tsv)

# Using curl
TOKEN=$(curl -X POST "https://login.microsoftonline.com/your-tenant/oauth2/v2.0/token" \
  -d "client_id=your-client-id" \
  -d "client_secret=your-secret" \
  -d "scope=api://your-app-id/.default" \
  -d "grant_type=client_credentials" \
  | jq -r .access_token)
```

#### HTTP API Examples

```bash
# Initialize session
curl -X POST http://localhost:5179/api/mcp/initialize \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: test-tenant"

# Get manifest
curl http://localhost:5179/api/mcp/manifest

# List tools
curl http://localhost:5179/api/mcp/tools \
  -H "Authorization: Bearer $TOKEN"

# Execute history_get tool
curl -X POST http://localhost:5179/api/mcp/tools/history_get \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: test-tenant" \
  -d '{
    "sessionId": "123e4567-e89b-12d3-a456-426614174000",
    "limit": 10,
    "before": "2025-01-15T00:00:00Z"
  }'

# Execute memory_search tool
curl -X POST http://localhost:5179/api/mcp/tools/memory_search \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: test-tenant" \
  -d '{
    "queryText": "Azure cloud services",
    "k": 5,
    "ontologyClass": "TechnicalDocument"
  }'

# Execute graph_query tool
curl -X POST http://localhost:5179/api/mcp/tools/graph_query \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -H "X-Tenant-Id: test-tenant" \
  -d '{
    "subjectIds": ["vertex-123"],
    "keyMode": "ontology",
    "filters": [
      {
        "property": "hasStatus",
        "op": "eq",
        "value": "active"
      }
    ],
    "traverse": [
      {
        "direction": "out",
        "relation": "relatedTo",
        "hops": 2
      }
    ],
    "limit": 20
  }'
```

#### Server-Sent Events (SSE)

```bash
# Connect to SSE endpoint
curl -N http://localhost:5179/api/mcp/events \
  -H "Authorization: Bearer $TOKEN" \
  -H "Accept: text/event-stream"

# Using JavaScript
const eventSource = new EventSource('/api/mcp/events', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

eventSource.addEventListener('connected', (e) => {
  console.log('Connected:', JSON.parse(e.data));
});

eventSource.addEventListener('tool_executed', (e) => {
  console.log('Tool executed:', JSON.parse(e.data));
});

eventSource.addEventListener('error', (e) => {
  console.error('SSE Error:', e);
});
```

## Advanced Usage

### Custom Tool Timeouts

Configure tool-specific timeouts in `appsettings.json`:

```json
{
  "Mcp": {
    "ToolTimeouts": {
      "graph_query": "00:02:00",
      "memory_search": "00:01:30",
      "history_append": "00:00:30"
    }
  }
}
```

### Retry Configuration

```json
{
  "Mcp": {
    "RetryPolicy": {
      "MaxRetryAttempts": 3,
      "BaseDelay": "00:00:01",
      "MaxDelay": "00:00:30",
      "JitterFactor": 0.2
    }
  }
}
```

### Circuit Breaker Configuration

```json
{
  "Mcp": {
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "BreakDuration": "00:00:30",
      "SamplingDuration": "00:01:00",
      "MinimumThroughput": 10
    }
  }
}
```

## Monitoring and Debugging

### Enable Request Logging

```json
{
  "Mcp": {
    "EnableRequestLogging": true
  },
  "Logging": {
    "LogLevel": {
      "LimboDancer.MCP.McpServer": "Debug"
    }
  }
}
```

### View Metrics

When OpenTelemetry is configured, metrics are available at:

- Tool execution count: `mcp.tool.executions`
- Tool execution duration: `mcp.tool.duration`
- Tool execution errors: `mcp.tool.errors`
- Active sessions: `mcp.sessions.active`
- Request/response sizes: `mcp.request.size`, `mcp.response.size`

### Health Check

```bash
curl http://localhost:5179/health
```

## Integration Examples

### Python Client

```python
import json
import sys

def send_request(method, params=None, id=1):
    request = {
        "jsonrpc": "2.0",
        "id": id,
        "method": method
    }
    if params:
        request["params"] = params
    
    print(json.dumps(request))
    sys.stdout.flush()
    
    # Read response
    response = sys.stdin.readline()
    return json.loads(response)

# Initialize
result = send_request("initialize")
print(f"Protocol version: {result['result']['protocolVersion']}")

# List tools
tools = send_request("tools/list")
for tool in tools['result']['tools']:
    print(f"Tool: {tool['name']} - {tool['description']}")

# Execute tool
tool_result = send_request("tools/call", {
    "name": "history_get",
    "arguments": {
        "sessionId": "test-session",
        "limit": 5
    }
})
```

### Node.js Client

```javascript
const readline = require('readline');

class McpClient {
  constructor() {
    this.rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout
    });
    this.id = 0;
  }

  async sendRequest(method, params) {
    const request = {
      jsonrpc: "2.0",
      id: ++this.id,
      method: method,
      params: params || {}
    };

    console.log(JSON.stringify(request));

    return new Promise((resolve) => {
      this.rl.once('line', (line) => {
        resolve(JSON.parse(line));
      });
    });
  }

  async initialize() {
    return await this.sendRequest('initialize');
  }

  async listTools() {
    return await this.sendRequest('tools/list');
  }

  async executeTool(name, args) {
    return await this.sendRequest('tools/call', {
      name: name,
      arguments: args
    });
  }
}

// Usage
const client = new McpClient();
await client.initialize();
const tools = await client.listTools();
const result = await client.executeTool('memory_search', {
  queryText: 'test query',
  k: 5
});
```

## Troubleshooting

### Common Issues

1. **Tool timeout errors**
   - Increase timeout in configuration
   - Check if external services (database, search) are responsive

2. **Authentication failures**
   - Verify token is valid and not expired
   - Check audience and issuer configuration

3. **Circuit breaker open**
   - Check logs for repeated failures
   - Verify external service health
   - Wait for break duration before retrying

4. **Stdio mode not responding**
   - Check stderr for error logs
   - Ensure input is valid JSON-RPC
   - Verify line endings (must be newline-delimited)

### Debug Mode

Run with detailed logging:

```bash
# Stdio mode
ldm serve --stdio --verbose 2>debug.log

# HTTP mode
ASPNETCORE_ENVIRONMENT=Development ldm serve
```

## Performance Tips

1. **Use connection pooling** for database and Gremlin connections
2. **Enable response caching** for frequently accessed data
3. **Configure appropriate timeouts** based on your infrastructure
4. **Use circuit breakers** to prevent cascade failures
5. **Monitor metrics** to identify bottlenecks
6. **Scale horizontally** using Azure Container Apps

## Security Best Practices

1. **Always use authentication** in production
2. **Validate tenant isolation** at every layer
3. **Use HTTPS** for HTTP mode
4. **Rotate API keys** regularly
5. **Monitor for suspicious activity** using telemetry
6. **Implement rate limiting** to prevent abuse
7. **Use least-privilege** service accounts