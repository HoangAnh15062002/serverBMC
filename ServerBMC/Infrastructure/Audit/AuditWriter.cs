namespace ServerBMC.Infrastructure.Audit;

using ServerBMC.Domain.Entities;
using ServerBMC.Infrastructure.Data;

public interface IAuditWriter
{
    Task WriteAsync(ServerBMCDbContext db, int? userId, string action, string entityType,
                    int? entityId, object? oldValues, object? newValues, string? description,
                    string? ip, string? userAgent, CancellationToken ct = default);
}

public class AuditWriter : IAuditWriter
{
    public async Task WriteAsync(ServerBMCDbContext db, int? userId, string action, string entityType,
                    int? entityId, object? oldValues, object? newValues, string? description,
                    string? ip, string? userAgent, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues is null ? null : System.Text.Json.JsonSerializer.Serialize(oldValues),
            NewValues = newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues),
            Description = description,
            IPAddress = ip,
            UserAgent = userAgent
        });
        await Task.CompletedTask;
    }
}