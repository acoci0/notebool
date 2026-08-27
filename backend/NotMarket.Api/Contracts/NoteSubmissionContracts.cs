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
    