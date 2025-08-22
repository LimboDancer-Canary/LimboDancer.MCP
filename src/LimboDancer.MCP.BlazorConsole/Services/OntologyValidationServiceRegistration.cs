using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.BlazorConsole.Services;

public static class OntologyValidationServiceRegistration
{
    public static IServiceCollection AddOntologyValidationService(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = "OntologyApi")
    {
        services
            .AddOptions<OntologyApiOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseUrl), $"{sectionName}:BaseUrl is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.TenantHeaderName), $"{sectionName}:TenantHeaderName is required.")
            .Validate(o => o.TimeoutSeconds is >= 2 and <= 60, $"{sectionName}:TimeoutSeconds must be between 2 and 60.")
            .ValidateOnStart();

        services.AddHttpClient(OntologyValidationService.HttpClientName, (sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<OntologyApiOptions>>().Value;
                http.BaseAddress = new Uri(opts.BaseUrl!, UriKind.Absolute);
                http.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 2, 60));
                http.DefaultRequestHeaders.Accept.Clear();
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<TenantHeaderHandler>();

        services.AddScoped<IOntologyValidationService, OntologyValidationService>();
        return services;
    }
}