using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace LimboDancer.MCP.BlazorConsole.Services;

public static class OntologyValidationServiceRegistration
{
    public static IServiceCollection AddOntologyValidationService(
        this IServiceCollection services,
        IConfiguration config,
        string sectionName = "OntologyApi")
    {
        services.Configure<OntologyApiOptions>(config.GetSection(sectionName));

        // Validate options early
        using var sp = services.BuildServiceProvider();
        var tmp = sp.GetRequiredService<IOptions<OntologyApiOptions>>().Value;
        if (string.IsNullOrWhiteSpace(tmp.BaseUrl))
            throw new InvalidOperationException($"Configuration '{sectionName}:BaseUrl' is required for Ontology API.");

        services.AddHttpClient(OntologyValidationService.HttpClientName, (provider, http) =>
        {
            var opts = provider.GetRequiredService<IOptions<OntologyApiOptions>>().Value;

            http.BaseAddress = new Uri(opts.BaseUrl!, UriKind.Absolute);
            http.Timeout = TimeSpan.FromSeconds(Math.Clamp(opts.TimeoutSeconds, 2, 60));
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        services.AddScoped<IOntologyValidationService, OntologyValidationService>();
        return services;
    }
}