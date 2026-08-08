namespace NotMarket.Api.Contracts;

public sealed class StudentVerificationUploadRequest
{
    /*
     * Canonical üniversite ID'si.
     */
    public required Guid UniversityId { get; init; }

    /*
     * Canonical fakülte, enstitü,
     * yüksekokul veya diğer akademik
     * birim ID'si.
     */
    public Guid AcademicUnitId { get; init; }

    /*
     * Canonical bölüm veya program ID'si.
     */
    public Guid AcademicProgramId { get; init; }

    /*
     * Bu iki metin alanı mevcut controller
     * henüz onları kullandığı için geçici olarak
     * korunmaktadır.
     *
     * Controller ve frontend yeni ID sistemine
     * geçirildikten sonra kaldırılacaklar.
     */
    public string FacultyName { get; init; } =
        string.Empty;

    public string DepartmentName { get; init; } =
        string.Empty;

    public required string DocumentIssueDate { get; init; }

    public required IFormFile Document { get; init; }
}

public sealed record StudentVerificationCreatedResponse(
    Guid Id,
    string UniversityName,
    string FacultyName,
    string DepartmentName,
    string Status,
    DateOnly DocumentIssueDate,
    DateTimeOffset CreatedAt);

public sealed record StudentVerificationListItemResponse(
    Guid Id,
    string UniversityName,
    string FacultyName,
    string DepartmentName,
    string Status,
    DateOnly DocumentIssueDate,
    string? ReviewNote,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset CreatedAt);