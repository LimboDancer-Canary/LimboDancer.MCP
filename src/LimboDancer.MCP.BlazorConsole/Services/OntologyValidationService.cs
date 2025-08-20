using System.Net.Http.Json;

namespace LimboDancer.MCP.BlazorConsole.Services;

public sealed class OntologyValidationService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public OntologyValidationService(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public async Task<IReadOnlyList<ValidatorOutcome>> RunAsync(Guid tenantId, string? package, string? channel, CancellationToken ct = default)
    {
        var baseUrl = _cfg["Server:BaseUrl"]?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Server:BaseUrl not configured.");

        var url = $"{baseUrl}/api/ontology/validate?tenant={tenantId:D}&package={Uri.EscapeDataString(package ?? "default")}&channel={Uri.EscapeDataString(channel ?? "dev")}";
        var list = await _http.GetFromJsonAsync<List<ValidatorOutcome>>(url, ct) ?? new List<ValidatorOutcome>();
        return list;
    }
}

public sealed record ValidatorOutcome(
    string Kind,
    string Id,
    string Severity,
    string Message,
    DateTimeOffset At);