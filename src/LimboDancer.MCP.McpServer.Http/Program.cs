using LimboDancer.MCP.McpServer.Http.Chat;
using LimboDancer.MCP.McpServer.Http.Infrastructure;
using LimboDancer.MCP.McpServer.Tenancy;
using LimboDancer.MCP.Storage;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Tenancy accessor - use the correct one from McpServer.Tenancy namespace
builder.Services.AddScoped<LimboDancer.MCP.Core.Tenancy.ITenantAccessor, HttpTenantAccessor>();

// Storage
builder.Services.AddStorage(builder.Configuration);

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
builder.Services.AddHostedService<ChatOrchestratorCleanupService>();

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

// Health endpoints
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/ready", async (IServiceProvider sp) =>
{
    try
    {
        // Check database connectivity
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var canConnect = await dbContext.Database.CanConnectAsync();

        if (!canConnect)
            return Results.Json(new { status = "not ready", database = "disconnected" }, statusCode: 503);

        // Initialize persistence if configured
        var config = sp.GetRequiredService<IConfiguration>();
        if (config.GetValue<bool>("Storage:ApplyMigrationsAtStartup"))
        {
            await dbContext.Database.MigrateAsync();
        }

        return Results.Ok(new { status = "ready", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "not ready", error = ex.Message }, statusCode: 503);
    }
});

app.Run();