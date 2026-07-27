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
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUsersController(
    AppDbContext db,
    IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserListItemDto>>> Get(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Email.ToLower().Contains(term) ||
                x.DisplayName.ToLower().Contains(term));
        }

        var users = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AdminUserListItemDto(
                x.Id,
                x.Email,
                x.DisplayName,
                x.Role.ToString(),
                x.Status.ToString(),
                x.StudentVerifications.Count,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpPatch("{userId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid userId,
        AccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AccountStatus>(
                request.Status,
                ignoreCase: true,
                out var newStatus))
        {
            return BadRequest(new
            {
                message = "Geçersiz hesap durumu."
            });
        }

        var user = await db.Users.FindAsync([userId], cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.Status = newStatus;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.WriteAsync(
            GetAdminId(),
            "USER_STATUS_CHANGED",
            nameof(ApplicationUser),
            user.Id.ToString(),
            new { status = newStatus.ToString() },
            cancellationToken);

        return NoContent();
    }

    private Guid? GetAdminId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
