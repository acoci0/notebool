namespace NotMarket.Api.Services;
using NotMarket.Api.Domain;

public sealed record NoteReviewInput(
    Guid NoteSubmissionId,
    string Title,
    string UniversityName,
    string DepartmentName,
    string CourseName,
    string? CriteriaJson,
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> DocumentBytes);


public sealed record NoteReviewComponentScores(
    int Readability,
    int CourseMatch,
    int DepartmentMatch,
    int ContentCompleteness,
    int OriginalityAndReliability);

public sealed record AiNoteEvaluation(
    bool IsTechnicallyValid,
    NoteReviewComponentScores Scores,
    int ConfidenceScore,
    string Summary,
    IReadOnlyList<string> Findings,
    string? DetectedCourse,
    string? DetectedDepartment,
    string ModelName,
    string PromptVersion);

public sealed record NoteReviewResult(
    Guid NoteSubmissionId,
    NoteReviewComponentScores Scores,
    int OverallScore,
    int OriginalityRiskScore,
    int ConfidenceScore,
    NoteReviewDecision Decision,
    string Summary,
    IReadOnlyList<string> Findings,
    string? DetectedCourse,
    string? DetectedDepartment,
    string ModelName,
    string PromptVersion,
    DateTimeOffset ReviewedAt);

public interface INoteReviewService
{
    Task<NoteReviewResult> ReviewAsync(
        NoteReviewInput input,
        CancellationToken cancellationToken);
}
