namespace NotMarket.Api.Services;

/*
 * Tamamlanan PDF üretim işleminin sonucu.
 */
public sealed record NotePdfGenerationResult(
    Guid NoteSubmissionId,
    string GeneratedPdfBlobPath,
    int GeneratedPdfBytes,
    string ContentModelName,
    string PromptVersion,
    string TemplateVersion,
    string CompilerName,
    DateTimeOffset GeneratedAt);

/*
 * Bir not gönderiminin içerik dönüştürme,
 * LaTeX oluşturma, derleme ve storage
 * işlemlerini yönetir.
 */
public interface INotePdfGenerationOrchestrator
{
    Task<NotePdfGenerationResult>
        GenerateAsync(
            Guid noteSubmissionId,
            CancellationToken cancellationToken);
}