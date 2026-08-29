namespace NotMarket.Api.Contracts;

public sealed record DashboardSummaryDto(
    int TotalUsers,
    int ActiveUsers,
    int PendingVerifications,
    int PendingNoteReviews,
    int ApprovedNotes,
    decimal TotalRevenue,
    IReadOnlyList<RecentActivityDto> RecentActivities);

public sealed record RecentActivityDto(
    string Action,
    string EntityType,
    string EntityId,
    DateTimeOffset CreatedAt);

public sealed record AdminUserListItemDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    int VerificationCount,
    DateTimeOffset CreatedAt);

public sealed record AdminVerificationDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    string UniversityName,
    string FacultyName,
    string DepartmentName,
    DateOnly DocumentIssueDate,
    string Status,
    string DocumentBlobPath,
    string? ReviewNote,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record AdminVerificationDetailDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string UserEmail,
    string UniversityName,
    string FacultyName,
    string DepartmentName,
    DateOnly DocumentIssueDate,
    string Status,
    string DocumentBlobPath,
    string DocumentHash,
    string? ReviewNote,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);

public sealed record AdminNoteSubmissionDto(
    Guid Id,
    string Title,
    string SellerName,
    string UniversityName,
    string DepartmentName,
    string CourseName,
    int MatchScore,
    int ReadabilityScore,
    int OriginalityRiskScore,
    decimal SuggestedMinPrice,
    decimal SuggestedMaxPrice,
    decimal? SalePrice,
    string Status,
    string? GeneratedPdfBlobPath,
    int PdfGenerationAttemptCount,
    string? PdfGenerationError,
    DateTimeOffset? PdfGeneratedAt,
    string? PdfGenerationModelName,
    string? PdfConversionPromptVersion,
    string? PdfTemplateVersion,
    string? PdfCompilerName,
    DateTimeOffset CreatedAt);

/*
 * Öğrenci doğrulama kararı için kullanılır.
 */
public sealed record ReviewDecisionRequest(
    bool Approve,
    string? ReviewNote);

/*
 * Not onayında satış fiyatı da belirlenir.
 */
public sealed record NoteReviewDecisionRequest(
    bool Approve,
    string? ReviewNote,
    decimal? SalePrice);

public sealed record AccountStatusRequest(
    string Status);