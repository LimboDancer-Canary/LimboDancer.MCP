using LimboDancer.MCP.McpServer.Http.Chat;
using LimboDancer.MCP.McpServer.Http.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Tenancy accessor is already registered in your code as Core.Tenancy.ITenantAccessor
builder.Services.AddScoped<LimboDancer.MCP.Core.Tenancy.ITenantAccessor, LimboDancer.MCP.McpServer.Http.Tenancy.HttpTenantAccessor>();

// AuthN/AuthZ
builder.Services.AddApiAuthentication(builder.Configuration);

// CORS (tighten origins in appsettings)
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Controllers + minimal APIs
builder.Services.AddControllers();

// Chat orchestrator (MVP)
builder.Services.AddSingleton<IChatOrchestrator, InMemoryChatOrchestrator>();

// OpenAPI (dev only)
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Example existing endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();