# Tenancy Conventions

This repository uses HTTP headers to scope operations to a tenant (and optionally package/channel). All code and docs should use the canonical casing below. Servers treat header names case-insensitively, but a single canonical form avoids drift.

| Purpose | Header | Example |
|---------|--------|---------|
| Tenant  | `X-Tenant-Id` | `5e0b2c7f-9c9d-4c2e-9b5b-8e7e9d1a2b3c` |
| Package | `X-Tenant-Package` | `default` |
| Channel | `X-Tenant-Channel` | `dev` |

## Architecture

There are two layers:

1. Edge/API host (McpServer.Http): Responsible for authenticating the caller, extracting claims and/or headers, and enforcing policies.
2. Internal server (McpServer): Implements application logic. It may also expose HTTP endpoints of its own (legacy / internal) that accept the same headers.

Each layer has its own HttpTenantAccessor implementation today (by design). Do not unify them unless the layering strategy changes; instead, keep behavior consistent by sharing constants.

## Constants

Defined in `src/LimboDancer.MCP.Core/Tenancy/TenantHeaders.cs`:

```csharp
public static class TenantHeaders
{
    public const string TenantId = "X-Tenant-Id";
    public const string Package  = "X-Tenant-Package";
    public const string Channel  = "X-Tenant-Channel";
}
```

## Outgoing Requests (Clients / Blazor)

Blazor UI automatically adds `X-Tenant-Id` via `TenantHeaderHandler` if the user selected a tenant. If you manually craft a request and already set the header, the handler will not overwrite it.

## Query Parameter Usage (Legacy)

Some existing endpoints still accept `?tenant=&package=&channel=` query parameters. This is considered legacy and may be deprecated. New clients should send headers only.

Proposed deprecation steps (optional):
1. Log a warning when query parameters are used.
2. Announce removal in a minor release.
3. Remove query parameter handling in the next major release.

## Testing Recommendations

- Case-insensitive acceptance (e.g. client sends `x-tenant-id`).
- Missing tenant header -> appropriate error or default (depending on layer policy).
- Claim vs header precedence (if relevant in edge host).
