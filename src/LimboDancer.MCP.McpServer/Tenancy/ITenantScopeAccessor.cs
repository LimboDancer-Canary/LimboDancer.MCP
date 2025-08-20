using LimboDancer.MCP.Ontology.Runtime;
using Microsoft.AspNetCore.Http;
using System;

namespace LimboDancer.MCP.McpServer.Tenancy
{
    /// <summary>
    /// Typed contract to resolve the current TenantScope (tenant, package, channel) for the request.
    /// Avoids dynamic/brittle access patterns.
    /// </summary>
    public interface ITenantScopeAccessor
    {
        TenantScope GetCurrentScope();
    }
}