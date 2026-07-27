namespace NotMarket.Api.Services;

public interface IAuditService
{
    Task WriteAsync(
        Guid? actorUserId,
        string action,
        string entityType,
        string entityId,
        object? details = null,
        CancellationToken cancellationToken = default);
}
