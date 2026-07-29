using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerBMC.Common;
using ServerBMC.DTOs;
using ServerBMC.Infrastructure.Data;
using ServerBMC.Services;

namespace ServerBMC.Features.Estimates;

public static class ExcelImportEndpoints
{
    public static IEndpointRouteBuilder MapExcelImportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/estimates").WithTags("Estimates - Import")
            .RequireAuthorization();

        g.MapPost("/import", ImportFromExcelAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "VP", "Director"));

        return app;
    }

    private static async Task<IResult> ImportFromExcelAsync(
        [FromBody] ImportExcelDto dto,
        ServerBMCDbContext db,
        IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.FilePath))
            return Results.BadRequest(ApiResponse<object>.Fail("Đường dẫn file không hợp lệ"));

        // Resolve path
        string filePath = dto.FilePath;
        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.Combine(env.ContentRootPath, filePath);
        }

        if (!File.Exists(filePath))
            return Results.NotFound(ApiResponse<object>.Fail($"File không tồn tại: {filePath}"));

        try
        {
            var service = new ExcelImportService(db);
            var estimateId = await service.ImportFromExcel(filePath, ct);

            return Results.Ok(ApiResponse<object>.Ok(new 
            { 
                estimateId,
                message = "Import thành công từ Excel"
            }, "Import dữ liệu từ Excel thành công"));
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ApiResponse<object>.Fail($"Lỗi import: {ex.Message}"));
        }
    }
}
