using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ServerBMC.Features.AuditLogs;
using ServerBMC.Features.Auth;
using ServerBMC.Features.Categories;
using ServerBMC.Features.PaymentPlans;
using ServerBMC.Features.Progress;
using ServerBMC.Features.Projects;
using ServerBMC.Features.Reports;
using ServerBMC.Features.WorkItems;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;
using ServerBMC.Infrastructure.Errors;
using ServerBMC.Infrastructure.Security;

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
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
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
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", bearerScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [bearerScheme] = Array.Empty<string>()
    });
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

app.MapAuthEndpoints();
app.MapProjectEndpoints();
app.MapCategoryEndpoints();
app.MapWorkItemEndpoints();
app.MapProgressEndpoints();
app.MapPaymentPlanEndpoints();
app.MapReportEndpoints();
app.MapAuditLogEndpoints();

app.Run();