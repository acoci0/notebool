namespace NotMarket.Api.Services;

/*
 * LaTeX belgesinde gösterilecek ve
 * veritabanından gelen güvenilir bilgiler.
 */
public sealed record LatexDocumentMetadata(
    Guid NoteSubmissionId,
    string Title,
    string UniversityName,
    string DepartmentName,
    string CourseName,
    DateTimeOffset GeneratedAt);

/*
 * Renderer servisine gönderilecek bilgiler.
 */
public sealed record LatexDocumentRenderInput(
    LatexDocumentMetadata Metadata,
    NoteDocumentModel Document);

/*
 * Renderer işleminin sonucu.
 */
public sealed record LatexDocumentRenderResult(
    string Source,
    string TemplateVersion);

/*
 * Yapılandırılmış belge modelini sabit
 * LaTeX şablonuna dönüştürür.
 */
public interface ILatexDocumentRenderer
{
    LatexDocumentRenderResult Render(
        LatexDocumentRenderInput input);
}
