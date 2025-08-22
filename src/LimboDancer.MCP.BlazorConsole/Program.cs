using LimboDancer.MCP.BlazorConsole;
using LimboDancer.MCP.BlazorConsole.Services;
using LimboDancer.MCP.Ontology.Mapping;

var builder = WebApplication.CreateBuilder(args);

// 1) MVC/Blazor
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 2) BlazorConsole services (tenant UI state, validation, mappers)
builder.Services.AddBlazorConsoleServices(builder.Configuration);

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