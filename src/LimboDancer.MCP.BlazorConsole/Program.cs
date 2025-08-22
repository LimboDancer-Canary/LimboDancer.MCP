// File: /src/LimboDancer.MCP.BlazorConsole/Program.cs

using System.Net.Http.Headers;
using LimboDancer.MCP.BlazorConsole.Services;
using Microsoft.Extensions.Options;
using LimboDancer.MCP.Ontology.Mapping;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------
// 1) MVC/Blazor
// --------------------------------------------
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// --------------------------------------------
// 2) Tenant UI state (scoped, per-user connection)
//    Exposed to components via CascadingParameter and to handlers/services via DI
// --------------------------------------------
builder.Services.AddScoped<TenantUiState>();

// --------------------------------------------
// 3) Ontology API options from config
//    appsettings.json:
//      "OntologyApi": {
//        "BaseUrl": "http://localhost:5179",
//        "TenantHeaderName": "X-Tenant-Id",
//        "TimeoutSeconds": 10
//      }
// --------------------------------------------
builder.Services.Configure<OntologyApiOptions>(builder.Configuration.GetSection("OntologyApi"));

// --------------------------------------------
// 4) Delegating handler that appends tenant header
//    - Adds header ONLY if not already present on the request
//    - Reads header name from OntologyApiOptions
//    - Reads current tenant from TenantUiState
// --------------------------------------------
builder.Services.AddTransient<TenantHeaderHandler>();

// --------------------------------------------
// 5) Named HttpClient for the MCP server ("OntologyApi")
//    - BaseAddress & default Accept header
//    - Timeout from options
//    - TenantHeaderHandler to propagate tenant automatically
// --------------------------------------------
builder.Services.AddHttpClient(OntologyValidationService.HttpClientName, (sp, http) =>
{
    var opts = sp.GetRequiredService<IOptions<OntologyApiOptions>>().Value;
    if (string.IsNullOrWhiteSpace(opts.BaseUrl))
        throw new InvalidOperationException("Configuration 'OntologyApi:BaseUrl' is required.");

    http.BaseAddress = new Uri(opts.BaseUrl!, UriKind.Absolute);
    http.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 2, 60));
    http.DefaultRequestHeaders.Accept.Clear();
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<TenantHeaderHandler>();

// --------------------------------------------
// 6) Ontology validation service
//    (uses the named client above)
// --------------------------------------------
builder.Services.AddScoped<IOntologyValidationService, OntologyValidationService>();

builder.Services.AddSingleton<IPropertyKeyMapper, DefaultPropertyKeyMapper>();

// --------------------------------------------
// 7) App pipeline
// --------------------------------------------
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();


// =======================================================================
// Support types (same namespace as the app to avoid separate files)
// =======================================================================

namespace LimboDancer.MCP.BlazorConsole.Services
{
    /// <summary>
    /// Minimal tenant UI state you can cascade into pages/components.
    /// </summary>
    public sealed class TenantUiState
    {
        public string? CurrentTenantId { get; set; }
    }

    /// <summary>
    /// Delegating handler that ensures the tenant header is present on outgoing requests.
    /// If the request already added the header explicitly, this handler leaves it alone.
    /// </summary>
    public sealed class TenantHeaderHandler : DelegatingHandler
    {
        private readonly TenantUiState _tenant;
        private readonly IOptions<OntologyApiOptions> _options;

        public TenantHeaderHandler(TenantUiState tenant, IOptions<OntologyApiOptions> options)
        {
            _tenant = tenant ?? throw new ArgumentNullException(nameof(tenant));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headerName = _options.Value.TenantHeaderName ?? "X-Tenant-Id";
            var tenantId = _tenant.CurrentTenantId;

            // Only add if a tenant is known AND header not already present.
            if (!string.IsNullOrWhiteSpace(tenantId) &&
                !request.Headers.Contains(headerName))
            {
                request.Headers.TryAddWithoutValidation(headerName, tenantId);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
