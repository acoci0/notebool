using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class NoteSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RequestId { get; set; }
    public NoteRequest Request { get; set; } = null!;

    public Guid SellerId { get; set; }
    public ApplicationUser Seller { get; set; } = null!;

    [MaxLength(220)]
    public required string Title { get; set; }

    [MaxLength(500)]
    public required string OriginalBlobPath { get; set; }

    [MaxLength(500)]
    public string? GeneratedPdfBlobPath { get; set; }

    public int MatchScore { get; set; }
    public int ReadabilityScore { get; set; }
    public int OriginalityRiskScore { get; set; }

    public decimal? SalePrice { get; set; }

    public NoteSubmissionStatus Status { get; set; } =
        NoteSubmissionStatus.Uploaded;

    [MaxLength(800)]
    public string? ReviewNote { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<NoteAiReview> AiReviews { get; set; } =
        [];
    public ICollection<Order> Orders { get; set; } =
        [];
}
