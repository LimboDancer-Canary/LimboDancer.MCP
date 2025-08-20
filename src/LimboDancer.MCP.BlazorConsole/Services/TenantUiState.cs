using System;

namespace LimboDancer.MCP.BlazorConsole.Services;

public sealed class TenantUiState
{
    public Guid? TenantId { get; private set; }

    public bool HasTenant => TenantId.HasValue;

    public string TenantLabel => TenantId?.ToString() ?? "(none)";

    public event Action? Changed;

    public void SetTenant(Guid? id)
    {
        TenantId = id;
        Changed?.Invoke();
    }

    public void Clear()
    {
        SetTenant(null);
    }
}