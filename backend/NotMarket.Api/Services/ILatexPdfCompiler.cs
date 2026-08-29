namespace NotMarket.Api.Services;

/*
 * PDF derleyicisine gönderilecek bilgiler.
 */
public sealed record LatexPdfCompilationInput(
    Guid NoteSubmissionId,
    string LatexSource);

/*
 * Başarılı PDF derleme işleminin sonucu.
 */
public sealed record LatexPdfCompilationResult(
    byte[] PdfBytes,
    string CompilerName,
    string CompilerOutput,
    DateTimeOffset CompiledAt);

/*
 * LaTeX kaynağını izole bir geçici klasörde
 * PDF dosyasına dönüştürür.
 */
public interface ILatexPdfCompiler
{
    Task<LatexPdfCompilationResult>
        CompileAsync(
            LatexPdfCompilationInput input,
            CancellationToken cancellationToken);
}
