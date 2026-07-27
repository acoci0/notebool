using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class NoteRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BuyerId { get; set; }

    [MaxLength(180)]
    public required string UniversityName { get; set; }

    [MaxLength(180)]
    public required string DepartmentName { get; set; }

    [MaxLength(180)]
    public required string CourseName { get; set; }

    public int ClassLevel { get; set; }

    [MaxLength(1400)]
    public required string CriteriaJson { get; set; }

    public decimal SuggestedMinPrice { get; set; }
    public decimal SuggestedMaxPrice { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<NoteSubmission> Submissions { get; set; } = [];
}
