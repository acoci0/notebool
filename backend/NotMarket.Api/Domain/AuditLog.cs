using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class AuditLog
{
    public long Id { get; set; }

    public Guid? ActorUserId { get; set; }

    [MaxLength(100)]
    public required string Action { get; set; }

    [MaxLength(100)]
    public required string EntityType { get; set; }

    [MaxLength(100)]
    public required string EntityId { get; set; }

    [MaxLength(2000)]
    public string? DetailsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
