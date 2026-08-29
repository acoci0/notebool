namespace NotMarket.Api.Services;

/*
 * OpenAI içerik dönüştürme işlemine
 * gönderilecek bilgiler.
 */
public sealed record NoteContentConversionInput(
    Guid NoteSubmissionId,
    string Title,
    string UniversityName,
    string DepartmentName,
    string CourseName,
    string? CriteriaJson,
    string FileName,
    string ContentType,
    ReadOnlyMemory<byte> DocumentBytes);

/*
 * İçerik dönüştürme işleminin sonucu.
 */
public sealed record NoteContentConversionResult(
    Guid NoteSubmissionId,
    NoteDocumentModel Document,
    string ModelName,
    string PromptVersion,
    DateTimeOffset ConvertedAt);

/*
 * Orijinal not PDF'ini yapılandırılmış
 * belge modeline dönüştürür.
 */
public interface INoteContentConversionService
{
    Task<NoteContentConversionResult>
        ConvertAsync(
            NoteContentConversionInput input,
            CancellationToken cancellationToken);
}
