namespace NotMarket.Api.Domain;

public sealed class AcademicProgram
{
    public string? CatalogKey { get; set; }

    public string? CatalogVersion { get; set; }

    public string? SourceName { get; set; }

    public string? DegreeLevel { get; set; }

    public string? EducationLanguage { get; set; }

    public bool IsSelectable { get; set; } = true;

    public DateTimeOffset? LastVerifiedAt { get; set; }
        
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AcademicUnitId { get; set; }

    public AcademicUnit AcademicUnit { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<StudentVerification>
        StudentVerifications { get; set; } = [];
}
