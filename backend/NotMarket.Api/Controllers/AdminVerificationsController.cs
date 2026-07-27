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
[Route("api/admin/verifications")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminVerificationsController(
    AppDbContext db,
    IAuditService auditService,
    IVerificationDocumentStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminVerificationDto>>> Get(
        [FromQuery] VerificationStatus? status,
        CancellationToken cancellationToken)
    {
        var query = db.StudentVerifications
            .AsNoTracking()
            .Include(x => x.User)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminVerificationDto(
                x.Id,
                x.UserId,
                x.User.DisplayName,
                x.User.Email,
                x.UniversityName,
                x.FacultyName,
                x.DepartmentName,
                x.DocumentIssueDate,
                x.Status.ToString(),
                x.DocumentBlobPath,
                x.ReviewNote,
                x.ReviewedAt,
                x.ExpiresAt,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{verificationId:guid}/document")]
    public async Task<IActionResult> GetDocument(
        Guid verificationId,
        CancellationToken cancellationToken)
    {
        var verification =
            await db.StudentVerifications
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == verificationId,
                    cancellationToken);

        if (verification is null)
        {
            return NotFound(new
            {
                message =
                    "Doğrulama kaydı bulunamadı."
            });
        }

        var stream = await storage.OpenReadAsync(
            verification.DocumentBlobPath,
            cancellationToken);

        if (stream is null)
        {
            return NotFound(new
            {
                message =
                    "Öğrenci belgesi dosyası bulunamadı."
            });
        }

        return File(
            stream,
            "application/pdf",
            enableRangeProcessing: true);
    }

    [HttpGet("{verificationId:guid}")]
    public async Task<ActionResult<AdminVerificationDetailDto>> GetById(
        Guid verificationId,
        CancellationToken cancellationToken)
    {
        var item = await db.StudentVerifications
            .AsNoTracking()
            .Include(x => x.User)
            .Where(x => x.Id == verificationId)
            .Select(x => new AdminVerificationDetailDto(
                x.Id,
                x.UserId,
                x.User.DisplayName,
                x.User.Email,
                x.UniversityName,
                x.FacultyName,
                x.DepartmentName,
                x.DocumentIssueDate,
                x.Status.ToString(),
                x.DocumentBlobPath,
                x.DocumentHash,
                x.ReviewNote,
                x.ReviewedAt,
                x.ExpiresAt,
                x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return NotFound(new
            {
                message = "Doğrulama kaydı bulunamadı."
            });
        }

        return Ok(item);
    }

    [HttpPost("{verificationId:guid}/decision")]
    public async Task<IActionResult> Decide(
        Guid verificationId,
        ReviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var verification = await db.StudentVerifications
            .SingleOrDefaultAsync(
                x => x.Id == verificationId,
                cancellationToken);

        if (verification is null)
        {
            return NotFound(new
            {
                message = "Doğrulama kaydı bulunamadı."
            });
        }

        if (verification.Status != VerificationStatus.Pending)
        {
            return BadRequest(new
            {
                message =
                    "Yalnızca bekleyen doğrulamalar için karar verilebilir."
            });
        }

        if (!request.Approve &&
            string.IsNullOrWhiteSpace(request.ReviewNote))
        {
            return BadRequest(new
            {
                message = "Ret işlemi için gerekçe zorunludur."
            });
        }

        verification.Status = request.Approve
            ? VerificationStatus.Approved
            : VerificationStatus.Rejected;

        verification.ReviewNote =
            string.IsNullOrWhiteSpace(request.ReviewNote)
                ? null
                : request.ReviewNote.Trim();

        verification.ReviewedByUserId = GetAdminId();
        verification.ReviewedAt = DateTimeOffset.UtcNow;

        verification.ExpiresAt = request.Approve
            ? DateTimeOffset.UtcNow.AddMonths(6)
            : null;

        await db.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            GetAdminId(),
            request.Approve
                ? "VERIFICATION_APPROVED"
                : "VERIFICATION_REJECTED",
            nameof(StudentVerification),
            verification.Id.ToString(),
            new
            {
                verification.UniversityName,
                verification.FacultyName,
                verification.DepartmentName,
                verification.ReviewNote
            },
            cancellationToken);

        return NoContent();
    }

    private Guid? GetAdminId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : null;
    }
}