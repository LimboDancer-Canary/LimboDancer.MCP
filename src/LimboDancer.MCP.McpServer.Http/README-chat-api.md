# Chat API (MVP)

Routes (all require Authorization: Bearer token with roles: ChatUser)
- POST /api/v1/tenants/{tenantId}/chat/sessions
- GET  /api/v1/tenants/{tenantId}/chat/sessions/{sessionId}/history
- POST /api/v1/tenants/{tenantId}/chat/sessions/{sessionId}/messages
- GET  /api/v1/tenants/{tenantId}/chat/sessions/{sessionId}/stream (SSE)

SSE headers to set at proxies:
- X-Accel-Buffering: no
- Cache-Control: no-store
- Keep-alive timeouts >= 60s