using LimboDancer.MCP.BlazorConsole.Services;
using LimboDancer.MCP.Ontology.Mapping;

var builder = WebApplication.CreateBuilder(args);

// 1) MVC/Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 2) Tenant UI state
builder.Services.AddScoped<TenantUiState>();

// 3) Tenant header delegating handler
builder.Services.AddTransient<TenantHeaderHandler>();

// 4) Ontology validation + HttpClient (unified via extension)
builder.Services.AddOntologyValidationService(builder.Configuration);

// 5) Ontology property key mapper
builder.Services.AddSingleton<IPropertyKeyMapper, DefaultPropertyKeyMapper>();

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