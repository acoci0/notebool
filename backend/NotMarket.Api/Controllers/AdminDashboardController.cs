using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminDashboardController(AppDbContext db)
    : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get(
        CancellationToken cancellationToken)
    {
        var totalUsers = await db.Users.CountAsync(cancellationToken);
        var activeUsers = await db.Users.CountAsync(
            x => x.Status == AccountStatus.Active,
            cancellationToken);

        var pendingVerifications =
            await db.StudentVerifications.CountAsync(
                x => x.Status == VerificationStatus.Pending,
                cancellationToken);

        var pendingNoteReviews =
            await db.NoteSubmissions.CountAsync(
                x => x.Status == NoteSubmissionStatus.ManualReview ||
                     x.Status == NoteSubmissionStatus.AiReview,
                cancellationToken);

        var approvedNotes =
            await db.NoteSubmissions.CountAsync(
                x => x.Status == NoteSubmissionStatus.Approved,
                cancellationToken);

        var recent = await db.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(8)
            .Select(x => new RecentActivityDto(
                x.Action,
                x.EntityType,
                x.EntityId,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(new DashboardSummaryDto(
            totalUsers,
            activeUsers,
            pendingVerifications,
            pendingNoteReviews,
            approvedNotes,
            TotalRevenue: 0,
            recent));
    }
}
