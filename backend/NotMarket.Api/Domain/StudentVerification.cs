using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class StudentVerification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    [MaxLength(180)]
    public required string UniversityName { get; set; }

    [MaxLength(180)]
    public required string FacultyName { get; set; }

    [MaxLength(180)]
    public required string DepartmentName { get; set; }

    [MaxLength(500)]
    public required string DocumentBlobPath { get; set; }

    [MaxLength(128)]
    public required string DocumentHash { get; set; }

    public DateOnly DocumentIssueDate { get; set; }

    public VerificationStatus Status { get; set; }
        = VerificationStatus.Pending;

    [MaxLength(600)]
    public string? ReviewNote { get; set; }

    public Guid? ReviewedByUserId { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
        = DateTimeOffset.UtcNow;

    public bool IsExpired =>
        ExpiresAt.HasValue &&
        ExpiresAt.Value <= DateTimeOffset.UtcNow;
}

