using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    IVerificationDocumentStorage storage,
    ILogger<StudentVerificationsController> logger)
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
                    x => x.UserId == userId.Value)
                .OrderByDescending(
                    x => x.CreatedAt)
                .Select(
                    x => new StudentVerificationListItemResponse(
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
         * Kayıt hem belirtilen ID'ye hem de
         * giriş yapan öğrenciye ait olmalıdır.
         */
        var verification =
            await db.StudentVerifications
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == verificationId &&
                        x.UserId == userId.Value,
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
         * Öğrenci yalnızca Pending durumundaki
         * başvurusunu geri çekebilir.
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

        var documentPath =
            verification.DocumentBlobPath;

        /*
         * Önce veritabanı kaydını kaldır.
         */
        db.StudentVerifications.Remove(
            verification);

        await db.SaveChangesAsync(
            cancellationToken);

        /*
         * Ardından private storage dosyasını
         * temizlemeyi dene.
         *
         * Dosya temizliği başarısız olsa bile
         * veritabanı kaydı silindiği için kullanıcıya
         * başarısız sonuç göstermiyoruz. Hata loglanır.
         */
        try
        {
            await storage.DeleteAsync(
                documentPath,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Silinen öğrenci doğrulamasının dosyası temizlenemedi. VerificationId: {VerificationId}, Path: {DocumentPath}",
                verificationId,
                documentPath);
        }

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
         * Zorunlu akademik alan kontrolleri.
         */
        if (
            request.UniversityId == Guid.Empty ||
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
         * Frontend'den gelen UniversityId değerini
         * backend'in akademik master datasında doğrula.
         *
         * Sadece:
         * - mevcut,
         * - aktif,
         * - Türkiye'ye ait
         *
         * üniversiteler kabul edilir.
         */
        var university =
            await db.AcademicUniversities
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == request.UniversityId &&
                        x.IsActive &&
                        x.CountryCode == "TR",
                    cancellationToken);

        if (university is null)
        {
            return BadRequest(new
            {
                message =
                    "Geçerli bir Türkiye üniversitesi seçmelisiniz."
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
         * Maksimum dosya boyutu: 10 MB.
         */
        if (request.Document.Length > MaxFileSize)
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
         * Yalnızca uzantıya güvenilmez.
         * Dosya içeriğinin %PDF- header'ı
         * kontrol edilir.
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
         * Belge tarihi yyyy-MM-dd biçiminde olmalı.
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

        if (documentIssueDate > today)
        {
            return BadRequest(new
            {
                message =
                    "Belge tarihi gelecekte olamaz."
            });
        }

        /*
         * Şimdilik formdan gelen tarih üzerinden
         * son 30 gün kontrolü yapılır.
         *
         * İleride PDF içerisindeki gerçek belge
         * tarihi ayrıca okunacaktır.
         */
        if (documentIssueDate < today.AddDays(-30))
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi son 30 gün içinde alınmış olmalıdır."
            });
        }

        /*
         * Fakülte ve bölüm metinlerindeki
         * gereksiz boşlukları temizle.
         *
         * Üniversite adı kullanıcıdan alınmaz.
         * Veritabanındaki canonical isim kullanılır.
         */
        var faculty =
            NormalizeDisplayText(
                request.FacultyName);

        var department =
            NormalizeDisplayText(
                request.DepartmentName);

        /*
         * PDF SHA-256 hash değeri.
         */
        var documentHash =
            await CalculateSha256Async(
                request.Document,
                cancellationToken);

        /*
         * Aynı dosya daha önce yüklenmiş mi?
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
         * Aynı öğrenci aynı üniversite, fakülte
         * ve bölüm için Pending başvuruya sahip mi?
         *
         * UniversityId null olan eski kayıtlar için
         * UniversityName fallback kontrolü yapılır.
         */
        var duplicatePending =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId == userId.Value &&

                        (
                            x.UniversityId ==
                                university.Id ||

                            (
                                x.UniversityId == null &&
                                x.UniversityName ==
                                    university.Name
                            )
                        ) &&

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
         * Aynı akademik yetki için geçerli bir
         * Approved doğrulama bulunuyor mu?
         */
        var now =
            DateTimeOffset.UtcNow;

        var activeVerificationExists =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId == userId.Value &&

                        (
                            x.UniversityId ==
                                university.Id ||

                            (
                                x.UniversityId == null &&
                                x.UniversityName ==
                                    university.Name
                            )
                        ) &&

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
         * StudentVerification kaydı oluşturulur.
         *
         * UniversityName frontend'den değil,
         * AcademicUniversities tablosundaki
         * canonical kayıttan alınır.
         */
        var verification =
            new StudentVerification
            {
                UserId =
                    userId.Value,

                UniversityId =
                    university.Id,

                UniversityName =
                    university.Name,

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

        try
        {
            await db.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            /*
             * Veritabanına kayıt başarısız olursa
             * storage'a yazılmış PDF'i temizlemeyi dene.
             */
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
                    "Başarısız doğrulama kaydının dosyası temizlenemedi. Path: {DocumentPath}",
                    relativePath);
            }

            throw;
        }

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
     * Baştaki, sondaki ve tekrarlanan
     * boşlukları temizler.
     */
    private static string NormalizeDisplayText(
        string value)
    {
        var parts =
            value.Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

        return string.Join(
            ' ',
            parts);
    }

    /*
     * PDF magic-byte kontrolü.
     *
     * Geçerli başlangıç:
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
     * PDF'in SHA-256 hash değerini oluşturur.
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