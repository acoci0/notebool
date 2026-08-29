using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;
using NotMarket.Api.Services;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/student/note-submissions")]
[Authorize(Policy = "StudentOnly")]
public sealed class StudentNoteSubmissionsController(
    AppDbContext db,
    INoteDocumentStorage storage,
    INoteReviewQueue reviewQueue,
    IOptions<OpenAiOptions> openAiOptions,
    ILogger<StudentNoteSubmissionsController> logger)
    : ControllerBase
{
    /*
     * Satıcının gönderdiği son 100 notu listeler.
     *
     * GET /api/student/note-submissions
     */
    [HttpGet]
    public async Task<
        ActionResult<
            IReadOnlyList<NoteSubmissionListItemResponse>>>
        GetMine(
            CancellationToken cancellationToken)
    {
        var sellerId =
            GetUserId();

        if (sellerId is null)
        {
            return Unauthorized();
        }

        var submissions =
            await db.NoteSubmissions
                .AsNoTracking()
                .Where(
                    x =>
                        x.SellerId ==
                        sellerId.Value)
                .Include(
                    x => x.Request)
                .Include(
                    x => x.AiReviews)
                .OrderByDescending(
                    x => x.CreatedAt)
                .Take(100)
                .ToListAsync(
                    cancellationToken);

        var items =
            submissions
                .Select(submission =>
                {
                    var latestReview =
                        submission.AiReviews
                            .OrderByDescending(
                                x => x.ReviewedAt)
                            .FirstOrDefault();

                    return
                        new NoteSubmissionListItemResponse(
                            submission.Id,
                            submission.RequestId,
                            submission.Title,
                            submission.Request.UniversityName,
                            submission.Request.DepartmentName,
                            submission.Request.CourseName,
                            submission.Status.ToString(),
                            submission.SalePrice,
                            latestReview?.OverallScore,
                            latestReview?.Decision.ToString(),
                            !string.IsNullOrWhiteSpace(submission.GeneratedPdfBlobPath),
                            submission.PdfGenerationAttemptCount,submission.PdfGeneratedAt,
                            GetSafePdfGenerationMessage(submission.Status),latestReview?.ReviewedAt,submission.CreatedAt);
                })
                .ToList();

        return Ok(items);
    }

    /*
     * Satıcının tek bir not gönderiminin
     * ayrıntılarını ve son AI incelemesini döndürür.
     *
     * GET:
     * /api/student/note-submissions/{submissionId}
     */
    [HttpGet("{submissionId:guid}")]
    public async Task<
        ActionResult<NoteSubmissionDetailResponse>>
        GetById(
            Guid submissionId,
            CancellationToken cancellationToken)
    {
        var sellerId =
            GetUserId();

        if (sellerId is null)
        {
            return Unauthorized();
        }

        var submission =
            await db.NoteSubmissions
                .AsNoTracking()
                .Include(
                    x => x.Request)
                .Include(
                    x => x.AiReviews)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            submissionId &&
                        x.SellerId ==
                            sellerId.Value,
                    cancellationToken);

        if (submission is null)
        {
            return NotFound(new
            {
                message =
                    "Not gönderimi bulunamadı."
            });
        }

        var latestReview =
            submission.AiReviews
                .OrderByDescending(
                    x => x.ReviewedAt)
                .FirstOrDefault();

        var response =
            new NoteSubmissionDetailResponse(
                submission.Id,
                submission.RequestId,
                submission.Title,
                submission.Request.UniversityName,
                submission.Request.DepartmentName,
                submission.Request.CourseName,
                submission.Status.ToString(),
                submission.SalePrice,
                !string.IsNullOrWhiteSpace(
                    submission.GeneratedPdfBlobPath),
                submission.PdfGenerationAttemptCount,
                submission.PdfGeneratedAt,
                GetSafePdfGenerationMessage(
                    submission.Status),
                MapAiReview(latestReview),
                submission.CreatedAt);

        return Ok(response);
    }

    /*
     * Başarısız AI incelemesini yeniden
     * kuyruğa ekler.
     *
     * POST:
     * /api/student/note-submissions/
     * {submissionId}/retry-review
     */
    [HttpPost("{submissionId:guid}/retry-review")]
    public async Task<IActionResult> RetryReview(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var sellerId =
            GetUserId();

        if (sellerId is null)
        {
            return Unauthorized();
        }

        var submission =
            await db.NoteSubmissions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            submissionId &&
                        x.SellerId ==
                            sellerId.Value,
                    cancellationToken);

        if (submission is null)
        {
            return NotFound(new
            {
                message =
                    "Not gönderimi bulunamadı."
            });
        }

        if (
            submission.Status !=
            NoteSubmissionStatus.Uploaded
        )
        {
            return Conflict(new
            {
                message =
                    "Yalnızca Uploaded durumundaki notlar yeniden incelenebilir."
            });
        }

        if (
            string.IsNullOrWhiteSpace(
                submission.OriginalBlobPath)
        )
        {
            return Conflict(new
            {
                message =
                    "İncelenecek orijinal PDF bulunamadı."
            });
        }

        using var enqueueTimeout =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

        enqueueTimeout.CancelAfter(
            TimeSpan.FromSeconds(10));

        try
        {
            await reviewQueue.EnqueueAsync(
                submission.Id,
                enqueueTimeout.Token);
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message =
                        "AI inceleme kuyruğu şu anda yoğun. Lütfen tekrar deneyin."
                });
        }

        return Accepted(new
        {
            submissionId =
                submission.Id,

            status =
                submission.Status.ToString(),

            message =
                "Not AI inceleme kuyruğuna yeniden eklendi."
        });
    }

    /*
    * Satıcı kendi oluşturulmuş PDF dosyasını
    * indirir.
    *
    * GET:
    * /api/student/note-submissions/{submissionId}/generated-document
    */
    [HttpGet(
        "{submissionId:guid}/generated-document")]
    public async Task<IActionResult>
        GetGeneratedDocument(
            Guid submissionId,
            CancellationToken cancellationToken)
    {
        var sellerId =
            GetUserId();

        if (sellerId is null)
        {
            return Unauthorized();
        }

        var submission =
            await db.NoteSubmissions
                .AsNoTracking()
                .Where(
                    x =>
                        x.Id ==
                            submissionId &&
                        x.SellerId ==
                            sellerId.Value)
                .Select(
                    x => new
                    {
                        x.Id,
                        x.Status,
                        x.GeneratedPdfBlobPath
                    })
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (submission is null)
        {
            return NotFound(new
            {
                message =
                    "Not gönderimi bulunamadı."
            });
        }

        if (
            submission.Status !=
                NoteSubmissionStatus.Approved ||
            string.IsNullOrWhiteSpace(
                submission.GeneratedPdfBlobPath)
        )
        {
            return Conflict(new
            {
                message =
                    "Oluşturulmuş PDF henüz kullanıma hazır değil."
            });
        }

        var stream =
            await storage.OpenReadAsync(
                submission.GeneratedPdfBlobPath,
                cancellationToken);

        if (stream is null)
        {
            return NotFound(new
            {
                message =
                    "Oluşturulmuş PDF dosyası bulunamadı."
            });
        }

        return File(
            stream,
            "application/pdf",
            $"notmarket-{submission.Id:N}.pdf",
            enableRangeProcessing:
                true);
    }

    /*
     * Satıcı el yazısı not PDF'ini yükler.
     *
     * POST /api/student/note-submissions
     */
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<NoteSubmissionCreatedResponse>>
        Upload(
            [FromForm]
            NoteSubmissionUploadRequest upload,
            CancellationToken cancellationToken)
    {
        var sellerId =
            GetUserId();

        if (sellerId is null)
        {
            return Unauthorized();
        }

        if (upload.RequestId == Guid.Empty)
        {
            return BadRequest(new
            {
                message =
                    "Not talebi seçilmelidir."
            });
        }

        var title =
            upload.Title.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest(new
            {
                message =
                    "Not başlığı boş olamaz."
            });
        }

        if (title.Length > 220)
        {
            return BadRequest(new
            {
                message =
                    "Not başlığı en fazla 220 karakter olabilir."
            });
        }

        if (
            upload.Document is null ||
            upload.Document.Length == 0
        )
        {
            return BadRequest(new
            {
                message =
                    "Not PDF dosyası boş olamaz."
            });
        }

        var options =
            openAiOptions.Value;

        if (
            upload.Document.Length >
            options.MaxDocumentBytes
        )
        {
            return BadRequest(new
            {
                message =
                    $"Not PDF dosyası en fazla " +
                    $"{options.MaxDocumentBytes / 1024 / 1024} MB olabilir."
            });
        }

        if (
            !string.Equals(
                Path.GetExtension(
                    upload.Document.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return BadRequest(new
            {
                message =
                    "Not dosyası PDF formatında olmalıdır."
            });
        }

        /*
         * Yalnızca dosya uzantısına güvenilmez.
         * Dosyanın %PDF- başlığıyla başlaması
         * gerekir.
         */
        if (
            !await IsPdfAsync(
                upload.Document,
                cancellationToken)
        )
        {
            return BadRequest(new
            {
                message =
                    "Dosyanın gerçek PDF formatında olduğu doğrulanamadı."
            });
        }

        var noteRequest =
            await db.NoteRequests
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        upload.RequestId,
                    cancellationToken);

        if (noteRequest is null)
        {
            return NotFound(new
            {
                message =
                    "Not talebi bulunamadı."
            });
        }

        /*
         * Kullanıcı kendi oluşturduğu talebe
         * satıcı olarak not gönderemez.
         */
        if (
            noteRequest.BuyerId ==
            sellerId.Value
        )
        {
            return Conflict(new
            {
                message =
                    "Kendi not talebinize satıcı olarak not gönderemezsiniz."
            });
        }

        /*
         * Satıcının talepte belirtilen üniversite
         * ve bölüm için aktif öğrenci doğrulaması
         * bulunmalıdır.
         */
        var now =
            DateTimeOffset.UtcNow;

        var hasActiveVerification =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId ==
                            sellerId.Value &&

                        x.Status ==
                            VerificationStatus.Approved &&

                        (
                            x.ExpiresAt ==
                                null ||
                            x.ExpiresAt >
                                now
                        ) &&

                        x.UniversityName ==
                            noteRequest.UniversityName &&

                        x.DepartmentName ==
                            noteRequest.DepartmentName,
                    cancellationToken);

        if (!hasActiveVerification)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                new
                {
                    message =
                        "Bu talebe not gönderebilmek için ilgili üniversite ve bölümde aktif öğrenci doğrulamanız bulunmalıdır."
                });
        }

        /*
         * Satıcı aynı talep için halen işlemde
         * olan başka bir not gönderimine sahip
         * olamaz.
         */
        var activeStatuses =
            new[]
            {
                NoteSubmissionStatus.Uploaded,
                NoteSubmissionStatus.AiReview,
                NoteSubmissionStatus.ManualReview,
                NoteSubmissionStatus.PdfGeneration,
                NoteSubmissionStatus.Approved
            };

        var existingSubmission =
            await db.NoteSubmissions
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.RequestId ==
                            noteRequest.Id &&

                        x.SellerId ==
                            sellerId.Value &&

                        activeStatuses.Contains(
                            x.Status),
                    cancellationToken);

        if (existingSubmission)
        {
            return Conflict(new
            {
                message =
                    "Bu talep için halen işlemde olan bir not gönderiminiz bulunuyor."
            });
        }

        /*
         * Orijinal satıcı PDF'i private storage
         * alanına kaydedilir.
         */
        var relativePath =
            await storage.SaveOriginalAsync(
                sellerId.Value,
                upload.Document,
                cancellationToken);

        var submission =
            new NoteSubmission
            {
                RequestId =
                    noteRequest.Id,

                SellerId =
                    sellerId.Value,

                Title =
                    title,

                OriginalBlobPath =
                    relativePath,

                GeneratedPdfBlobPath =
                    null,

                SalePrice =
                    null,

                Status =
                    NoteSubmissionStatus.Uploaded
            };

        db.NoteSubmissions.Add(
            submission);

        try
        {
            await db.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await TryDeleteDocumentAsync(
                relativePath);

            throw;
        }

        /*
         * Veritabanı kaydı başarıyla oluşturulan
         * not AI inceleme kuyruğuna gönderilir.
         */
        try
        {
            using var enqueueTimeout =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        cancellationToken);

            enqueueTimeout.CancelAfter(
                TimeSpan.FromSeconds(10));

            await reviewQueue.EnqueueAsync(
                submission.Id,
                enqueueTimeout.Token);
        }
        catch
        {
            /*
             * Kuyruğa ekleme başarısızsa henüz
             * AI işlemi başlamadığı için kayıt ve
             * dosya geri alınır.
             */
            db.NoteSubmissions.Remove(
                submission);

            try
            {
                await db.SaveChangesAsync(
                    CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                logger.LogError(
                    cleanupException,
                    "Kuyruğa eklenemeyen not kaydı temizlenemedi. NoteSubmissionId: {NoteSubmissionId}",
                    submission.Id);
            }

            await TryDeleteDocumentAsync(
                relativePath);

            throw;
        }

        var response =
            new NoteSubmissionCreatedResponse(
                submission.Id,
                submission.RequestId,
                submission.Title,
                submission.Status.ToString(),
                submission.CreatedAt);

        /*
         * AI incelemesi arka planda devam ettiği
         * için 202 Accepted döndürülür.
         */
        return Accepted(response);
    }

    private static string?
        GetSafePdfGenerationMessage(
            NoteSubmissionStatus status)
    {
        return status switch
        {
            NoteSubmissionStatus.PdfGeneration =>
                "PDF oluşturma sırasına alındı.",

            NoteSubmissionStatus.PdfGenerating =>
                "PDF oluşturuluyor.",

            NoteSubmissionStatus.PdfGenerationFailed =>
                "PDF oluşturulamadı. Kayıt yönetici incelemesine gönderildi.",

            NoteSubmissionStatus.Approved =>
                "PDF hazır.",

            _ =>
                null
        };
    }
    

    private static NoteSubmissionAiReviewResponse?
        MapAiReview(
            NoteAiReview? review)
    {
        if (review is null)
        {
            return null;
        }

        return new NoteSubmissionAiReviewResponse(
            review.IsTechnicallyValid,
            review.ReadabilityScore,
            review.CourseMatchScore,
            review.DepartmentMatchScore,
            review.ContentCompletenessScore,
            review.OriginalityAndReliabilityScore,
            review.OriginalityRiskScore,
            review.OverallScore,
            review.ConfidenceScore,
            review.Decision.ToString(),
            review.Summary,
            ParseFindings(
                review.FindingsJson),
            review.DetectedCourse,
            review.DetectedDepartment,
            review.ModelName,
            review.PromptVersion,
            review.ReviewedAt);
    }

    private static IReadOnlyList<string>
        ParseFindings(
            string findingsJson)
    {
        if (
            string.IsNullOrWhiteSpace(
                findingsJson)
        )
        {
            return [];
        }

        try
        {
            return
                JsonSerializer.Deserialize<
                    string[]>(findingsJson)
                ??
                [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private Guid? GetUserId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out var id)
            ? id
            : null;
    }

    private static async Task<bool> IsPdfAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream =
            file.OpenReadStream();

        var header =
            new byte[5];

        var totalRead =
            0;

        while (totalRead < header.Length)
        {
            var read =
                await stream.ReadAsync(
                    header.AsMemory(
                        totalRead,
                        header.Length -
                        totalRead),
                    cancellationToken);

            if (read == 0)
            {
                return false;
            }

            totalRead +=
                read;
        }

        return header
            .AsSpan()
            .SequenceEqual(
                "%PDF-"u8);
    }

    private async Task TryDeleteDocumentAsync(
        string relativePath)
    {
        try
        {
            await storage.DeleteAsync(
                relativePath,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Not belgesi temizlenemedi. Path: {DocumentPath}",
                relativePath);
        }
    }
}