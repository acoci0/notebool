using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class SiteVisit
{
    public long Id { get; set; }

    [MaxLength(64)]
    public required string SessionHash { get; set; }

    [MaxLength(300)]
    public required string Path { get; set; }

    public DateTimeOffset VisitedAt { get; set; }
        = DateTimeOffset.UtcNow;
}
