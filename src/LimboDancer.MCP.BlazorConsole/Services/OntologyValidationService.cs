using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LimboDancer.MCP.BlazorConsole.Services;

public sealed class OntologyApiOptions
{
    public string? BaseUrl { get; set; }
    public string TenantHeaderName { get; set; } = "X-Tenant-Id";
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
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation(_options.Value.TenantHeaderName, tenantId);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            throw await ToHttpRequestExceptionAsync(resp, "ontology validate").ConfigureAwait(false);

        var text = await resp.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(text))
            return new ValidationResultDto { IsSuccess = true, Messages = new List<string>() };

        try
        {
            return JsonSerializer.Deserialize<ValidationResultDto>(text, JsonOpts)
                   ?? new ValidationResultDto { IsSuccess = true, Messages = new List<string>() };
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
        string body = "";
        try { body = await resp.Content.ReadAsStringAsync(); } catch { /* ignore */ }

        var msg = status switch
        {
            404 => $"[{op}] Endpoint not found (404).",
            400 => $"[{op}] Bad request (400). Body: {body}",
            _ => $"[{op}] HTTP {(int)resp.StatusCode} ({reason}). Body: {body}"
        };
        return new HttpRequestException(msg);
    }
}