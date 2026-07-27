using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NotMarket.Api.Contracts;
using NotMarket.Api.Data;
using NotMarket.Api.Domain;
using NotMarket.Api.Services;

namespace NotMarket.Api.Controllers;

[ApiController]
[Route("api/student/verifications")]
[Authorize(Policy = "StudentOnly")]
public sealed class StudentVerificationsController(
    AppDbContext db,
    IVerificationDocumentStorage storage)
    : ControllerBase
{
    private const long MaxFileSize = 10 * 1024 * 1024;

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<StudentVerificationCreatedResponse>> Upload(
        [FromForm] StudentVerificationUploadRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.UniversityName) ||
            string.IsNullOrWhiteSpace(request.FacultyName) ||
            string.IsNullOrWhiteSpace(request.DepartmentName))
        {
            return BadRequest(new
            {
                message =
                    "Üniversite, fakülte ve bölüm bilgileri zorunludur."
            });
        }

        if (request.Document.Length == 0)
        {
            return BadRequest(new
            {
                message = "Belge dosyası boş olamaz."
            });
        }

        if (request.Document.Length > MaxFileSize)
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi en fazla 10 MB olabilir."
            });
        }

        if (!string.Equals(
                Path.GetExtension(request.Document.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi PDF formatında olmalıdır."
            });
        }

        if (!await IsPdfAsync(
                request.Document,
                cancellationToken))
        {
            return BadRequest(new
            {
                message =
                    "Dosyanın gerçek PDF formatında olduğu doğrulanamadı."
            });
        }

        if (!DateOnly.TryParseExact(
                request.DocumentIssueDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var documentIssueDate))
        {
            return BadRequest(new
            {
                message =
                    "Belge tarihi yyyy-MM-dd formatında olmalıdır."
            });
        }

        var today = DateOnly.FromDateTime(
            DateTime.UtcNow);

        if (documentIssueDate > today)
        {
            return BadRequest(new
            {
                message =
                    "Belge tarihi gelecekte olamaz."
            });
        }

        if (documentIssueDate <
            today.AddDays(-30))
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi son 30 gün içinde alınmış olmalıdır."
            });
        }

        var documentHash = await CalculateSha256Async(
            request.Document,
            cancellationToken);

        var documentAlreadyExists =
            await db.StudentVerifications.AnyAsync(
                x => x.DocumentHash == documentHash,
                cancellationToken);

        if (documentAlreadyExists)
        {
            return Conflict(new
            {
                message =
                    "Bu öğrenci belgesi daha önce sisteme yüklenmiş."
            });
        }

        var university =
            request.UniversityName.Trim();

        var faculty =
            request.FacultyName.Trim();

        var department =
            request.DepartmentName.Trim();

        var duplicatePending =
            await db.StudentVerifications.AnyAsync(
                x =>
                    x.UserId == userId.Value &&
                    x.UniversityName == university &&
                    x.FacultyName == faculty &&
                    x.DepartmentName == department &&
                    x.Status == VerificationStatus.Pending,
                cancellationToken);

        if (duplicatePending)
        {
            return Conflict(new
            {
                message =
                    "Bu üniversite ve bölüm için zaten bekleyen doğrulamanız var."
            });
        }

        var relativePath = await storage.SaveAsync(
            userId.Value,
            request.Document,
            cancellationToken);

        var verification = new StudentVerification
        {
            UserId = userId.Value,
            UniversityName = university,
            FacultyName = faculty,
            DepartmentName = department,
            DocumentBlobPath = relativePath,
            DocumentHash = documentHash,
            DocumentIssueDate = documentIssueDate,
            Status = VerificationStatus.Pending,
            ExpiresAt = null
        };

        db.StudentVerifications.Add(verification);

        await db.SaveChangesAsync(
            cancellationToken);

        return Created(
            $"/api/student/verifications/{verification.Id}",
            new StudentVerificationCreatedResponse(
                verification.Id,
                verification.UniversityName,
                verification.FacultyName,
                verification.DepartmentName,
                verification.Status.ToString(),
                verification.DocumentIssueDate,
                verification.CreatedAt));
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return Guid.TryParse(value, out var id)
            ? id
            : null;
    }

    private static async Task<bool> IsPdfAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream =
            file.OpenReadStream();

        var header = new byte[5];

        var read = await stream.ReadAsync(
            header.AsMemory(0, header.Length),
            cancellationToken);

        if (read < 5)
        {
            return false;
        }

        return header[0] == 0x25 && // %
               header[1] == 0x50 && // P
               header[2] == 0x44 && // D
               header[3] == 0x46 && // F
               header[4] == 0x2D;   // -
    }

    private static async Task<string> CalculateSha256Async(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream =
            file.OpenReadStream();

        using var sha256 =
            SHA256.Create();

        var hash = await sha256.ComputeHashAsync(
            stream,
            cancellationToken);

        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }
}