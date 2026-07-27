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
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminNoteSubmissionDto>>> Get(
        [FromQuery] NoteSubmissionStatus? status,
        CancellationToken cancellationToken)
    {
        var query = db.NoteSubmissions
            .AsNoTracking()
            .Include(x => x.Seller)
            .Include(x => x.Request)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminNoteSubmissionDto(
                x.Id,
                x.Title,
                x.Seller.DisplayName,
                x.Request.UniversityName,
                x.Request.DepartmentName,
                x.Request.CourseName,
                x.MatchScore,
                x.ReadabilityScore,
                x.OriginalityRiskScore,
                x.Status.ToString(),
                x.GeneratedPdfBlobPath,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPost("{submissionId:guid}/decision")]
    public async Task<IActionResult> Decide(
        Guid submissionId,
        ReviewDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var submission = await db.NoteSubmissions.FindAsync(
            [submissionId],
            cancellationToken);

        if (submission is null)
        {
            return NotFound();
        }

        submission.Status = request.Approve
            ? NoteSubmissionStatus.Approved
            : NoteSubmissionStatus.Rejected;
        submission.ReviewNote = request.ReviewNote;
        submission.ReviewedByUserId = GetAdminId();
        submission.ReviewedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            GetAdminId(),
            request.Approve
                ? "NOTE_APPROVED"
                : "NOTE_REJECTED",
            nameof(NoteSubmission),
            submission.Id.ToString(),
            new { request.ReviewNote },
            cancellationToken);

        return NoContent();
    }

    private Guid? GetAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
