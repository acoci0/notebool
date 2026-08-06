using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class AcademicUniversity
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    [MaxLength(100)]
    public string? CatalogKey { get; set; }

    [MaxLength(250)]
    public required string Name { get; set; }

    [MaxLength(250)]
    public required string NormalizedName { get; set; }


    [MaxLength(2)]
    public string CountryCode { get; set; } =
        "TR";
    [MaxLength(100)]
    
    public string? City { get; set; }

    [MaxLength(50)]
    
    public string? CatalogVersion { get; set; }

    [MaxLength(200)]

    public string? SourceName { get; set; }

    public DateTimeOffset? LastVerifiedAt { get; set; }

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }


    public ICollection<AcademicUniversityAlias>
        Aliases { get; set; } =
            new List<AcademicUniversityAlias>();


    public ICollection<AcademicUnit>
        AcademicUnits { get; set; } =
            new List<AcademicUnit>();


    public ICollection<StudentVerification>
        StudentVerifications { get; set; } =
            new List<StudentVerification>();
}