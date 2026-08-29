using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Contracts;

public sealed class NoteSubmissionUploadRequest
{
    public Guid RequestId { get; init; }

    [Required]
    [MaxLength(220)]
    public string Title { get; init; } =
        string.Empty;

    [Required]
    public IFormFile? Document { get; init; }
}

public sealed record NoteSubmissionCreatedResponse(
    Guid Id,
    Guid RequestId,
    string Title,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record NoteSubmissionListItemResponse(
    Guid Id,
    Guid RequestId,
    string Title,
    string UniversityName,
    string DepartmentName,
    string CourseName,
    string Status,
    decimal? SalePrice,
    int? OverallScore,
    string? ReviewDecision,
    bool GeneratedPdfAvailable,
    int PdfGenerationAttemptCount,
    DateTimeOffset? PdfGeneratedAt,
    string? PdfGenerationMessage,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset CreatedAt);

public sealed record NoteSubmissionDetailResponse(
    Guid Id,
    Guid RequestId,
    string Title,
    string UniversityName,
    string DepartmentName,
    string CourseName,
    string Status,
    decimal? SalePrice,
    bool GeneratedPdfAvailable,
    int PdfGenerationAttemptCount,
    DateTimeOffset? PdfGeneratedAt,
    string? PdfGenerationMessage,
    NoteSubmissionAiReviewResponse? AiReview,
    DateTimeOffset CreatedAt);

public sealed record NoteSubmissionAiReviewResponse(
    bool IsTechnicallyValid,
    int ReadabilityScore,
    int CourseMatchScore,
    int DepartmentMatchScore,
    int ContentCompletenessScore,
    int OriginalityAndReliabilityScore,
    int OriginalityRiskScore,
    int OverallScore,
    int ConfidenceScore,
    string Decision,
    string Summary,
    IReadOnlyList<string> Findings,
    string? DetectedCourse,
    string? DetectedDepartment,
    string ModelName,
    string PromptVersion,
    DateTimeOffset ReviewedAt);