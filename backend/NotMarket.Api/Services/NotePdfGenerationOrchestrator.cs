using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Services;

public sealed class NotePdfGenerationOrchestrator(
    AppDbContext db,
    INoteDocumentStorage storage,
    INoteContentConversionService
        contentConversionService,
    ILatexDocumentRenderer documentRenderer,
    ILatexPdfCompiler pdfCompiler,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<NotePdfGenerationOrchestrator> logger)
    : INotePdfGenerationOrchestrator
{
    private readonly OpenAiOptions
        _openAiOptions =
            openAiOptions.Value;

    public async Task<NotePdfGenerationResult>
        GenerateAsync(
            Guid noteSubmissionId,
            CancellationToken cancellationToken)
    {
        if (noteSubmissionId == Guid.Empty)
        {
            throw new ArgumentException(
                "PDF üretilecek not gönderim ID'si geçersiz.",
                nameof(noteSubmissionId));
        }

        /*
         * Aynı notun iki worker tarafından aynı
         * anda işlenmesini engellemek için kayıt
         * atomik olarak PdfGenerating durumuna
         * geçirilir.
         */
        var claimed =
            await db.NoteSubmissions
                .Where(
                    x =>
                        x.Id ==
                            noteSubmissionId &&
                        x.Status ==
                            NoteSubmissionStatus
                                .PdfGeneration)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.Status,
                                NoteSubmissionStatus
                                    .PdfGenerating)
                            .SetProperty(
                                x =>
                                    x.PdfGenerationAttemptCount,
                                x =>
                                    x.PdfGenerationAttemptCount +
                                    1)
                            .SetProperty(
                                x =>
                                    x.PdfGenerationError,
                                (string?)null),
                    cancellationToken);

        if (claimed == 0)
        {
            var exists =
                await db.NoteSubmissions
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.Id ==
                                noteSubmissionId,
                        cancellationToken);

            if (!exists)
            {
                throw new KeyNotFoundException(
                    "PDF üretilecek not bulunamadı.");
            }

            throw new InvalidOperationException(
                "Not PDF üretimine uygun durumda değil veya hâlihazırda işleniyor.");
        }

        string? newlyGeneratedPath =
            null;

        try
        {
            var submission =
                await db.NoteSubmissions
                    .Include(
                        x => x.Request)
                    .SingleAsync(
                        x =>
                            x.Id ==
                                noteSubmissionId,
                        cancellationToken);

            /*
             * PdfGeneration durumuna yalnızca:
             *
             * - AI AutoApprove kararı veya
             * - admin manuel onayı
             *
             * sonucunda geçilmiş olmalıdır.
             */
            await ValidateGenerationApprovalAsync(
                submission,
                cancellationToken);

            await using var originalDocument =
                await storage.OpenReadAsync(
                    submission.OriginalBlobPath,
                    cancellationToken);

            if (originalDocument is null)
            {
                throw new FileNotFoundException(
                    "PDF üretimi için orijinal not dosyası bulunamadı.",
                    submission.OriginalBlobPath);
            }

            var documentBytes =
                await ReadWithLimitAsync(
                    originalDocument,
                    _openAiOptions
                        .MaxDocumentBytes,
                    cancellationToken);

            if (
                documentBytes.Length >
                _openAiOptions.MaxDocumentBytes
            )
            {
                throw new InvalidOperationException(
                    "Orijinal not PDF'i izin verilen dosya boyutunu aşıyor.");
            }

            var conversionInput =
                new NoteContentConversionInput(
                    submission.Id,
                    submission.Title,
                    submission.Request
                        .UniversityName,
                    submission.Request
                        .DepartmentName,
                    submission.Request
                        .CourseName,
                    submission.Request
                        .CriteriaJson,
                    Path.GetFileName(
                        submission.OriginalBlobPath),
                    "application/pdf",
                    documentBytes);

            /*
             * Orijinal PDF yapılandırılmış
             * akademik belge modeline dönüştürülür.
             */
            var conversionResult =
                await contentConversionService
                    .ConvertAsync(
                        conversionInput,
                        cancellationToken);

            var generatedAt =
                DateTimeOffset.UtcNow;

            var renderInput =
                new LatexDocumentRenderInput(
                    new LatexDocumentMetadata(
                        submission.Id,
                        submission.Title,
                        submission.Request
                            .UniversityName,
                        submission.Request
                            .DepartmentName,
                        submission.Request
                            .CourseName,
                        generatedAt),
                    conversionResult.Document);

            /*
             * Yapılandırılmış içerik sabit ve
             * güvenli LaTeX şablonuna yerleştirilir.
             */
            var renderResult =
                documentRenderer.Render(
                    renderInput);

            /*
             * LaTeX kaynağı izole geçici klasörde
             * gerçek PDF dosyasına dönüştürülür.
             */
            var compilationResult =
                await pdfCompiler.CompileAsync(
                    new LatexPdfCompilationInput(
                        submission.Id,
                        renderResult.Source),
                    cancellationToken);

            await using var generatedDocument =
                new MemoryStream(
                    compilationResult.PdfBytes,
                    writable:
                        false);

            /*
             * Yeni PDF önce storage'a yazılır.
             * Veritabanı güncellenemezse bu dosya
             * catch bloğunda temizlenecektir.
             */
            newlyGeneratedPath =
                await storage.SaveGeneratedAsync(
                    submission.Id,
                    generatedDocument,
                    cancellationToken);

            var previousGeneratedPath =
                submission.GeneratedPdfBlobPath;

            submission.GeneratedPdfBlobPath =
                newlyGeneratedPath;

            submission.PdfGeneratedAt =
                compilationResult.CompiledAt;

            submission.PdfGenerationModelName =
                conversionResult.ModelName;

            submission.PdfConversionPromptVersion =
                conversionResult.PromptVersion;

            submission.PdfTemplateVersion =
                renderResult.TemplateVersion;

            submission.PdfCompilerName =
                compilationResult.CompilerName;

            submission.PdfGenerationError =
                null;

            /*
             * Approved durumu yalnızca PDF
             * storage'a kaydedildikten sonra verilir.
             */
            submission.Status =
                NoteSubmissionStatus.Approved;

            await db.SaveChangesAsync(
                cancellationToken);

            /*
             * Yeniden üretim yapılmışsa eski PDF,
             * yeni kayıt başarıyla veritabanına
             * yazıldıktan sonra temizlenir.
             */
            if (
                !string.IsNullOrWhiteSpace(
                    previousGeneratedPath) &&
                !string.Equals(
                    previousGeneratedPath,
                    newlyGeneratedPath,
                    StringComparison.Ordinal)
            )
            {
                await TryDeleteOldDocumentAsync(
                    previousGeneratedPath);
            }

            return new NotePdfGenerationResult(
                submission.Id,
                newlyGeneratedPath,
                compilationResult
                    .PdfBytes
                    .Length,
                conversionResult.ModelName,
                conversionResult.PromptVersion,
                renderResult.TemplateVersion,
                compilationResult.CompilerName,
                compilationResult.CompiledAt);
        }
        catch (OperationCanceledException)
            when (
                cancellationToken
                    .IsCancellationRequested
            )
        {
            /*
             * Uygulama kapanışı sırasında işlem
             * yarım kalırsa kayıt PdfGenerating
             * olarak bırakılır. Background service
             * sonraki başlangıçta tekrar kuyruğa alır.
             */
            if (
                !string.IsNullOrWhiteSpace(
                    newlyGeneratedPath)
            )
            {
                await TryDeleteNewDocumentAsync(
                    newlyGeneratedPath);
            }

            throw;
        }
        catch (Exception exception)
        {
            if (
                !string.IsNullOrWhiteSpace(
                    newlyGeneratedPath)
            )
            {
                await TryDeleteNewDocumentAsync(
                    newlyGeneratedPath);
            }

            await MarkGenerationFailedAsync(
                noteSubmissionId,
                exception);

            throw;
        }
    }

    private async Task
        ValidateGenerationApprovalAsync(
            NoteSubmission submission,
            CancellationToken cancellationToken)
    {
        /*
         * Admin onayında ReviewedByUserId
         * dolu olacaktır.
         */
        if (
            submission.ReviewedByUserId
            is not null
        )
        {
            return;
        }

        /*
         * Admin onayı yoksa son AI incelemesinin
         * AutoApprove olması gerekir.
         */
        var latestDecision =
            await db.NoteAiReviews
                .AsNoTracking()
                .Where(
                    x =>
                        x.NoteSubmissionId ==
                            submission.Id)
                .OrderByDescending(
                    x => x.ReviewedAt)
                .Select(
                    x =>
                        (NoteReviewDecision?)
                            x.Decision)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (
            latestDecision !=
            NoteReviewDecision.AutoApprove
        )
        {
            throw new InvalidOperationException(
                "Not için geçerli AI otomatik onayı veya admin onayı bulunmuyor.");
        }
    }

    private async Task MarkGenerationFailedAsync(
        Guid noteSubmissionId,
        Exception exception)
    {
        var errorMessage =
            CreateErrorMessage(
                exception);

        try
        {
            /*
             * Takip edilen entity üzerinde başarısız
             * değişiklik kalmış olabileceği için
             * doğrudan güncellemeden önce temizlenir.
             */
            db.ChangeTracker.Clear();

            await db.NoteSubmissions
                .Where(
                    x =>
                        x.Id ==
                            noteSubmissionId &&
                        x.Status ==
                            NoteSubmissionStatus
                                .PdfGenerating)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.Status,
                                NoteSubmissionStatus
                                    .PdfGenerationFailed)
                            .SetProperty(
                                x =>
                                    x.PdfGenerationError,
                                errorMessage),
                    CancellationToken.None);
        }
        catch (Exception updateException)
        {
            logger.LogError(
                updateException,
                "Başarısız PDF üretim durumu veritabanına yazılamadı. NoteSubmissionId: {NoteSubmissionId}",
                noteSubmissionId);
        }
    }

    private async Task TryDeleteNewDocumentAsync(
        string relativePath)
    {
        try
        {
            await storage.DeleteAsync(
                relativePath,
                CancellationToken.None);
        }
        catch (Exception cleanupException)
        {
            logger.LogWarning(
                cleanupException,
                "Başarısız PDF üretiminin yeni dosyası temizlenemedi. Path: {DocumentPath}",
                relativePath);
        }
    }

    private async Task TryDeleteOldDocumentAsync(
        string relativePath)
    {
        try
        {
            await storage.DeleteAsync(
                relativePath,
                CancellationToken.None);
        }
        catch (Exception cleanupException)
        {
            /*
             * Yeni PDF ve veritabanı kaydı
             * başarılı olduğundan eski dosyanın
             * temizlenememesi ana işlemi bozmaz.
             */
            logger.LogWarning(
                cleanupException,
                "Eski oluşturulmuş PDF temizlenemedi. Path: {DocumentPath}",
                relativePath);
        }
    }

    private static string CreateErrorMessage(
        Exception exception)
    {
        const int maximumLength =
            2000;

        var message =
            exception
                .GetBaseException()
                .Message
                .Replace(
                    '\r',
                    ' ')
                .Replace(
                    '\n',
                    ' ')
                .Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            message =
                "Bilinmeyen PDF üretim hatası.";
        }

        if (message.Length <= maximumLength)
        {
            return message;
        }

        return
            message[..maximumLength];
    }

    private static async Task<byte[]>
        ReadWithLimitAsync(
            Stream source,
            int maximumBytes,
            CancellationToken cancellationToken)
    {
        await using var target =
            new MemoryStream();

        var buffer =
            new byte[81920];

        while (
            target.Length <=
            maximumBytes
        )
        {
            var remaining =
                maximumBytes +
                1L -
                target.Length;

            var requested =
                (int)Math.Min(
                    buffer.Length,
                    remaining);

            if (requested <= 0)
            {
                break;
            }

            var read =
                await source.ReadAsync(
                    buffer.AsMemory(
                        0,
                        requested),
                    cancellationToken);

            if (read == 0)
            {
                break;
            }

            await target.WriteAsync(
                buffer.AsMemory(
                    0,
                    read),
                cancellationToken);
        }

        return target.ToArray();
    }
}
