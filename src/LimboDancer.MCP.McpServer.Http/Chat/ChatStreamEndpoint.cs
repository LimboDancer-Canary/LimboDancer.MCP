using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CoreTenant = LimboDancer.MCP.Core.Tenancy.ITenantAccessor;

namespace LimboDancer.MCP.McpServer.Http.Chat;

[ApiController]
[Route("api/v1/tenants/{tenantId}/chat")]
[Authorize(Policy = "ChatUser")]
public sealed class ChatStreamEndpoint : ControllerBase
{
    private readonly CoreTenant _tenant;
    private readonly IChatOrchestrator _orchestrator;

    public ChatStreamEndpoint(CoreTenant tenant, IChatOrchestrator orchestrator)
    {
        _tenant = tenant;
        _orchestrator = orchestrator;
    }

    [HttpGet("sessions/{sessionId}/stream")]
    public async Task GetStream([FromRoute] string tenantId, [FromRoute] string sessionId, CancellationToken ct)
    {
        if (tenantId != _tenant.TenantId.ToString()) { Response.StatusCode = 403; return; }

        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["X-Accel-Buffering"] = "no"; // nginx
        await foreach (var ev in _orchestrator.SubscribeAsync(tenantId, sessionId, ct))
        {
            var json = System.Text.Json.JsonSerializer.Serialize(ev);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}