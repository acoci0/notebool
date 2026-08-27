using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class NoteAiReview
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid NoteSubmissionId { get; set; }

    public NoteSubmission NoteSubmission { get; set; } =
        null!;

    public bool IsTechnicallyValid { get; set; }

    public int ReadabilityScore { get; set; }

    public int CourseMatchScore { get; set; }

    public int DepartmentMatchScore { get; set; }

    public int ContentCompletenessScore { get; set; }

    public int OriginalityAndReliabilityScore { get; set; }

    public int OriginalityRiskScore { get; set; }

    public int OverallScore { get; set; }

    public int ConfidenceScore { get; set; }

    public NoteReviewDecision Decision { get; set; }

    [MaxLength(2000)]
    public required string Summary { get; set; }

    /*
     * AI tarafından üretilen bulgular JSON
     * dizisi olarak saklanır.
     */
    public string FindingsJson { get; set; } =
        "[]";

    [MaxLength(220)]
    public string? DetectedCourse { get; set; }

    [MaxLength(220)]
    public string? DetectedDepartment { get; set; }

    [MaxLength(100)]
    public required string ModelName { get; set; }

    [MaxLength(50)]
    public required string PromptVersion { get; set; }

    public DateTimeOffset ReviewedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
