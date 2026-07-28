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
    private const long MaxFileSize =
        10 * 1024 * 1024;

    /*
     * Öğrencinin kendi doğrulamalarını listeler.
     *
     * GET /api/student/verifications
     */
    [HttpGet]
    public async Task<
        ActionResult<IReadOnlyList<StudentVerificationListItemResponse>>> GetMine(
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        var items =
            await db.StudentVerifications
                .AsNoTracking()
                .Where(
                    x =>
                        x.UserId ==
                        userId.Value)
                .OrderByDescending(
                    x => x.CreatedAt)
                .Select(
                    x =>
                        new StudentVerificationListItemResponse(
                            x.Id,
                            x.UniversityName,
                            x.FacultyName,
                            x.DepartmentName,
                            x.Status.ToString(),
                            x.DocumentIssueDate,
                            x.ReviewNote,
                            x.ReviewedAt,
                            x.ExpiresAt,
                            x.CreatedAt))
                .ToListAsync(
                    cancellationToken);

        return Ok(items);
    }

    /*
     * Öğrencinin henüz admin tarafından
     * karara bağlanmamış doğrulamasını siler.
     *
     * DELETE /api/student/verifications/{verificationId}
     *
     * Sadece:
     * - Kaydın sahibi
     * - Pending durumundaki kayıt
     *
     * silinebilir.
     */
    [HttpDelete("{verificationId:guid}")]
    public async Task<IActionResult> DeletePending(
        Guid verificationId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        /*
         * Sadece giriş yapan öğrenciye ait
         * doğrulama kaydı bulunabilir.
         *
         * Böylece başka bir öğrencinin
         * verification ID'sini bilen kullanıcı
         * o kaydı silemez.
         */
        var verification =
            await db.StudentVerifications
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                        verificationId &&
                        x.UserId ==
                        userId.Value,
                    cancellationToken);

        if (verification is null)
        {
            return NotFound(new
            {
                message =
                    "Doğrulama kaydı bulunamadı."
            });
        }

        /*
         * Sadece Pending kayıtlar
         * öğrenci tarafından geri çekilebilir.
         */
        if (verification.Status !=
            VerificationStatus.Pending)
        {
            return Conflict(new
            {
                message =
                    "Yalnızca inceleme bekleyen doğrulamalar silinebilir."
            });
        }

        /*
         * Dosya yolunu DB kaydını silmeden
         * önce saklıyoruz.
         */
        var documentPath =
            verification.DocumentBlobPath;

        /*
         * Önce veritabanındaki doğrulama
         * kaydını kaldırıyoruz.
         *
         * Böylece dosya silme işleminde
         * problem yaşansa bile kullanıcı
         * açısından doğrulama geri çekilmiş olur.
         */
        db.StudentVerifications.Remove(
            verification);

        await db.SaveChangesAsync(
            cancellationToken);

        /*
         * Ardından private storage'daki
         * öğrenci belgesini kaldırıyoruz.
         */
        await storage.DeleteAsync(
            documentPath,
            cancellationToken);

        /*
         * 204 No Content:
         * Silme işlemi başarıyla tamamlandı.
         */
        return NoContent();
    }

    /*
     * Yeni öğrenci doğrulama başvurusu oluşturur.
     *
     * POST /api/student/verifications
     */
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<StudentVerificationCreatedResponse>> Upload(
        [FromForm]
        StudentVerificationUploadRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        /*
         * Akademik alan kontrolleri.
         */
        if (
            string.IsNullOrWhiteSpace(
                request.UniversityName) ||
            string.IsNullOrWhiteSpace(
                request.FacultyName) ||
            string.IsNullOrWhiteSpace(
                request.DepartmentName))
        {
            return BadRequest(new
            {
                message =
                    "Üniversite, fakülte ve bölüm bilgileri zorunludur."
            });
        }

        /*
         * Dosya mevcut mu?
         */
        if (
            request.Document is null ||
            request.Document.Length == 0)
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi dosyası boş olamaz."
            });
        }

        /*
         * Maksimum 10 MB.
         */
        if (
            request.Document.Length >
            MaxFileSize)
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi en fazla 10 MB olabilir."
            });
        }

        /*
         * Dosya uzantısı PDF olmalı.
         */
        if (!string.Equals(
                Path.GetExtension(
                    request.Document.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi PDF formatında olmalıdır."
            });
        }

        /*
         * Sadece uzantıya güvenmiyoruz.
         *
         * Dosyanın gerçek PDF header'ına
         * sahip olması gerekir.
         */
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

        /*
         * Belge tarihi kontrolü.
         */
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

        var today =
            DateOnly.FromDateTime(
                DateTime.UtcNow);

        /*
         * Belge tarihi gelecekte olamaz.
         */
        if (documentIssueDate > today)
        {
            return BadRequest(new
            {
                message =
                    "Belge tarihi gelecekte olamaz."
            });
        }

        /*
         * Şimdilik formdan gelen tarih
         * üzerinden son 30 gün kontrolü.
         *
         * İleride PDF içerisindeki gerçek
         * belge tarihi okunacak.
         */
        if (
            documentIssueDate <
            today.AddDays(-30))
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi son 30 gün içinde alınmış olmalıdır."
            });
        }

        /*
         * Kullanıcı tarafından girilen
         * akademik bilgileri normalize et.
         */
        var university =
            request.UniversityName.Trim();

        var faculty =
            request.FacultyName.Trim();

        var department =
            request.DepartmentName.Trim();

        /*
         * PDF SHA-256 hash değeri.
         */
        var documentHash =
            await CalculateSha256Async(
                request.Document,
                cancellationToken);

        /*
         * Aynı PDF daha önce sisteme
         * yüklenmiş mi?
         */
        var documentAlreadyExists =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.DocumentHash ==
                        documentHash,
                    cancellationToken);

        if (documentAlreadyExists)
        {
            return Conflict(new
            {
                message =
                    "Bu öğrenci belgesi daha önce sisteme yüklenmiş."
            });
        }

        /*
         * Aynı öğrenci aynı akademik alan
         * için zaten Pending başvuru
         * oluşturmuş mu?
         */
        var duplicatePending =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId ==
                        userId.Value &&

                        x.UniversityName ==
                        university &&

                        x.FacultyName ==
                        faculty &&

                        x.DepartmentName ==
                        department &&

                        x.Status ==
                        VerificationStatus.Pending,
                    cancellationToken);

        if (duplicatePending)
        {
            return Conflict(new
            {
                message =
                    "Bu üniversite, fakülte ve bölüm için zaten bekleyen bir doğrulamanız var."
            });
        }

        /*
         * Aynı akademik yetki halen
         * aktif durumda mı?
         */
        var now =
            DateTimeOffset.UtcNow;

        var activeVerificationExists =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId ==
                        userId.Value &&

                        x.UniversityName ==
                        university &&

                        x.FacultyName ==
                        faculty &&

                        x.DepartmentName ==
                        department &&

                        x.Status ==
                        VerificationStatus.Approved &&

                        (
                            x.ExpiresAt == null ||
                            x.ExpiresAt > now
                        ),
                    cancellationToken);

        if (activeVerificationExists)
        {
            return Conflict(new
            {
                message =
                    "Bu üniversite, fakülte ve bölüm için zaten aktif bir öğrenci doğrulamanız bulunuyor."
            });
        }

        /*
         * PDF private storage'a kaydedilir.
         */
        var relativePath =
            await storage.SaveAsync(
                userId.Value,
                request.Document,
                cancellationToken);

        /*
         * StudentVerification kaydı
         * Pending olarak oluşturulur.
         */
        var verification =
            new StudentVerification
            {
                UserId =
                    userId.Value,

                UniversityName =
                    university,

                FacultyName =
                    faculty,

                DepartmentName =
                    department,

                DocumentBlobPath =
                    relativePath,

                DocumentHash =
                    documentHash,

                DocumentIssueDate =
                    documentIssueDate,

                Status =
                    VerificationStatus.Pending,

                ExpiresAt =
                    null
            };

        db.StudentVerifications.Add(
            verification);

        await db.SaveChangesAsync(
            cancellationToken);

        /*
         * Oluşturulan doğrulamayı
         * frontend'e döndür.
         */
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

    /*
     * JWT içerisindeki kullanıcı ID'sini alır.
     */
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

    /*
     * PDF magic-byte kontrolü.
     *
     * Geçerli PDF başlangıcı:
     *
     * %PDF-
     */
    private static async Task<bool> IsPdfAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream =
            file.OpenReadStream();

        var header =
            new byte[5];

        var read =
            await stream.ReadAsync(
                header.AsMemory(
                    0,
                    header.Length),
                cancellationToken);

        if (read < header.Length)
        {
            return false;
        }

        return
            header[0] == 0x25 && // %
            header[1] == 0x50 && // P
            header[2] == 0x44 && // D
            header[3] == 0x46 && // F
            header[4] == 0x2D;   // -
    }

    /*
     * PDF'in SHA-256 hash değerini
     * oluşturur.
     */
    private static async Task<string>
        CalculateSha256Async(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        await using var stream =
            file.OpenReadStream();

        using var sha256 =
            SHA256.Create();

        var hash =
            await sha256.ComputeHashAsync(
                stream,
                cancellationToken);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}