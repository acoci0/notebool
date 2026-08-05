namespace NotMarket.Api.Domain;

public sealed class AcademicProgram
{
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
