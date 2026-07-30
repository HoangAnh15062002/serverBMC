using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServerBMC.Features.AuditLogs;
using ServerBMC.Features.Auth;
using ServerBMC.Features.Categories;
using ServerBMC.Features.Estimates;
using ServerBMC.Features.PaymentPlans;
using ServerBMC.Features.Progress;
using ServerBMC.Features.Projects;
using ServerBMC.Features.Reports;
using ServerBMC.Features.WorkItems;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;
using ServerBMC.Infrastructure.Errors;
using ServerBMC.Infrastructure.Security;
using ServerBMC.Infrastructure.Swagger;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Configuration
// ============================================================
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
                 ?? throw new InvalidOperationException("Missing Jwt configuration");
builder.Services.AddSingleton(jwtOptions);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection");

// ============================================================
// Database
// ============================================================
builder.Services.AddDbContext<ServerBMCDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// ============================================================
// Auth & Security
// ============================================================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.FromMinutes(5),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// DI
// ============================================================
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();

// ============================================================
// Swagger / OpenAPI
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ServerBMC API",
        Version = "v1",
        Description = "Backend API quản lý dự án xây dựng — Phase 1"
    });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Nhập JWT token: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", bearerScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearerScheme] = Array.Empty<string>()
    });

    // Operation filter để thêm security icon vào từng endpoint
    c.OperationFilter<AuthorizeOperationFilter>();

    // Bỏ required markers
    c.SchemaFilter<NonRequiredSchemaFilter>();
});

// ============================================================
// CORS
// ============================================================
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(p => p
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ============================================================
// Build
// ============================================================
var app = builder.Build();

// ============================================================
// Seed data (roles, users, sample projects)
// ============================================================
try
{
    await DataSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Seed data failed — continuing startup (DB may not be reachable yet)");
}

app.UseGlobalExceptionHandler();

// Swagger UI (luôn bật để demo; production nên đặt sau auth)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ServerBMC API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ============================================================
// Map Endpoints
// ============================================================
app.MapGet("/", () => Results.Ok(new { app = "ServerBMC", status = "running", version = "v1" }));
app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTime.UtcNow }));

// Debug endpoint - decode token
app.MapGet("/debug/decode", (HttpContext ctx) =>
{
    var token = ctx.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
        return Results.BadRequest("No token");

    try
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
            return Results.BadRequest("Invalid JWT format");

        string DecodeBase64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            switch (s.Length % 4)
            {
                case 2: s += "=="; break;
                case 3: s += "="; break;
            }
            var bytes = Convert.FromBase64String(s);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        var payload = DecodeBase64(parts[1]);
        var json = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(payload);

        return Results.Ok(new
        {
            header = DecodeBase64(parts[0]),
            payload = json.GetProperty("iss").GetString(),
            aud = json.TryGetProperty("aud", out var aud) ? aud.GetString() : null,
            sub = json.TryGetProperty("sub", out var sub) ? sub.GetString() : null,
            exp = json.TryGetProperty("exp", out var exp) ? DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) : (DateTimeOffset?)null,
            iat = json.TryGetProperty("iat", out var iat) ? DateTimeOffset.FromUnixTimeSeconds(iat.GetInt64()) : (DateTimeOffset?)null,
            now = DateTimeOffset.UtcNow,
            roles = json.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var role) 
                ? role.GetString() 
                : "NO ROLE CLAIM",
            rawPayload = payload
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest($"Error: {ex.Message}");
    }
}).ExcludeFromDescription();

app.MapAuthEndpoints();
app.MapProjectEndpoints();
app.MapCategoryEndpoints();
app.MapWorkItemEndpoints();
app.MapProgressEndpoints();
app.MapPaymentPlanEndpoints();
app.MapReportEndpoints();
app.MapAuditLogEndpoints();
app.MapEstimateEndpoints();

app.Run();