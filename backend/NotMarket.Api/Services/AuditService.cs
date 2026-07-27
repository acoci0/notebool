using System.Text.Json;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Services;

public sealed class AuditService(AppDbContext db) : IAuditService
{
    public async Task WriteAsync(
        Guid? actorUserId,
        string action,
        string entityType,
        string entityId,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = details is null
                ? null
                : JsonSerializer.Serialize(details)
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
