using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;
using ServerBMC.Infrastructure.Security;

namespace ServerBMC.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/login", LoginAsync);
        g.MapPost("/register", RegisterAsync).RequireAuthorization(p => p.RequireRole("Admin"));
        g.MapGet("/me", MeAsync).RequireAuthorization();
        g.MapGet("/roles", GetRolesAsync).RequireAuthorization();

        return app;
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest req,
        ServerBMCDbContext db,
        IJwtTokenService jwt,
        IAuditWriter audit,
        HttpContext http,
        CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == req.Email, ct);

        if (user is null || !user.IsActive || !PasswordHasher.Verify(req.Password, user.PasswordHash))
        {
            return Results.Json(ApiResponse<object>.Fail("Email hoặc mật khẩu không đúng"), statusCode: 401);
        }

        var roles = user.UserRoles.Select(ur => ur.Role).ToList();
        var token = jwt.CreateAccessToken(user, roles);

        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIP = http.Connection.RemoteIpAddress?.ToString();
        await audit.WriteAsync(db, user.Id, "Login", "User", user.Id, null, null,
            "Đăng nhập thành công", user.LastLoginIP, http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        var resp = new AuthResponse(
            token,
            DateTime.UtcNow.AddHours(8),
            new UserInfo(user.Id, user.Email, user.FullName, user.Phone, user.Avatar,
                roles.Select(r => r.Code).ToList()));

        return Results.Ok(ApiResponse<AuthResponse>.Ok(resp, "Đăng nhập thành công"));
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest req,
        ServerBMCDbContext db,
        IAuditWriter audit,
        ClaimsPrincipal principal,
        HttpContext http,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Email đã tồn tại"));

        var roles = await db.Roles.Where(r => req.RoleCodes.Contains(r.Code) && r.IsActive).ToListAsync(ct);
        if (roles.Count != req.RoleCodes.Distinct().Count())
            return Results.BadRequest(ApiResponse<object>.Fail("Một số mã vai trò không hợp lệ"));

        var user = new User
        {
            Email = req.Email,
            PasswordHash = PasswordHasher.Hash(req.Password),
            FullName = req.FullName,
            Phone = req.Phone,
            IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        foreach (var role in roles)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                IsPrimary = role.Code == "Admin"
            });
        }
        var actorId = principal.GetUserId();
        await audit.WriteAsync(db, actorId, "Create", "User", user.Id, null, new { req.Email, req.FullName },
            "Tạo người dùng mới", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);

        return Results.Ok(ApiResponse<object>.Ok(new { user.Id, user.Email }, "Tạo người dùng thành công"));
    }

    private static async Task<IResult> MeAsync(
        ServerBMCDbContext db,
        ClaimsPrincipal principal,
        CancellationToken ct)
    {
        var userId = principal.GetUserId();
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return Results.NotFound();

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        return Results.Ok(ApiResponse<UserInfo>.Ok(
            new UserInfo(user.Id, user.Email, user.FullName, user.Phone, user.Avatar, roles)));
    }

    private static async Task<IResult> GetRolesAsync(ServerBMCDbContext db, CancellationToken ct)
    {
        var roles = await db.Roles.Where(r => r.IsActive)
            .Select(r => new { r.Id, r.Code, r.Name, r.Description })
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(roles));
    }
}