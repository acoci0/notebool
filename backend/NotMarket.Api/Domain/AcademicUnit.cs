using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class AcademicUnit
{
    public string? CatalogKey { get; set; }

    public string? CatalogVersion { get; set; }

    public string? SourceName { get; set; }

    public DateTimeOffset? LastVerifiedAt { get; set; }
        
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid UniversityId { get; set; }

    public AcademicUniversity University { get; set; } =
        null!;

    [MaxLength(250)]
    public required string Name { get; set; }

    [MaxLength(250)]
    public required string NormalizedName { get; set; }

    public AcademicUnitType UnitType { get; set; } =
        AcademicUnitType.Faculty;

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }


    public ICollection<AcademicProgram>
        Programs
    { get; set; } =
            new List<AcademicProgram>();


    public ICollection<StudentVerification>
        StudentVerifications
    { get; set; } =
            new List<StudentVerification>();
}