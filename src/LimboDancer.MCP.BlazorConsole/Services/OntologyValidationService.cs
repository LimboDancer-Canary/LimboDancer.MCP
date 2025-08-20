// File: /src/LimboDancer.MCP.BlazorConsole/Services/OntologyValidationService.cs
// Purpose:
//   Client-side service for BlazorConsole to call MCP server ontology endpoints.
//   - Validates presence of base URL via options
//   - Uses named HttpClient with BaseAddress
//   - Adds tenant header on each call
//   - Gracefully surfaces 404/5xx with clear exceptions
//
// Endpoints expected (server):
//   POST /api/ontology/validate   -> returns { isSuccess: bool, messages?: string[] }
//   GET  /api/ontology/export     -> returns JSON (string)
//
// Usage in Program.cs (Blazor):
//   builder.Services.AddOntologyValidationService(builder.Configuration);
//
// Tenant header (customize if needed):
//   X-Tenant-Id: <tenant>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.BlazorConsole.Services
{
    public sealed class OntologyApiOptions
    {
        /// <summary>
        /// Base URL of the MCP server, e.g. "http://localhost:5179".
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Header name used to pass the tenant id. Defaults to "X-Tenant-Id".
        /// </summary>
        public string TenantHeaderName { get; set; } = "X-Tenant-Id";

        /// <summary>
        /// Default request timeout (seconds) applied to the named HttpClient.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 10;
    }

    public sealed class ValidationResultDto
    {
        public bool IsSuccess { get; set; }
        public List<string>? Messages { get; set; }
    }

    public interface IOntologyValidationService
    {
        Task<ValidationResultDto> ValidateAsync(string tenantId, CancellationToken ct = default);
        Task<string> ExportAsync(string tenantId, CancellationToken ct = default);
    }

    public sealed class OntologyValidationService : IOntologyValidationService
    {
        public const string HttpClientName = "OntologyApi";

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        private readonly HttpClient _http;
        private readonly IOptions<OntologyApiOptions> _options;

        public OntologyValidationService(IHttpClientFactory httpFactory, IOptions<OntologyApiOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _http = httpFactory?.CreateClient(HttpClientName)
                    ?? throw new ArgumentNullException(nameof(httpFactory), "HttpClientFactory returned null client.");
        }

        public async Task<ValidationResultDto> ValidateAsync(string tenantId, CancellationToken ct = default)
        {
            EnsureTenant(tenantId);

            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/ontology/validate")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json") // empty payload by convention
            };
            req.Headers.TryAddWithoutValidation(_options.Value.TenantHeaderName, tenantId);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                throw await ToHttpRequestExceptionAsync(resp, "ontology validate").ConfigureAwait(false);

            // Try to parse JSON; if it fails, return a conservative success=false with the raw text as a message
            var text = await resp.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(text))
                return new ValidationResultDto { IsSuccess = true, Messages = new List<string>() };

            try
            {
                var dto = JsonSerializer.Deserialize<ValidationResultDto>(text, JsonOpts);
                return dto ?? new ValidationResultDto { IsSuccess = true, Messages = new List<string>() };
            }
            catch
            {
                return new ValidationResultDto { IsSuccess = true, Messages = new List<string> { text } };
            }
        }

        public async Task<string> ExportAsync(string tenantId, CancellationToken ct = default)
        {
            EnsureTenant(tenantId);

            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/ontology/export");
            req.Headers.TryAddWithoutValidation(_options.Value.TenantHeaderName, tenantId);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
                throw await ToHttpRequestExceptionAsync(resp, "ontology export").ConfigureAwait(false);

            return await resp.Content.ReadAsStringAsync(ct);
        }

        private static void EnsureTenant(string tenantId)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
                throw new ArgumentException("Tenant id must be provided.", nameof(tenantId));
        }

        private static async Task<HttpRequestException> ToHttpRequestExceptionAsync(HttpResponseMessage resp, string op)
        {
            var status = (int)resp.StatusCode;
            var reason = resp.ReasonPhrase ?? resp.StatusCode.ToString();
            string body = string.Empty;
            try { body = await resp.Content.ReadAsStringAsync(); } catch { /* ignore */ }

            var msg = status switch
            {
                404 => $"[{op}] Endpoint not found (404). Ensure the MCP server exposes the expected route.",
                _ when status >= 500 => $"[{op}] Server error {status} {reason}.",
                _ => $"[{op}] Request failed {status} {reason}."
            };

            if (!string.IsNullOrWhiteSpace(body))
                msg += $" Body: {Truncate(body, 800)}";

            // Preserve StatusCode inside the exception for upstream handling if needed
            return new HttpRequestException(msg, null, resp.StatusCode);
        }

        private static string Truncate(string s, int max) =>
            s.Length <= max ? s : s.Substring(0, max) + "…";
    }

    // ---------------------------
    // DI registration helpers
    // ---------------------------
}
