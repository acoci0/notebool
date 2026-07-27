namespace NotMarket.Api.Contracts;

public sealed class StudentVerificationUploadRequest
{
    public required string UniversityName { get; init; }

    public required string FacultyName { get; init; }

    public required string DepartmentName { get; init; }

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