using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;
using NotMarket.Api.Services;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/admin/notes")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminNotesController(
    AppDbContext db,
    IAuditService auditService,
    INotePdfGenerationQueue pdfGenerationQueue,
    INoteDocumentStorage storage)
    : ControllerBase
{
    /*
     * Admin incelemesindeki notları listeler.
     *
     * GET /api/admin/notes
     */
    [HttpGet]
    public async Task<
        ActionResult<
            IReadOnlyList<AdminNoteSubmissionDto>>>
        Get(
            [FromQuery]
            NoteSubmissionStatus? status,
            CancellationToken cancellationToken)
    {
        var query =
            db.NoteSubmissions
                .AsNoTracking()
                .Include(
                    x => x.Seller)
                .Include(
                    x => x.Request)
                .AsQueryable();

        if (status is not null)
        {
            query =
                query.Where(
                    x =>
                        x.Status ==
                        status);
        }

        var items =
            await query
                .OrderByDescending(
                    x => x.CreatedAt)
                .Select(
                    x =>
                        new AdminNoteSubmissionDto(
                            x.Id,
                            x.Title,
                            x.Seller.DisplayName,
                            x.Request.UniversityName,
                            x.Request.DepartmentName,
                            x.Request.CourseName,
                            x.MatchScore,
                            x.ReadabilityScore,
                            x.OriginalityRiskScore,
                            x.Request.SuggestedMinPrice,
                            x.Request.SuggestedMaxPrice,
                            x.SalePrice,
                            x.Status.ToString(),
                            x.GeneratedPdfBlobPath,
                            x.PdfGenerationAttemptCount,
                            x.PdfGenerationError,
                            x.PdfGeneratedAt,
                            x.PdfGenerationModelName,
                            x.PdfConversionPromptVersion,
                            x.PdfTemplateVersion,
                            x.PdfCompilerName,
                            x.CreatedAt))
                .ToListAsync(
                    cancellationToken);

        return Ok(
            items);
    }

    /*
    * Admin oluşturulmuş PDF dosyasını indirir.
    *
    * GET:
    * /api/admin/notes/{submissionId}/generated-document
    */
    [HttpGet(
        "{submissionId:guid}/generated-document")]
    public async Task<IActionResult>
        GetGeneratedDocument(
            Guid submissionId,
            CancellationToken cancellationToken)
    {
        var submission =
            await db.NoteSubmissions
                .AsNoTracking()
                .Where(
                    x =>
                        x.Id ==
                            submissionId)
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
                    "Not kaydı bulunamadı."
            });
        }

        if (
            string.IsNullOrWhiteSpace(
                submission.GeneratedPdfBlobPath)
        )
        {
            return Conflict(new
            {
                message =
                    "Bu not için oluşturulmuş PDF dosyası bulunmuyor."
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
                    "Oluşturulmuş PDF dosyası storage alanında bulunamadı."
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
     * Admin manuel incelemedeki notu
     * onaylar veya reddeder.
     *
     * Onay:
     * ManualReview → PdfGeneration
     *
     * Ret:
     * ManualReview → Rejected
     *
     * POST:
     * /api/admin/notes/{submissionId}/decision
     */
    [HttpPost("{submissionId:guid}/decision")]
    public async Task<IActionResult> Decide(
        Guid submissionId,
        NoteReviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var adminId =
            GetAdminId();

        if (adminId is null)
        {
            return Unauthorized();
        }

        var submission =
            await db.NoteSubmissions
                .Include(
                    x => x.Request)
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            submissionId,
                    cancellationToken);

        if (submission is null)
        {
            return NotFound(new
            {
                message =
                    "Not kaydı bulunamadı."
            });
        }

        if (
            submission.Status !=
            NoteSubmissionStatus.ManualReview
        )
        {
            return Conflict(new
            {
                message =
                    "Yalnızca manuel inceleme bekleyen notlar için karar verilebilir."
            });
        }

        if (
            !request.Approve &&
            string.IsNullOrWhiteSpace(
                request.ReviewNote)
        )
        {
            return BadRequest(new
            {
                message =
                    "Ret işlemi için gerekçe zorunludur."
            });
        }

        if (request.Approve)
        {
            if (
                request.SalePrice is null ||
                request.SalePrice <= 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "Onay işlemi için geçerli bir satış fiyatı zorunludur."
                });
            }

            if (
                request.SalePrice <
                    submission.Request
                        .SuggestedMinPrice ||
                request.SalePrice >
                    submission.Request
                        .SuggestedMaxPrice
            )
            {
                return BadRequest(new
                {
                    message =
                        "Satış fiyatı önerilen fiyat aralığında olmalıdır.",

                    suggestedMinPrice =
                        submission.Request
                            .SuggestedMinPrice,

                    suggestedMaxPrice =
                        submission.Request
                            .SuggestedMaxPrice
                });
            }
        }

        submission.Status =
            request.Approve
                ? NoteSubmissionStatus
                    .PdfGeneration
                : NoteSubmissionStatus
                    .Rejected;

        submission.SalePrice =
            request.Approve
                ? decimal.Round(
                    request.SalePrice!.Value,
                    2,
                    MidpointRounding
                        .AwayFromZero)
                : null;

        submission.ReviewNote =
            string.IsNullOrWhiteSpace(
                request.ReviewNote)
                ? null
                : request.ReviewNote.Trim();

        submission.ReviewedByUserId =
            adminId.Value;

        submission.ReviewedAt =
            DateTimeOffset.UtcNow;

        submission.PdfGenerationError =
            null;

        await db.SaveChangesAsync(
            cancellationToken);

        /*
         * Admin onayında PDF üretimi
         * arka plan kuyruğuna gönderilir.
         */
        if (request.Approve)
        {
            using var enqueueTimeout =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(10));

            await pdfGenerationQueue
                .EnqueueAsync(
                    submission.Id,
                    enqueueTimeout.Token);
        }

        await auditService.WriteAsync(
            adminId.Value,
            request.Approve
                ? "NOTE_PDF_GENERATION_REQUESTED"
                : "NOTE_REJECTED",
            nameof(NoteSubmission),
            submission.Id.ToString(),
            new
            {
                submission.ReviewNote,
                submission.SalePrice,

                Status =
                    submission.Status
                        .ToString(),

                SuggestedMinPrice =
                    submission.Request
                        .SuggestedMinPrice,

                SuggestedMaxPrice =
                    submission.Request
                        .SuggestedMaxPrice
            },
            cancellationToken);

        if (request.Approve)
        {
            return Accepted(new
            {
                submissionId =
                    submission.Id,

                status =
                    NoteSubmissionStatus
                        .PdfGeneration
                        .ToString(),

                message =
                    "Not onaylandı ve PDF üretim kuyruğuna gönderildi."
            });
        }

        return NoContent();
    }

    /*
     * Başarısız PDF üretimini admin
     * yeniden kuyruğa gönderir.
     *
     * POST:
     * /api/admin/notes/{submissionId}/retry-pdf-generation
     */
    [HttpPost(
        "{submissionId:guid}/retry-pdf-generation")]
    public async Task<IActionResult>
        RetryPdfGeneration(
            Guid submissionId,
            CancellationToken cancellationToken)
    {
        var adminId =
            GetAdminId();

        if (adminId is null)
        {
            return Unauthorized();
        }

        var submission =
            await db.NoteSubmissions
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            submissionId,
                    cancellationToken);

        if (submission is null)
        {
            return NotFound(new
            {
                message =
                    "Not kaydı bulunamadı."
            });
        }

        if (
            submission.Status !=
            NoteSubmissionStatus
                .PdfGenerationFailed
        )
        {
            return Conflict(new
            {
                message =
                    "Yalnızca PDF üretimi başarısız olan notlar yeniden denenebilir."
            });
        }

        /*
         * İlk onayın varlığı korunur.
         * Yalnızca üretim durumu ve son hata
         * temizlenir.
         */
        submission.Status =
            NoteSubmissionStatus
                .PdfGeneration;

        submission.PdfGenerationError =
            null;

        await db.SaveChangesAsync(
            cancellationToken);

        using var enqueueTimeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(10));

        await pdfGenerationQueue.EnqueueAsync(
            submission.Id,
            enqueueTimeout.Token);

        await auditService.WriteAsync(
            adminId.Value,
            "NOTE_PDF_GENERATION_RETRIED",
            nameof(NoteSubmission),
            submission.Id.ToString(),
            new
            {
                submission
                    .PdfGenerationAttemptCount,

                Status =
                    submission.Status
                        .ToString()
            },
            cancellationToken);

        return Accepted(new
        {
            submissionId =
                submission.Id,

            status =
                submission.Status
                    .ToString(),

            message =
                "Not yeniden PDF üretim kuyruğuna gönderildi."
        });
    }

    private Guid? GetAdminId()
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        return Guid.TryParse(
            value,
            out var adminId)
                ? adminId
                : null;
    }
}