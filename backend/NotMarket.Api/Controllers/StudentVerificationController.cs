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
        ActionResult<
            IReadOnlyList<StudentVerificationListItemResponse>>>
        GetMine(
            CancellationToken cancellationToken)
    {
        var userId =
            GetUserId();

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
     * Öğrencinin kendi Pending doğrulama
     * başvurusunu siler.
     *
     * DELETE:
     * /api/student/verifications/{verificationId}
     */
    [HttpDelete("{verificationId:guid}")]
    public async Task<IActionResult> DeletePending(
        Guid verificationId,
        CancellationToken cancellationToken)
    {
        var userId =
            GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        /*
         * Doğrulama kaydı hem verilen ID'ye hem de
         * giriş yapan öğrenciye ait olmalıdır.
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
         * Öğrenci yalnızca inceleme bekleyen
         * başvurusunu geri çekebilir.
         */
        if (
            verification.Status !=
            VerificationStatus.Pending
        )
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
         * Önce veritabanı kaydı silinir.
         */
        db.StudentVerifications.Remove(
            verification);

        await db.SaveChangesAsync(
            cancellationToken);

        /*
         * Ardından private storage içerisindeki
         * dosya temizlenmeye çalışılır.
         *
         * Dosya silme başarısız olursa kullanıcı
         * işlemi bozulmaz; hata loglanır.
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
        ActionResult<StudentVerificationCreatedResponse>>
        Upload(
            [FromForm]
            StudentVerificationUploadRequest request,
            CancellationToken cancellationToken)
    {
        var userId =
            GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        /*
         * Canonical akademik ID alanları
         * zorunludur.
         */
        if (
            request.UniversityId ==
                Guid.Empty ||
            request.AcademicUnitId ==
                Guid.Empty ||
            request.AcademicProgramId ==
                Guid.Empty
        )
        {
            return BadRequest(new
            {
                message =
                    "Üniversite, akademik birim ve bölüm/program seçimi zorunludur."
            });
        }

        /*
         * Üniversite:
         * - mevcut olmalı,
         * - aktif olmalı,
         * - Türkiye'ye ait olmalı.
         */
        var university =
            await db.AcademicUniversities
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.UniversityId &&
                        x.IsActive &&
                        x.CountryCode ==
                            "TR",
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
         * Akademik birim:
         * - mevcut olmalı,
         * - aktif olmalı,
         * - seçilen üniversiteye bağlı olmalı.
         */
        var academicUnit =
            await db.AcademicUnits
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.AcademicUnitId &&
                        x.UniversityId ==
                            university.Id &&
                        x.IsActive,
                    cancellationToken);

        if (academicUnit is null)
        {
            return BadRequest(new
            {
                message =
                    "Seçilen akademik birim bu üniversiteye bağlı değil veya aktif değil."
            });
        }

        /*
         * Akademik program:
         * - mevcut olmalı,
         * - aktif olmalı,
         * - seçilen akademik birime bağlı olmalı.
         */
        var academicProgram =
            await db.AcademicPrograms
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.Id ==
                            request.AcademicProgramId &&
                        x.AcademicUnitId ==
                            academicUnit.Id &&
                        x.IsActive,
                    cancellationToken);

        if (academicProgram is null)
        {
            return BadRequest(new
            {
                message =
                    "Seçilen bölüm veya program bu akademik birime bağlı değil veya aktif değil."
            });
        }

        /*
         * Dosya gönderilmiş olmalıdır.
         */
        if (
            request.Document is null ||
            request.Document.Length == 0
        )
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi dosyası boş olamaz."
            });
        }

        /*
         * Maksimum dosya boyutu:
         * 10 MB.
         */
        if (
            request.Document.Length >
            MaxFileSize
        )
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi en fazla 10 MB olabilir."
            });
        }

        /*
         * Dosya uzantısı PDF olmalıdır.
         */
        if (
            !string.Equals(
                Path.GetExtension(
                    request.Document.FileName),
                ".pdf",
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi PDF formatında olmalıdır."
            });
        }

        /*
         * Yalnızca dosya uzantısına güvenilmez.
         *
         * Dosya içeriğinin %PDF- başlığıyla
         * başlaması gerekir.
         */
        if (
            !await IsPdfAsync(
                request.Document,
                cancellationToken)
        )
        {
            return BadRequest(new
            {
                message =
                    "Dosyanın gerçek PDF formatında olduğu doğrulanamadı."
            });
        }

        /*
         * Belge tarihi yyyy-MM-dd formatında
         * olmalıdır.
         */
        if (
            !DateOnly.TryParseExact(
                request.DocumentIssueDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var documentIssueDate)
        )
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
         * Öğrenci belgesi son 30 gün içerisinde
         * alınmış olmalıdır.
         */
        if (
            documentIssueDate <
            today.AddDays(-30)
        )
        {
            return BadRequest(new
            {
                message =
                    "Öğrenci belgesi son 30 gün içinde alınmış olmalıdır."
            });
        }

        /*
         * Yüklenen PDF'in SHA-256 hash değeri
         * hesaplanır.
         */
        var documentHash =
            await CalculateSha256Async(
                request.Document,
                cancellationToken);

        /*
         * Aynı PDF daha önce herhangi bir
         * doğrulamada kullanılmış mı?
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
         * Aynı öğrenci aynı canonical akademik
         * alan için Pending başvuruya sahip mi?
         *
         * UniversityId, AcademicUnitId veya
         * AcademicProgramId bulunmayan eski
         * kayıtlar için snapshot isimleri
         * yedek kontrol olarak kullanılır.
         */
        var duplicatePending =
            await db.StudentVerifications
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.UserId ==
                            userId.Value &&

                        (
                            x.UniversityId ==
                                university.Id ||

                            (
                                x.UniversityId ==
                                    null &&
                                x.UniversityName ==
                                    university.Name
                            )
                        ) &&

                        (
                            x.AcademicUnitId ==
                                academicUnit.Id ||

                            (
                                x.AcademicUnitId ==
                                    null &&
                                x.FacultyName ==
                                    academicUnit.Name
                            )
                        ) &&

                        (
                            x.AcademicProgramId ==
                                academicProgram.Id ||

                            (
                                x.AcademicProgramId ==
                                    null &&
                                x.DepartmentName ==
                                    academicProgram.Name
                            )
                        ) &&

                        x.Status ==
                            VerificationStatus.Pending,
                    cancellationToken);

        if (duplicatePending)
        {
            return Conflict(new
            {
                message =
                    "Bu üniversite, akademik birim ve bölüm/program için zaten bekleyen bir doğrulamanız var."
            });
        }

        /*
         * Aynı canonical akademik alan için halen
         * aktif Approved doğrulama var mı?
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

                        (
                            x.UniversityId ==
                                university.Id ||

                            (
                                x.UniversityId ==
                                    null &&
                                x.UniversityName ==
                                    university.Name
                            )
                        ) &&

                        (
                            x.AcademicUnitId ==
                                academicUnit.Id ||

                            (
                                x.AcademicUnitId ==
                                    null &&
                                x.FacultyName ==
                                    academicUnit.Name
                            )
                        ) &&

                        (
                            x.AcademicProgramId ==
                                academicProgram.Id ||

                            (
                                x.AcademicProgramId ==
                                    null &&
                                x.DepartmentName ==
                                    academicProgram.Name
                            )
                        ) &&

                        x.Status ==
                            VerificationStatus.Approved &&

                        (
                            x.ExpiresAt ==
                                null ||
                            x.ExpiresAt >
                                now
                        ),
                    cancellationToken);

        if (activeVerificationExists)
        {
            return Conflict(new
            {
                message =
                    "Bu üniversite, akademik birim ve bölüm/program için zaten aktif bir öğrenci doğrulamanız bulunuyor."
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
         * Yeni doğrulama canonical ID'ler ve
         * snapshot isimleriyle oluşturulur.
         *
         * Snapshot isimleri frontend'den değil,
         * backend master tablolarından alınır.
         */
        var verification =
            new StudentVerification
            {
                UserId =
                    userId.Value,

                UniversityId =
                    university.Id,

                AcademicUnitId =
                    academicUnit.Id,

                AcademicProgramId =
                    academicProgram.Id,

                UniversityName =
                    university.Name,

                FacultyName =
                    academicUnit.Name,

                DepartmentName =
                    academicProgram.Name,

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
             * Veritabanına kayıt işlemi başarısız
             * olursa storage'a yazılmış PDF'in
             * yetim kalmaması için temizlenir.
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

        /*
         * Oluşturulan doğrulama kaydını
         * frontend'e döndürür.
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
     * JWT içindeki kullanıcı ID'sini döndürür.
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
     * PDF dosyasının magic-byte kontrolünü yapar.
     *
     * Beklenen başlangıç:
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
     * PDF'in SHA-256 hash değerini hesaplar.
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