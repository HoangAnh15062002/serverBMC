using Microsoft.AspNetCore.Diagnostics;
using ServerBMC.Common;

namespace ServerBMC.Infrastructure.Errors;

public static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = feature?.Error;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("GlobalExceptionHandler");
                logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);

                context.Response.StatusCode = ex switch
                {
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    InvalidOperationException => StatusCodes.Status400BadRequest,
                    ArgumentException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };
                context.Response.ContentType = "application/json";

                var msg = context.Response.StatusCode == 500
                    ? "Lỗi hệ thống, vui lòng thử lại sau"
                    : ex?.Message ?? "Lỗi không xác định";

                await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body,
                    ApiResponse<object>.Fail(msg),
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                    });
            });
        });
    }
}