using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ServerBMC.Common;
using ServerBMC.Domain.Entities;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Audit;
using ServerBMC.Infrastructure.Data;

namespace ServerBMC.Features.Reports;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reports").WithTags("Reports")
            .RequireAuthorization();

        g.MapGet("/", ListAsync);
        g.MapGet("/{id:int}", GetAsync);
        g.MapPost("/", CreateAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "VP", "Director"));
        g.MapPut("/{id:int}", UpdateAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "VP", "Director"));
        g.MapDelete("/{id:int}", DeleteAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        g.MapPost("/{id:int}/submit", SubmitAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "VP", "Director"));
        g.MapPost("/{id:int}/approve", ApproveAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        // Attachments
        var att = app.MapGroup("/api/reports/{reportId:int}/attachments").WithTags("ReportAttachments")
            .RequireAuthorization();
        att.MapGet("/", ListAttachmentsAsync);
        att.MapPost("/", RegisterAttachmentAsync).RequireAuthorization(p => p.RequireRole("Admin", "Accountant", "VP", "Director"));
        att.MapDelete("/{id:int}", DeleteAttachmentAsync).RequireAuthorization(p => p.RequireRole("Admin", "Director"));

        return app;
    }

    private static async Task<IResult> ListAsync(
        string? status, string? type, int? projectId,
        ServerBMCDbContext db, CancellationToken ct)
    {
        var q = db.Reports.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(r => r.Status == status);
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(r => r.ReportType == type);
        if (projectId.HasValue) q = q.Where(r => r.ProjectId == projectId.Value);
        var items = await q.OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> GetAsync(int id, ServerBMCDbContext db, CancellationToken ct)
    {
        var r = await db.Reports.AsNoTracking()
            .Include(x => x.Attachments)
            .Include(x => x.Approvals)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return r is null ? Results.NotFound() : Results.Ok(ApiResponse<object>.Ok(r));
    }

    private static async Task<IResult> CreateAsync(
        ReportCreateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (await db.Reports.AnyAsync(r => r.ReportCode == dto.ReportCode, ct))
            return Results.BadRequest(ApiResponse<object>.Fail("Mã báo cáo đã tồn tại"));

        var userId = principal.GetUserId();
        var entity = new Domain.Entities.Report
        {
            ReportCode = dto.ReportCode,
            ReportTitle = dto.ReportTitle,
            ReportType = dto.ReportType,
            ProjectId = dto.ProjectId,
            PeriodFrom = dto.PeriodFrom,
            PeriodTo = dto.PeriodTo,
            Content = dto.Content,
            Status = "Nháp",
            CreatedBy = userId
        };
        db.Reports.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Create", "Report", entity.Id, null, entity,
            "Tạo báo cáo", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }));
    }

    private static async Task<IResult> UpdateAsync(
        int id, ReportUpdateDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var r = await db.Reports.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Results.NotFound();
        if (r.Status == "Đã duyệt")
            return Results.BadRequest(ApiResponse<object>.Fail("Báo cáo đã duyệt, không thể sửa"));

        var old = new { r.ReportTitle, r.Content };
        r.ReportTitle = dto.ReportTitle;
        r.Content = dto.Content;
        r.PeriodFrom = dto.PeriodFrom;
        r.PeriodTo = dto.PeriodTo;
        r.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, principal.GetUserId(), "Update", "Report", id, old, dto,
            "Cập nhật báo cáo", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }

    private static async Task<IResult> DeleteAsync(
        int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var r = await db.Reports.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Results.NotFound();
        db.Reports.Remove(r);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "Report", id, r, null,
            "Xóa báo cáo", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }

    private static async Task<IResult> SubmitAsync(
        int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var r = await db.Reports.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Results.NotFound();
        if (r.Status != "Nháp")
            return Results.BadRequest(ApiResponse<object>.Fail($"Báo cáo đang ở trạng thái '{r.Status}', không thể gửi duyệt"));
        r.Status = "Chờ duyệt";
        r.UpdatedAt = DateTime.UtcNow;
        await audit.WriteAsync(db, principal.GetUserId(), "Submit", "Report", id, null, null,
            "Gửi duyệt báo cáo", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }

    private static async Task<IResult> ApproveAsync(
        int id, ApprovalRequestDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var r = await db.Reports.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return Results.NotFound();
        if (r.Status != "Chờ duyệt")
            return Results.BadRequest(ApiResponse<object>.Fail("Báo cáo không ở trạng thái chờ duyệt"));

        var approverId = principal.GetUserId();
        db.ReportApprovals.Add(new ReportApproval
        {
            ReportId = id,
            ApproverId = approverId,
            ApprovalStatus = dto.ApprovalStatus,
            Comments = dto.Comments
        });
        r.Status = dto.ApprovalStatus == "Duyet" ? "Đã duyệt"
                 : dto.ApprovalStatus == "TuChoi" ? "Từ chối" : r.Status;
        r.UpdatedAt = DateTime.UtcNow;

        await audit.WriteAsync(db, approverId,
            dto.ApprovalStatus == "Duyet" ? "Approve" : "Reject",
            "Report", id, null, dto,
            $"{(dto.ApprovalStatus == "Duyet" ? "Duyệt" : "Từ chối")} báo cáo",
            http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id, r.Status }));
    }

    private static async Task<IResult> ListAttachmentsAsync(int reportId, ServerBMCDbContext db, CancellationToken ct)
    {
        var items = await db.ReportAttachments.AsNoTracking()
            .Where(a => a.ReportId == reportId)
            .OrderByDescending(a => a.UploadedAt)
            .Select(a => new ReportAttachmentDto(
                a.Id, a.FileName, a.FileOriginalName, a.FileSize, a.FileType,
                a.FileCategory, a.UploadedAt, a.UploadedBy))
            .ToListAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(items));
    }

    private static async Task<IResult> RegisterAttachmentAsync(
        int reportId, ReportAttachmentDto dto,
        ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        if (!await db.Reports.AnyAsync(r => r.Id == reportId, ct))
            return Results.NotFound(ApiResponse<object>.Fail("Báo cáo không tồn tại"));
        var userId = principal.GetUserId();
        var entity = new ReportAttachment
        {
            ReportId = reportId,
            FileName = dto.FileName,
            FileOriginalName = dto.FileOriginalName,
            FilePath = $"uploads/reports/{reportId}/{dto.FileName}", // server upload sẽ tự ghi FilePath
            FileSize = dto.FileSize,
            FileType = dto.FileType,
            FileCategory = dto.FileCategory,
            UploadedBy = userId
        };
        db.ReportAttachments.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(db, userId, "Upload", "ReportAttachment", entity.Id, null, entity,
            "Đính kèm file báo cáo", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { entity.Id }));
    }

    private static async Task<IResult> DeleteAttachmentAsync(
        int reportId, int id, ServerBMCDbContext db, IAuditWriter audit,
        ClaimsPrincipal principal, HttpContext http, CancellationToken ct)
    {
        var a = await db.ReportAttachments.FirstOrDefaultAsync(x => x.ReportId == reportId && x.Id == id, ct);
        if (a is null) return Results.NotFound();
        db.ReportAttachments.Remove(a);
        await audit.WriteAsync(db, principal.GetUserId(), "Delete", "ReportAttachment", id, a, null,
            "Xóa file đính kèm", http.Connection.RemoteIpAddress?.ToString(),
            http.Request.Headers.UserAgent.ToString(), ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(ApiResponse<object>.Ok(new { id }));
    }
}