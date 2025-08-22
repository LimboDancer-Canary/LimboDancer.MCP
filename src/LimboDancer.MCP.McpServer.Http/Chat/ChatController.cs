using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LimboDancer.MCP.McpServer.Http.Chat;
using CoreTenant = LimboDancer.MCP.Core.Tenancy.ITenantAccessor;

namespace LimboDancer.MCP.McpServer.Http.Chat;

[ApiController]
[Route("api/v1/tenants/{tenantId}/chat")]
[Authorize(Policy = "ChatUser")]
public sealed class ChatController : ControllerBase
{
    private readonly CoreTenant _tenant;
    private readonly IChatOrchestrator _orchestrator;

    public ChatController(CoreTenant tenant, IChatOrchestrator orchestrator)
    {
        _tenant = tenant;
        _orchestrator = orchestrator;
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<CreateSessionResponse>> CreateSession([FromRoute] string tenantId, [FromBody] CreateSessionRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var routeTenantId) || routeTenantId != _tenant.TenantId)
            return Forbid();
        var sessionId = await _orchestrator.CreateSessionAsync(tenantId, req.SystemPrompt, ct);
        return Ok(new CreateSessionResponse(sessionId));
    }

    [HttpGet("sessions/{sessionId}/history")]
    public async Task<ActionResult<object>> GetHistory([FromRoute] string tenantId, [FromRoute] string sessionId, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var routeTenantId) || routeTenantId != _tenant.TenantId)
            return Forbid();
        var history = await _orchestrator.GetHistoryAsync(tenantId, sessionId, ct);
        return Ok(history);
    }

    [HttpPost("sessions/{sessionId}/messages")]
    public async Task<ActionResult<PostMessageResponse>> PostMessage([FromRoute] string tenantId, [FromRoute] string sessionId, [FromBody] PostMessageRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(tenantId, out var routeTenantId) || routeTenantId != _tenant.TenantId)
            return Forbid();
        var correlationId = await _orchestrator.EnqueueUserMessageAsync(tenantId, sessionId, req.Role, req.Content, ct);
        return Accepted(new PostMessageResponse(correlationId));
    }
}