using System.ComponentModel.DataAnnotations;

namespace NotMarket.Api.Domain;

public sealed class NotePdfGenerationArtifact
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public Guid NoteSubmissionId { get; set; }

    public NoteSubmission NoteSubmission { get; set; } =
        null!;

    [MaxLength(64)]
    public required string SourceDocumentSha256 { get; set; }

    public required string DocumentModelJson { get; set; }

    public required string LatexSource { get; set; }

    [MaxLength(100)]
    public required string ModelName { get; set; }

    [MaxLength(100)]
    public required string PromptVersion { get; set; }

    [MaxLength(100)]
    public required string TemplateVersion { get; set; }

    public DateTimeOffset ConvertedAt { get; set; }

    public DateTimeOffset RenderedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
