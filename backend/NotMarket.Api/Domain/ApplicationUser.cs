using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(160)]
    public required string Email { get; set; }

    [MaxLength(80)]
    public required string DisplayName { get; set; }

    public required string PasswordHash { get; set; }

    public UserRole Role { get; set; } = UserRole.Student;

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<StudentVerification> StudentVerifications { get; set; } = [];
    public ICollection<NoteSubmission> NoteSubmissions { get; set; } = [];
}
