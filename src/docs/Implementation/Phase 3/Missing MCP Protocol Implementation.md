# Missing MCP Protocol Implementation  
  
I'll search the project knowledge to understand the current state of the LimboDancer.MCP system and identify what needs to be implemented for the MCP protocol.Based on my analysis of the project knowledge, I can see that the LimboDancer.MCP system has all the infrastructure components built (tools, storage, vector search, graph database, authentication), but is indeed missing the critical MCP protocol handler implementation. Here's what needs to be built:

## Missing MCP Protocol Implementation

### 1. **MCP Server Core Implementation**

Create a new file `McpServer.cs` in the `LimboDancer.MCP.McpServer` project:### 2. **Stdio Transport Implementation**

Create `StdioTransport.cs` for handling JSON-RPC over stdio:### 3. **HTTP Transport Implementation**

Create `HttpTransport.cs` for serving MCP over HTTP:### 4. **Update ServeCommand to Use MCP Implementation**

Update the `ServeCommand.cs` to actually run the MCP server:### 5. **Update Program.cs to Register MCP Server**

Update the main `Program.cs` to include MCP server registration:## Summary of MCP Protocol Implementation

I've created the core components needed to implement the Model Context Protocol (MCP) server:

### 1. **McpServer.cs** - Core MCP Server
- Tool registration and management
- Tool execution with proper service scoping
- Tool discovery (listing available tools)
- Integration with existing tool implementations

### 2. **StdioTransport.cs** - Standard I/O Transport
- JSON-RPC message handling over stdin/stdout
- Proper async read/write loops
- Error handling and logging to stderr
- Support for MCP protocol methods:
  - `initialize` - Protocol handshake
  - `tools/list` - Tool discovery
  - `tools/call` - Tool execution
  - `shutdown` - Graceful shutdown

### 3. **HttpTransport.cs** - HTTP Transport
- RESTful endpoints for MCP operations
- Authentication via JWT Bearer tokens
- Tenant-aware execution
- Server-Sent Events for real-time updates
- Endpoints:
  - `POST /api/mcp/initialize`
  - `GET /api/mcp/tools`
  - `POST /api/mcp/tools/{toolName}`
  - `GET /api/mcp/events` (SSE)

### 4. **Updated ServeCommand.cs**
- Proper stdio mode implementation
- HTTP mode support
- Logging configuration (stderr for stdio, console for HTTP)
- Tenant configuration support

### 5. **Updated Program.cs**
- MCP server registration in DI container
- Controller mapping for HTTP endpoints
- Authentication configuration

## What's Still Needed

### 1. **Configuration Updates**
Add to `appsettings.json`:
```json
{
  "Mcp": {
    "MaxToolExecutionTime": "00:05:00",
    "EnableRequestLogging": true
  }
}
```

### 2. **Tool Manifest Generation**
Create a tool manifest endpoint that generates the complete tool schema:
```csharp
[HttpGet("api/mcp/manifest")]
public async Task<IActionResult> GetManifest()
{
    var tools = _mcpServer.GetTools();
    return Ok(new
    {
        name = "LimboDancer.MCP",
        version = "1.0.0",
        tools = tools
    });
}
```

### 3. **Error Handling Enhancements**
- Add retry logic for transient failures
- Implement circuit breakers for external services
- Add detailed error reporting for tool failures

### 4. **Monitoring and Telemetry**
- Add OpenTelemetry spans for tool execution
- Track tool usage metrics
- Log tool execution times

### 5. **Testing Infrastructure**
- Unit tests for JSON-RPC processing
- Integration tests for tool execution
- End-to-end tests for stdio communication

## Usage Examples

### Stdio Mode
```bash
# Run the MCP server in stdio mode
ldm serve --stdio --tenant "00000000-0000-0000-0000-000000000000"

# Connect with an MCP client
echo '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}' | ldm serve --stdio
```

### HTTP Mode
```bash
# Run the MCP server in HTTP mode
ldm serve --tenant "00000000-0000-0000-0000-000000000000"

# Initialize session
curl -X POST http://localhost:5179/api/mcp/initialize \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json"

# List tools
curl http://localhost:5179/api/mcp/tools \
  -H "Authorization: Bearer <token>"

# Execute a tool
curl -X POST http://localhost:5179/api/mcp/tools/history_get \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"123e4567-e89b-12d3-a456-426614174000","limit":10}'
```

This implementation provides a complete MCP server that can be used both as a CLI tool (stdio mode) and as a web service (HTTP mode), fully integrated with the existing LimboDancer.MCP infrastructure.