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
    IAuditService auditService) : ControllerBase
{
    /*
     * Admin incelemesindeki notları listeler.
     *
     * GET /api/admin/notes
     */
    [HttpGet]
    public async Task<
        ActionResult<IReadOnlyList<AdminNoteSubmissionDto>>>
        Get(
            [FromQuery]
            NoteSubmissionStatus? status,
            CancellationToken cancellationToken)
    {
        var query =
            db.NoteSubmissions
                .AsNoTracking()
                .Include(x => x.Seller)
                .Include(x => x.Request)
                .AsQueryable();

        if (status is not null)
        {
            query =
                query.Where(
                    x => x.Status == status);
        }

        var items =
            await query
                .OrderByDescending(
                    x => x.CreatedAt)
                .Select(
                    x => new AdminNoteSubmissionDto(
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
                        x.CreatedAt))
                .ToListAsync(
                    cancellationToken);

        return Ok(items);
    }

    /*
     * Admin notu onaylar veya reddeder.
     *
     * Onay sırasında satış fiyatı zorunludur
     * ve sistemin önerdiği aralıkta olmalıdır.
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
                .Include(x => x.Request)
                .SingleOrDefaultAsync(
                    x => x.Id == submissionId,
                    cancellationToken);

        if (submission is null)
        {
            return NotFound(new
            {
                message =
                    "Not kaydı bulunamadı."
            });
        }

        /*
         * Tamamlanmış bir kararın yanlışlıkla
         * tekrar değiştirilmesini engeller.
         */
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
                string.IsNullOrWhiteSpace(
                    submission.GeneratedPdfBlobPath)
            )
            {
                return BadRequest(new
                {
                    message =
                        "Oluşturulmuş PDF dosyası bulunmayan not onaylanamaz."
                });
            }

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
                ? NoteSubmissionStatus.Approved
                : NoteSubmissionStatus.Rejected;

        submission.SalePrice =
            request.Approve
                ? decimal.Round(
                    request.SalePrice!.Value,
                    2,
                    MidpointRounding.AwayFromZero)
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

        await db.SaveChangesAsync(
            cancellationToken);

        await auditService.WriteAsync(
            adminId.Value,
            request.Approve
                ? "NOTE_APPROVED"
                : "NOTE_REJECTED",
            nameof(NoteSubmission),
            submission.Id.ToString(),
            new
            {
                submission.ReviewNote,
                submission.SalePrice,
                SuggestedMinPrice =
                    submission.Request
                        .SuggestedMinPrice,
                SuggestedMaxPrice =
                    submission.Request
                        .SuggestedMaxPrice
            },
            cancellationToken);

        return NoContent();
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