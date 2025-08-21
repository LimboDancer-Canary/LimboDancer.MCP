using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace LimboDancer.MCP.McpServer.Http.Infrastructure;

public static class AuthExtensions
{
    public static IServiceCollection AddApiAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var section = config.GetSection("Authentication:Jwt");
        var authority = section["Authority"];
        var audience = section["Audience"];

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = "roles"
                };
                // Map tid -> tenant_id for consistency if needed
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var claims = ctx.Principal!.Claims.ToList();
                        var tid = claims.FirstOrDefault(c => c.Type == "tid")?.Value;
                        if (tid is not null && ctx.Principal!.FindFirst("tenant_id") is null)
                        {
                            var id = new ClaimsIdentity();
                            id.AddClaim(new Claim("tenant_id", tid));
                            ctx.Principal!.AddIdentity(id);
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("ChatUser", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("tenant_id")
                      .RequireRole("ChatUser"));

            options.AddPolicy("Operator", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("tenant_id")
                      .RequireRole("Operator"));

            options.AddPolicy("Admin", policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("tenant_id")
                      .RequireRole("Admin"));
        });

        return services;
    }
}