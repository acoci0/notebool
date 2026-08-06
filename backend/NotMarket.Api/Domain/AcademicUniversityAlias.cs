namespace NotMarket.Api.Domain;

public sealed class AcademicUniversityAlias
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UniversityId { get; set; }

    public AcademicUniversity University { get; set; } =
        null!;

    public string Alias { get; set; } = string.Empty;

    public string NormalizedAlias { get; set; } =
        string.Empty;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
