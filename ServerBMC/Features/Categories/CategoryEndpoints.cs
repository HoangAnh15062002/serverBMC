using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Categories;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/lots/{lotId:int}/categories").WithTags("Categories")
            .RequireAuthorization();

        g.MapGet("/", ListAsync);
        g.MapGet("/{categoryId:int}", GetAsync);
        g.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director", "Engineer"));
        g.MapPut("/{categoryId:int}", UpdateAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director", "Engineer"));
        g.MapDelete("/{categoryId:int}", DeleteAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        var s = app.MapGroup("/api/categories/{categoryId:int}/subcategories").WithTags("SubCategories")
            .RequireAuthorization();

        s.MapGet("/", ListSubAsync);
        s.MapPost("/", CreateSubAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director", "Engineer"));
        s.MapPut("/{subId:int}", UpdateSubAsync).RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director", "Engineer"));
        s.MapDelete("/{subId:int}", DeleteSubAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        return app;
    }

    // ============ Categories ============

    private static async Task<IResult> ListAsync(int lotId, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.Categories.AsNoTracking()
            .Where(c => c.ProjectLotId == lotId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> GetAsync(int lotId, int categoryId, ServerBMCDbContext db, CancellationToken ct)
    {
        var c = await db.Categories.AsNoTracking()
            .Include(x => x.SubCategories)
            .FirstOrDefaultAsync(x => x.ProjectLotId == lotId && x.Id == categoryId, ct);
        return c is null ? Results.NotFound() : Results.Ok(ApiResponse<object>.Ok(c));
    }

    private static async Task<IResult> CreateAsync(
        int lotId, CategoryCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.ProjectLots.AnyAsync(l => l.Id == lotId, ct))
            return Results.NotFound(ApiResponse<object>.Fail("Lô không tồn tại"));
        if (await db.Categories.AnyAsync(c => c.ProjectLotId == lotId && c.CategoryCode == dto.CategoryCode, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Mã hạng mục đã tồn tại"));

        var userId = principal.GetUserId();
        var entity = new Domain.Entities.Category
        {
            ProjectLotId = lotId,
            CategoryCode = dto.CategoryCode,
            CategoryName = dto.CategoryName,
            CategoryType = dto.CategoryType,
            SortOrder = dto.SortOrder,
            PlannedStartDate = dto.PlannedStartDate,
            PlannedEndDate = dto.PlannedEndDate,
            Status = "Chưa bắt đầu",
            Description = dto.Description,
            CreatedBy = userId
        };
        db.Categories.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "Category", entity.Id, null, entity,
            $"Tạo hạng mục {entity.CategoryCode}", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }));
    }

    private static async Task<IResult> UpdateAsync(
        int lotId, int categoryId, CategoryUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var c = await db.Categories.FirstOrDefaultAsync(x => x.ProjectLotId == lotId && x.Id == categoryId, ct);
        if (c is null) return Results.NotFound();

        var old = new { c.CategoryName, c.Status, c.PlannedStartDate, c.PlannedEndDate, c.ProgressPercent };
        c.CategoryName = dto.CategoryName;
        c.CategoryType = dto.CategoryType;
        c.SortOrder = dto.SortOrder;
        if (dto.Status is not null) c.Status = dto.Status;
        c.PlannedStartDate = dto.PlannedStartDate;
        c.PlannedEndDate = dto.PlannedEndDate;
        c.ActualStartDate = dto.ActualStartDate;
        c.ActualEndDate = dto.ActualEndDate;
        if (dto.ProgressPercent.HasValue) c.ProgressPercent = dto.ProgressPercent.Value;
        c.Description = dto.Description;
        c.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "Category", categoryId, old, dto,
            $"Cập nhật hạng mục {c.CategoryCode}", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { categoryId }));
    }

    private static async Task<IResult> DeleteAsync(
        int lotId, int categoryId,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var c = await db.Categories.FirstOrDefaultAsync(x => x.ProjectLotId == lotId && x.Id == categoryId, ct);
        if (c is null) return Results.NotFound();
        db.Categories.Remove(c);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "Category", categoryId, c, null,
            $"Xóa hạng mục {c.CategoryCode}", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { categoryId }, "Đã xóa hạng mục"));
    }

    // ============ SubCategories ============

    private static async Task<IResult> ListSubAsync(int categoryId, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.SubCategories.AsNoTracking()
            .Where(s => s.CategoryId == categoryId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> CreateSubAsync(
        int categoryId, SubCategoryCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId, ct))
            return Results.NotFound(ApiResponse<object>.Fail("Hạng mục không tồn tại"));
        if (await db.SubCategories.AnyAsync(s => s.CategoryId == categoryId && s.SubCategoryCode == dto.SubCategoryCode, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Mã hạng mục phụ đã tồn tại"));

        var userId = principal.GetUserId();
        var entity = new Domain.Entities.SubCategory
        {
            CategoryId = categoryId,
            SubCategoryCode = dto.SubCategoryCode,
            SubCategoryName = dto.SubCategoryName,
            SubCategoryType = dto.SubCategoryType,
            SortOrder = dto.SortOrder,
            PlannedStartDate = dto.PlannedStartDate,
            PlannedEndDate = dto.PlannedEndDate,
            Status = "Chưa bắt đầu",
            Description = dto.Description,
            CreatedBy = userId
        };
        db.SubCategories.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "SubCategory", entity.Id, null, entity,
            "Tạo hạng mục phụ", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }));
    }

    private static async Task<IResult> UpdateSubAsync(
        int categoryId, int subId, SubCategoryUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var s = await db.SubCategories.FirstOrDefaultAsync(x => x.CategoryId == categoryId && x.Id == subId, ct);
        if (s is null) return Results.NotFound();
        var old = new { s.SubCategoryName, s.Status, s.PlannedStartDate, s.PlannedEndDate, s.ProgressPercent };
        s.SubCategoryName = dto.SubCategoryName;
        s.SubCategoryType = dto.SubCategoryType;
        s.SortOrder = dto.SortOrder;
        if (dto.Status is not null) s.Status = dto.Status;
        s.PlannedStartDate = dto.PlannedStartDate;
        s.PlannedEndDate = dto.PlannedEndDate;
        s.ActualStartDate = dto.ActualStartDate;
        s.ActualEndDate = dto.ActualEndDate;
        if (dto.ProgressPercent.HasValue) s.ProgressPercent = dto.ProgressPercent.Value;
        s.Description = dto.Description;
        s.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "SubCategory", subId, old, dto,
            "Cập nhật hạng mục phụ", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { subId }));
    }

    private static async Task<IResult> DeleteSubAsync(
        int categoryId, int subId,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var s = await db.SubCategories.FirstOrDefaultAsync(x => x.CategoryId == categoryId && x.Id == subId, ct);
        if (s is null) return Results.NotFound();
        db.SubCategories.Remove(s);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "SubCategory", subId, s, null,
            "Xóa hạng mục phụ", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { subId }, "Đã xóa"));
    }
}